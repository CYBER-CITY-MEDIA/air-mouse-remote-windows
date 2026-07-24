using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

/// <summary>
/// Air Mouse Remote — product shell for EASYTONE/G10-class remotes.
/// MIC (HID 0xCF) -> set air mouse mic -> Win+H
/// First-run wizard teaches setup + click vs Enter lock behavior.
/// </summary>
public class AirMouseRemote : Form {
    const int WM_INPUT = 0x00FF;
    const int RID_INPUT = 0x10000003;
    const int RIDI_DEVICENAME = 0x20000007;
    const int RIM_TYPEHID = 2;
    const uint RIDEV_INPUTSINK = 0x00000100;
    const string AppTitle = "Air Mouse Remote";
    const string MutexName = "Global\\AirMouseRemote_SingleInstance";

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

    string dir, logPath, flagPath;
    NotifyIcon tray;
    Label status;
    TextBox guide;
    DateTime lastFire = DateTime.MinValue;
    bool micWasDown;
    System.Windows.Forms.Timer heartbeat;

    public AirMouseRemote() {
        dir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
        logPath = Path.Combine(dir, "airmouse-remote.log");
        flagPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AirMouseRemote", "setup-done.flag");

        Text = AppTitle + " — MIC = Win+H";
        Width = 640;
        Height = 520;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(18, 20, 26);
        ForeColor = Color.FromArgb(230, 235, 240);
        Font = new Font("Segoe UI", 10f);

        status = new Label {
            Left = 16, Top = 12, Width = 600, Height = 56,
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = Color.LightGreen,
            Text = "RUNNING — press MIC on the air mouse"
        };

        guide = new TextBox {
            Left = 16, Top = 72, Width = 600, Height = 340,
            Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
            BackColor = Color.FromArgb(28, 32, 40), ForeColor = Color.WhiteSmoke,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9.5f),
            Text = GuideText()
        };

        var btnTest = MkBtn("Test Win+H", 16, 430, 120);
        var btnGuide = MkBtn("Show quick guide", 150, 430, 140);
        var btnHide = MkBtn("Hide to tray", 304, 430, 120);
        var btnStartup = MkBtn("Install startup", 438, 430, 130);
        btnTest.Click += (s, e) => Fire("test");
        btnGuide.Click += (s, e) => ShowWizard(false);
        btnHide.Click += (s, e) => HideToTray();
        btnStartup.Click += (s, e) => { InstallStartup(); MessageBox.Show(this, "Will start with Windows.", AppTitle); };

        Controls.AddRange(new Control[] { status, guide, btnTest, btnGuide, btnHide, btnStartup });

        tray = new NotifyIcon {
            Icon = SystemIcons.Information,
            Text = AppTitle + " (MIC = Win+H)",
            Visible = true
        };
        tray.DoubleClick += (s, e) => { Show(); Activate(); };
        var menu = new ContextMenuStrip();
        menu.Items.Add("Show", null, (s, e) => { Show(); Activate(); });
        menu.Items.Add("Quick guide", null, (s, e) => { Show(); ShowWizard(false); });
        menu.Items.Add("Test Win+H", null, (s, e) => Fire("tray"));
        menu.Items.Add("Exit", null, (s, e) => { tray.Visible = false; Application.Exit(); });
        tray.ContextMenuStrip = menu;

