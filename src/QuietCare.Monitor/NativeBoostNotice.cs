using System;
using System.Drawing;
using System.Windows.Forms;
namespace QuietCare;

// A user-requested Boost reports its result beside the widget even if Windows balloons are muted.
internal sealed class NativeBoostNotice : Form
{
    readonly Label message;
    readonly Timer dismiss=new Timer{Interval=12000};
    public NativeBoostNotice(Action details)
    {
        Text="QuietCare RAM Boost";FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;
        StartPosition=FormStartPosition.Manual;AutoScaleMode=AutoScaleMode.Dpi;
        ClientSize=new Size(342,190);BackColor=Color.FromArgb(21,33,53);ForeColor=Color.FromArgb(236,243,255);
        Font=new Font("Segoe UI",9f);
        Controls.Add(new Label{Text="RAM Boost",Location=new Point(14,12),Size=new Size(260,22),Font=new Font("Segoe UI",10f,FontStyle.Bold)});
        message=new Label{Location=new Point(14,39),Size=new Size(312,112)};Controls.Add(message);
        var link=new LinkLabel{Text="Details",Location=new Point(14,160),AutoSize=true,LinkColor=Color.FromArgb(110,218,255)};
        link.LinkClicked+=(_,_)=>{Close();details();};Controls.Add(link);
        var close=new Label{Text="×",Location=new Point(312,9),Size=new Size(23,23),Cursor=Cursors.Hand};close.Click+=(_,_)=>Close();Controls.Add(close);
        dismiss.Tick+=(_,_)=>Close();
    }
    protected override bool ShowWithoutActivation=>true;
    internal static void RenderPreview(string path)
    {
        using var form=new NativeBoostNotice(()=>{});
        form.message.Text="Fixture: RAM checked after 3 seconds. Available +64 MB; 2/3 processes still have a lower resident footprint. RAM used: 65.4% → 65.0%.";
        form.Show();form.PerformLayout();form.Update();using var bitmap=new Bitmap(form.ClientSize.Width,form.ClientSize.Height);
        form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,form.ClientSize));bitmap.Save(path,System.Drawing.Imaging.ImageFormat.Png);
    }
    protected override CreateParams CreateParams{get{var value=base.CreateParams;value.ExStyle|=0x80|0x08000000;return value;}}
    public void UpdateResult(string text,bool busy,Rectangle anchor)
    {
        message.Text=text;dismiss.Stop();
        var area=Screen.FromRectangle(anchor).WorkingArea;
        Location=new Point(Math.Clamp(anchor.Right-Width,area.Left,Math.Max(area.Left,area.Right-Width)),Math.Clamp(anchor.Bottom+6,area.Top,Math.Max(area.Top,area.Bottom-Height)));
        if(!Visible)Show();if(!busy)dismiss.Start();
    }
    protected override void Dispose(bool disposing){if(disposing)dismiss.Dispose();base.Dispose(disposing);}
}
