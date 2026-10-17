using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace PadGlass;

public sealed class Hotkeys : IDisposable
{
    readonly IntPtr handle;
    readonly HwndSource source;
    readonly Action<string> action;
    readonly Dictionary<int,string> actions=new();
    [DllImport("user32.dll",SetLastError=true)] static extern bool RegisterHotKey(IntPtr hWnd,int id,uint modifiers,uint key);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr hWnd,int id);
    public Hotkeys(IntPtr handle,Action<string> action)
    {this.handle=handle;this.action=action;source=HwndSource.FromHwnd(handle);source.AddHook(Hook);}
    public static (uint Modifiers,uint Key) Parse(string text)
    {
        var parts=text.Split('+',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);
        if(parts.Length<2)throw new FormatException("Use at least one modifier, for example Ctrl+Alt+H.");
        uint mods=0;
        for(int i=0;i<parts.Length-1;i++)mods|=parts[i].ToLowerInvariant() switch {"ctrl" or "control"=>2u,"alt"=>1u,"shift"=>4u,"win"=>8u,_=>throw new FormatException("Unknown modifier: "+parts[i])};
        var key=(Key)new KeyConverter().ConvertFromInvariantString(parts[^1])!;
        if(key==System.Windows.Input.Key.None || key is System.Windows.Input.Key.LeftCtrl or System.Windows.Input.Key.RightCtrl or System.Windows.Input.Key.LeftAlt or System.Windows.Input.Key.RightAlt or System.Windows.Input.Key.LeftShift or System.Windows.Input.Key.RightShift or System.Windows.Input.Key.LWin or System.Windows.Input.Key.RWin)throw new FormatException("Choose a normal key after the modifier.");
        return(mods,unchecked((uint)KeyInterop.VirtualKeyFromKey(key)));
    }
    public string Apply(Dictionary<string,string> bindings)
    {
        var parsed=new List<(string Action,uint Mod,uint Key)>();var seen=new HashSet<(uint,uint)>();
        foreach(var b in bindings)
        {
            try{var p=Parse(b.Value);if(!seen.Add(p))return "Duplicate shortcut: "+b.Value;parsed.Add((b.Key,p.Modifiers,p.Key));}
            catch(Exception e){return b.Key+": "+e.Message;}
        }
        foreach(int id in actions.Keys)UnregisterHotKey(handle,id);actions.Clear();
        var errors=new List<string>();int next=100;
        foreach(var p in parsed){int id=next++;uint noRepeat=p.Action.StartsWith("Move ") || p.Action is "Smaller" or "Larger"?0u:0x4000u;if(RegisterHotKey(handle,id,p.Mod|noRepeat,p.Key))actions[id]=p.Action;else errors.Add(p.Action);}
        return errors.Count==0?"":"Shortcut already in use: "+string.Join(", ",errors);
    }
    IntPtr Hook(IntPtr hwnd,int msg,IntPtr w,IntPtr l,ref bool handled)
    {if(msg==0x312 && actions.TryGetValue(w.ToInt32(),out string? name)){action(name);handled=true;}return IntPtr.Zero;}
    public void Dispose(){foreach(int id in actions.Keys)UnregisterHotKey(handle,id);source.RemoveHook(Hook);}
}
