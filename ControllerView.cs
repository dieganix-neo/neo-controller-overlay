using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PadGlass;

// Positions and sprite regions follow Gamepad Viewer's published style.css.
// Original skin art belongs to Gamepad Viewer; see assets/skins/ATTRIBUTION.md.
public sealed class ControllerView : FrameworkElement
{
    public PadState State = new();
    public string Preset="Auto";
    public double Deadzone=.08;
    static readonly Dictionary<string,BitmapSource> images=new();
    public bool IsXbox => (Preset=="Auto"?State.Preset:Preset)=="Xbox";
    public Size NaturalSize => IsXbox?new Size(750,630):new Size(806,599);
    static BitmapSource Image(string skin,string asset)
    {
        string key=skin+"/"+asset;
        if(!images.TryGetValue(key,out var image))
        {
            var bitmap=new BitmapImage(new Uri("pack://application:,,,/assets/skins/"+key+".png"));bitmap.Freeze();images[key]=image=bitmap;
        }
        return image;
    }
    protected override void OnRender(DrawingContext d)
    {
        base.OnRender(d);
        Size size=NaturalSize;string skin=IsXbox?"xbox":"ps4";
        d.PushTransform(new ScaleTransform(ActualWidth/size.Width,ActualHeight/size.Height));
        void Sprite(string asset,double x,double y,double w,double h,double sx=0,double sy=0,double alpha=1,bool mirror=false)
        {
            if(alpha<=0)return;
            var image=Image(skin,asset);d.PushOpacity(Math.Clamp(alpha,0,1));
            if(mirror)d.PushTransform(new ScaleTransform(-1,1,x+w/2,0));
            d.PushClip(new RectangleGeometry(new Rect(x,y,w,h)));
            d.DrawImage(image,new Rect(x-sx,y-sy,image.PixelWidth/2.0,image.PixelHeight/2.0));
            d.Pop();if(mirror)d.Pop();d.Pop();
        }
        bool Down(int b)=>State.Buttons[b];
        double Offset(int axis,bool y)
        {var v=PadState.Deadzone(State.Axes[axis],State.Axes[axis+1],Deadzone);return (y?v.Y:v.X)*22;}
        if(!IsXbox)
        {
            Sprite("base",0,0,806,599);
            Sprite("triggers",109,0,99,90,0,0,State.Axes[4]);Sprite("triggers",598,0,99,90,99,0,State.Axes[5]);
            if(Down(9))Sprite("bumper",109,94,99,23);if(Down(10))Sprite("bumper",598,94,99,23,mirror:true);
            Sprite("touchpad",272,122,262,151,alpha:Down(20)?1:.12);
            if(Down(5))Sprite("meta",382,341,42,42);
            if(Down(4))Sprite("start",227,142,28,46);if(Down(6))Sprite("start",551,142,28,46,28);
            int[] buttons={0,1,2,3};double[] x={625,682,567,625},y={275,217,217,159},sx={0,57,113,169};
            for(int i=0;i<4;i++)Sprite("face",x[i],y[i],55,55,sx[i],Down(buttons[i])?55:0);
            Sprite("sticks",228+Offset(0,false),308+Offset(0,true),94,94,Down(7)?96:0);
            Sprite("sticks",484+Offset(2,false),308+Offset(2,true),94,94,Down(8)?192:0);
            Sprite("dpad",136,181,36,52,37,Down(11)?52:0);Sprite("dpad",136,255,36,52,0,Down(12)?52:0);
            Sprite("dpad",92,226,52,36,73,Down(13)?52:0);Sprite("dpad",165,226,52,36,125,Down(14)?52:0);
        }
        else
        {
            Sprite("base-white",0,0,750,630);
            Sprite("trigger",152,0,88,121,alpha:State.Axes[4]);Sprite("trigger",510,0,88,121,alpha:State.Axes[5],mirror:true);
            if(Down(9))Sprite("bumper",107,129,170,61);if(Down(10))Sprite("bumper",473,129,170,61,mirror:true);
            if(Down(4))Sprite("start-select",306,264,33,33);if(Down(6))Sprite("start-select",414,264,33,33,33);
            if(Down(5))Sprite("quadrant",354,258,45,45);
            int[] buttons={0,1,2,3};double[] x={543,593,492,543},y={300,250,250,199},sx={0,49,98,146};
            for(int i=0;i<4;i++)Sprite("abxy",x[i],y[i]+(Down(buttons[i])?5:0),48,48,sx[i],Down(buttons[i])?48:0);
            Sprite("stick",144+Offset(0,false),239+Offset(0,true),83,83,Down(7)?0:85);
            Sprite("stick",432+Offset(2,false),352+Offset(2,true),83,83,Down(8)?0:85);
            if(Down(11))Sprite("dpad",261,345,34,56,34);if(Down(12))Sprite("dpad",261,400,34,56);
            if(Down(13))Sprite("dpad",223,383,55,35,0,93);if(Down(14))Sprite("dpad",278,383,55,35,0,57);
        }
        d.Pop();
    }
}
