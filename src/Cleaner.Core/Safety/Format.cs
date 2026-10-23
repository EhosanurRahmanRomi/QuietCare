#nullable disable
using System;
namespace QuietCareSafety {
public static class Format {
    public static string Rate(double? value){return value.HasValue?Bytes(value.Value)+"/s":"—";}
    public static string Number(double? value,string format="F0"){return value.HasValue?value.Value.ToString(format,System.Globalization.CultureInfo.InvariantCulture):"";}
    public static string Bytes(double value) { string[] units={"B","KB","MB","GB","TB"};int i=0;while(value>=1024 && i<4){value/=1024;i++;}return value.ToString(i==0?"0":"0.0")+" "+units[i]; }
    public static string Percent(double? value) {return value.HasValue?value.Value.ToString("0")+"%":"—";}
}
public static class MonitorPolicy {
    public static bool Active(bool mainVisible,bool minimized,bool widgetVisible,bool suspended){return !suspended && ((mainVisible && !minimized)||widgetVisible);}
}
}