        Load += OnLoad;
        FormClosing += (s, e) => {
            if (e.CloseReason == CloseReason.UserClosing) {
                e.Cancel = true;
                HideToTray();
            }
        };
    }

    Button MkBtn(string t, int x, int y, int w) {
        var b = new Button {
            Text = t, Left = x, Top = y, Width = w, Height = 34,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(50, 90, 160),
            ForeColor = Color.White
        };
        b.FlatAppearance.BorderSize = 0;
        return b;
    }

    static string GuideText() {
        return
            "SAME DEVICE AS AMAZON EASYTONE / G10 AIR MOUSE\r\n" +
            "================================================\r\n\r\n" +
            "WHAT THIS APP DOES\r\n" +
            "  • Listens for the MIC button (special HID signal, not a normal key)\r\n" +
            "  • Selects the air mouse microphone in Windows\r\n" +
            "  • Presses Windows + H (built-in Voice Typing)\r\n" +
            "  • Runs in the background (tray icon)\r\n\r\n" +
            "HOW TO DICTATE\r\n" +
            "  1. Click inside a text box (chat, email, Notepad…)\r\n" +
            "  2. Press MIC on the remote (beep = software heard it)\r\n" +
            "  3. Speak into the mic hole on the remote\r\n" +
            "  4. Stop with MIC again or the Voice Typing UI\r\n\r\n" +
            "CENTER BUTTON = CLICK vs ENTER  (very important)\r\n" +
            "  • Air mouse UNLOCKED (cursor moves when you wave)\r\n" +
            "      → center button = MOUSE CLICK\r\n" +
            "  • Air mouse LOCKED (use the mouse on/off key; pointer stops flying)\r\n" +
            "      → center button = ENTER\r\n" +
            "  For dictation, lock the air mouse when you need Enter to send/confirm.\r\n\r\n" +
            "WHY SOFTWARE IS REQUIRED\r\n" +
            "  The listing is “Windows compatible” for mouse/keyboard. The MIC button\r\n" +
            "  was designed for Android TV voice search. This app maps it to Win+H.\r\n";
    }

    void OnLoad(object s, EventArgs e) {
        Register();
        Log("STARTED");
        heartbeat = new System.Windows.Forms.Timer { Interval = 5000 };
        heartbeat.Tick += (a, b) => {
            try {
                string hb = Path.Combine(dir, "heartbeat.txt");
                File.WriteAllText(hb, DateTime.Now.ToString("o"));
            } catch { }
        };
        heartbeat.Start();

        bool first = !File.Exists(flagPath);
        if (first) {
            InstallStartup();
            ShowWizard(true);
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(flagPath));
                File.WriteAllText(flagPath, DateTime.Now.ToString("o"));
            } catch { }
        } else {
            HideToTray();
            try { tray.ShowBalloonTip(2500, AppTitle, "Running — press MIC for dictation", ToolTipIcon.Info); } catch { }
        }
    }

    void ShowWizard(bool firstRun) {
        Show();
        Activate();
        string title = firstRun ? "Welcome — 60 second setup" : "Air mouse quick guide";
        string msg =
            "For the EASYTONE / G10 air mouse on Windows:\r\n\r\n" +
            "1) Dongle plugged in\r\n" +
            "2) This app running in the tray (auto-starts with Windows)\r\n" +
            "3) Click a text box → press MIC → speak\r\n\r\n" +
            "CENTER BUTTON:\r\n" +
            "• Wave mode (unlocked) = CLICK\r\n" +
            "• Locked air mouse = ENTER\r\n\r\n" +
            "Lock the air mouse when you need Enter after dictating.";
        MessageBox.Show(this, msg, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    void HideToTray() {
        Hide();
        try { tray.ShowBalloonTip(1500, AppTitle, "Still running in tray", ToolTipIcon.Info); } catch { }
    }

    void InstallStartup() {
        try {
            string startup = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string lnk = Path.Combine(startup, "Air Mouse Remote.lnk");
            string exe = Application.ExecutablePath;
            // Use WScript via temp vbs to avoid COM issues
            string vbs = Path.Combine(Path.GetTempPath(), "amr-startup.vbs");
            File.WriteAllText(vbs,
                "Set s=CreateObject(\"WScript.Shell\")\r\n" +
                "Set sc=s.CreateShortcut(\"" + lnk.Replace("\\", "\\\\") + "\")\r\n" +
                "sc.TargetPath=\"" + exe.Replace("\\", "\\\\") + "\"\r\n" +
                "sc.WorkingDirectory=\"" + dir.Replace("\\", "\\\\") + "\"\r\n" +
                "sc.Save\r\n");
            // simpler: PowerShell
            string ps = string.Format(
                "$w=New-Object -ComObject WScript.Shell; $s=$w.CreateShortcut('{0}'); $s.TargetPath='{1}'; $s.WorkingDirectory='{2}'; $s.Save()",
                lnk.Replace("'", "''"), exe.Replace("'", "''"), dir.Replace("'", "''"));
            Process.Start(new ProcessStartInfo {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -Command " + ps,
                CreateNoWindow = true,
                UseShellExecute = false
            });
            Log("startup installed");
        } catch (Exception ex) { Log("startup fail " + ex.Message); }
    }

    void Register() {
        var devs = new RAWINPUTDEVICE[] {
            new RAWINPUTDEVICE { usUsagePage = 0x0C, usUsage = 0x01, dwFlags = RIDEV_INPUTSINK, hwndTarget = Handle },
            new RAWINPUTDEVICE { usUsagePage = 0x01, usUsage = 0x06, dwFlags = RIDEV_INPUTSINK, hwndTarget = Handle },
            new RAWINPUTDEVICE { usUsagePage = 0x01, usUsage = 0x02, dwFlags = RIDEV_INPUTSINK, hwndTarget = Handle },
            new RAWINPUTDEVICE { usUsagePage = 0x01, usUsage = 0x01, dwFlags = RIDEV_INPUTSINK, hwndTarget = Handle },
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
        if ((DateTime.Now - lastFire).TotalMilliseconds < 800) return;
        lastFire = DateTime.Now;
        Log("FIRE " + reason);
        try { Beep(1400, 50); Beep(1800, 50); } catch { }

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

        ThreadPool.QueueUserWorkItem(_ => {
            try {
                Thread.Sleep(250);
                keybd_event(0x5B, 0, 0, UIntPtr.Zero);
                keybd_event(0x48, 0, 0, UIntPtr.Zero);
                keybd_event(0x48, 0, 2, UIntPtr.Zero);
                keybd_event(0x5B, 0, 2, UIntPtr.Zero);
                Log("sent Win+H");
            } catch (Exception ex) { Log("winh " + ex.Message); }
        });

        try {
            if (status.IsHandleCreated)
                BeginInvoke(new Action(() => {
                    status.Text = "MIC heard — Win+H sent — speak now";
                    status.ForeColor = Color.Yellow;
                }));
        } catch { }
        try { tray.ShowBalloonTip(1200, "Dictation", "Win+H — speak now", ToolTipIcon.Info); } catch { }
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
        } catch (Exception ex) { Log("WndProc " + ex.Message); }
        base.WndProc(ref m);
    }

    [STAThread]
    public static void Main() {
        try {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            bool created;
            using (var mx = new Mutex(true, MutexName, out created)) {
                if (!created) {
                    MessageBox.Show(AppTitle + " is already running (check the system tray).", AppTitle);
                    return;
                }
                Application.Run(new AirMouseRemote());
            }
        } catch (Exception ex) {
            MessageBox.Show(ex.Message, AppTitle + " error");
        }
    }
}
