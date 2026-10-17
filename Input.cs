using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PadGlass;

public record PadInfo(uint Id, string Name, string Preset) { public override string ToString() => Name; }
public sealed class PadState
{
    public bool Connected;
    public string Name = "Connect a controller";
    public string Preset = "PS4";
    public bool[] Buttons = new bool[26];
    public double[] Axes = new double[6];
    public static double Normalize(short value) => value < 0 ? value / 32768.0 : value / 32767.0;
    public static (double X, double Y) Deadzone(double x, double y, double deadzone)
    {
        double length = Math.Sqrt(x*x+y*y);
        if (length <= deadzone) return (0,0);
        double scale = Math.Min(1,(length-deadzone)/(1-deadzone))/length;
        return (x*scale,y*scale);
    }
    public static string DetectPreset(int type, string name) => type switch
    {
        5 => "PS4", 6 => "PS5", 2 or 3 => "Xbox",
        _ when name.Contains("DualSense",StringComparison.OrdinalIgnoreCase) => "PS5",
        _ when name.Contains("DualShock",StringComparison.OrdinalIgnoreCase) || name.Contains("PS4",StringComparison.OrdinalIgnoreCase) => "PS4",
        _ => "Xbox"
    };
}

public sealed class ControllerInput : IDisposable
{
    IntPtr pad;
    uint current;
    public uint Preferred;
    public string Error = "";
    public List<PadInfo> Devices = new();
    public ControllerInput()
    {
        Native.SDL_SetHint("SDL_JOYSTICK_ALLOW_BACKGROUND_EVENTS", "1");
        if (!Native.SDL_Init(0x2000)) throw new InvalidOperationException(Marshal.PtrToStringUTF8(Native.SDL_GetError()));
    }
    public PadState Poll()
    {
        Native.SDL_PumpEvents();
        Native.SDL_FlushEvents(0, 0xffff);
        Devices.Clear();
        var ids = Native.SDL_GetGamepads(out int count);
        try
        {
            for(int i=0;i<count;i++)
            {
                uint id=unchecked((uint)Marshal.ReadInt32(ids,i*4));
                string name=Marshal.PtrToStringUTF8(Native.SDL_GetGamepadNameForID(id)) ?? "Controller";
                Devices.Add(new(id,name,PadState.DetectPreset(Native.SDL_GetGamepadTypeForID(id),name)));
            }
        }
        finally { Native.SDL_free(ids); }
        uint wanted = Devices.Find(d=>d.Id==Preferred)?.Id ?? (Devices.Count>0?Devices[0].Id:0);
        if (current!=wanted || (pad!=IntPtr.Zero && !Native.SDL_GamepadConnected(pad)))
        {
            if(pad!=IntPtr.Zero) Native.SDL_CloseGamepad(pad);
            current=wanted; pad=wanted==0?IntPtr.Zero:Native.SDL_OpenGamepad(wanted);
        }
        if(pad==IntPtr.Zero) return new();
        var info=Devices.Find(d=>d.Id==current)!;
        var state=new PadState { Connected=true,Name=info.Name,Preset=info.Preset };
        for(int i=0;i<26;i++) state.Buttons[i]=Native.SDL_GetGamepadButton(pad,i);
        for(int i=0;i<6;i++) state.Axes[i]=i<4?PadState.Normalize(Native.SDL_GetGamepadAxis(pad,i)):Math.Clamp(Native.SDL_GetGamepadAxis(pad,i)/32767.0,0,1);
        return state;
    }
    public void Dispose() { if(pad!=IntPtr.Zero) Native.SDL_CloseGamepad(pad); Native.SDL_Quit(); }
}

internal static class Native
{
    const string Lib="SDL3";
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] [return: MarshalAs(UnmanagedType.I1)] public static extern bool SDL_Init(uint flags);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] [return: MarshalAs(UnmanagedType.I1)] public static extern bool SDL_SetHint([MarshalAs(UnmanagedType.LPUTF8Str)] string name,[MarshalAs(UnmanagedType.LPUTF8Str)] string value);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] public static extern IntPtr SDL_GetError();
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] public static extern void SDL_PumpEvents();
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] public static extern void SDL_FlushEvents(uint min,uint max);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] public static extern IntPtr SDL_GetGamepads(out int count);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] public static extern void SDL_free(IntPtr memory);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] public static extern IntPtr SDL_GetGamepadNameForID(uint id);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] public static extern int SDL_GetGamepadTypeForID(uint id);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] public static extern IntPtr SDL_OpenGamepad(uint id);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] public static extern void SDL_CloseGamepad(IntPtr pad);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] [return: MarshalAs(UnmanagedType.I1)] public static extern bool SDL_GamepadConnected(IntPtr pad);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] [return: MarshalAs(UnmanagedType.I1)] public static extern bool SDL_GetGamepadButton(IntPtr pad,int button);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] public static extern short SDL_GetGamepadAxis(IntPtr pad,int axis);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] public static extern void SDL_Quit();
}
