using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

static class MicaProbe
{
    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);
    [DllImport("dwmapi.dll")]
    static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS m);
    [StructLayout(LayoutKind.Sequential)]
    struct MARGINS { public int l, r, t, b; }

    [STAThread]
    static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        var f = new Form();
        f.FormBorderStyle = FormBorderStyle.None;
        f.StartPosition = FormStartPosition.Manual;
        f.Location = new Point(600, 200);
        f.Size = new Size(900, 420);
        f.BackColor = Color.FromArgb(1, 1, 1);
        f.TransparencyKey = Color.FromArgb(1, 1, 1);
        f.ShowInTaskbar = false;

        var l1 = new Label();
        l1.Text = "字体替换  Font Test 0123456789";
        l1.Font = new Font("Microsoft YaHei", 15f, FontStyle.Regular, GraphicsUnit.Pixel);
        l1.ForeColor = Color.White;
        l1.AutoSize = false;
        l1.Location = new Point(30, 30);
        l1.Size = new Size(700, 40);

        var card = new Panel();
        card.BackColor = Color.FromArgb(40, 255, 255, 255);
        card.Location = new Point(30, 90);
        card.Size = new Size(500, 120);

        var l2 = new Label();
        l2.Text = "卡片里的文字 Card Text 9:41";
        l2.Font = new Font("Microsoft YaHei", 14f, FontStyle.Regular, GraphicsUnit.Pixel);
        l2.ForeColor = Color.White;
        l2.AutoSize = false;
        l2.Location = new Point(14, 14);
        l2.Size = new Size(460, 34);
        card.Controls.Add(l2);

        f.Controls.Add(l1);
        f.Controls.Add(card);

        f.HandleCreated += delegate
        {
            var margins = new MARGINS { l = -1, r = -1, t = -1, b = -1 };
            DwmExtendFrameIntoClientArea(f.Handle, ref margins);
            int t = 2; // Mica
            int hr = DwmSetWindowAttribute(f.Handle, 38, ref t, 4);
            Console.WriteLine("mica hr=0x" + hr.ToString("x"));
        };
        f.Shown += delegate
        {
            var tmr = new Timer();
            tmr.Interval = 8000;
            tmr.Tick += delegate { tmr.Stop(); f.Close(); };
            tmr.Start();
        };
        Application.Run(f);
    }
}
