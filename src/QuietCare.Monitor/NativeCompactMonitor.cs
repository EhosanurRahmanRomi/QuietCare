using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace QuietCare;

// Paints only when values/settings change; the compact view has no WPF composition loop.
internal sealed class NativeCompactMonitor : Form
{
    readonly MonitorController controller;
    readonly Settings settings;
    Snapshot latest;
    string signature;
    float scale = 1;
    bool closing;
    bool boostBusy;
    public NativeCompactMonitor(MonitorController controller,Settings settings)
    {
        this.controller=controller;this.settings=settings;
        Text="QuietCare Next Monitor";FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;
        TopMost=settings.Topmost;AutoScaleMode=AutoScaleMode.None;StartPosition=FormStartPosition.Manual;
        Size=new Size(212,28);
        var screen=Screen.PrimaryScreen.WorkingArea;
        Location=new Point(settings.WidgetX>=screen.Left&&settings.WidgetX<=screen.Right-Width?(int)settings.WidgetX:screen.Right-Width-18,
            settings.WidgetY>=screen.Top&&settings.WidgetY<=screen.Bottom-Height?(int)settings.WidgetY:screen.Top+32);
        var menu=new ContextMenuStrip();
        menu.Items.Add("Smart RAM Boost",null,(_,_)=>controller.BoostRam());
        menu.Items.Add("Last Boost results",null,(_,_)=>controller.Reveal("Boost results"));
        menu.Items.Add("Open QuietCare Next",null,(_,_)=>controller.Reveal());
        var rates=new ToolStripMenuItem("Refresh speed");
        foreach(var value in new[]{100,250,500,1000,2000,5000,10000}){int ms=value;rates.DropDownItems.Add(ms+" ms",null,(_,_)=>controller.SetRefresh(ms));}
        menu.Items.Add(rates);
        menu.Items.Add("Transparency and startup settings",null,(_,_)=>controller.Reveal("Settings"));
        menu.Items.Add("Hide monitor",null,(_,_)=>controller.ToggleWidget());
        menu.Items.Add("Exit QuietCare Next",null,(_,_)=>controller.Exit());
        ContextMenuStrip=menu;
    }
    protected override bool ShowWithoutActivation=>true;
    protected override CreateParams CreateParams {get{var value=base.CreateParams;value.ExStyle|=0x80000|0x80|0x08000000;return value;}}
    protected override void OnShown(EventArgs e){base.OnShown(e);scale=DeviceDpi/96f;Size=new Size((int)Math.Ceiling(212*scale),(int)Math.Ceiling(28*scale));PaintLayer();}
    protected override void OnDpiChanged(DpiChangedEventArgs e){base.OnDpiChanged(e);scale=e.DeviceDpiNew/96f;Size=new Size((int)Math.Ceiling(212*scale),(int)Math.Ceiling(28*scale));PaintLayer();}
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);if(e.Button!=MouseButtons.Left)return;
        float x=e.X/scale;
        if(x<27){controller.BoostRam();return;}
        if(x>190){controller.OpenDetails();return;}
        ReleaseCapture();SendMessage(Handle,0xA1,(IntPtr)2,IntPtr.Zero);
    }
    protected override void OnMouseDoubleClick(MouseEventArgs e){base.OnMouseDoubleClick(e);if(e.Button==MouseButtons.Left)controller.OpenDetails();}
    protected override void WndProc(ref Message m){base.WndProc(ref m);if(m.Msg==0x232){settings.WidgetX=Left;settings.WidgetY=Top;settings.Save();}}
    protected override void OnFormClosing(FormClosingEventArgs e){if(!closing){e.Cancel=true;controller.ToggleWidget();return;}base.OnFormClosing(e);}
    public void Shutdown(){closing=true;Close();Dispose();}
    public void ApplySettings(){TopMost=settings.Topmost;signature=null;PaintLayer();}
    public void Update(Snapshot value)
    {
        latest=value;
        string next=Format.Percent(value.Cpu)+"|"+Format.Percent(value.Gpu)+"|"+Format.Percent(value.Ram);
        if(next==signature)return;signature=next;if(Visible)PaintLayer();
    }
    public void Feedback(string text){controller.ShowBoostNotice(text);}
    public void SetBusy(bool value){boostBusy=value;PaintLayer();}
    internal static Bitmap Draw(Snapshot value,double opacity,float scale=1,bool busy=false)
    {
        var bitmap=new Bitmap((int)Math.Ceiling(212*scale),(int)Math.Ceiling(28*scale),PixelFormat.Format32bppPArgb);
        using(var g=Graphics.FromImage(bitmap))
        using(var font=new Font("Segoe UI",7.875f,FontStyle.Regular,GraphicsUnit.Point))
        using(var bold=new Font("Segoe UI",7.875f,FontStyle.Bold,GraphicsUnit.Point))
        using(var background=new SolidBrush(Color.FromArgb((int)(255*Settings.Opacity(opacity)),8,12,35)))
        using(var border=new Pen(Color.FromArgb(100,97,142,184)))
        using(var text=new SolidBrush(Color.FromArgb(242,246,255)))
        using(var cyan=new SolidBrush(Color.FromArgb(98,221,255)))
        using(var violet=new SolidBrush(Color.FromArgb(186,154,255)))
        using(var mint=new SolidBrush(Color.FromArgb(100,229,183)))
        using(var shape=new GraphicsPath())
        {
            g.ScaleTransform(scale,scale);g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;
            shape.AddArc(0,0,11,11,180,90);shape.AddArc(200,0,11,11,270,90);shape.AddArc(200,16,11,11,0,90);shape.AddArc(0,16,11,11,90,90);shape.CloseFigure();
            g.FillPath(background,shape);g.DrawPath(border,shape);
            if(busy)g.DrawString("…",bold,mint,8,6);
            else g.FillPolygon(mint,new[]{new PointF(15,5),new PointF(9,15),new PointF(14,15),new PointF(11,23),new PointF(21,11),new PointF(15,11)});
            g.DrawString("›",bold,cyan,199,6);
            float x=28;
            foreach(var item in new[]{Tuple.Create("CPU",Format.Percent(value?.Cpu),cyan),Tuple.Create("GPU",Format.Percent(value?.Gpu),violet),Tuple.Create("RAM",value==null?"—":Format.Percent(value.Ram),mint)})
            {
                g.DrawString(item.Item1,font,item.Item3,x,7,StringFormat.GenericTypographic);
                float number=x+g.MeasureString(item.Item1,font,100,StringFormat.GenericTypographic).Width+3;
                g.DrawString(item.Item2,bold,text,number,7,StringFormat.GenericTypographic);x+=56;
            }
        }
        return bitmap;
    }
    void PaintLayer()
    {
        if(!IsHandleCreated||IsDisposed)return;
        using(var bitmap=Draw(latest,settings.WidgetOpacity,scale,boostBusy))
        {
            IntPtr screen=GetDC(IntPtr.Zero),memory=IntPtr.Zero,hBitmap=IntPtr.Zero,old=IntPtr.Zero;
            try
            {
                memory=CreateCompatibleDC(screen);hBitmap=bitmap.GetHbitmap(Color.FromArgb(0));old=SelectObject(memory,hBitmap);
                var destination=new Point(Left,Top);var source=new Point(0,0);var size=bitmap.Size;var blend=new Blend{SourceConstantAlpha=255,AlphaFormat=1};
                if(!UpdateLayeredWindow(Handle,screen,ref destination,ref size,memory,ref source,0,ref blend,2))throw new System.ComponentModel.Win32Exception();
            }
            finally{if(old!=IntPtr.Zero)SelectObject(memory,old);if(hBitmap!=IntPtr.Zero)DeleteObject(hBitmap);if(memory!=IntPtr.Zero)DeleteDC(memory);if(screen!=IntPtr.Zero)ReleaseDC(IntPtr.Zero,screen);}
        }
    }
    [StructLayout(LayoutKind.Sequential,Pack=1)]struct Blend{public byte BlendOp,BlendFlags,SourceConstantAlpha,AlphaFormat;}
    [DllImport("user32.dll",SetLastError=true)]static extern bool UpdateLayeredWindow(IntPtr window,IntPtr dc,ref Point destination,ref Size size,IntPtr memory,ref Point source,uint key,ref Blend blend,uint flags);
    [DllImport("user32.dll")]static extern IntPtr GetDC(IntPtr window);
    [DllImport("user32.dll")]static extern int ReleaseDC(IntPtr window,IntPtr dc);
    [DllImport("gdi32.dll")]static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")]static extern bool DeleteDC(IntPtr dc);
    [DllImport("gdi32.dll")]static extern IntPtr SelectObject(IntPtr dc,IntPtr value);
    [DllImport("gdi32.dll")]static extern bool DeleteObject(IntPtr value);
    [DllImport("user32.dll")]static extern bool ReleaseCapture();
    [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr window,uint message,IntPtr wParam,IntPtr lParam);
}
