// IT Support Tools v3.0 — Form1.cs — .NET 4.8 | WinForms | Admin
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ITSupportToolkit
{
    public partial class Form1 : Form
    {
        static readonly Color C_DARK    = Color.FromArgb(10,  14,  26);
        static readonly Color C_SIDEBAR = Color.FromArgb(15,  20,  35);
        static readonly Color C_PANEL   = Color.FromArgb(20,  27,  46);
        static readonly Color C_CARD    = Color.FromArgb(26,  35,  58);
        static readonly Color C_BORDER  = Color.FromArgb(40,  55,  85);
        static readonly Color C_ACCENT  = Color.FromArgb(56, 139, 253);
        static readonly Color C_ACCENT2 = Color.FromArgb(139, 92, 246);
        static readonly Color C_GREEN   = Color.FromArgb(35, 197, 135);
        static readonly Color C_RED     = Color.FromArgb(248,  81,  73);
        static readonly Color C_YELLOW  = Color.FromArgb(240, 184,  43);
        static readonly Color C_TEXT    = Color.FromArgb(220, 230, 245);
        static readonly Color C_SUBTEXT = Color.FromArgb(120, 140, 175);
        static readonly Color C_LOG_BG  = Color.FromArgb(  8,  11,  20);

        const int SW = 215, HH = 62, FH = 185;

        Panel pnlSidebar, pnlHeader, pnlContent, pnlFooter;
        RichTextBox rtbLog;
        ProgressBar pbMain;
        Label lblTask, lblClock, lblSysName, lblSysOS, lblSysIP, lblSysRAM;
        System.Windows.Forms.Timer tmrClock, tmrMon;

        ListView lvDrivers;
        Label lblDriverStatus;

        // Dashboard monitor
        PictureBox pboxGraph;
        Label lblCPUpct, lblRAMpct, lblDISKpct, lblNET, lblTEMP;
        PerformanceCounter pcCPU;
        List<float> cpuHist = new List<float>();
        float lastCPU, lastRAM, lastDisk;

        Dictionary<string, Panel> pages = new Dictionary<string, Panel>();
        List<NavBtn> navBtns = new List<NavBtn>();

        public Form1()
        {
            InitializeComponent();
            BuildUI();
            LoadSysInfoAsync();
            ShowPage("dashboard");
            StartClock();
            InitMon();
            Log("IT Support Tools v3.0 — San sang.", C_GREEN);
            Log("Dang chay voi quyen Administrator.", C_ACCENT);
        }

        // ─── BUILD UI ─────────────────────────────────────────────────────────
        void BuildUI()
        {
            Text = "IT Support Tools v3.0";
            Size = new Size(1200, 820);
            MinimumSize = new Size(1000, 700);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = C_DARK;
            Font = new Font("Segoe UI", 9f);
            DoubleBuffered = true;
            try { string ico = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "logo.ico"); Icon = File.Exists(ico) ? new Icon(ico) : SystemIcons.Shield; } catch { Icon = SystemIcons.Shield; }
            BuildHeader(); BuildSidebar(); BuildContent(); BuildFooter(); BuildAllPages();
            Resize += (s, e) => DoLayout();
            DoLayout();
        }

        void BuildHeader()
        {
            pnlHeader = new Panel { BackColor = C_SIDEBAR };
            pnlHeader.Paint += (s, e) => { using (var p = new Pen(C_ACCENT, 2)) e.Graphics.DrawLine(p, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1); };
            var pb = new PictureBox { Size = new Size(42, 42), Location = new Point(10, 9), SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Transparent };
            try { string ico = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "logo.ico"); if (File.Exists(ico)) pb.Image = new Icon(ico, 42, 42).ToBitmap(); else pb.Visible = false; } catch { pb.Visible = false; }
            var t1 = new Label { Text = "IT Support Tools", Font = new Font("Segoe UI", 14f, FontStyle.Bold), ForeColor = C_TEXT, AutoSize = true, Location = new Point(60, 8) };
            var t2 = new Label { Text = "v3.0  —  Administrator Mode", Font = new Font("Segoe UI", 7.5f), ForeColor = C_SUBTEXT, AutoSize = true, Location = new Point(62, 36) };
            lblClock = new Label { Font = new Font("Consolas", 11f, FontStyle.Bold), ForeColor = C_ACCENT, AutoSize = true };
            pnlHeader.Controls.AddRange(new Control[] { pb, t1, t2, lblClock });
            Controls.Add(pnlHeader);
        }

        void BuildSidebar()
        {
            pnlSidebar = new Panel { BackColor = C_SIDEBAR };
            pnlSidebar.Paint += (s, e) => { using (var p = new Pen(C_BORDER)) e.Graphics.DrawLine(p, pnlSidebar.Width - 1, 0, pnlSidebar.Width - 1, pnlSidebar.Height); };
            var nav = new[] { ("🏠","dashboard","Dashboard"), ("🖥","drivers","Driver Suite"), ("📦","software","Phan Mem"), ("🔧","optimize","Toi Uu He Thong"), ("🌐","network","Mang & Ket Noi"), ("ℹ","sysinfo","Thong Tin He Thong") };
            int y = 14;
            foreach (var (ico, id, lbl) in nav) { var b = new NavBtn(ico, lbl, id) { Location = new Point(8, y) }; b.Click += (s, e) => ShowPage(((NavBtn)s).PageId); navBtns.Add(b); pnlSidebar.Controls.Add(b); y += 50; }
            lblSysName = SL("PC: ...", new Point(8, 2)); lblSysOS = SL("OS: ...", new Point(8, 20)); lblSysIP = SL("IP: ...", new Point(8, 38)); lblSysRAM = SL("RAM: ...", new Point(8, 56));
            var mini = new Panel { BackColor = Color.FromArgb(12, 17, 30), Size = new Size(SW - 2, 78), Name = "ms" };
            mini.Controls.AddRange(new Control[] { lblSysName, lblSysOS, lblSysIP, lblSysRAM });
            pnlSidebar.Controls.Add(mini);
            Controls.Add(pnlSidebar);
        }

        Label SL(string t, Point p) => new Label { Text = t, ForeColor = C_SUBTEXT, Font = new Font("Segoe UI", 7.5f), AutoSize = false, Width = SW - 18, Height = 17, Location = p };

        void BuildContent() { pnlContent = new Panel { BackColor = C_PANEL }; Controls.Add(pnlContent); }

        void BuildFooter()
        {
            pnlFooter = new Panel { BackColor = C_LOG_BG };
            pnlFooter.Paint += (s, e) => { using (var p = new Pen(C_BORDER)) e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0); };
            pbMain = new ProgressBar { Minimum = 0, Maximum = 100, Style = ProgressBarStyle.Continuous, Height = 4, Name = "pb" };
            lblTask = new Label { ForeColor = C_SUBTEXT, Font = new Font("Segoe UI", 7.5f), AutoSize = true, Name = "lt" };
            var lh = new Label { Text = "  ▌ Console Log", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = C_ACCENT, AutoSize = true, Name = "lh" };
            var bc = new Button { Text = "✕ Clear", BackColor = Color.FromArgb(40, 55, 80), ForeColor = Color.FromArgb(160, 175, 210), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 7.5f), Size = new Size(65, 22), Cursor = Cursors.Hand, Name = "bc" };
            bc.FlatAppearance.BorderSize = 0; bc.Click += (s, e) => rtbLog.Clear();
            rtbLog = new RichTextBox { ReadOnly = true, BackColor = C_LOG_BG, ForeColor = C_TEXT, Font = new Font("Consolas", 8.5f), BorderStyle = BorderStyle.None, ScrollBars = RichTextBoxScrollBars.Vertical, DetectUrls = false };
            pnlFooter.Controls.AddRange(new Control[] { pbMain, lblTask, lh, bc, rtbLog });
            Controls.Add(pnlFooter);
        }

        void DoLayout()
        {
            int W = ClientSize.Width, H = ClientSize.Height;
            pnlHeader.SetBounds(0, 0, W, HH);
            if (lblClock != null) lblClock.Location = new Point(W - lblClock.Width - 16, 20);
            pnlSidebar.SetBounds(0, HH, SW, H - HH - FH);
            var ms = pnlSidebar.Controls["ms"]; if (ms != null) ms.Location = new Point(0, pnlSidebar.Height - ms.Height - 4);
            foreach (var nb in navBtns) nb.Width = SW - 16;
            pnlContent.SetBounds(SW, HH, W - SW, H - HH - FH);
            foreach (var pg in pages.Values) pg.Size = pnlContent.ClientSize;
            pnlFooter.SetBounds(0, H - FH, W, FH);
            var pb_ = pnlFooter.Controls["pb"] as ProgressBar; if (pb_ != null) pb_.SetBounds(0, 0, pnlFooter.Width, 4);
            var lt_ = pnlFooter.Controls["lt"] as Label; if (lt_ != null) lt_.Location = new Point(pnlFooter.Width / 2 - 100, 6);
            var bc_ = pnlFooter.Controls["bc"]; if (bc_ != null) bc_.Location = new Point(pnlFooter.Width - bc_.Width - 4, 2);
            var lh_ = pnlFooter.Controls["lh"]; if (lh_ != null) lh_.Location = new Point(4, 6);
            rtbLog.SetBounds(4, 26, pnlFooter.Width - 8, FH - 30);
            if (pboxGraph != null) LayoutDash();
        }

        void BuildAllPages()
        {
            pages["dashboard"] = BuildDashboard();
            pages["drivers"]   = BuildDriverPage();
            pages["software"]  = BuildSoftwarePage();
            pages["optimize"]  = BuildOptimizePage();
            pages["network"]   = BuildNetworkPage();
            pages["sysinfo"]   = BuildSysInfoPage();
            foreach (var pg in pages.Values) { pg.Visible = false; pg.Dock = DockStyle.Fill; pg.BackColor = C_PANEL; pnlContent.Controls.Add(pg); }
        }

        void ShowPage(string id)
        {
            if (!pages.ContainsKey(id)) return;
            foreach (var pg in pages.Values) pg.Visible = false;
            pages[id].Visible = true;
            foreach (var nb in navBtns) nb.SetActive(nb.PageId == id);
        }

        // ─── DASHBOARD — REAL-TIME MONITOR ────────────────────────────────────
        Panel BuildDashboard()
        {
            var pg = new Panel { AutoScroll = true, BackColor = C_PANEL };
            var lblT = new Label { Text = "📊  System Monitor — Real-time", Font = new Font("Segoe UI", 13f, FontStyle.Bold), ForeColor = C_TEXT, AutoSize = true, Location = new Point(16, 14) };
            var line = new Panel { BackColor = C_ACCENT, Location = new Point(16, 44), Height = 2, Name = "dl" };
            pg.Controls.AddRange(new Control[] { lblT, line });
            pg.Resize += (s, e) => { var l = pg.Controls["dl"]; if (l != null) l.Width = pg.Width - 32; LayoutDash(); };

            // CPU Graph
            pboxGraph = new PictureBox { Location = new Point(16, 56), Size = new Size(420, 110), BackColor = Color.FromArgb(12, 18, 32) };
            pboxGraph.Paint += PaintGraph;
            pg.Controls.Add(pboxGraph);

            // Stats labels — positioned in LayoutDash
            lblCPUpct  = MkMonLbl("CPU:  0%");
            lblRAMpct  = MkMonLbl("RAM:  0%");
            lblDISKpct = MkMonLbl("Disk C: 0%");
            lblNET     = MkMonLbl("Net: --");
            lblTEMP    = MkMonLbl("Temp: --");
            pg.Controls.AddRange(new Control[] { lblCPUpct, lblRAMpct, lblDISKpct, lblNET, lblTEMP });

            // Quick actions
            var lblQ = new Label { Text = "⚡  Thao Tac Nhanh", Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = C_ACCENT, AutoSize = true, Location = new Point(16, 180) };
            pg.Controls.Add(lblQ);
            var flow = new FlowLayoutPanel { Location = new Point(16, 206), Height = 120, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = true };
            var cards = new[] {
                ("🖥","Quet Driver",     C_ACCENT,  (Action)(()=>ShowPage("drivers"))),
                ("🟢","Cai Chrome",      C_GREEN,   ()=>Task.Run(async()=>await InstallDirect("Chrome","https://dl.google.com/chrome/install/latest/chrome_installer.exe",Path.Combine(Path.GetTempPath(),"ChromeSetup.exe"),"/silent /install"))),
                ("🔑","Ban Quyen Win",   C_ACCENT2, ()=>BtnActivation_Click(null,null)),
                ("🖨","Fix May In",      C_YELLOW,  ()=>BtnSpooler_Click(null,null)),
                ("🌐","Flush DNS",       C_ACCENT,  ()=>QuickFlushDns()),
                ("🗑","Don Temp",        C_RED,     ()=>BtnCleanTemp_Click(null,null)),
                ("⚡","Speed Test",      C_GREEN,   ()=>BtnSpeedTest_Click(null,null)),
            };
            foreach (var (ico, lbl, clr, act) in cards) { var c = new QuickCard(ico, lbl, clr); c.Click += (s, e) => act(); flow.Controls.Add(c); }
            pg.Controls.Add(flow);
            return pg;
        }

        Label MkMonLbl(string t) => new Label { Text = t, ForeColor = C_TEXT, Font = new Font("Consolas", 9.5f, FontStyle.Bold), AutoSize = true };

        void LayoutDash()
        {
            if (pboxGraph == null || pboxGraph.Parent == null) return;
            int pw = pboxGraph.Parent.Width;
            pboxGraph.Width = Math.Min(430, pw / 2 - 20);
            int bx = pboxGraph.Right + 16, by = 56, gap = 22;
            if (lblCPUpct  != null) lblCPUpct.Location  = new Point(bx, by);
            if (lblRAMpct  != null) lblRAMpct.Location  = new Point(bx, by + gap);
            if (lblDISKpct != null) lblDISKpct.Location = new Point(bx, by + gap * 2);
            if (lblNET     != null) lblNET.Location     = new Point(bx, by + gap * 3);
            if (lblTEMP    != null) lblTEMP.Location    = new Point(bx, by + gap * 4);
        }

        void PaintGraph(object s, PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.FromArgb(12, 18, 32));
            // Grid
            for (int i = 0; i <= 4; i++) {
                int gy = (int)(pboxGraph.Height * (1 - i / 4.0f));
                using (var p = new Pen(Color.FromArgb(25, 45, 75))) g.DrawLine(p, 0, gy, pboxGraph.Width, gy);
                using (var b = new SolidBrush(C_SUBTEXT)) g.DrawString($"{i * 25}%", new Font("Consolas", 6f), b, 2, gy - 8);
            }
            using (var b = new SolidBrush(C_ACCENT)) g.DrawString("CPU History", new Font("Segoe UI", 7.5f, FontStyle.Bold), b, 4, 3);
            if (cpuHist.Count < 2) return;
            int W = pboxGraph.Width, H = pboxGraph.Height;
            int start = Math.Max(0, cpuHist.Count - W / 2);
            var pts = new List<PointF>();
            for (int i = start; i < cpuHist.Count; i++) {
                float px = (float)(i - start) / Math.Max(1, cpuHist.Count - start - 1) * W;
                float py = H - cpuHist[i] / 100f * H;
                pts.Add(new PointF(px, py));
            }
            if (pts.Count > 1) {
                var fill = new List<PointF>(pts); fill.Add(new PointF(pts[pts.Count-1].X, H)); fill.Add(new PointF(pts[0].X, H));
                using (var b = new SolidBrush(Color.FromArgb(35, C_ACCENT))) g.FillPolygon(b, fill.ToArray());
                using (var p = new Pen(C_ACCENT, 2)) g.DrawLines(p, pts.ToArray());
                var last = pts[pts.Count - 1];
                using (var b = new SolidBrush(Color.White)) g.FillEllipse(b, last.X - 4, last.Y - 4, 8, 8);
            }
        }

        void InitMon()
        {
            try { pcCPU = new PerformanceCounter("Processor", "% Processor Time", "_Total"); pcCPU.NextValue(); } catch { }
            tmrMon = new System.Windows.Forms.Timer { Interval = 1500 };
            tmrMon.Tick += async (s, e) => await UpdateMon();
            tmrMon.Start();
        }

        async Task UpdateMon()
        {
            try {
                float cpu = 0; try { cpu = Math.Max(0, Math.Min(100, pcCPU?.NextValue() ?? 0)); } catch { }
                cpuHist.Add(cpu); if (cpuHist.Count > 150) cpuHist.RemoveAt(0);
                lastCPU = cpu;

                float ram = 0; string ramStr = "";
                await Task.Run(() => {
                    try {
                        using (var m = new ManagementObjectSearcher("SELECT FreePhysicalMemory,TotalVisibleMemorySize FROM Win32_OperatingSystem"))
                            foreach (ManagementObject mo in m.Get()) {
                                long free = Convert.ToInt64(mo["FreePhysicalMemory"]);
                                long total = Convert.ToInt64(mo["TotalVisibleMemorySize"]);
                                ram = (float)(total - free) / total * 100;
                                ramStr = $"{FormatBytes((total-free)*1024)} / {FormatBytes(total*1024)}";
                            }
                    } catch { }
                });
                lastRAM = ram;

                float disk = 0; string diskStr = "";
                try { var di = new DriveInfo("C"); disk = (float)(di.TotalSize - di.AvailableFreeSpace) / di.TotalSize * 100; diskStr = $"{FormatBytes(di.TotalSize-di.AvailableFreeSpace)} / {FormatBytes(di.TotalSize)}"; } catch { }
                lastDisk = disk;

                // Temp
                int temp = -1;
                await Task.Run(() => { try { using (var m = new ManagementObjectSearcher(@"root\wmi", "SELECT * FROM MSAcpi_ThermalZoneTemperature")) foreach (ManagementObject mo in m.Get()) { temp = (int)(Convert.ToDouble(mo["CurrentTemperature"]) - 2731) / 10; break; } } catch { } });

                // Network
                string netStr = "";
                try { foreach (var ni in NetworkInterface.GetAllNetworkInterfaces()) { if (ni.OperationalStatus != OperationalStatus.Up || ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue; var st = ni.GetIPv4Statistics(); netStr = $"{ni.Name}  ↑{FormatBytes(st.BytesSent)} ↓{FormatBytes(st.BytesReceived)}"; break; } } catch { }

                SafeInvoke(() => {
                    Color cpuClr = cpu > 80 ? C_RED : cpu > 50 ? C_YELLOW : C_GREEN;
                    Color ramClr = ram > 85 ? C_RED : ram > 60 ? C_YELLOW : C_GREEN;
                    Color dskClr = disk > 90 ? C_RED : disk > 75 ? C_YELLOW : C_GREEN;

                    if (lblCPUpct  != null) { lblCPUpct.Text  = $"CPU:   {MkBar(cpu,20)}  {cpu:F0}%"; lblCPUpct.ForeColor = cpuClr; }
                    if (lblRAMpct  != null) { lblRAMpct.Text  = $"RAM:   {MkBar(ram,20)}  {ram:F0}%  {ramStr}"; lblRAMpct.ForeColor = ramClr; }
                    if (lblDISKpct != null) { lblDISKpct.Text = $"Disk:  {MkBar(disk,20)}  {disk:F0}%  {diskStr}"; lblDISKpct.ForeColor = dskClr; }
                    if (lblNET     != null)   lblNET.Text     = $"Net:   {(string.IsNullOrEmpty(netStr) ? "--" : netStr)}";
                    if (lblTEMP    != null)   lblTEMP.Text    = temp > 0 ? $"Temp:  {temp}°C  {(temp > 85 ? "⚠ QUA NONG!" : temp > 70 ? "Nong" : "OK")}" : "Temp:  -- (khong co sensor)";
                    if (pboxGraph  != null)   pboxGraph.Refresh();
                });
            } catch { }
        }

        static string MkBar(float pct, int len) { int f = (int)(pct / 100f * len); return "[" + new string('█', f) + new string('░', len - f) + "]"; }

        // ─── DRIVER PAGE — GROUPED BY CATEGORY ────────────────────────────────
        Panel BuildDriverPage()
        {
            var pg = new Panel { AutoScroll = false };
            var lblT = new Label { Text = "🖥  Driver Suite — Nhom Theo Loai Thiet Bi", Font = new Font("Segoe UI", 13f, FontStyle.Bold), ForeColor = C_TEXT, AutoSize = true, Location = new Point(16, 14) };
            var line = new Panel { BackColor = C_ACCENT, Location = new Point(16, 44), Height = 2, Name = "dl" };
            var flow = new FlowLayoutPanel { Location = new Point(16, 56), Height = 44, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            var b1 = MkBtn("🔍  Quet Driver",    C_ACCENT);   b1.Click += BtnScanDrivers_Click;
            var b2 = MkBtn("🔧  Cai Driver Loi", C_RED);      b2.Click += BtnFixDrivers_Click;
            var b3 = MkBtn("🔄  Windows Update", C_GREEN);    b3.Click += BtnWinUpdateDriver_Click;
            var b4 = MkBtn("📋  Xuat Bao Cao",   C_ACCENT2);  b4.Click += BtnExportDrivers_Click;
            flow.Controls.AddRange(new Control[] { b1, b2, b3, b4 });
            lblDriverStatus = new Label { Text = "Nhan 'Quet Driver' — ket qua hien thi theo nhom thiet bi...", ForeColor = C_SUBTEXT, Font = new Font("Segoe UI", 9f), AutoSize = true, Location = new Point(16, 106) };
            lvDrivers = new ListView { Location = new Point(16, 130), View = View.Details, FullRowSelect = true, GridLines = false, BackColor = Color.FromArgb(18, 25, 42), ForeColor = C_TEXT, Font = new Font("Segoe UI", 8.5f), BorderStyle = BorderStyle.None, Name = "lvd" };
            lvDrivers.Columns.Add("Thiet Bi / Nhom", 320); lvDrivers.Columns.Add("Trang Thai", 130); lvDrivers.Columns.Add("Nha San Xuat", 185); lvDrivers.Columns.Add("Driver Ver", 145); lvDrivers.Columns.Add("Ngay", 110);
            pg.Controls.AddRange(new Control[] { lblT, line, flow, lblDriverStatus, lvDrivers });
            pg.Resize += (s, e) => { line.Width = pg.Width - 32; lvDrivers.Size = new Size(pg.Width - 32, pg.Height - 160); };
            return pg;
        }

        async void BtnScanDrivers_Click(object s, EventArgs e)
        {
            await RunGuarded("Quet & Phan Nhom Driver", async () => {
                SafeInvoke(() => { lvDrivers.Items.Clear(); lblDriverStatus.Text = "Dang quet phan cung qua WMI..."; lblDriverStatus.ForeColor = C_YELLOW; });
                Log("Quet Win32_PnPEntity + Win32_PnPSignedDriver...", C_YELLOW);

                // Category groups
                var groups = new Dictionary<string, (string Icon, List<DrvItem> Items)> {
                    ["display"]   = ("🖥  Display / GPU",    new List<DrvItem>()),
                    ["network"]   = ("🌐  Network / WiFi",   new List<DrvItem>()),
                    ["audio"]     = ("🔊  Audio / Sound",    new List<DrvItem>()),
                    ["storage"]   = ("💾  Storage / Disk",   new List<DrvItem>()),
                    ["input"]     = ("🖱  HID / Input",      new List<DrvItem>()),
                    ["usb"]       = ("🔌  USB",              new List<DrvItem>()),
                    ["bluetooth"] = ("📡  Bluetooth",        new List<DrvItem>()),
                    ["other"]     = ("🔧  System / Other",   new List<DrvItem>()),
                };

                var raw = new List<DrvItem>();
                await Task.Run(() => {
                    // Pass 1 — basic info + error code
                    using (var m = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity"))
                        foreach (ManagementObject mo in m.Get())
                            try {
                                int err = Convert.ToInt32(mo["ConfigManagerErrorCode"] ?? 0);
                                string name = mo["Name"]?.ToString() ?? ""; if (name.Length < 2) continue;
                                string mfr  = mo["Manufacturer"]?.ToString() ?? "";
                                string cls  = mo["PNPClass"]?.ToString() ?? "";
                                raw.Add(new DrvItem { Name = name, Mfr = mfr, PnpClass = cls, Status = err == 0 ? "OK" : ErrDesc(err), HasErr = err != 0 });
                            } catch { }
                    // Pass 2 — version + date
                    using (var m = new ManagementObjectSearcher("SELECT * FROM Win32_PnPSignedDriver WHERE DeviceName IS NOT NULL"))
                        foreach (ManagementObject mo in m.Get())
                            try {
                                string dn  = mo["DeviceName"]?.ToString() ?? "";
                                string ver = mo["DriverVersion"]?.ToString() ?? "";
                                string raw2= mo["DriverDate"]?.ToString() ?? "";
                                string mfr = mo["Manufacturer"]?.ToString() ?? "";
                                string date = "";
                                if (raw2.Length >= 8) try { date = ManagementDateTimeConverter.ToDateTime(raw2).ToString("dd/MM/yyyy"); } catch { }
                                var found = raw.Find(r => r.Name == dn);
                                if (found != null) { found.Ver = ver; found.Date = date; if (!string.IsNullOrEmpty(mfr)) found.Mfr = mfr; }
                            } catch { }
                    // Classify
                    foreach (var item in raw) {
                        string cls = item.PnpClass.ToLower();
                        string nm  = item.Name.ToLower();
                        string key = "other";
                        if (cls == "display" || nm.Contains("nvidia") || nm.Contains("amd") || nm.Contains("radeon") || nm.Contains("geforce") || nm.Contains("intel graphics") || nm.Contains("vga") || nm.Contains("video controller"))
                            key = "display";
                        else if (cls == "net" || cls == "netclient" || nm.Contains("ethernet") || nm.Contains("wireless") || nm.Contains("wi-fi") || nm.Contains("wifi") || nm.Contains("wlan") || nm.Contains("lan controller"))
                            key = "network";
                        else if (cls == "media" || cls == "audio" || nm.Contains("audio") || nm.Contains("sound") || nm.Contains("speaker") || nm.Contains("microphone") || nm.Contains("realtek high def"))
                            key = "audio";
                        else if (cls == "diskdrive" || cls == "cdrom" || nm.Contains("nvme") || nm.Contains("ssd") || nm.Contains("sata") || nm.Contains("disk drive") || nm.Contains("storage"))
                            key = "storage";
                        else if (cls == "hidclass" || cls == "mouse" || cls == "keyboard" || nm.Contains("mouse") || nm.Contains("keyboard") || nm.Contains("touchpad") || (nm.Contains("hid") && !nm.Contains("bluetooth")))
                            key = "input";
                        else if (cls == "usb" || nm.Contains("usb") || nm.Contains("universal serial bus"))
                            key = "usb";
                        else if (cls == "bluetooth" || nm.Contains("bluetooth"))
                            key = "bluetooth";
                        groups[key].Items.Add(item);
                    }
                });

                int totalErr = 0, totalDev = 0;
                SafeInvoke(() => {
                    lvDrivers.Items.Clear();
                    foreach (var kv in groups) {
                        var (ico, items) = kv.Value;
                        if (items.Count == 0) continue;
                        int grpErr = items.FindAll(x => x.HasErr).Count;
                        // Group header
                        var hdr = new ListViewItem($"  {ico}   [{items.Count} thiet bi{(grpErr > 0 ? $"  —  ⚠ {grpErr} loi" : "")}]");
                        hdr.BackColor = Color.FromArgb(22, 34, 60);
                        hdr.ForeColor = grpErr > 0 ? C_YELLOW : C_ACCENT;
                        hdr.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                        for (int i = 0; i < 4; i++) hdr.SubItems.Add("");
                        lvDrivers.Items.Add(hdr);
                        // Device rows
                        items.Sort((a, b2) => b2.HasErr.CompareTo(a.HasErr));
                        foreach (var r in items) {
                            var row = new ListViewItem($"    {r.Name}");
                            row.SubItems.Add(r.Status); row.SubItems.Add(r.Mfr); row.SubItems.Add(r.Ver); row.SubItems.Add(r.Date);
                            row.ForeColor = r.HasErr ? C_RED : (r.Status == "OK" ? Color.FromArgb(170, 200, 230) : C_TEXT);
                            row.BackColor = r.HasErr ? Color.FromArgb(38, 12, 12) : Color.FromArgb(16, 23, 40);
                            lvDrivers.Items.Add(row);
                            totalDev++; if (r.HasErr) totalErr++;
                        }
                    }
                    lblDriverStatus.Text = totalErr > 0 ? $"⚠ {totalErr} loi / {totalDev} thiet bi — Nhan 'Cai Driver Loi' de sua" : $"✔ Tat ca {totalDev} thiet bi hoat dong binh thuong";
                    lblDriverStatus.ForeColor = totalErr > 0 ? C_RED : C_GREEN;
                });
                Log($"Quet xong: {totalDev} thiet bi, {totalErr} loi.", totalErr > 0 ? C_YELLOW : C_GREEN);
            });
        }

        async void BtnFixDrivers_Click(object s, EventArgs e) { await RunGuarded("Cai Driver Loi", async () => { Log("pnputil /scan-devices...", C_YELLOW); await RunProcAsync("pnputil.exe", "/scan-devices"); Log("Windows Update tim driver...", C_YELLOW); await RunProcAsync("powershell.exe", "-NonInteractive -NoProfile -Command \"$s=New-Object -ComObject Microsoft.Update.Session;$q=$s.CreateUpdateSearcher().Search(\\\"Type='Driver' AND IsInstalled=0\\\");if($q.Updates.Count -gt 0){$c=New-Object -ComObject Microsoft.Update.UpdateColl;foreach($u in $q.Updates){$c.Add($u)|Out-Null};$i=$s.CreateUpdateInstaller();$i.Updates=$c;$i.Install()|Out-Null}\""); Log("Cap nhat driver xong. Dang quet lai...", C_GREEN); await Task.Delay(800); BtnScanDrivers_Click(null, null); }); }
        async void BtnWinUpdateDriver_Click(object s, EventArgs e) { await RunGuarded("WinUpdate", async () => { await Task.Run(() => Process.Start("ms-settings:windowsupdate-optionalupdates")); Log("Da mo Windows Update Optional Updates.", C_GREEN); }); }
        async void BtnExportDrivers_Click(object s, EventArgs e) { await RunGuarded("Xuat Bao Cao Driver", async () => { string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "DriverReport.txt"); var sb = new StringBuilder($"=== DRIVER REPORT — {DateTime.Now} ===\r\n\r\n"); foreach (ListViewItem item in lvDrivers.Items) sb.AppendLine($"[{item.SubItems[1].Text}] {item.Text.Trim()} | {item.SubItems[2].Text} | v{item.SubItems[3].Text} | {item.SubItems[4].Text}"); File.WriteAllText(path, sb.ToString(), Encoding.UTF8); Log($"Bao cao: {path}", C_GREEN); Process.Start(new ProcessStartInfo("notepad.exe", path) { UseShellExecute = true }); }); }

        static string ErrDesc(int c) { switch (c) { case 1: return "Loi cau hinh"; case 3: return "Driver hong"; case 10: return "Khong khoi dong"; case 12: return "Xung dot TN"; case 14: return "Can restart"; case 18: return "Can cai lai"; case 22: return "Bi tat"; case 28: return "Khong co driver"; case 43: return "Thiet bi loi"; default: return $"Loi #{c}"; } }
        class DrvItem { public string Name = "", Mfr = "", Ver = "", Date = "", Status = "", PnpClass = ""; public bool HasErr; }

        // ─── SOFTWARE — DIRECT DOWNLOAD ───────────────────────────────────────
        Panel BuildSoftwarePage()
        {
            var pg = PageScroll("📦  Phan Mem — Cai Dat Truc Tiep (Khong can Winget)");
            var flow = GetPageFlow(pg);
            void SW2(string title, string desc, string btn, Color clr, string url, string tmp, string args) {
                var g = AddGrp(flow, title, desc); AddBtn(AddRow(g), btn, clr, async (s, e) => await InstallDirect(title, url, tmp, args));
            }
            SW2("Google Chrome",    "Tai tu dl.google.com, cai ngam. Khong can Winget.",              "⬇  Cai Chrome",      C_GREEN,                        "https://dl.google.com/chrome/install/latest/chrome_installer.exe",                                                                                                   Path.Combine(Path.GetTempPath(),"ChromeSetup.exe"),     "/silent /install");
            SW2("Mozilla Firefox",  "Tai tu Mozilla chinh thuc, cai ngam.",                           "⬇  Cai Firefox",     Color.FromArgb(220,80,0),       "https://download.mozilla.org/?product=firefox-latest&os=win64&lang=vi",                                                                                              Path.Combine(Path.GetTempPath(),"FirefoxSetup.exe"),    "-ms");
            SW2("7-Zip",            "Giai nen 100+ dinh dang. Tai tu 7-zip.org.",                     "⬇  Cai 7-Zip",       Color.FromArgb(0,150,110),      "https://7-zip.org/a/7z2301-x64.exe",                                                                                                                                 Path.Combine(Path.GetTempPath(),"7zSetup.exe"),         "/S");
            SW2("VLC Media Player", "Phat video/audio. Tai tu videolan.org.",                          "⬇  Cai VLC",         Color.FromArgb(220,80,0),       "https://download.videolan.org/pub/videolan/vlc/last/win64/vlc-3.0.20-win64.exe",                                                                                    Path.Combine(Path.GetTempPath(),"VLCSetup.exe"),        "/S");
            SW2("Notepad++ 8.6",    "Trinh soan thao code. Tai tu GitHub.",                           "⬇  Cai Notepad++",   Color.FromArgb(0,130,200),      "https://github.com/notepad-plus-plus/notepad-plus-plus/releases/download/v8.6/npp.8.6.Installer.x64.exe",                                                         Path.Combine(Path.GetTempPath(),"NppSetup.exe"),        "/S");
            SW2("Zoom Meetings",    "Hop truc tuyen. Tai tu zoom.us.",                                "⬇  Cai Zoom",        Color.FromArgb(45,140,255),     "https://zoom.us/client/latest/ZoomInstallerFull.exe",                                                                                                                 Path.Combine(Path.GetTempPath(),"ZoomSetup.exe"),       "/quiet /norestart");
            SW2("WinRAR 7.01",      "Giai nen RAR/ZIP. Tai tu win-rar.com.",                          "⬇  Cai WinRAR",      Color.FromArgb(140,80,20),      "https://www.win-rar.com/fileadmin/winrar-versions/winrar/winrar-x64-701.exe",                                                                                       Path.Combine(Path.GetTempPath(),"WinRARSetup.exe"),     "/S");
            var ge  = AddGrp(flow, "EVKey — Go Tieng Viet",  "Tai phien ban moi nhat tu GitHub, giai nen + shortcut Desktop."); AddBtn(AddRow(ge),  "⬇  Cai EVKey",      C_ACCENT,               BtnEVKey_Click);
            var guk = AddGrp(flow, "UniKey — Go Tieng Viet", "Tai tu unikey.org, giai nen + shortcut Desktop.");                  AddBtn(AddRow(guk), "⬇  Cai UniKey",     Color.FromArgb(0,100,180), BtnUniKey_Click);
            var go  = AddGrp(flow, "Office 365 ProPlus",     "Tai ODT tu Microsoft, config Word+Excel+PPT VI+EN, cai ngam ~4GB.");AddBtn(AddRow(go),  "⬇  Deploy Office 365", C_ACCENT2,           BtnOffice_Click);
            return pg;
        }

        async Task InstallDirect(string name, string url, string localPath, string args) {
            await RunGuarded($"Cai {name}", async () => {
                Log($"Dang tai {name}...", C_YELLOW); Log($"  URL: {url}", C_SUBTEXT);
                await DownloadAsync(url, localPath);
                Log($"Dang chay installer {name}...", C_YELLOW);
                int code = await Task.Run(() => { using (var p = new Process()) { p.StartInfo = new ProcessStartInfo { FileName = localPath, Arguments = args, UseShellExecute = true, Verb = "runas", WindowStyle = ProcessWindowStyle.Minimized }; p.Start(); p.WaitForExit(); return p.ExitCode; } });
                try { File.Delete(localPath); } catch { }
                if (code == 0 || code == 3010) Log($"✔ {name} cai xong!" + (code == 3010 ? " (Restart de hoan tat)" : ""), C_GREEN);
                else if (code == 1638 || code == 1602) Log($"⚠ {name} co the da cai roi (code {code}).", C_YELLOW);
                else Log($"⚠ Installer tra ve {code} — co the da cai xong.", C_YELLOW);
            });
        }

        async void BtnEVKey_Click(object s, EventArgs e) { await RunGuarded("Cai EVKey", async () => { string json = await GetStrAsync("https://api.github.com/repos/lamquangminh/EVKey/releases/latest"); string zip = ParseGhAsset(json, ".zip"); if (string.IsNullOrEmpty(zip)) throw new Exception("Khong tim thay .zip EVKey."); string tmp = Path.Combine(Path.GetTempPath(), "EVKey.zip"); await DownloadAsync(zip, tmp); const string dest = @"C:\Program Files\EVKey"; if (Directory.Exists(dest)) Directory.Delete(dest, true); Directory.CreateDirectory(dest); await Task.Run(() => ZipFile.ExtractToDirectory(tmp, dest)); File.Delete(tmp); string[] exes = Directory.GetFiles(dest, "EVKey*.exe", SearchOption.AllDirectories); string exePath = exes.Length > 0 ? exes[0] : Path.Combine(dest, "EVKey.exe"); await Task.Run(() => CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "EVKey.lnk"), exePath, dest, "EVKey")); Log("✔ EVKey xong + shortcut Desktop.", C_GREEN); }); }
        async void BtnUniKey_Click(object s, EventArgs e) { await RunGuarded("Cai UniKey", async () => { string tmp = Path.Combine(Path.GetTempPath(), "UniKey.zip"); await DownloadAsync("https://www.unikey.org/assets/unikey/unikey4.3RC5-140925-win64.zip", tmp); const string dest = @"C:\Program Files\UniKey"; if (Directory.Exists(dest)) Directory.Delete(dest, true); Directory.CreateDirectory(dest); await Task.Run(() => ZipFile.ExtractToDirectory(tmp, dest)); File.Delete(tmp); string[] exes = Directory.GetFiles(dest, "*.exe", SearchOption.AllDirectories); if (exes.Length == 0) throw new Exception("Khong tim thay EXE UniKey."); await Task.Run(() => CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "UniKey.lnk"), exes[0], dest, "UniKey")); Log("✔ UniKey xong + shortcut Desktop.", C_GREEN); }); }
        async void BtnOffice_Click(object s, EventArgs e) { await RunGuarded("Deploy Office 365", async () => { const string dir = @"C:\IT_Tools\ODT"; Directory.CreateDirectory(dir); string odtExe = Path.Combine(dir, "ODTSetup.exe"); await DownloadAsync("https://download.microsoft.com/download/2/7/A/27AF1BE6-DD20-4CB4-B154-EBAB8A7D4A7E/officedeploymenttool_18129-20030.exe", odtExe); await RunProcAsync(odtExe, $"/quiet /extract:\"{dir}\""); string cfg = Path.Combine(dir, "configuration.xml"); File.WriteAllText(cfg, "<Configuration><Add OfficeClientEdition=\"64\" Channel=\"Current\"><Product ID=\"O365ProPlusRetail\"><Language ID=\"vi-vn\"/><Language ID=\"en-us\"/><ExcludeApp ID=\"Access\"/><ExcludeApp ID=\"Groove\"/><ExcludeApp ID=\"Lync\"/><ExcludeApp ID=\"OneDrive\"/><ExcludeApp ID=\"OneNote\"/><ExcludeApp ID=\"Outlook\"/><ExcludeApp ID=\"Publisher\"/><ExcludeApp ID=\"Teams\"/></Product></Add><Display Level=\"None\" AcceptEULA=\"TRUE\"/><Property Name=\"AUTOACTIVATE\" Value=\"1\"/></Configuration>", Encoding.UTF8); string setup = Path.Combine(dir, "setup.exe"); if (!File.Exists(setup)) throw new FileNotFoundException("setup.exe khong tim thay.", setup); Log("Cai Office ~4GB — co the mat 20-60 phut...", C_YELLOW); int code = await RunProcAsync(setup, $"/configure \"{cfg}\""); Log(code == 0 ? "✔ Office 365 cai xong!" : $"Code {code}.", code == 0 ? C_GREEN : C_YELLOW); }); }

        // ─── OPTIMIZE ─────────────────────────────────────────────────────────
        Panel BuildOptimizePage()
        {
            var pg = PageScroll("🔧  Toi Uu He Thong"); var flow = GetPageFlow(pg);
            var g1 = AddGrp(flow, "BitLocker — Tat Ma Hoa O C:", "Tat BitLocker, qua trinh giai ma chay nen."); AddBtn(AddRow(g1), "🔓  Tat BitLocker C:", C_RED, BtnBitLocker_Click);
            var g2 = AddGrp(flow, "Print Spooler — Fix Loi May In", "Dung > Xoa job ket > Khoi dong lai."); AddBtn(AddRow(g2), "🖨  Reset Print Spooler", C_YELLOW, BtnSpooler_Click);
            var g3 = AddGrp(flow, "Don File Rac — Temp Cleaner", "Xoa %TEMP%, Windows Temp, Prefetch."); AddBtn(AddRow(g3), "🗑  Xoa Temp Files", C_RED, BtnCleanTemp_Click);
            var g4 = AddGrp(flow, "SFC — System File Checker", "Quet va sua file he thong bi loi. Mat 10-15 phut."); AddBtn(AddRow(g4), "🔍  Chay SFC /scannow", Color.FromArgb(0, 160, 100), async (s, e) => await RunGuarded("SFC", async () => { Log("sfc /scannow...", C_YELLOW); int c = await RunProcAsync("sfc.exe", "/scannow"); Log(c == 0 ? "✔ SFC hoan thanh." : "SFC " + c, c == 0 ? C_GREEN : C_YELLOW); }));
            var g5 = AddGrp(flow, "DISM — Repair Windows", "Sua Windows image bi hong. Chay sau SFC."); AddBtn(AddRow(g5), "🛠  DISM /RestoreHealth", Color.FromArgb(100, 60, 200), async (s, e) => await RunGuarded("DISM", async () => { Log("DISM RestoreHealth...", C_YELLOW); int c = await RunProcAsync("dism.exe", "/Online /Cleanup-Image /RestoreHealth"); Log(c == 0 ? "✔ DISM xong." : "DISM " + c, c == 0 ? C_GREEN : C_YELLOW); }));
            var g6 = AddGrp(flow, "Disk Cleanup", "Mo Disk Cleanup o C:."); AddBtn(AddRow(g6), "💿  Mo Disk Cleanup", Color.FromArgb(0, 140, 190), async (s, e) => await RunGuarded("Disk Cleanup", async () => { await Task.Run(() => Process.Start(new ProcessStartInfo("cleanmgr.exe", "/d C:") { UseShellExecute = true })); Log("✔ Disk Cleanup da mo.", C_GREEN); }));
            var g7 = AddGrp(flow, "Windows Update", "Kiem tra ban va moi nhat."); AddBtn(AddRow(g7), "🔄  Mo Windows Update", C_ACCENT, async (s, e) => await RunGuarded("WinUpdate", async () => { await Task.Run(() => Process.Start("ms-settings:windowsupdate")); Log("✔ Windows Update da mo.", C_GREEN); }));
            return pg;
        }

        async void BtnBitLocker_Click(object s, EventArgs e) { if (MessageBox.Show("Tat BitLocker C:?", "Xac Nhan", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return; await RunGuarded("Tat BitLocker", async () => { int c = await RunProcAsync("powershell.exe", "-NonInteractive -NoProfile -Command \"Disable-BitLocker -MountPoint 'C:'\""); Log(c == 0 ? "✔ Giai ma BitLocker bat dau." : "Loi: " + c, c == 0 ? C_GREEN : C_YELLOW); }); }
        async void BtnSpooler_Click(object s, EventArgs e) { await RunGuarded("Reset Print Spooler", async () => { await RunProcAsync("net", "stop spooler"); await Task.Run(() => { const string d = @"C:\Windows\System32\spool\PRINTERS"; if (Directory.Exists(d)) foreach (string f in Directory.GetFiles(d)) try { File.Delete(f); } catch { } }); int c = await RunProcAsync("net", "start spooler"); Log(c == 0 ? "✔ Print Spooler reset xong!" : "Loi: " + c, c == 0 ? C_GREEN : C_RED); }); }
        async void BtnCleanTemp_Click(object s, EventArgs e) { await RunGuarded("Xoa Temp", async () => { long total = 0; var dirs = new[] { Environment.GetEnvironmentVariable("TEMP"), @"C:\Windows\Temp", @"C:\Windows\Prefetch" }; await Task.Run(() => { foreach (string d in dirs) { if (!Directory.Exists(d)) continue; foreach (string f in Directory.GetFiles(d, "*", SearchOption.TopDirectoryOnly)) try { var fi = new FileInfo(f); total += fi.Length; File.Delete(f); } catch { } foreach (string sub in Directory.GetDirectories(d)) try { Directory.Delete(sub, true); } catch { } } }); Log($"✔ Da don {FormatBytes(total)}.", C_GREEN); }); }

        // ─── NETWORK — PING, DNS, SET IP TINH ────────────────────────────────
        Panel BuildNetworkPage()
        {
            var pg = PageScroll("🌐  Mang & Ket Noi");
            var flow = GetPageFlow(pg);

            // Speed Test
            var gs = AddGrp(flow, "⚡  Speed Test — Do Toc Do Mang", "Do toc do thuc te tu nhieu server, hien thi toan bo trong Console Log ben duoi.");
            AddBtn(AddRow(gs), "⚡  Chay Speed Test", C_GREEN, BtnSpeedTest_Click);

            // Ping
            var g1 = AddGrp(flow, "Ping Test — Kiem Tra Internet", "Ping Google DNS, Cloudflare, Google.com, Facebook.");
            AddBtn(AddRow(g1), "📡  Ping Test", C_ACCENT, async (s, e) => await RunGuarded("Ping Test", async () => {
                var hosts = new[] { ("Google DNS", "8.8.8.8"), ("Cloudflare", "1.1.1.1"), ("Google.com", "google.com"), ("Facebook", "facebook.com") };
                foreach (var (name, host) in hosts) { using (var p = new Ping()) { try { var r = await Task.Run(() => p.Send(host, 2000)); Log(r.Status == IPStatus.Success ? $"  ✔ {name,-15} ({host,-15}) — {r.RoundtripTime}ms" : $"  ✖ {name,-15} ({host,-15}) — {r.Status}", r.Status == IPStatus.Success ? C_GREEN : C_RED); } catch (Exception ex) { Log($"  ✖ {name} — {ex.Message}", C_RED); } } }
            }));

            // Flush DNS
            var g2 = AddGrp(flow, "Flush DNS Cache", "Xoa cache DNS — giai quyet loi khong vao duoc web.");
            AddBtn(AddRow(g2), "🌐  Flush DNS", C_ACCENT, async (s, e) => await QuickFlushDns());

            // Reset TCP/IP
            var g3 = AddGrp(flow, "Reset TCP/IP & Winsock", "Dat lai mang ve mac dinh. Can restart sau.");
            AddBtn(AddRow(g3), "♻  Reset TCP/IP + Winsock", C_RED, async (s, e) => { if (MessageBox.Show("Reset TCP/IP va Winsock?\nMay can RESTART sau.", "Xac Nhan", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return; await RunGuarded("Reset Network", async () => { await RunProcAsync("netsh", "int ip reset"); await RunProcAsync("netsh", "winsock reset"); Log("✔ Reset xong. Vui long RESTART may.", C_GREEN); }); });

            // IP Config
            var g4 = AddGrp(flow, "IP Configuration", "Hien thi IP, Gateway, DNS, MAC address day du.");
            AddBtn(AddRow(g4), "📋  Xem IP Config /all", C_ACCENT, async (s, e) => await RunGuarded("IP Config", async () => { Log("-- IP Configuration --", C_ACCENT); Log(await CaptureAsync("ipconfig.exe", "/all"), C_TEXT); }));

            // Release/Renew
            var g5 = AddGrp(flow, "Release & Renew IP", "Tra IP hien tai va xin IP moi tu DHCP server.");
            AddBtn(AddRow(g5), "🔄  Release & Renew IP", C_YELLOW, async (s, e) => await RunGuarded("Renew IP", async () => { await RunProcAsync("ipconfig.exe", "/release"); await RunProcAsync("ipconfig.exe", "/renew"); Log("✔ IP moi da duoc cap.", C_GREEN); }));

            // ── SET IP TINH ──────────────────────────────────────────────────
            var gip = AddGrp(flow, "⚙  Thiet Lap IP Tinh / DHCP", "Chon adapter, nhap thong so mang, nhan Apply. Nhan DHCP de chuyen lai tu dong.");
            gip.Height = 180;

            // Adapter list
            var cboAdapter = new ComboBox { Location = new Point(90, 20), Width = 280, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(28, 38, 62), ForeColor = C_TEXT, Font = new Font("Segoe UI", 9f), FlatStyle = FlatStyle.Flat };
            gip.Controls.Add(new Label { Text = "Adapter:", ForeColor = C_SUBTEXT, Font = new Font("Segoe UI", 8.5f), AutoSize = true, Location = new Point(10, 24) });
            gip.Controls.Add(cboAdapter);
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces()) if (ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback) cboAdapter.Items.Add(ni.Name);
            if (cboAdapter.Items.Count > 0) cboAdapter.SelectedIndex = 0;

            // Input fields
            TextBox mkTxt(string val, int x, int y) { var t = new TextBox { Text = val, Location = new Point(x, y), Width = 130, BackColor = Color.FromArgb(22, 32, 54), ForeColor = C_TEXT, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9f) }; gip.Controls.Add(t); return t; }
            Label mkLbl(string txt, int x, int y) { var l = new Label { Text = txt, ForeColor = C_SUBTEXT, Font = new Font("Segoe UI", 8.5f), AutoSize = true, Location = new Point(x, y) }; gip.Controls.Add(l); return l; }

            mkLbl("IP Address:",   10,  52); var txtIP  = mkTxt("192.168.1.100", 90,  48);
            mkLbl("Subnet Mask:", 240,  52); var txtSN  = mkTxt("255.255.255.0", 320, 48);
            mkLbl("Gateway:",      10,  82); var txtGW  = mkTxt("192.168.1.1",   90,  78);
            mkLbl("DNS Primary:", 240,  82); var txtDNS = mkTxt("8.8.8.8",       320, 78);
            mkLbl("DNS Alt:",     460,  82); var txtDN2 = mkTxt("8.8.4.4",       520, 78); txtDN2.Width = 100;

            var btnApply = new Button { Text = "✔  Apply IP Tinh", BackColor = C_GREEN, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold), Size = new Size(165, 30), Location = new Point(10, 118), Cursor = Cursors.Hand };
            var btnDHCP  = new Button { Text = "↺  Chuyen ve DHCP", BackColor = Color.FromArgb(55, 75, 115), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold), Size = new Size(165, 30), Location = new Point(185, 118), Cursor = Cursors.Hand };
            btnApply.FlatAppearance.BorderSize = 0; btnDHCP.FlatAppearance.BorderSize = 0;
            gip.Controls.Add(btnApply); gip.Controls.Add(btnDHCP);

            btnApply.Click += async (s, e) => {
                string adp = cboAdapter.SelectedItem?.ToString() ?? "";
                if (string.IsNullOrEmpty(adp)) { MessageBox.Show("Chon adapter mang truoc!", "Loi", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                await RunGuarded("Set IP Tinh", async () => {
                    Log($"Adapter: [{adp}]", C_YELLOW);
                    Log($"  IP: {txtIP.Text}  Mask: {txtSN.Text}  GW: {txtGW.Text}", C_YELLOW);
                    Log($"  DNS: {txtDNS.Text} / {txtDN2.Text}", C_YELLOW);
                    int c1 = await RunProcAsync("netsh", $"interface ip set address \"{adp}\" static {txtIP.Text} {txtSN.Text} {txtGW.Text}");
                    if (c1 != 0) throw new Exception($"Khong dat duoc IP (code {c1}). Kiem tra quyen admin va ten adapter.");
                    await RunProcAsync("netsh", $"interface ip set dns \"{adp}\" static {txtDNS.Text}");
                    if (!string.IsNullOrWhiteSpace(txtDN2.Text)) await RunProcAsync("netsh", $"interface ip add dns \"{adp}\" {txtDN2.Text} index=2");
                    Log($"✔ Da dat IP tinh cho [{adp}]!", C_GREEN);
                    Log($"  IP: {txtIP.Text}  |  GW: {txtGW.Text}  |  DNS: {txtDNS.Text}", C_GREEN);
                });
            };
            btnDHCP.Click += async (s, e) => {
                string adp = cboAdapter.SelectedItem?.ToString() ?? "";
                if (string.IsNullOrEmpty(adp)) { MessageBox.Show("Chon adapter!", "Loi", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                await RunGuarded("Chuyen ve DHCP", async () => {
                    await RunProcAsync("netsh", $"interface ip set address \"{adp}\" dhcp");
                    await RunProcAsync("netsh", $"interface ip set dns \"{adp}\" dhcp");
                    Log($"✔ [{adp}] da chuyen ve DHCP.", C_GREEN);
                });
            };

            flow.Controls.Add(gip);
            return pg;
        }

        async Task QuickFlushDns() { await RunGuarded("Flush DNS", async () => { await RunProcAsync("ipconfig.exe", "/flushdns"); Log("✔ DNS cache da xoa.", C_GREEN); }); }

        // SPEED TEST IN CONSOLE
        async void BtnSpeedTest_Click(object s, EventArgs e)
        {
            await RunGuarded("Speed Test", async () => {
                Log("", C_TEXT);
                Log("╔══════════════════════════════════════════════════════╗", C_ACCENT);
                Log("║           SPEED TEST — DO TOC DO MANG               ║", C_ACCENT);
                Log("╚══════════════════════════════════════════════════════╝", C_ACCENT);
                Log("", C_TEXT);

                var servers = new[] {
                    ("Cloudflare Speed",   "https://speed.cloudflare.com/__down?bytes=10000000",      10_000_000L),
                    ("Hetzner (DE)",       "https://speed.hetzner.de/10MB.bin",                      10_000_000L),
                    ("Linode (Singapore)", "https://sgp1.speed.hetzner.com/100MB.bin",               10_000_000L),
                };

                double bestMbps = 0;
                foreach (var (srvName, url, _) in servers) {
                    Log($"  ▶ Dang test: {srvName}...", C_YELLOW);
                    try {
                        var sw = Stopwatch.StartNew();
                        byte[] data;
                        using (var wc = new WebClient()) { wc.Headers["User-Agent"] = "ITSupportTools/3.0"; data = await wc.DownloadDataTaskAsync(url); }
                        sw.Stop();
                        double secs = sw.Elapsed.TotalSeconds;
                        if (data.Length < 1000 || secs < 0.01) { Log($"  ⚠ {srvName}: Du lieu khong hop le, bo qua.", C_YELLOW); continue; }
                        double mbps    = data.Length / secs / 1024 / 1024;
                        double mbpsBit = mbps * 8;
                        bestMbps = Math.Max(bestMbps, mbpsBit);
                        int barFill = (int)Math.Min(40, mbpsBit / 2.5);
                        string bar = "[" + new string('█', barFill) + new string('░', 40 - barFill) + "]";
                        Color barClr = mbpsBit >= 50 ? C_GREEN : mbpsBit >= 10 ? C_YELLOW : C_RED;
                        Log($"  {srvName}:", C_TEXT);
                        Log($"    {bar}  {mbpsBit:F1} Mbps  ({mbps:F2} MB/s)", barClr);
                        Log($"    Da tai: {FormatBytes(data.Length)}  Thoi gian: {secs:F2}s", C_SUBTEXT);
                    } catch (Exception ex) { Log($"  ✖ {srvName}: {ex.Message}", C_RED); }
                    Log("", C_TEXT);
                }

                // Latency
                Log("  ▶ Kiem tra Latency...", C_YELLOW);
                var pingHosts = new[] { ("8.8.8.8", "Google DNS"), ("1.1.1.1", "Cloudflare"), ("203.113.131.1", "Viettel DNS") };
                foreach (var (host, name) in pingHosts) {
                    try {
                        long total = 0; int ok = 0;
                        using (var p = new Ping()) for (int i = 0; i < 4; i++) { var r = await Task.Run(() => p.Send(host, 1500)); if (r.Status == IPStatus.Success) { total += r.RoundtripTime; ok++; } }
                        long avg = ok > 0 ? total / ok : 9999;
                        Color lc = avg < 20 ? C_GREEN : avg < 80 ? C_YELLOW : C_RED;
                        Log($"    🌐 {name,-18} ({host,-16}) — avg {avg}ms  {(ok < 4 ? $"({ok}/4 OK)" : "")}", lc);
                    } catch (Exception ex) { Log($"    ✖ {name}: {ex.Message}", C_RED); }
                }
                Log("", C_TEXT);
                Log($"  ═══ Toc do tot nhat: ~{bestMbps:F1} Mbps ═══", bestMbps >= 50 ? C_GREEN : bestMbps >= 10 ? C_YELLOW : C_RED);
                Log("╚══════════════════════════════════════════════════════╝", C_ACCENT);
            });
        }

        // ─── SYSTEM INFO ──────────────────────────────────────────────────────
        Panel BuildSysInfoPage()
        {
            var pg = PageScroll("ℹ  Thong Tin He Thong"); var flow = GetPageFlow(pg);
            var g1 = AddGrp(flow, "Kich Hoat Windows",              "Kiem tra trang thai ban quyen hien tai.");  AddBtn(AddRow(g1), "🔑  Kiem Tra Ban Quyen", Color.FromArgb(80,60,200), BtnActivation_Click);
            var g2 = AddGrp(flow, "CPU / RAM / GPU / Mainboard",     "Hien thi thong so phan cung chi tiet qua WMI."); AddBtn(AddRow(g2), "📊  Xem Thong So Phan Cung", C_ACCENT, BtnHWInfo_Click);
            var g3 = AddGrp(flow, "Dung Luong O Dia",                "Hien thi % dung luong tung o voi thanh bar."); AddBtn(AddRow(g3), "💾  Xem Dung Luong O Dia", C_GREEN, BtnDisk_Click);
            var g4 = AddGrp(flow, "Task Manager",                     "Mo Task Manager quan ly tien trinh va startup."); AddBtn(AddRow(g4), "🚀  Mo Task Manager", C_YELLOW, async (s, e) => await RunGuarded("TaskMgr", async () => { await Task.Run(() => Process.Start(new ProcessStartInfo("taskmgr.exe") { UseShellExecute = true })); Log("✔ Task Manager da mo.", C_GREEN); }));
            var g5 = AddGrp(flow, "Event Viewer",                     "Xem log loi BSOD, crash, hardware."); AddBtn(AddRow(g5), "📋  Mo Event Viewer", C_ACCENT2, async (s, e) => await RunGuarded("EventVwr", async () => { await Task.Run(() => Process.Start(new ProcessStartInfo("eventvwr.msc") { UseShellExecute = true })); Log("✔ Event Viewer da mo.", C_GREEN); }));
            var g6 = AddGrp(flow, "System Information (msinfo32)",    "Xem toan bo thong tin he thong day du."); AddBtn(AddRow(g6), "ℹ  Mo msinfo32", C_ACCENT, async (s, e) => await RunGuarded("msinfo32", async () => { await Task.Run(() => Process.Start(new ProcessStartInfo("msinfo32.exe") { UseShellExecute = true })); Log("✔ msinfo32 da mo.", C_GREEN); }));
            return pg;
        }

        async void BtnActivation_Click(object s, EventArgs e) { await RunGuarded("Ban Quyen Windows", async () => { Log("-- Trang Thai Ban Quyen --", C_ACCENT); Log(await CaptureAsync("cscript.exe", @"//Nologo C:\Windows\System32\slmgr.vbs /dli"), C_TEXT); }); }
        async void BtnHWInfo_Click(object s, EventArgs e) { await RunGuarded("Thong So Phan Cung", async () => { var sb = new StringBuilder(); await Task.Run(() => { void Q(string title, string q, string[] cols) { sb.AppendLine(title); using (var m = new ManagementObjectSearcher(q)) foreach (ManagementObject mo in m.Get()) sb.AppendLine("  " + string.Join("  |  ", Array.ConvertAll(cols, c => mo[c]?.ToString() ?? ""))); sb.AppendLine(); } Q("── CPU ──────────────────────────────────────────", "SELECT * FROM Win32_Processor", new[] { "Name", "NumberOfCores", "ThreadCount", "MaxClockSpeed" }); Q("── RAM ──────────────────────────────────────────", "SELECT * FROM Win32_PhysicalMemory", new[] { "DeviceLocator", "Capacity", "Speed" }); Q("── GPU ──────────────────────────────────────────", "SELECT * FROM Win32_VideoController", new[] { "Name", "AdapterRAM", "CurrentHorizontalResolution", "CurrentVerticalResolution" }); Q("── Mainboard ────────────────────────────────────", "SELECT * FROM Win32_BaseBoard", new[] { "Manufacturer", "Product", "SerialNumber" }); Q("── BIOS ─────────────────────────────────────────", "SELECT * FROM Win32_BIOS", new[] { "Manufacturer", "SMBIOSBIOSVersion", "SerialNumber" }); Q("── Disk ─────────────────────────────────────────", "SELECT * FROM Win32_DiskDrive", new[] { "Model", "Size", "MediaType" }); }); Log("-- Thong So Phan Cung --", C_ACCENT); Log(sb.ToString(), C_TEXT); }); }
        async void BtnDisk_Click(object s, EventArgs e) { await RunGuarded("Dung Luong O Dia", async () => { await Task.Run(() => { Log("── Dung Luong O Dia ─────────────────────────", C_ACCENT); foreach (var d in DriveInfo.GetDrives()) { if (!d.IsReady) continue; double pct = (double)(d.TotalSize - d.AvailableFreeSpace) / d.TotalSize * 100; Log($"  {d.Name}  {MkBar((float)pct, 20)}  {pct:F1}%  —  {FormatBytes(d.TotalSize - d.AvailableFreeSpace)} / {FormatBytes(d.TotalSize)}", pct > 90 ? C_RED : pct > 70 ? C_YELLOW : C_GREEN); } }); }); }

        // ─── SIDEBAR / CLOCK ──────────────────────────────────────────────────
        async void LoadSysInfoAsync() { await Task.Run(() => { try { string pc = Environment.MachineName, os = "", ram = "", ip = ""; using (var m = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem")) foreach (ManagementObject mo in m.Get()) os = mo["Caption"]?.ToString()?.Replace("Microsoft ", "") ?? ""; using (var m = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem")) foreach (ManagementObject mo in m.Get()) ram = FormatBytes(Convert.ToInt64(mo["TotalVisibleMemorySize"]) * 1024) + " RAM"; foreach (var ni in NetworkInterface.GetAllNetworkInterfaces()) { if (ni.OperationalStatus != OperationalStatus.Up || ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue; foreach (var ua in ni.GetIPProperties().UnicastAddresses) if (ua.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) { ip = ua.Address.ToString(); break; } if (!string.IsNullOrEmpty(ip)) break; } SafeInvoke(() => { lblSysName.Text = $"PC: {pc}"; lblSysOS.Text = os.Length > 22 ? os.Substring(0, 22) + "..." : os; lblSysIP.Text = $"IP: {(string.IsNullOrEmpty(ip) ? "No net" : ip)}"; lblSysRAM.Text = ram; }); } catch { } }); }
        void StartClock() { tmrClock = new System.Windows.Forms.Timer { Interval = 1000 }; tmrClock.Tick += (s, e) => { lblClock.Text = DateTime.Now.ToString("HH:mm:ss  dd/MM/yyyy"); SafeInvoke(() => lblClock.Location = new Point(ClientSize.Width - lblClock.Width - 16, 20)); }; tmrClock.Start(); }

        // ─── UI HELPERS ───────────────────────────────────────────────────────
        Panel PageScroll(string title) { var pg = new Panel { AutoScroll = true }; var lbl = new Label { Text = title, Font = new Font("Segoe UI", 13f, FontStyle.Bold), ForeColor = C_TEXT, AutoSize = true, Location = new Point(16, 14) }; var line = new Panel { BackColor = C_ACCENT, Location = new Point(16, 44), Height = 2, Name = "ptl" }; pg.Controls.AddRange(new Control[] { lbl, line }); pg.Resize += (s, e) => { var l = pg.Controls["ptl"]; if (l != null) l.Width = pg.Width - 32; }; return pg; }
        FlowLayoutPanel GetPageFlow(Panel pg) { var f = new FlowLayoutPanel { Location = new Point(0, 58), AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Name = "mf" }; pg.Controls.Add(f); pg.Resize += (s, e) => { var fl = pg.Controls["mf"] as FlowLayoutPanel; if (fl != null) fl.Width = pg.Width; }; return f; }
        GroupBox AddGrp(FlowLayoutPanel parent, string title, string desc) { var g = new GroupBox { Text = title, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = C_ACCENT, BackColor = C_CARD, Margin = new Padding(16, 8, 16, 0), Width = Math.Max(200, parent.Width - 40), Height = 88 }; g.Paint += (s, e) => { using (var p = new Pen(C_BORDER)) e.Graphics.DrawRectangle(p, 0, 0, g.Width - 1, g.Height - 1); }; var lb = new Label { Text = desc, ForeColor = C_SUBTEXT, Font = new Font("Segoe UI", 8f), AutoSize = false, Location = new Point(10, 20), Width = g.Width - 20, Height = 18 }; g.Controls.Add(lb); parent.Controls.Add(g); parent.Resize += (s, e) => { g.Width = Math.Max(200, parent.Width - 40); lb.Width = g.Width - 20; }; return g; }
        FlowLayoutPanel AddRow(GroupBox g) { var f = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Location = new Point(10, 44), WrapContents = false }; g.Controls.Add(f); g.Height = 90; return f; }
        void AddBtn(FlowLayoutPanel row, string text, Color color, EventHandler handler) { var b = new Button { Text = text, BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold), Size = new Size(225, 34), Cursor = Cursors.Hand, Margin = new Padding(0, 0, 8, 0) }; b.FlatAppearance.BorderSize = 0; b.Click += handler; b.MouseEnter += (s, e) => b.BackColor = ControlPaint.Light(color, 0.15f); b.MouseLeave += (s, e) => b.BackColor = color; row.Controls.Add(b); }
        static Button MkBtn(string text, Color color) { var b = new Button { Text = text, BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold), Size = new Size(190, 34), Cursor = Cursors.Hand, Margin = new Padding(0, 0, 8, 0) }; b.FlatAppearance.BorderSize = 0; b.MouseEnter += (s, e) => b.BackColor = ControlPaint.Light(color, 0.15f); b.MouseLeave += (s, e) => b.BackColor = color; return b; }

        // ─── ASYNC INFRA ──────────────────────────────────────────────────────
        async Task RunGuarded(string name, Func<Task> action) { Log($"\n[{DateTime.Now:HH:mm:ss}] ▶ {name}", C_ACCENT); SetProg(0, name + "..."); try { await action(); SetProg(100, name + " xong."); Log($"[{DateTime.Now:HH:mm:ss}] ✔ {name} hoan thanh.\n", C_GREEN); } catch (Exception ex) { SetProg(0, ""); Log($"[{DateTime.Now:HH:mm:ss}] ✖ {name} LOI: {ex.Message}\n", C_RED); MessageBox.Show($"Loi trong '{name}':\r\n\r\n{ex.Message}", "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
        async Task DownloadAsync(string url, string dest) { using (var wc = new WebClient()) { wc.Headers["User-Agent"] = "ITSupportTools/3.0"; wc.DownloadProgressChanged += (s, e) => SafeInvoke(() => SetProg(e.ProgressPercentage, $"Dang tai... {e.ProgressPercentage}%  ({e.BytesReceived / 1024:N0} KB)")); await wc.DownloadFileTaskAsync(new Uri(url), dest); } SetProg(100, "Tai xong."); Log($"  Da luu: {dest}", C_GREEN); }
        async Task<string> GetStrAsync(string url) { using (var wc = new WebClient()) { wc.Headers["User-Agent"] = "ITSupportTools/3.0"; return await wc.DownloadStringTaskAsync(url); } }
        Task<int> RunProcAsync(string exe, string args) { return Task.Run(() => { using (var p = new Process()) { p.StartInfo = new ProcessStartInfo { FileName = exe, Arguments = args, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true }; p.OutputDataReceived += (s, e) => { if (e.Data != null) SafeInvoke(() => Log("  " + e.Data, Color.FromArgb(155, 175, 200))); }; p.ErrorDataReceived += (s, e) => { if (e.Data != null) SafeInvoke(() => Log("  " + e.Data, C_RED)); }; p.Start(); p.BeginOutputReadLine(); p.BeginErrorReadLine(); p.WaitForExit(); return p.ExitCode; } }); }
        Task<string> CaptureAsync(string exe, string args) { return Task.Run(() => { using (var p = new Process()) { p.StartInfo = new ProcessStartInfo { FileName = exe, Arguments = args, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true }; p.Start(); string o = p.StandardOutput.ReadToEnd(); string er = p.StandardError.ReadToEnd(); p.WaitForExit(); return string.IsNullOrWhiteSpace(o) ? er : o; } }); }
        void Log(string msg, Color color) { SafeInvoke(() => { rtbLog.SelectionStart = rtbLog.TextLength; rtbLog.SelectionLength = 0; rtbLog.SelectionColor = color; rtbLog.AppendText(msg + "\n"); rtbLog.SelectionColor = rtbLog.ForeColor; rtbLog.ScrollToCaret(); }); }
        void SetProg(int pct, string msg) { SafeInvoke(() => { pbMain.Value = Math.Max(0, Math.Min(100, pct)); lblTask.Text = msg; }); }
        void SafeInvoke(Action a) { if (InvokeRequired) BeginInvoke(a); else a(); }

        // ─── UTILITIES ────────────────────────────────────────────────────────
        static string ParseGhAsset(string json, string ext) { int i = 0; while (true) { i = json.IndexOf("browser_download_url", i); if (i < 0) break; int s = json.IndexOf('"', i + 22) + 1, e2 = json.IndexOf('"', s); if (s < 0 || e2 < 0) break; string url = json.Substring(s, e2 - s); if (url.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) return url; i = e2; } return null; }
        static void CreateShortcut(string lnk, string target, string workDir, string desc) { Type t = Type.GetTypeFromProgID("WScript.Shell"); object sh = Activator.CreateInstance(t); object sc = t.InvokeMember("CreateShortcut", System.Reflection.BindingFlags.InvokeMethod, null, sh, new object[] { lnk }); Type st = sc.GetType(); st.InvokeMember("TargetPath", System.Reflection.BindingFlags.SetProperty, null, sc, new object[] { target }); st.InvokeMember("WorkingDirectory", System.Reflection.BindingFlags.SetProperty, null, sc, new object[] { workDir }); st.InvokeMember("Description", System.Reflection.BindingFlags.SetProperty, null, sc, new object[] { desc }); st.InvokeMember("Save", System.Reflection.BindingFlags.InvokeMethod, null, sc, new object[0]); Marshal.FinalReleaseComObject(sc); Marshal.FinalReleaseComObject(sh); }
        static string FormatBytes(long b) { if (b <= 0) return "0 B"; string[] s = { "B", "KB", "MB", "GB", "TB" }; int i = 0; double d = b; while (d >= 1024 && i < s.Length - 1) { d /= 1024; i++; } return $"{d:F1} {s[i]}"; }
        void InitializeComponent() { SuspendLayout(); AutoScaleDimensions = new SizeF(6f, 13f); AutoScaleMode = AutoScaleMode.Font; ResumeLayout(false); }
    }

    // ─── CUSTOM CONTROLS ──────────────────────────────────────────────────────
    class NavBtn : Panel
    {
        public string PageId { get; }
        bool _active; Label lblI, lblT; Panel ind;
        static readonly Color CH = Color.FromArgb(30, 40, 65), CA = Color.FromArgb(25, 35, 58), CN = Color.FromArgb(15, 20, 35);
        public NavBtn(string icon, string text, string pageId) {
            PageId = pageId; Size = new Size(199, 44); BackColor = CN; Cursor = Cursors.Hand;
            ind  = new Panel { Size = new Size(3, 28), BackColor = Color.Transparent, Location = new Point(0, 8) };
            lblI = new Label { Text = icon, Font = new Font("Segoe UI Emoji", 13f), ForeColor = Color.FromArgb(100, 130, 180), AutoSize = true, Location = new Point(14, 11) };
            lblT = new Label { Text = text, Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(140, 160, 200), AutoSize = true, Location = new Point(44, 14) };
            Controls.AddRange(new Control[] { ind, lblI, lblT });
            MouseEnter += (s, e) => { if (!_active) BackColor = CH; };
            MouseLeave += (s, e) => { if (!_active) BackColor = CN; };
            foreach (Control c in Controls) { c.MouseEnter += (s, e) => { if (!_active) BackColor = CH; }; c.MouseLeave += (s, e) => { if (!_active) BackColor = CN; }; c.Click += (s, e2) => OnClick(e2); }
        }
        public void SetActive(bool a) { _active = a; BackColor = a ? CA : CN; ind.BackColor = a ? Color.FromArgb(56, 139, 253) : Color.Transparent; lblI.ForeColor = a ? Color.FromArgb(56, 139, 253) : Color.FromArgb(100, 130, 180); lblT.ForeColor = a ? Color.FromArgb(220, 230, 245) : Color.FromArgb(140, 160, 200); lblT.Font = new Font("Segoe UI", 9f, a ? FontStyle.Bold : FontStyle.Regular); }
    }

    class QuickCard : Panel
    {
        Color _ac; bool _hov; Label lblI, lblT;
        public QuickCard(string icon, string text, Color accent) {
            _ac = accent; Size = new Size(150, 100); Margin = new Padding(6); BackColor = Color.FromArgb(22, 30, 50); Cursor = Cursors.Hand;
            lblI = new Label { Text = icon, Font = new Font("Segoe UI Emoji", 20f), ForeColor = accent, AutoSize = true, Location = new Point(10, 10) };
            lblT = new Label { Text = text, Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = Color.FromArgb(180, 195, 220), AutoSize = false, Width = 130, Height = 36, Location = new Point(10, 58) };
            Controls.AddRange(new Control[] { lblI, lblT });
            MouseEnter += OnE; MouseLeave += OnL;
            foreach (Control c in Controls) { c.MouseEnter += OnE; c.MouseLeave += OnL; c.Click += (s, e) => OnClick(e); }
        }
        void OnE(object s, EventArgs e) { _hov = true; Invalidate(); }
        void OnL(object s, EventArgs e) { _hov = false; Invalidate(); }
        protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; using (var p = new Pen(_hov ? _ac : Color.FromArgb(40, 55, 85), _hov ? 2 : 1)) e.Graphics.DrawRectangle(p, 0, 0, Width - 1, Height - 1); if (_hov) using (var b = new SolidBrush(Color.FromArgb(18, _ac))) e.Graphics.FillRectangle(b, ClientRectangle); }
    }
}
