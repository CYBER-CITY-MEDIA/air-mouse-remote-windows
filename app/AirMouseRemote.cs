using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

// MIC HID 0xCF -> Win+H. Always visible in TASKBAR + SYSTEM TRAY.

public class AirMouseRemote : Form {
    const int WM_INPUT = 0x00FF;
    const int RID_INPUT = 0x10000003;
    const int RIDI_DEVICENAME = 0x20000007;
    const int RIM_TYPEHID = 2;
    const uint RIDEV_INPUTSINK = 0x00000100;

    [StructLayout(LayoutKind.Sequential)]
    struct RAWINPUTDEVICE { public ushort usUsagePage; public ushort usUsage; public uint dwFlags; public IntPtr hwndTarget; }
    [StructLayout(LayoutKind.Sequential)]
    struct RAWINPUTHEADER { public int dwType; public int dwSize; public IntPtr hDevice; public IntPtr wParam; }
    [StructLayout(LayoutKind.Sequential)]
    struct RAWHID { public int dwSizeHid; public int dwCount; }

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool RegisterRawInputDevices([In] RAWINPUTDEVICE[] p, uint n, uint cb);
    [DllImport("user32.dll")]
    static extern uint GetRawInputData(IntPtr h, int cmd, IntPtr p, ref uint sz, int hdr);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern uint GetRawInputDeviceInfo(IntPtr h, uint cmd, StringBuilder p, ref uint sz);
    [DllImport("user32.dll")]
    static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr extra);
    [DllImport("kernel32.dll")]
    static extern bool Beep(int f, int d);

    NotifyIcon tray;
    Label lbl;
    string dir, logPath;
    DateTime lastFire = DateTime.MinValue;
    bool micWasDown;

    public AirMouseRemote() {
        dir = Path.GetDirectoryName(Application.ExecutablePath);
        if (string.IsNullOrEmpty(dir))
            dir = @"C:\Users\trent\Documents\AirMouse";
        logPath = Path.Combine(dir, "airmouse-remote.log");

        // TASKBAR: always
        Text = "Air Mouse Remote - MIC = Win+H (RUNNING)";
        Name = "AirMouseRemoteMain";
        Width = 520;
        Height = 260;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = true;
        ShowInTaskbar = true;          // FORCE taskbar button
        ShowIcon = true;
        TopMost = true;
        Visible = true;
        WindowState = FormWindowState.Normal;
        BackColor = Color.FromArgb(0, 90, 45);
        ForeColor = Color.White;
        try { Icon = SystemIcons.Shield; } catch { }

        lbl = new Label {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Text =
                "AIR MOUSE REMOTE IS ON\r\n\r\n" +
                "Taskbar + tray icon should both show this app.\r\n\r\n" +
                "1) Click a text box\r\n" +
                "2) Press MIC on remote\r\n" +
                "3) Win+H -> speak\r\n\r\n" +
                "X minimizes (does not quit). Tray = Exit completely."
        };
        Controls.Add(lbl);

        // SYSTEM TRAY: always
        tray = new NotifyIcon();
        try {
            tray.Icon = SystemIcons.Shield;
            tray.Text = "Air Mouse Remote ON - MIC = Win+H";
            tray.Visible = true;
            tray.BalloonTipTitle = "Air Mouse Remote";
            tray.BalloonTipText = "Running - MIC = Win+H. Also on taskbar.";
            tray.DoubleClick += (s, e) => ShowMain();
            var menu = new ContextMenuStrip();
            menu.Items.Add("Show window (taskbar)", null, (s, e) => ShowMain());
            menu.Items.Add("Test Win+H", null, (s, e) => Fire("tray"));
            menu.Items.Add("Exit completely", null, (s, e) => {
                try { tray.Visible = false; tray.Dispose(); } catch { }
                Application.Exit();
            });
            tray.ContextMenuStrip = menu;
        } catch (Exception ex) {
            Log("tray fail " + ex.Message);
        }

        Load += (s, e) => {
            try {
                // ensure visible on taskbar after handle created
                ShowInTaskbar = true;
                WindowState = FormWindowState.Normal;
                Show();
                Activate();
                BringToFront();
                RegisterRaw();
                Log("STARTED taskbar+tray build");
                try {
                    tray.Visible = true;
                    tray.ShowBalloonTip(4000, "Air Mouse Remote ON",
                        "In taskbar AND tray. MIC = Win+H.", ToolTipIcon.Info);
                } catch { }
            } catch (Exception ex) {
                Log("Load " + ex);
            }
        };

        // X = MINIMIZE to taskbar (still visible on taskbar), keep tray
        FormClosing += (s, e) => {
            if (e.CloseReason == CloseReason.UserClosing) {
                e.Cancel = true;
                WindowState = FormWindowState.Minimized;
                ShowInTaskbar = true; // stay on taskbar when minimized
                Log("minimized (still running taskbar+tray)");
                try {
                    tray.ShowBalloonTip(2000, "Still running",
                        "Minimized to taskbar. Tray icon also on. MIC still works.", ToolTipIcon.Info);
                } catch { }
            }
        };

        // heartbeat file so we know it's alive
        var hb = new System.Windows.Forms.Timer { Interval = 3000 };
        hb.Tick += (s, e) => {
            try {
                File.WriteAllText(Path.Combine(dir, "heartbeat.txt"), DateTime.Now.ToString("o"));
                if (tray != null && !tray.Visible) tray.Visible = true;
            } catch { }
        };
        hb.Start();

        Application.ThreadException += (s, e) => Log("UI " + e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (s, e) => Log("FATAL " + e.ExceptionObject);
    }

    void ShowMain() {
        ShowInTaskbar = true;
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
        BringToFront();
    }

    void RegisterRaw() {
        var devs = new RAWINPUTDEVICE[] {
            new RAWINPUTDEVICE { usUsagePage = 0x0C, usUsage = 0x01, dwFlags = RIDEV_INPUTSINK, hwndTarget = Handle },
            new RAWINPUTDEVICE { usUsagePage = 0x01, usUsage = 0x06, dwFlags = RIDEV_INPUTSINK, hwndTarget = Handle },
            new RAWINPUTDEVICE { usUsagePage = 0x01, usUsage = 0x02, dwFlags = RIDEV_INPUTSINK, hwndTarget = Handle },
        };
        bool ok = RegisterRawInputDevices(devs, (uint)devs.Length, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE)));
        Log(ok ? "RawInput OK" : "RawInput FAIL " + Marshal.GetLastWin32Error());
    }

    string DevName(IntPtr h) {
        try {
            uint sz = 0;
            GetRawInputDeviceInfo(h, RIDI_DEVICENAME, null, ref sz);
            if (sz == 0) return "";
            var sb = new StringBuilder((int)sz);
            GetRawInputDeviceInfo(h, RIDI_DEVICENAME, sb, ref sz);
            return sb.ToString();
        } catch { return ""; }
    }

    bool IsAir(string n) {
        if (string.IsNullOrEmpty(n)) return false;
        string u = n.ToUpperInvariant();
        return u.Contains("VID_1EA7") || u.Contains("VID_1915") || u.Contains("PID_1025") || u.Contains("PID_0066");
    }

    void Log(string m) {
        try { File.AppendAllText(logPath, DateTime.Now.ToString("HH:mm:ss.fff") + " " + m + "\r\n"); } catch { }
    }

    void Fire(string reason) {
        try {
            if ((DateTime.Now - lastFire).TotalMilliseconds < 700) return;
            lastFire = DateTime.Now;
            Log("FIRE " + reason);
            try { Beep(1200, 50); Beep(1600, 50); } catch { }

            try {
                string ps1 = Path.Combine(dir, "Set-BestMic.ps1");
                if (File.Exists(ps1)) {
                    Process.Start(new ProcessStartInfo {
                        FileName = "powershell.exe",
                        Arguments = "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"" + ps1 + "\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                }
            } catch (Exception ex) { Log("mic " + ex.Message); }

            var t = new System.Windows.Forms.Timer { Interval = 200 };
            t.Tick += (s, e) => {
                try {
                    t.Stop(); t.Dispose();
                    keybd_event(0x5B, 0, 0, UIntPtr.Zero);
                    keybd_event(0x48, 0, 0, UIntPtr.Zero);
                    keybd_event(0x48, 0, 2, UIntPtr.Zero);
                    keybd_event(0x5B, 0, 2, UIntPtr.Zero);
                    Log("sent Win+H");
                    lbl.Text = "SENT Win+H — SPEAK NOW\r\n\r\nApp still on taskbar + tray";
                    try { tray.ShowBalloonTip(1500, "Win+H", "Speak now", ToolTipIcon.Info); } catch { }
                } catch (Exception ex) { Log("winh " + ex.Message); }
            };
            t.Start();
        } catch (Exception ex) {
            Log("Fire ERR " + ex.Message);
        }
    }

    static bool HasCf(byte[] data) {
        if (data == null) return false;
        for (int i = 0; i < data.Length; i++)
            if (data[i] == 0xCF) return true;
        return false;
    }

    protected override void WndProc(ref Message m) {
        try {
            if (m.Msg == WM_INPUT) {
                uint sz = 0;
                GetRawInputData(m.LParam, RID_INPUT, IntPtr.Zero, ref sz, Marshal.SizeOf(typeof(RAWINPUTHEADER)));
                if (sz > 0 && sz < 4096) {
                    IntPtr buf = Marshal.AllocHGlobal((int)sz);
                    try {
                        if (GetRawInputData(m.LParam, RID_INPUT, buf, ref sz, Marshal.SizeOf(typeof(RAWINPUTHEADER))) == sz) {
                            var h = (RAWINPUTHEADER)Marshal.PtrToStructure(buf, typeof(RAWINPUTHEADER));
                            if (h.dwType == RIM_TYPEHID && IsAir(DevName(h.hDevice))) {
                                IntPtr p = IntPtr.Add(buf, Marshal.SizeOf(typeof(RAWINPUTHEADER)));
                                var hid = (RAWHID)Marshal.PtrToStructure(p, typeof(RAWHID));
                                int n = hid.dwSizeHid * Math.Max(hid.dwCount, 1);
                                if (n > 0 && n <= 64) {
                                    byte[] data = new byte[n];
                                    Marshal.Copy(IntPtr.Add(p, Marshal.SizeOf(typeof(RAWHID))), data, 0, n);
                                    bool down = HasCf(data);
                                    if (down && !micWasDown) {
                                        Log("MIC " + BitConverter.ToString(data));
                                        Fire("hid-cf");
                                    }
                                    micWasDown = down;
                                }
                            }
                        }
                    } finally { Marshal.FreeHGlobal(buf); }
                }
            }
        } catch (Exception ex) {
            Log("WndProc " + ex.Message);
        }
        base.WndProc(ref m);
    }

    [STAThread]
    public static void Main() {
        try {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            bool created;
            using (var mx = new Mutex(true, "Local\\AirMouseRemote_TaskbarTray_v3", out created)) {
                if (!created) {
                    // already running - don't silently vanish; ping log
                    try {
                        File.AppendAllText(
                            Path.Combine(@"C:\Users\trent\Documents\AirMouse", "airmouse-remote.log"),
                            DateTime.Now.ToString("HH:mm:ss") + " second instance exit (already running)\r\n");
                    } catch { }
                    return;
                }
                Application.Run(new AirMouseRemote());
            }
        } catch (Exception ex) {
            try {
                File.AppendAllText(@"C:\Users\trent\Documents\AirMouse\airmouse-remote.log", "MAIN FATAL " + ex + "\r\n");
                MessageBox.Show("Failed to start:\r\n" + ex.Message, "Air Mouse Remote");
            } catch { }
        }
    }
}
