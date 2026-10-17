using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
namespace PadGlass;
public sealed class Settings
{
    public double Left {get;set;}=80;
    public double Top {get;set;}=80;
    public double Scale {get;set;}=.65;
    public double Deadzone {get;set;}=.08;
    public static Dictionary<string,string> Defaults()=>new()
    {
        ["Move left"]="Ctrl+Alt+Left",["Move right"]="Ctrl+Alt+Right",["Move up"]="Ctrl+Alt+Up",["Move down"]="Ctrl+Alt+Down",
        ["Smaller"]="Ctrl+Alt+Q",["Larger"]="Ctrl+Alt+E",["Show / hide"]="Ctrl+Alt+H",["Reset position"]="Ctrl+Alt+R",["Quit"]="Ctrl+Alt+X"
    };
    public static string FilePath=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"PadGlass","settings.json");
    public static Settings Load()
    {
        try{var s=JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath))??new();s.Scale=double.IsFinite(s.Scale)?Math.Clamp(s.Scale,.2,2.5):.65;s.Deadzone=double.IsFinite(s.Deadzone)?Math.Clamp(s.Deadzone,0,.5):.08;if(!double.IsFinite(s.Left))s.Left=80;if(!double.IsFinite(s.Top))s.Top=80;return s;}catch{return new();}
    }
    public static void Log(string message)
    {try{Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);File.AppendAllText(Path.Combine(Path.GetDirectoryName(FilePath)!,"PadGlass.log"),DateTimeOffset.Now+" "+message+Environment.NewLine);}catch{}}
    public void Save()
    {try{Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);File.WriteAllText(FilePath,JsonSerializer.Serialize(this,new JsonSerializerOptions{WriteIndented=true}));}catch(Exception e){Log("Could not save settings: "+e.Message);}}
}
