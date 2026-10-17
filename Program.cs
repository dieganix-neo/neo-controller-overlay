using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Forms=System.Windows.Forms;

namespace NeoControllerOverlay;

public static class Program
{
    [STAThread] public static void Main(string[] args)
    {
        try
        {
            // Layered transparent windows must render even on GPU/driver combinations
            // where WPF hardware composition produces an entirely invisible surface.
            RenderOptions.ProcessRenderMode=RenderMode.SoftwareOnly;
            if(args.Contains("--self-test")){SelfTest.Run(args);return;}
            if(args.Contains("--smoke-test")){SelfTest.Smoke(args[1]);return;}
            using var mutex=new System.Threading.Mutex(true,"Local\\NeoControllerOverlay-Overlay",out bool first);
            using var reveal=new System.Threading.EventWaitHandle(false,System.Threading.EventResetMode.AutoReset,"Local\\NeoControllerOverlay-Reveal");
            if(!first){reveal.Set();return;}
            var app=new Application {ShutdownMode=ShutdownMode.OnExplicitShutdown};
            var overlay=new Overlay();overlay.Show();overlay.Reveal();
            var listener=System.Threading.ThreadPool.RegisterWaitForSingleObject(reveal,(_,_)=>app.Dispatcher.BeginInvoke(new Action(overlay.Reveal)),null,System.Threading.Timeout.Infinite,false);
            try{app.Run();}finally{listener.Unregister(null);}
        }
        catch(Exception e){Settings.Log(e.ToString());Environment.ExitCode=1;}
    }
}
public sealed class Overlay : Window
{
    public readonly Settings Config;
    public readonly ControllerView View=new();
    public readonly ControllerInput Input=new();
    public PadState State=new();
    readonly DispatcherTimer poll=new(){Interval=TimeSpan.FromMilliseconds(16)},save=new(){Interval=TimeSpan.FromMilliseconds(400)};
    Hotkeys? hotkeys;IntPtr handle;readonly bool testing;public string ShortcutError="";
    [DllImport("user32.dll",EntryPoint="GetWindowLongW")] static extern int GetWindowLong(IntPtr h,int index);
    [DllImport("user32.dll",EntryPoint="SetWindowLongW")] static extern int SetWindowLong(IntPtr h,int index,int value);
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hwnd,int command);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hwnd,IntPtr insertAfter,int x,int y,int width,int height,uint flags);
    public Overlay(bool testing=false)
    {
        this.testing=testing;Config=testing?new Settings():Settings.Load();
        Title="Neo Controller Overlay";WindowStyle=WindowStyle.None;AllowsTransparency=true;Background=Brushes.Transparent;
        ResizeMode=ResizeMode.NoResize;Topmost=true;ShowInTaskbar=false;ShowActivated=false;Content=View;Left=Config.Left;Top=Config.Top;
        SourceInitialized+=(_,_)=>{
            handle=new WindowInteropHelper(this).Handle;hotkeys=new(handle,Act);ShortcutError=hotkeys.Apply(Settings.Defaults());
            if(ShortcutError.Length>0)Settings.Log(ShortcutError);Apply();EnsureVisible();
        };
        poll.Tick+=(_,_)=>{bool previousXbox=View.IsXbox;State=Input.Poll();View.State=State;if(previousXbox!=View.IsXbox)Apply();View.InvalidateVisual();};poll.Start();
        save.Tick+=(_,_)=>{save.Stop();Config.Save();};
        Closed+=(_,_)=>{poll.Stop();save.Stop();if(!testing)Config.Save();hotkeys?.Dispose();Input.Dispose();};
    }
    public void Remember(){Config.Left=Left;Config.Top=Top;if(!testing){save.Stop();save.Start();}}
    public void Apply()
    {
        Config.Scale=Math.Clamp(Config.Scale,.2,2.5);Width=View.NaturalSize.Width*Config.Scale;Height=View.NaturalSize.Height*Config.Scale;View.Deadzone=Config.Deadzone;
        if(handle!=IntPtr.Zero)SetWindowLong(handle,-20,GetWindowLong(handle,-20)|0x20|0x08000000|0x80);
        View.InvalidateVisual();
    }
    public void EnsureVisible()
    {
        var r=new Rect(Left,Top,Width,Height);
        bool visible=Forms.Screen.AllScreens.Any(s=>{var p=PresentationSource.FromVisual(this);double factor=p?.CompositionTarget.TransformFromDevice.M11??1;var b=s.WorkingArea;return r.IntersectsWith(new Rect(b.X*factor,b.Y*factor,b.Width*factor,b.Height*factor));});
        if(!visible)ResetPosition();
    }
    public void Reveal()
    {
        if(!IsVisible)Show();Apply();EnsureVisible();
        ShowWindow(handle,8);
        SetWindowPos(handle,new IntPtr(-1),0,0,0,0,0x1|0x2|0x10|0x40);
        View.InvalidateVisual();
    }
    void ResetPosition(){Left=SystemParameters.WorkArea.Left+40;Top=SystemParameters.WorkArea.Top+40;Config.Scale=.65;Remember();}
    public void Act(string action)
    {
        switch(action)
        {
            case "Move left":Left-=10;break;case "Move right":Left+=10;break;case "Move up":Top-=10;break;case "Move down":Top+=10;break;
            case "Larger":Config.Scale+=.025;break;case "Smaller":Config.Scale-=.025;break;
            case "Show / hide":if(IsVisible)Hide();else Reveal();break;
            case "Reset position":ResetPosition();Reveal();break;
            case "Quit":Close();Application.Current.Shutdown();return;
        }
        Apply();Remember();
    }
}
