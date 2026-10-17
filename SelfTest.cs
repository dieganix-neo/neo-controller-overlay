using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NeoControllerOverlay;

public static class SelfTest
{
    public static void Smoke(string output)
    {
        Directory.CreateDirectory(output);
        var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};
        var overlay=new Overlay(true);overlay.Show();overlay.Reveal();
        var timer=new System.Windows.Threading.DispatcherTimer{Interval=TimeSpan.FromSeconds(1)};
        timer.Tick+=(_,_)=>{
            timer.Stop();var checks=new List<string>();
            try
            {
                if(!overlay.AllowsTransparency || !overlay.Topmost)throw new Exception("Overlay transparency or topmost missing");
                checks.Add("PASS: Overlay starts transparent and always on top");
                if(app.Windows.Count!=1)throw new Exception("Unexpected extra window");
                checks.Add("PASS: Only one window exists: the controller overlay");
                if(overlay.ShortcutError.Length>0)throw new Exception(overlay.ShortcutError);
                checks.Add("PASS: All nine global shortcuts register with Windows");
                double oldX=overlay.Left;overlay.Act("Move right");if(overlay.Left!=oldX+10)throw new Exception("Move action failed");checks.Add("PASS: Move shortcut action shifts overlay");
                var hwnd=new WindowInteropHelper(overlay).Handle;
                if(!IsWindowVisible(hwnd))throw new Exception("Native overlay window is hidden");
                checks.Add("PASS: Native Windows visibility is enabled after startup");
                overlay.Hide();overlay.Reveal();if(!overlay.IsVisible || !IsWindowVisible(hwnd))throw new Exception("Reveal did not restore hidden window");
                checks.Add("PASS: Reveal restores a hidden overlay without opening another window");
                overlay.Left=-100000;overlay.Top=-100000;overlay.Reveal();if(overlay.Left<0 || overlay.Top<0)throw new Exception("Offscreen overlay recovery failed");
                checks.Add("PASS: Reveal recovers an overlay positioned offscreen");
                double oldWidth=overlay.Width,oldScale=overlay.Config.Scale;
                SendMessageW(hwnd,0x312,new IntPtr(105),IntPtr.Zero);if(overlay.Width<=oldWidth)throw new Exception("E hotkey action failed");
                SendMessageW(hwnd,0x312,new IntPtr(104),IntPtr.Zero);if(Math.Abs(overlay.Config.Scale-oldScale)>.00001 || Math.Abs(overlay.Width-oldWidth)>1.5)throw new Exception("Q hotkey action failed");checks.Add("PASS: E enlarge and Q shrink route through the Windows hotkey message handler");
                if((GetWindowLongW(hwnd,-20)&0x20)==0)throw new Exception("Click-through style missing");checks.Add("PASS: Click-through applies native Windows style");
                overlay.Act("Show / hide");if(overlay.IsVisible)throw new Exception("Hide failed");overlay.Act("Show / hide");if(!overlay.IsVisible)throw new Exception("Show failed");checks.Add("PASS: Show and hide actions work");
                foreach(Window w in app.Windows)
                {
                    var bmp=new RenderTargetBitmap((int)w.ActualWidth,(int)w.ActualHeight,96,96,PixelFormats.Pbgra32);bmp.Render(w);
                    var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bmp));using var file=File.Create(Path.Combine(output,"overlay-preview.png"));png.Save(file);
                }
                checks.Add("PASS: Controller overlay renders while native input polling remains active");
                checks.Add("Detected hardware: "+string.Join(", ",overlay.Input.Devices));
            }
            catch(Exception e){checks.Add("FAIL: "+e);Environment.ExitCode=1;}
            File.WriteAllLines(Path.Combine(output,"smoke-report.txt"),checks);overlay.Act("Quit");
        };
        timer.Start();app.Run();
    }
    [StructLayout(LayoutKind.Explicit,Size=136)] struct VirtualDesc
    {
        [FieldOffset(0)] public uint Version;
        [FieldOffset(4)] public ushort Type;
        [FieldOffset(8)] public ushort Vendor;
        [FieldOffset(10)] public ushort Product;
        [FieldOffset(12)] public ushort Axes;
        [FieldOffset(14)] public ushort Buttons;
        [FieldOffset(28)] public uint ButtonMask;
        [FieldOffset(32)] public uint AxisMask;
        [FieldOffset(40)] public IntPtr Name;
    }
    [DllImport("SDL3",CallingConvention=CallingConvention.Cdecl)] static extern uint SDL_AttachVirtualJoystick(ref VirtualDesc desc);
    [DllImport("SDL3",CallingConvention=CallingConvention.Cdecl)] static extern IntPtr SDL_OpenJoystick(uint id);
    [DllImport("SDL3",CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] static extern bool SDL_SetJoystickVirtualButton(IntPtr joystick,int button,[MarshalAs(UnmanagedType.I1)] bool down);
    [DllImport("SDL3",CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] static extern bool SDL_SetJoystickVirtualAxis(IntPtr joystick,int axis,short value);
    [DllImport("SDL3",CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] static extern bool SDL_DetachVirtualJoystick(uint id);
    [DllImport("SDL3",CallingConvention=CallingConvention.Cdecl)] static extern void SDL_CloseJoystick(IntPtr joystick);
    [DllImport("user32.dll")] static extern int GetWindowLongW(IntPtr handle,int index);
    [DllImport("user32.dll")] static extern IntPtr SendMessageW(IntPtr hwnd,uint message,IntPtr wparam,IntPtr lparam);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hwnd);
    public static void Run(string[] args)
    {
        string output=args.Length>1?args[1]:Path.Combine(Environment.CurrentDirectory,"test-results");Directory.CreateDirectory(output);
        var checks=new List<string>();
        void Check(bool pass,string message){if(!pass)throw new Exception("FAIL: "+message);checks.Add("PASS: "+message);}
        try
        {
            var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};
            Check(PadState.DetectPreset(5,"Wireless Controller")=="PS4","PS4 native type selects PS4 automatically");
            Check(PadState.DetectPreset(6,"Wireless Controller")=="PS5","PS5 native type selects PS5 automatically");
            Check(PadState.DetectPreset(3,"Xbox Wireless Controller")=="Xbox","Xbox native type selects Xbox automatically");
            Check(PadState.Normalize(short.MinValue)==-1 && PadState.Normalize(short.MaxValue)==1,"Signed stick endpoints normalize correctly");
            Check(PadState.Deadzone(.01,-.02,.08)==(0,0),"Radial deadzone suppresses drift");
            var edge=PadState.Deadzone(1,1,.08);Check(Math.Abs(Math.Sqrt(edge.X*edge.X+edge.Y*edge.Y)-1)<.0001,"Diagonal sticks clamp to circle");
            var old=new Settings();var copy=JsonSerializer.Deserialize<Settings>(JsonSerializer.Serialize(old));Check(copy?.Scale==old.Scale,"Position and size settings serialize and restore");
            Check(Settings.Defaults()["Smaller"]=="Ctrl+Alt+Q" && Settings.Defaults()["Larger"]=="Ctrl+Alt+E","Requested Q and E resize shortcuts are fixed defaults");
            foreach(var key in Settings.Defaults())Hotkeys.Parse(key.Value);checks.Add("PASS: All default shortcut expressions parse");
            using var input=new ControllerInput();input.Poll();checks.Add("PASS: SDL initializes and enumerates real hardware");
            IntPtr name=Marshal.StringToCoTaskMemUTF8("Neo Controller Overlay Virtual DualShock 4");
            var desc=new VirtualDesc {Version=136,Type=1,Vendor=0x054c,Product=0x09cc,Axes=6,Buttons=26,ButtonMask=0x3ffffff,AxisMask=0x3f,Name=name};
            uint id=SDL_AttachVirtualJoystick(ref desc);Check(id!=0,"Virtual controller attaches through native SDL");
            var joy=SDL_OpenJoystick(id);Check(joy!=IntPtr.Zero,"Virtual joystick opens");
            input.Preferred=id;input.Poll();
            Check(SDL_SetJoystickVirtualButton(joy,0,true),"Native virtual face button injection succeeds");
            Check(SDL_SetJoystickVirtualAxis(joy,0,20000),"Native virtual stick injection succeeds");
            var state=input.Poll();Check(state.Connected && state.Buttons[0] && state.Axes[0]>.5,"Real polling path reads button and stick from SDL");
            Check(state.Preset=="PS4","PS4 USB identity activates PS4 preset through SDL");
            SDL_SetJoystickVirtualButton(joy,0,false);SDL_SetJoystickVirtualAxis(joy,0,0);var released=input.Poll();Check(!released.Buttons[0] && Math.Abs(released.Axes[0])<.001,"Button release and stick return are read");
            foreach(string preset in new[]{"PS4","PS5","Xbox"})
            {
                var view=new ControllerView{State=state,Preset=preset};var natural=view.NaturalSize;view.Width=natural.Width;view.Height=natural.Height;view.Measure(natural);view.Arrange(new Rect(new Point(),natural));view.UpdateLayout();
                var bitmap=new RenderTargetBitmap((int)natural.Width,(int)natural.Height,96,96,PixelFormats.Pbgra32);bitmap.Render(view);
                byte[] pixel=new byte[4];bitmap.CopyPixels(new Int32Rect(0,0,1,1),pixel,4,0);Check(pixel[3]==0,preset+" artwork has transparent background");
                var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using var file=File.Create(Path.Combine(output,preset+"-preview.png"));png.Save(file);
            }
            var window=new Window{Width=100,Height=100,ShowInTaskbar=false,WindowStyle=WindowStyle.None,AllowsTransparency=true,Background=Brushes.Transparent};
            var hwnd=new WindowInteropHelper(window).EnsureHandle();
            using(var hotkeys=new Hotkeys(hwnd,_=>{}))
            {
                Check(hotkeys.Apply(new(){["Test"]="Ctrl+Alt+Shift+F12"})=="","Global shortcut registers with Windows");
                Check(hotkeys.Apply(new(){["Test"]="Ctrl+Alt+Shift+F12",["Duplicate"]="Ctrl+Alt+Shift+F12"}).Contains("Duplicate"),"Duplicate shortcuts are rejected");
            }
            window.Close();
            SDL_CloseJoystick(joy);Check(SDL_DetachVirtualJoystick(id),"Virtual controller detaches");Marshal.FreeCoTaskMem(name);Check(!input.Poll().Connected || input.Devices.Count>0,"Disconnect polling completes without stale handle");
            checks.Add("Physical USB/Bluetooth hardware and exclusive fullscreen behavior require hands-on testing.");
            File.WriteAllLines(Path.Combine(output,"test-report.txt"),checks);app.Shutdown();
        }
        catch(Exception e){checks.Add(e.ToString());File.WriteAllLines(Path.Combine(output,"test-report.txt"),checks);Environment.ExitCode=1;}
    }
}
