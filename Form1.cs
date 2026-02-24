// ══════════════════════════════════════════════════════════════════════════════
//  IT Support Toolkit v2.0  —  Form1.cs
//  .NET Framework 4.8 | WinForms | Requires Administrator
//  Modern dark-panel sidebar UI — zero NuGet dependencies
// ══════════════════════════════════════════════════════════════════════════════
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
        // ── Colour Palette ────────────────────────────────────────────────────
        static readonly Color C_DARK = Color.FromArgb(10, 14, 26);
        static readonly Color C_SIDEBAR = Color.FromArgb(15, 20, 35);
        static readonly Color C_PANEL = Color.FromArgb(20, 27, 46);
        static readonly Color C_CARD = Color.FromArgb(26, 35, 58);
        static readonly Color C_BORDER = Color.FromArgb(40, 55, 85);
        static readonly Color C_ACCENT = Color.FromArgb(56, 139, 253);
        static readonly Color C_ACCENT2 = Color.FromArgb(139, 92, 246);
        static readonly Color C_GREEN = Color.FromArgb(35, 197, 135);
        static readonly Color C_RED = Color.FromArgb(248, 81, 73);
        static readonly Color C_YELLOW = Color.FromArgb(240, 184, 43);
        static readonly Color C_TEXT = Color.FromArgb(220, 230, 245);
        static readonly Color C_SUBTEXT = Color.FromArgb(120, 140, 175);
        static readonly Color C_LOG_BG = Color.FromArgb(8, 11, 20);

        // ── Layout ───────────────────────────────────────────────────────────
        const int SIDEBAR_W = 210;
        const int HEADER_H = 60;
        const int FOOTER_H = 180;   // console log area

        // ── Widgets ──────────────────────────────────────────────────────────
        Panel pnlSidebar, pnlHeader, pnlContent, pnlFooter;
        RichTextBox rtbLog;
        ProgressBar pbMain;
        Label lblTask, lblClock, lblSysName, lblSysOS, lblSysIP, lblSysRAM;
        Panel pnlActive;      // currently visible content page
        System.Windows.Forms.Timer tmrClock;

        // ── Pages ────────────────────────────────────────────────────────────
        Dictionary<string, Panel> pages = new Dictionary<string, Panel>();
        List<NavBtn> navButtons = new List<NavBtn>();
        string currentPage = "";

        public Form1()
        {
            InitializeComponent();
            BuildUI();
            LoadSystemInfo();
            ShowPage("dashboard");
            StartClock();
            Log("IT Support Toolkit v2.0 — Khởi động thành công.", C_GREEN);
            Log("Đang chạy với quyền Administrator.", C_ACCENT);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  BUILD UI
        // ══════════════════════════════════════════════════════════════════════
        void BuildUI()
        {
            this.Text = "IT Support Toolkit v2.0";
            this.Size = new Size(1100, 780);
            this.MinimumSize = new Size(900, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = C_DARK;
            this.Font = new Font("Segoe UI", 9f);
            this.Icon = SystemIcons.Shield;
            this.DoubleBuffered = true;

            BuildHeader();
            BuildSidebar();
            BuildContent();
            BuildFooter();
            BuildPages();

            this.Resize += (s, e) => DoLayout();
            DoLayout();
        }

        void BuildHeader()
        {
            pnlHeader = new Panel { BackColor = C_SIDEBAR };
            pnlHeader.Paint += (s, e) => {
                // bottom border line
                using (var p = new Pen(C_ACCENT, 2))
                    e.Graphics.DrawLine(p, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
            };

            // Logo area
            var lblLogo = new Label
            {
                Text = "⚙",
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = C_ACCENT,
                AutoSize = true,
                Location = new Point(14, 12)
            };
            var lblTitle = new Label
            {
                Text = "IT Support Toolkit",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = C_TEXT,
                AutoSize = true,
                Location = new Point(50, 10)
            };
            var lblVer = new Label
            {
                Text = "v2.0 — Administrator",
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = C_SUBTEXT,
                AutoSize = true,
                Location = new Point(52, 34)
            };
            lblClock = new Label
            {
                Text = "",
                Font = new Font("Consolas", 11f, FontStyle.Bold),
                ForeColor = C_ACCENT,
                AutoSize = true
            };

            pnlHeader.Controls.AddRange(new Control[] { lblLogo, lblTitle, lblVer, lblClock });
            this.Controls.Add(pnlHeader);
        }

        void BuildSidebar()
        {
            pnlSidebar = new Panel { BackColor = C_SIDEBAR };
            pnlSidebar.Paint += (s, e) => {
                using (var p = new Pen(C_BORDER, 1))
                    e.Graphics.DrawLine(p, pnlSidebar.Width - 1, 0, pnlSidebar.Width - 1, pnlSidebar.Height);
            };

            // Nav buttons
            var navItems = new[] {
                ("🏠", "dashboard",   "Dashboard"),
                ("🖥", "drivers",     "Driver Suite"),
                ("📦", "software",    "Phần Mềm"),
                ("🔧", "optimize",    "Tối Ưu Hệ Thống"),
                ("🌐", "network",     "Mạng & Kết Nối"),
                ("ℹ",  "sysinfo",    "Thông Tin Hệ Thống"),
            };

            int y = 14;
            foreach (var (icon, id, label) in navItems)
            {
                var btn = new NavBtn(icon, label, id) { Location = new Point(8, y) };
                btn.Click += (s, e) => ShowPage(((NavBtn)s).PageId);
                navButtons.Add(btn);
                pnlSidebar.Controls.Add(btn);
                y += 50;
            }

            // System mini-info at bottom
            lblSysName = MakeInfoLabel("💻  ...", new Point(10, 0));
            lblSysOS = MakeInfoLabel("🪟  ...", new Point(10, 18));
            lblSysIP = MakeInfoLabel("🌐  ...", new Point(10, 36));
            lblSysRAM = MakeInfoLabel("🧠  ...", new Point(10, 54));
            var sysPanel = new Panel { BackColor = Color.FromArgb(12, 17, 30), Size = new Size(SIDEBAR_W - 2, 78) };
            sysPanel.Controls.AddRange(new Control[] { lblSysName, lblSysOS, lblSysIP, lblSysRAM });
            sysPanel.Name = "sysInfoMini";
            pnlSidebar.Controls.Add(sysPanel);

            this.Controls.Add(pnlSidebar);
        }

        Label MakeInfoLabel(string text, Point loc)
        {
            return new Label
            {
                Text = text,
                ForeColor = C_SUBTEXT,
                Font = new Font("Segoe UI", 7.5f),
                AutoSize = false,
                Width = SIDEBAR_W - 20,
                Height = 17,
                Location = loc
            };
        }

        void BuildContent()
        {
            pnlContent = new Panel { BackColor = C_PANEL };
            this.Controls.Add(pnlContent);
        }

        void BuildFooter()
        {
            pnlFooter = new Panel { BackColor = C_LOG_BG };
            pnlFooter.Paint += (s, e) => {
                using (var p = new Pen(C_BORDER, 1))
                    e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0);
            };

            var lblLog = new Label
            {
                Text = "  ▌ Console Log",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = C_ACCENT,
                AutoSize = true,
                Location = new Point(4, 6)
            };
            var btnClear = MakeSmallBtn("✕ Clear", Color.FromArgb(50, 60, 80));
            btnClear.Location = new Point(0, 2); btnClear.Name = "btnClear";
            btnClear.Click += (s, e) => rtbLog.Clear();

            pbMain = new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Style = ProgressBarStyle.Continuous,
                Height = 4
            };
            pbMain.Name = "pbMain";

            lblTask = new Label
            {
                Text = "",
                ForeColor = C_SUBTEXT,
                Font = new Font("Segoe UI", 7.5f),
                AutoSize = true
            };
            lblTask.Name = "lblTask";

            rtbLog = new RichTextBox
            {
                ReadOnly = true,
                BackColor = C_LOG_BG,
                ForeColor = C_TEXT,
                Font = new Font("Consolas", 8.5f),
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                DetectUrls = false
            };

            pnlFooter.Controls.AddRange(new Control[] { pbMain, lblTask, lblLog, btnClear, rtbLog });
            this.Controls.Add(pnlFooter);
        }

        void DoLayout()
        {
            int W = this.ClientSize.Width;
            int H = this.ClientSize.Height;

            pnlHeader.SetBounds(0, 0, W, HEADER_H);
            lblClock.Location = new Point(W - lblClock.Width - 16, 20);

            pnlSidebar.SetBounds(0, HEADER_H, SIDEBAR_W, H - HEADER_H - FOOTER_H);
            // position mini sys panel at bottom of sidebar
            var sysP = pnlSidebar.Controls["sysInfoMini"];
            if (sysP != null) sysP.Location = new Point(0, pnlSidebar.Height - sysP.Height - 4);

            // resize all nav buttons
            foreach (var nb in navButtons) nb.Width = SIDEBAR_W - 16;

            pnlContent.SetBounds(SIDEBAR_W, HEADER_H, W - SIDEBAR_W, H - HEADER_H - FOOTER_H);
            if (pnlActive != null) pnlActive.Size = pnlContent.ClientSize;

            pnlFooter.SetBounds(0, H - FOOTER_H, W, FOOTER_H);
            int fp = 4;
            var pb = pnlFooter.Controls["pbMain"] as ProgressBar;
            var lt = pnlFooter.Controls["lblTask"] as Label;
            var bc = pnlFooter.Controls["btnClear"] as Button;
            if (pb != null) pb.SetBounds(0, 0, pnlFooter.Width, 4);
            if (lt != null) lt.Location = new Point(pnlFooter.Width / 2 - 80, 6);
            if (bc != null) bc.Location = new Point(pnlFooter.Width - bc.Width - fp, 2);
            rtbLog.SetBounds(fp, 26, pnlFooter.Width - fp * 2, FOOTER_H - 30);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  PAGE SYSTEM
        // ══════════════════════════════════════════════════════════════════════
        void BuildPages()
        {
            pages["dashboard"] = BuildDashboard();
            pages["drivers"] = BuildDriversPage();
            pages["software"] = BuildSoftwarePage();
            pages["optimize"] = BuildOptimizePage();
            pages["network"] = BuildNetworkPage();
            pages["sysinfo"] = BuildSysInfoPage();

            foreach (var pg in pages.Values)
            {
                pg.Visible = false;
                pg.Dock = DockStyle.Fill;
                pg.BackColor = C_PANEL;
                pnlContent.Controls.Add(pg);
            }
        }

        void ShowPage(string id)
        {
            if (!pages.ContainsKey(id)) return;
            if (pnlActive != null) pnlActive.Visible = false;
            pnlActive = pages[id];
            pnlActive.Visible = true;
            currentPage = id;

            foreach (var nb in navButtons) nb.SetActive(nb.PageId == id);
            pnlContent.Invalidate();
        }

        // ══════════════════════════════════════════════════════════════════════
        //  PAGE: DASHBOARD
        // ══════════════════════════════════════════════════════════════════════
        Panel BuildDashboard()
        {
            var pg = new Panel { AutoScroll = true };
            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(16)
            };

            // Quick action cards
            var cards = new[] {
                ("🖥", "Check Drivers\n(SDIO)",          C_ACCENT,  (Action)(()=> { ShowPage("drivers");  })),
                ("🟢", "Install Chrome\nSilent",          C_GREEN,   ()=> QuickChrome()),
                ("🔑", "Cek Aktivasi\nWindows",           C_ACCENT2, ()=> QuickActivation()),
                ("🖨", "Reset Print\nSpooler",            C_YELLOW,  ()=> QuickSpooler()),
                ("🌐", "Flush DNS\nCache",                C_ACCENT,  ()=> QuickFlushDns()),
                ("🗑", "Bersihkan\nTemp Files",           C_RED,     ()=> QuickCleanTemp()),
            };

            foreach (var (ico, lbl, clr, action) in cards)
            {
                var card = new QuickCard(ico, lbl, clr);
                card.Click += (s, e) => action();
                flow.Controls.Add(card);
            }

            // Info section
            var pnlInfo = new Panel { Width = 780, Height = 120, Margin = new Padding(0, 8, 0, 0) };
            pnlInfo.Paint += (s, e) => DrawCard(e.Graphics, pnlInfo.ClientRectangle, "📋  Hướng Dẫn Nhanh");
            var lblGuide = new Label
            {
                Text = "1.  Bắt đầu từ tab 'Driver Suite' để quét driver.\r\n" +
                       "2.  Vào 'Phần Mềm' để cài Chrome, EVKey, Office 365 một click.\r\n" +
                       "3.  Dùng 'Tối Ưu Hệ Thống' để dọn dẹp, tắt BitLocker, fix máy in.\r\n" +
                       "4.  Kiểm tra IP, ping, flush DNS ở tab 'Mạng & Kết Nối'.\r\n" +
                       "5.  Xem thông số CPU/RAM/Disk ở 'Thông Tin Hệ Thống'.",
                ForeColor = C_SUBTEXT,
                Font = new Font("Segoe UI", 8.5f),
                Location = new Point(12, 28),
                AutoSize = false,
                Width = 740,
                Height = 90
            };
            pnlInfo.Controls.Add(lblGuide);
            flow.Controls.Add(pnlInfo);

            pg.Controls.Add(flow);
            return pg;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  PAGE: DRIVERS
        // ══════════════════════════════════════════════════════════════════════
        Panel BuildDriversPage()
        {
            var pg = PageWithScroll("🖥  Driver Suite");
            var grp = AddSection(pg, "Snappy Driver Installer Origin (SDIO)",
                "Tool tổng hợp driver tự động scan phần cứng và tải đúng driver từ nhiều nguồn.");

            var row = AddRow(grp);
            AddBtn(row, "⬇  Tải & Mở SDIO", C_ACCENT, BtnSDIO_Click);

            var grp2 = AddSection(pg, "Intel Driver & Support Assistant",
                "Tải về trình hỗ trợ driver chính hãng Intel — phù hợp máy Intel CPU/iGPU.");
            var row2 = AddRow(grp2);
            AddBtn(row2, "⬇  Tải Intel DSA", Color.FromArgb(0, 113, 197), BtnIntelDSA_Click);

            return pg;
        }

        async void BtnSDIO_Click(object s, EventArgs e)
        {
            await RunGuarded("SDIO Download", async () => {
                const string url = "https://www.glenn.delahoy.com/wp-content/uploads/SDIO_Update.exe";
                const string dest = @"C:\IT_Tools\SDIO\SDIO_Update.exe";
                Directory.CreateDirectory(@"C:\IT_Tools\SDIO");
                Log("Đang tải SDIO về C:\\IT_Tools\\SDIO\\...", C_YELLOW);
                await DownloadAsync(url, dest);
                Log("Đang mở SDIO...", C_ACCENT);
                Process.Start(new ProcessStartInfo(dest) { UseShellExecute = true });
            });
        }

        async void BtnIntelDSA_Click(object s, EventArgs e)
        {
            await RunGuarded("Intel DSA", async () => {
                const string url = "https://dsadata.intel.com/installer";
                const string dest = @"C:\IT_Tools\IntelDSA.exe";
                Directory.CreateDirectory(@"C:\IT_Tools");
                Log("Đang tải Intel Driver & Support Assistant...", C_YELLOW);
                await DownloadAsync(url, dest);
                Process.Start(new ProcessStartInfo(dest) { UseShellExecute = true });
            });
        }

        // ══════════════════════════════════════════════════════════════════════
        //  PAGE: SOFTWARE
        // ══════════════════════════════════════════════════════════════════════
        Panel BuildSoftwarePage()
        {
            var pg = PageWithScroll("📦  Phần Mềm — Cài Đặt Tự Động (Silent Install)");

            // Chrome
            var grp1 = AddSection(pg, "Google Chrome",
                "Cài Chrome mới nhất qua winget, hoàn toàn ngầm không cần thao tác.");
            var r1 = AddRow(grp1);
            AddBtn(r1, "⬇  Cài Chrome", C_GREEN, async (s, e) => await RunGuarded("Chrome", async () => {
                Log("winget install Google.Chrome --silent ...", C_YELLOW);
                int c = await RunProcAsync("winget", "install --id Google.Chrome -e --silent --accept-package-agreements --accept-source-agreements");
                Log(c == 0 ? "✔ Chrome đã cài xong." : "⚠ Winget trả về " + c + " (có thể đã cài rồi).", c == 0 ? C_GREEN : C_YELLOW);
            }));

            // EVKey
            var grp2 = AddSection(pg, "EVKey — Gõ Tiếng Việt",
                "Tải bản mới nhất từ GitHub, giải nén vào C:\\Program Files\\EVKey, tạo shortcut Desktop.");
            var r2 = AddRow(grp2);
            AddBtn(r2, "⬇  Cài EVKey", C_ACCENT, BtnEVKey_Click);

            // Office 365
            var grp3 = AddSection(pg, "Microsoft Office 365 ProPlus",
                "Tải ODT, tự tạo configuration.xml (chỉ Word+Excel+PPT, 64-bit), chạy cài ngầm ~4GB.");
            var r3 = AddRow(grp3);
            AddBtn(r3, "⬇  Deploy Office 365", C_ACCENT2, BtnOffice_Click);

            // 7-Zip
            var grp4 = AddSection(pg, "7-Zip", "Giải nén mạnh mẽ, miễn phí, hỗ trợ 100+ định dạng.");
            var r4 = AddRow(grp4);
            AddBtn(r4, "⬇  Cài 7-Zip", Color.FromArgb(0, 160, 120), async (s, e) => await RunGuarded("7-Zip", async () => {
                Log("winget install 7zip.7zip --silent ...", C_YELLOW);
                int c = await RunProcAsync("winget", "install --id 7zip.7zip -e --silent --accept-package-agreements --accept-source-agreements");
                Log(c == 0 ? "✔ 7-Zip đã cài xong." : "⚠ Winget trả về " + c, c == 0 ? C_GREEN : C_YELLOW);
            }));

            // Notepad++
            var grp5 = AddSection(pg, "Notepad++", "Trình soạn thảo code nhẹ và mạnh mẽ cho Windows.");
            var r5 = AddRow(grp5);
            AddBtn(r5, "⬇  Cài Notepad++", Color.FromArgb(0, 130, 200), async (s, e) => await RunGuarded("Notepad++", async () => {
                Log("winget install Notepad++.Notepad++ --silent ...", C_YELLOW);
                int c = await RunProcAsync("winget", "install --id Notepad++.Notepad++ -e --silent --accept-package-agreements --accept-source-agreements");
                Log(c == 0 ? "✔ Notepad++ đã cài xong." : "⚠ " + c, c == 0 ? C_GREEN : C_YELLOW);
            }));

            // VLC
            var grp6 = AddSection(pg, "VLC Media Player", "Phát media đa năng, hỗ trợ hầu hết mọi định dạng video/audio.");
            var r6 = AddRow(grp6);
            AddBtn(r6, "⬇  Cài VLC", Color.FromArgb(200, 80, 0), async (s, e) => await RunGuarded("VLC", async () => {
                Log("winget install VideoLAN.VLC --silent ...", C_YELLOW);
                int c = await RunProcAsync("winget", "install --id VideoLAN.VLC -e --silent --accept-package-agreements --accept-source-agreements");
                Log(c == 0 ? "✔ VLC đã cài xong." : "⚠ " + c, c == 0 ? C_GREEN : C_YELLOW);
            }));

            return pg;
        }

        async void BtnEVKey_Click(object s, EventArgs e)
        {
            await RunGuarded("EVKey", async () => {
                Log("Đang query GitHub API cho phiên bản EVKey mới nhất...", C_YELLOW);
                string json = await GetStrAsync("https://api.github.com/repos/lamquangminh/EVKey/releases/latest");
                string zipUrl = ParseGhAsset(json, ".zip");
                if (string.IsNullOrEmpty(zipUrl)) throw new Exception("Không tìm thấy file .zip trong release EVKey.");

                string tmp = Path.Combine(Path.GetTempPath(), "EVKey_latest.zip");
                Log($"Đang tải: {zipUrl}", C_YELLOW);
                await DownloadAsync(zipUrl, tmp);

                const string dest = @"C:\Program Files\EVKey";
                if (Directory.Exists(dest)) Directory.Delete(dest, true);
                Directory.CreateDirectory(dest);
                Log("Đang giải nén...", C_YELLOW);
                await Task.Run(() => ZipFile.ExtractToDirectory(tmp, dest));
                File.Delete(tmp);

                string[] exes = Directory.GetFiles(dest, "EVKey*.exe", SearchOption.AllDirectories);
                string exePath = exes.Length > 0 ? exes[0] : Path.Combine(dest, "EVKey.exe");

                Log("Đang tạo shortcut Desktop...", C_YELLOW);
                await Task.Run(() => CreateShortcut(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "EVKey.lnk"),
                    exePath, dest, "EVKey — Gõ Tiếng Việt"));

                Log("✔ EVKey cài xong + shortcut Desktop đã tạo.", C_GREEN);
            });
        }

        async void BtnOffice_Click(object s, EventArgs e)
        {
            await RunGuarded("Office 365", async () => {
                const string odtUrl = "https://download.microsoft.com/download/2/7/A/27AF1BE6-DD20-4CB4-B154-EBAB8A7D4A7E/officedeploymenttool_18129-20030.exe";
                const string dir = @"C:\IT_Tools\ODT";
                Directory.CreateDirectory(dir);

                string odtExe = Path.Combine(dir, "ODTSetup.exe");
                Log("Đang tải Office Deployment Tool...", C_YELLOW);
                await DownloadAsync(odtUrl, odtExe);

                Log("Đang giải nén ODT...", C_YELLOW);
                await RunProcAsync(odtExe, $"/quiet /extract:\"{dir}\"");

                string cfg = Path.Combine(dir, "configuration.xml");
                File.WriteAllText(cfg, @"<Configuration>
  <Add OfficeClientEdition=""64"" Channel=""Current"">
    <Product ID=""O365ProPlusRetail"">
      <Language ID=""vi-vn""/>
      <Language ID=""en-us""/>
      <ExcludeApp ID=""Access""/>
      <ExcludeApp ID=""Groove""/>
      <ExcludeApp ID=""Lync""/>
      <ExcludeApp ID=""OneDrive""/>
      <ExcludeApp ID=""OneNote""/>
      <ExcludeApp ID=""Outlook""/>
      <ExcludeApp ID=""Publisher""/>
      <ExcludeApp ID=""Teams""/>
    </Product>
  </Add>
  <Display Level=""None"" AcceptEULA=""TRUE""/>
  <Property Name=""AUTOACTIVATE"" Value=""1""/>
</Configuration>", Encoding.UTF8);
                Log("✔ configuration.xml đã tạo (Word + Excel + PowerPoint, tiếng Việt + Anh).", C_GREEN);

                string setup = Path.Combine(dir, "setup.exe");
                if (!File.Exists(setup)) throw new FileNotFoundException("setup.exe không tìm thấy sau khi giải nén ODT.", setup);

                Log("Đang chạy cài Office 365 — quá trình tải ~4GB, vui lòng chờ...", C_YELLOW);
                int code = await RunProcAsync(setup, $"/configure \"{cfg}\"");
                Log(code == 0 ? "✔ Office 365 ProPlus cài xong!" : $"⚠ setup.exe trả về {code}. Xem log tại %temp%\\Microsoft Office\\",
                    code == 0 ? C_GREEN : C_YELLOW);
            });
        }

        // ══════════════════════════════════════════════════════════════════════
        //  PAGE: OPTIMIZE
        // ══════════════════════════════════════════════════════════════════════
        Panel BuildOptimizePage()
        {
            var pg = PageWithScroll("🔧  Tối Ưu Hệ Thống");

            // BitLocker
            var g1 = AddSection(pg, "BitLocker — Mã Hóa Ổ Đĩa", "Tắt mã hoá BitLocker trên ổ C:. Quá trình giải mã chạy nền.");
            var r1 = AddRow(g1);
            AddBtn(r1, "🔓  Tắt BitLocker C:", C_RED, BtnBitLocker_Click);

            // Print Spooler
            var g2 = AddSection(pg, "Print Spooler — Dịch Vụ In", "Dừng dịch vụ → xóa job kẹt → khởi động lại. Fix lỗi in ấn 100%.");
            var r2 = AddRow(g2);
            AddBtn(r2, "🖨  Reset Print Spooler", C_YELLOW, BtnSpooler_Click);

            // Temp Clean
            var g3 = AddSection(pg, "Dọn File Rác — Temp Cleaner", "Xóa toàn bộ %temp%, C:\\Windows\\Temp, Prefetch để giải phóng dung lượng.");
            var r3 = AddRow(g3);
            AddBtn(r3, "🗑  Xóa Temp Files", C_RED, BtnCleanTemp_Click);

            // Windows Update
            var g4 = AddSection(pg, "Windows Update", "Mở Windows Update Settings để kiểm tra và cài bản vá mới nhất.");
            var r4 = AddRow(g4);
            AddBtn(r4, "🔄  Mở Windows Update", C_ACCENT, async (s, e) => await RunGuarded("Windows Update", async () => {
                await Task.Run(() => Process.Start("ms-settings:windowsupdate"));
                Log("✔ Đã mở Windows Update Settings.", C_GREEN);
            }));

            // Disk Cleanup
            var g5 = AddSection(pg, "Disk Cleanup (cleanmgr)", "Mở Disk Cleanup cho ổ C: — xóa file hệ thống, WinSxS...");
            var r5 = AddRow(g5);
            AddBtn(r5, "💿  Mở Disk Cleanup C:", Color.FromArgb(0, 140, 190), async (s, e) => await RunGuarded("Disk Cleanup", async () => {
                await Task.Run(() => Process.Start(new ProcessStartInfo("cleanmgr.exe", "/d C:") { UseShellExecute = true }));
                Log("✔ Disk Cleanup đã mở.", C_GREEN);
            }));

            // SFC
            var g6 = AddSection(pg, "System File Checker (SFC)", "Quét và sửa file hệ thống bị lỗi/thiếu. Chạy trong background.");
            var r6 = AddRow(g6);
            AddBtn(r6, "🔍  Chạy SFC /scannow", Color.FromArgb(0, 160, 100), async (s, e) => await RunGuarded("SFC", async () => {
                Log("Đang chạy sfc /scannow — có thể mất 10-15 phút...", C_YELLOW);
                int c = await RunProcAsync("sfc.exe", "/scannow");
                Log(c == 0 ? "✔ SFC hoàn thành — không tìm thấy lỗi hoặc đã sửa xong." : "⚠ SFC trả về " + c + " — xem CBS.log", c == 0 ? C_GREEN : C_YELLOW);
            }));

            return pg;
        }

        async void BtnBitLocker_Click(object s, EventArgs e)
        {
            if (MessageBox.Show("Tắt BitLocker trên C:?\n\nQuá trình giải mã chạy nền và có thể mất nhiều giờ.",
                "Xác Nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            await RunGuarded("Tắt BitLocker", async () => {
                Log("Chạy: Disable-BitLocker -MountPoint 'C:' ...", C_YELLOW);
                int c = await RunProcAsync("powershell.exe", "-NonInteractive -NoProfile -Command \"Disable-BitLocker -MountPoint 'C:'\"");
                Log(c == 0 ? "✔ Bắt đầu giải mã BitLocker. Dùng 'manage-bde -status C:' để kiểm tra tiến độ." : "⚠ Lỗi: " + c + " — có thể BitLocker chưa được bật.", c == 0 ? C_GREEN : C_YELLOW);
            });
        }

        async void BtnSpooler_Click(object s, EventArgs e)
        {
            await RunGuarded("Reset Print Spooler", async () => {
                Log("Đang dừng Print Spooler...", C_YELLOW);
                await RunProcAsync("net", "stop spooler");
                await Task.Run(() => {
                    const string d = @"C:\Windows\System32\spool\PRINTERS";
                    if (Directory.Exists(d))
                        foreach (string f in Directory.GetFiles(d))
                            try { File.Delete(f); } catch { }
                });
                Log("✔ Đã xóa job in kẹt.", C_GREEN);
                Log("Đang khởi động lại Print Spooler...", C_YELLOW);
                int c = await RunProcAsync("net", "start spooler");
                Log(c == 0 ? "✔ Print Spooler đã reset thành công!" : "⚠ Lỗi khởi động Spooler: " + c, c == 0 ? C_GREEN : C_RED);
            });
        }

        async void BtnCleanTemp_Click(object s, EventArgs e) => await QuickCleanTemp();
        async Task QuickCleanTemp()
        {
            await RunGuarded("Xóa Temp Files", async () => {
                long total = 0;
                var dirs = new[] {
                    Environment.GetEnvironmentVariable("TEMP"),
                    @"C:\Windows\Temp",
                    @"C:\Windows\Prefetch"
                };
                await Task.Run(() => {
                    foreach (string d in dirs)
                    {
                        if (!Directory.Exists(d)) continue;
                        foreach (string f in Directory.GetFiles(d, "*", SearchOption.TopDirectoryOnly))
                            try { var fi = new FileInfo(f); total += fi.Length; File.Delete(f); } catch { }
                        foreach (string sub in Directory.GetDirectories(d))
                            try { Directory.Delete(sub, true); } catch { }
                    }
                });
                Log($"✔ Đã dọn {FormatBytes(total)} khỏi Temp, Windows\\Temp, Prefetch.", C_GREEN);
            });
        }

        // ══════════════════════════════════════════════════════════════════════
        //  PAGE: NETWORK
        // ══════════════════════════════════════════════════════════════════════
        Panel BuildNetworkPage()
        {
            var pg = PageWithScroll("🌐  Mạng & Kết Nối");

            // Flush DNS
            var g1 = AddSection(pg, "Flush DNS Cache", "Xóa bộ nhớ cache DNS — giải quyết lỗi không vào được web dù có internet.");
            var r1 = AddRow(g1);
            AddBtn(r1, "🌐  Flush DNS", C_ACCENT, async (s, e) => await QuickFlushDns());

            // Reset TCP/IP
            var g2 = AddSection(pg, "Reset TCP/IP Stack", "Đặt lại toàn bộ cài đặt mạng TCP/IP về mặc định. Cần restart sau khi chạy.");
            var r2 = AddRow(g2);
            AddBtn(r2, "♻  Reset TCP/IP + Winsock", C_RED, BtnResetNetwork_Click);

            // Ping test
            var g3 = AddSection(pg, "Ping Test — Kiểm Tra Kết Nối",
                "Ping đến Google DNS (8.8.8.8) và Cloudflare (1.1.1.1) để kiểm tra internet.");
            var r3 = AddRow(g3);
            AddBtn(r3, "📡  Chạy Ping Test", C_GREEN, BtnPing_Click);

            // IP Config
            var g4 = AddSection(pg, "IP Configuration", "Hiển thị toàn bộ thông tin mạng — IP, Gateway, DNS, MAC address.");
            var r4 = AddRow(g4);
            AddBtn(r4, "📋  Xem IP Config /all", C_ACCENT, async (s, e) => await RunGuarded("IP Config", async () => {
                string out_ = await CaptureAsync("ipconfig.exe", "/all");
                Log("── IP Configuration ────────────────────────────────────────", C_ACCENT);
                Log(out_, C_TEXT);
                Log("────────────────────────────────────────────────────────────", C_ACCENT);
            }));

            // Renew IP
            var g5 = AddSection(pg, "Renew IP Address", "Release IP hiện tại và xin IP mới từ DHCP server.");
            var r5 = AddRow(g5);
            AddBtn(r5, "🔄  Release & Renew IP", C_YELLOW, async (s, e) => await RunGuarded("Renew IP", async () => {
                Log("ipconfig /release ...", C_YELLOW);
                await RunProcAsync("ipconfig.exe", "/release");
                Log("ipconfig /renew ...", C_YELLOW);
                await RunProcAsync("ipconfig.exe", "/renew");
                Log("✔ IP đã được cấp lại từ DHCP.", C_GREEN);
            }));

            return pg;
        }

        async Task QuickFlushDns()
        {
            await RunGuarded("Flush DNS", async () => {
                Log("ipconfig /flushdns ...", C_YELLOW);
                await RunProcAsync("ipconfig.exe", "/flushdns");
                Log("✔ DNS cache đã được xóa.", C_GREEN);
            });
        }

        async void BtnResetNetwork_Click(object s, EventArgs e)
        {
            if (MessageBox.Show("Reset TCP/IP và Winsock?\n\nMáy cần RESTART sau khi chạy.",
                "Xác Nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            await RunGuarded("Reset TCP/IP", async () => {
                Log("netsh int ip reset ...", C_YELLOW);
                await RunProcAsync("netsh", "int ip reset");
                Log("netsh winsock reset ...", C_YELLOW);
                await RunProcAsync("netsh", "winsock reset");
                Log("✔ TCP/IP & Winsock đã reset. Vui lòng RESTART máy.", C_GREEN);
            });
        }

        async void BtnPing_Click(object s, EventArgs e)
        {
            await RunGuarded("Ping Test", async () => {
                var hosts = new[] { ("Google DNS", "8.8.8.8"), ("Cloudflare", "1.1.1.1"), ("Google.com", "google.com") };
                foreach (var (name, host) in hosts)
                {
                    using (var p = new Ping())
                    {
                        try
                        {
                            var r = await Task.Run(() => p.Send(host, 2000));
                            if (r.Status == IPStatus.Success)
                                Log($"  ✔ {name,-15} ({host}) — {r.RoundtripTime}ms", C_GREEN);
                            else
                                Log($"  ✖ {name,-15} ({host}) — {r.Status}", C_RED);
                        }
                        catch (Exception ex)
                        {
                            Log($"  ✖ {name,-15} ({host}) — Lỗi: {ex.Message}", C_RED);
                        }
                    }
                }
            });
        }

        // ══════════════════════════════════════════════════════════════════════
        //  PAGE: SYSTEM INFO
        // ══════════════════════════════════════════════════════════════════════
        Panel BuildSysInfoPage()
        {
            var pg = PageWithScroll("ℹ  Thông Tin Hệ Thống");

            var g1 = AddSection(pg, "Kích Hoạt Windows", "Kiểm tra trạng thái bản quyền Windows hiện tại.");
            var r1 = AddRow(g1);
            AddBtn(r1, "🔑  Kiểm Tra Kích Hoạt (slmgr)", Color.FromArgb(80, 60, 200), BtnActivation_Click);

            var g2 = AddSection(pg, "Thông Tin Chi Tiết Hệ Thống", "Hiển thị CPU, RAM, mainboard, serial number...");
            var r2 = AddRow(g2);
            AddBtn(r2, "📊  Xem Thông Tin Đầy Đủ", C_ACCENT, BtnDetailedInfo_Click);

            var g3 = AddSection(pg, "Dung Lượng Ổ Đĩa", "Liệt kê tất cả ổ đĩa với dung lượng đã dùng và còn trống.");
            var r3 = AddRow(g3);
            AddBtn(r3, "💾  Kiểm Tra Dung Lượng", C_GREEN, BtnDiskInfo_Click);

            var g4 = AddSection(pg, "Startup Programs", "Xem danh sách phần mềm khởi động cùng Windows.");
            var r4 = AddRow(g4);
            AddBtn(r4, "🚀  Xem / Quản Lý Startup", C_YELLOW, async (s, e) => await RunGuarded("Startup", async () => {
                await Task.Run(() => Process.Start(new ProcessStartInfo("taskmgr.exe") { UseShellExecute = true }));
                Log("✔ Đã mở Task Manager — chọn tab Startup để quản lý.", C_GREEN);
            }));

            var g5 = AddSection(pg, "Event Viewer — Log Hệ Thống", "Mở Event Viewer để xem log lỗi hệ thống Windows.");
            var r5 = AddRow(g5);
            AddBtn(r5, "📋  Mở Event Viewer", C_ACCENT2, async (s, e) => await RunGuarded("Event Viewer", async () => {
                await Task.Run(() => Process.Start(new ProcessStartInfo("eventvwr.msc") { UseShellExecute = true }));
                Log("✔ Đã mở Event Viewer.", C_GREEN);
            }));

            return pg;
        }

        async void BtnActivation_Click(object s, EventArgs e)
        {
            await RunGuarded("Kiểm Tra Kích Hoạt", async () => {
                Log("Chạy: cscript slmgr.vbs /dli ...", C_YELLOW);
                string result = await CaptureAsync("cscript.exe", @"//Nologo C:\Windows\System32\slmgr.vbs /dli");
                Log("── Trạng Thái Kích Hoạt Windows ────────────────────────────", C_ACCENT);
                Log(result, C_TEXT);
                Log("────────────────────────────────────────────────────────────", C_ACCENT);
            });
        }

        async void BtnDetailedInfo_Click(object s, EventArgs e)
        {
            await RunGuarded("System Info", async () => {
                var sb = new StringBuilder();
                await Task.Run(() => {
                    sb.AppendLine("── CPU ─────────────────────────────────────────────────────");
                    using (var mos = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
                        foreach (ManagementObject mo in mos.Get())
                            sb.AppendLine($"  {mo["Name"]}  |  Cores: {mo["NumberOfCores"]}  |  Threads: {mo["ThreadCount"]}");

                    sb.AppendLine("\n── RAM ─────────────────────────────────────────────────────");
                    using (var mos = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory"))
                        foreach (ManagementObject mo in mos.Get())
                            sb.AppendLine($"  Slot: {mo["DeviceLocator"]}  |  {FormatBytes(Convert.ToInt64(mo["Capacity"]))}  |  {mo["Speed"]} MHz");

                    sb.AppendLine("\n── Mainboard ───────────────────────────────────────────────");
                    using (var mos = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard"))
                        foreach (ManagementObject mo in mos.Get())
                            sb.AppendLine($"  {mo["Manufacturer"]} {mo["Product"]}  |  S/N: {mo["SerialNumber"]}");

                    sb.AppendLine("\n── BIOS ────────────────────────────────────────────────────");
                    using (var mos = new ManagementObjectSearcher("SELECT * FROM Win32_BIOS"))
                        foreach (ManagementObject mo in mos.Get())
                            sb.AppendLine($"  {mo["Manufacturer"]}  |  Version: {mo["SMBIOSBIOSVersion"]}  |  S/N: {mo["SerialNumber"]}");

                    sb.AppendLine("\n── GPU ─────────────────────────────────────────────────────");
                    using (var mos = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
                        foreach (ManagementObject mo in mos.Get())
                            sb.AppendLine($"  {mo["Name"]}  |  RAM: {FormatBytes(Convert.ToInt64(mo["AdapterRAM"]))}");
                });
                Log("── Thông Tin Hệ Thống Chi Tiết ─────────────────────────────", C_ACCENT);
                Log(sb.ToString(), C_TEXT);
                Log("─────────────────────────────────────────────────────────────", C_ACCENT);
            });
        }

        async void BtnDiskInfo_Click(object s, EventArgs e)
        {
            await RunGuarded("Disk Info", async () => {
                await Task.Run(() => {
                    Log("── Dung Lượng Ổ Đĩa ─────────────────────────────────────────", C_ACCENT);
                    foreach (var d in DriveInfo.GetDrives())
                    {
                        if (d.IsReady)
                        {
                            double pct = (double)(d.TotalSize - d.AvailableFreeSpace) / d.TotalSize * 100;
                            string bar = new string('█', (int)(pct / 5)) + new string('░', 20 - (int)(pct / 5));
                            Color c = pct > 90 ? C_RED : pct > 70 ? C_YELLOW : C_GREEN;
                            Log($"  {d.Name}  [{bar}] {pct:F1}%  —  " +
                                $"Đã dùng: {FormatBytes(d.TotalSize - d.AvailableFreeSpace)} / " +
                                $"Tổng: {FormatBytes(d.TotalSize)}", c);
                        }
                    }
                    Log("──────────────────────────────────────────────────────────────", C_ACCENT);
                });
            });
        }

        // ══════════════════════════════════════════════════════════════════════
        //  QUICK ACTIONS (from Dashboard)
        // ══════════════════════════════════════════════════════════════════════
        async void QuickChrome() => await RunGuarded("Chrome", async () => {
            Log("winget install Google.Chrome --silent ...", C_YELLOW);
            int c = await RunProcAsync("winget", "install --id Google.Chrome -e --silent --accept-package-agreements --accept-source-agreements");
            Log(c == 0 ? "✔ Chrome đã cài xong." : "⚠ Winget trả về " + c, c == 0 ? C_GREEN : C_YELLOW);
        });
        async void QuickActivation() { ShowPage("sysinfo"); BtnActivation_Click(null, null); }
        async void QuickSpooler() => BtnSpooler_Click(null, null);

        // ══════════════════════════════════════════════════════════════════════
        //  SYSTEM INFO (Sidebar)
        // ══════════════════════════════════════════════════════════════════════
        void LoadSystemInfo()
        {
            Task.Run(() => {
                try
                {
                    string pcName = Environment.MachineName;
                    string os = "";
                    string ram = "";
                    using (var mos = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem"))
                        foreach (ManagementObject mo in mos.Get()) os = mo["Caption"].ToString().Replace("Microsoft ", "");
                    using (var mos = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem"))
                        foreach (ManagementObject mo in mos.Get())
                        {
                            long kb = Convert.ToInt64(mo["TotalVisibleMemorySize"]);
                            ram = FormatBytes(kb * 1024) + " RAM";
                        }
                    string ip = "";
                    foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                    {
                        if (ni.OperationalStatus != OperationalStatus.Up) continue;
                        if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                        foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                            if (ua.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                ip = ua.Address.ToString(); break;
                            }
                        if (!string.IsNullOrEmpty(ip)) break;
                    }
                    this.Invoke(new Action(() => {
                        lblSysName.Text = $"💻  {pcName}";
                        lblSysOS.Text = $"🪟  {(os.Length > 22 ? os.Substring(0, 22) + "…" : os)}";
                        lblSysIP.Text = $"🌐  {(string.IsNullOrEmpty(ip) ? "No network" : ip)}";
                        lblSysRAM.Text = $"🧠  {ram}";
                    }));
                }
                catch { }
            });
        }

        // ══════════════════════════════════════════════════════════════════════
        //  CLOCK
        // ══════════════════════════════════════════════════════════════════════
        void StartClock()
        {
            tmrClock = new System.Windows.Forms.Timer { Interval = 1000 };
            tmrClock.Tick += (s, e) => {
                lblClock.Text = DateTime.Now.ToString("HH:mm:ss  dd/MM/yyyy");
                DoLayout(); // reposition clock label
            };
            tmrClock.Start();
        }

        // ══════════════════════════════════════════════════════════════════════
        //  UI HELPERS
        // ══════════════════════════════════════════════════════════════════════
        Panel PageWithScroll(string title)
        {
            var pg = new Panel { AutoScroll = true };
            var lbl = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = C_TEXT,
                AutoSize = true,
                Location = new Point(18, 16)
            };
            var line = new Panel { BackColor = C_ACCENT, Location = new Point(18, 44), Height = 2 };
            line.Name = "pageTitle";
            pg.Controls.AddRange(new Control[] { lbl, line });
            pg.Resize += (s, e) => line.Width = pg.Width - 36;
            return pg;
        }

        GroupBox AddSection(Panel parent, string title, string desc)
        {
            int y = 60;
            foreach (Control c in parent.Controls)
                if (c is GroupBox) y = Math.Max(y, c.Bottom + 10);

            var grp = new GroupBox
            {
                Text = title,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = C_ACCENT,
                Location = new Point(16, y),
                BackColor = C_CARD
            };
            grp.Paint += (s, e) => {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var p = new Pen(C_BORDER, 1))
                    g.DrawRectangle(p, 0, 0, grp.Width - 1, grp.Height - 1);
            };

            var lblDesc = new Label
            {
                Text = desc,
                ForeColor = C_SUBTEXT,
                Font = new Font("Segoe UI", 8f),
                AutoSize = false,
                Location = new Point(10, 22),
                Width = 700,
                Height = 20
            };
            grp.Controls.Add(lblDesc);
            grp.Height = 90;
            parent.Controls.Add(grp);
            parent.Resize += (s, e) => grp.Width = parent.Width - 32;
            grp.Width = parent.Width - 32;
            return grp;
        }

        FlowLayoutPanel AddRow(GroupBox grp)
        {
            var flow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Location = new Point(10, 46),
                WrapContents = false
            };
            grp.Controls.Add(flow);
            grp.Height = 90;
            return flow;
        }

        void AddBtn(FlowLayoutPanel row, string text, Color color, EventHandler handler)
        {
            var btn = new Button
            {
                Text = text,
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Size = new Size(240, 34),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0),
                FlatAppearance = { BorderSize = 0 }
            };
            btn.Click += handler;
            btn.MouseEnter += (s, e) => btn.BackColor = ControlPaint.Light(color, 0.1f);
            btn.MouseLeave += (s, e) => btn.BackColor = color;
            row.Controls.Add(btn);
        }

        static Button MakeSmallBtn(string text, Color bg)
        {
            return new Button
            {
                Text = text,
                BackColor = bg,
                ForeColor = Color.FromArgb(180, 190, 210),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 7.5f),
                Size = new Size(72, 22),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
        }

        static void DrawCard(Graphics g, Rectangle r, string title)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var b = new SolidBrush(Color.FromArgb(26, 35, 58)))
                g.FillRectangle(b, r);
            using (var p = new Pen(Color.FromArgb(40, 55, 85)))
                g.DrawRectangle(p, r.X, r.Y, r.Width - 1, r.Height - 1);
            using (var f = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (var b = new SolidBrush(Color.FromArgb(56, 139, 253)))
                g.DrawString(title, f, b, new PointF(12, 8));
        }

        // ══════════════════════════════════════════════════════════════════════
        //  ASYNC INFRASTRUCTURE
        // ══════════════════════════════════════════════════════════════════════
        async Task RunGuarded(string name, Func<Task> action)
        {
            Log($"\n[{DateTime.Now:HH:mm:ss}] ▶ {name}", C_ACCENT);
            SetProg(0, name + " ...");
            try
            {
                await action();
                SetProg(100, name + " xong.");
                Log($"[{DateTime.Now:HH:mm:ss}] ✔ {name} hoàn thành.\n", C_GREEN);
            }
            catch (Exception ex)
            {
                SetProg(0, "");
                Log($"[{DateTime.Now:HH:mm:ss}] ✖ {name} LỖI: {ex.Message}\n", C_RED);
                MessageBox.Show($"Lỗi trong '{name}':\r\n\r\n{ex.Message}",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        async Task DownloadAsync(string url, string dest)
        {
            using (var wc = new WebClient())
            {
                wc.Headers["User-Agent"] = "ITSupportToolkit/2.0";
                wc.DownloadProgressChanged += (s, e) =>
                    SafeInvoke(() => SetProg(e.ProgressPercentage, $"Đang tải... {e.ProgressPercentage}%  ({e.BytesReceived / 1024:N0} KB)"));
                await wc.DownloadFileTaskAsync(new Uri(url), dest);
            }
            SetProg(100, "Tải xong.");
            Log($"  ✔ Đã lưu → {dest}", C_GREEN);
        }

        async Task<string> GetStrAsync(string url)
        {
            using (var wc = new WebClient())
            {
                wc.Headers["User-Agent"] = "ITSupportToolkit/2.0";
                return await wc.DownloadStringTaskAsync(url);
            }
        }

        Task<int> RunProcAsync(string exe, string args)
        {
            return Task.Run(() => {
                using (var p = new Process())
                {
                    p.StartInfo = new ProcessStartInfo
                    {
                        FileName = exe,
                        Arguments = args,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    p.OutputDataReceived += (s, e) => { if (e.Data != null) SafeInvoke(() => Log("  " + e.Data, Color.FromArgb(160, 175, 200))); };
                    p.ErrorDataReceived += (s, e) => { if (e.Data != null) SafeInvoke(() => Log("  " + e.Data, C_RED)); };
                    p.Start();
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();
                    p.WaitForExit();
                    return p.ExitCode;
                }
            });
        }

        Task<string> CaptureAsync(string exe, string args)
        {
            return Task.Run(() => {
                using (var p = new Process())
                {
                    p.StartInfo = new ProcessStartInfo
                    {
                        FileName = exe,
                        Arguments = args,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    p.Start();
                    string o = p.StandardOutput.ReadToEnd();
                    string er = p.StandardError.ReadToEnd();
                    p.WaitForExit();
                    return string.IsNullOrWhiteSpace(o) ? er : o;
                }
            });
        }

        void Log(string msg, Color color)
        {
            SafeInvoke(() => {
                rtbLog.SelectionStart = rtbLog.TextLength;
                rtbLog.SelectionLength = 0;
                rtbLog.SelectionColor = color;
                rtbLog.AppendText(msg + "\n");
                rtbLog.SelectionColor = rtbLog.ForeColor;
                rtbLog.ScrollToCaret();
            });
        }

        void SetProg(int pct, string msg)
        {
            SafeInvoke(() => {
                pbMain.Value = Math.Max(0, Math.Min(100, pct));
                lblTask.Text = msg;
            });
        }

        void SafeInvoke(Action a) { if (this.InvokeRequired) this.Invoke(a); else a(); }

        // ══════════════════════════════════════════════════════════════════════
        //  UTILITIES
        // ══════════════════════════════════════════════════════════════════════
        static string ParseGhAsset(string json, string ext)
        {
            int idx = 0;
            while (true)
            {
                idx = json.IndexOf("browser_download_url", idx);
                if (idx < 0) break;
                int s = json.IndexOf('"', idx + 22) + 1;
                int e = json.IndexOf('"', s);
                if (s < 0 || e < 0) break;
                string url = json.Substring(s, e - s);
                if (url.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) return url;
                idx = e;
            }
            return null;
        }

        static void CreateShortcut(string lnkPath, string target, string workDir, string desc)
        {
            Type t = Type.GetTypeFromProgID("WScript.Shell");
            object sh = Activator.CreateInstance(t);
            object sc = t.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, sh, new object[] { lnkPath });
            Type st = sc.GetType();
            st.InvokeMember("TargetPath", BindingFlags.SetProperty, null, sc, new object[] { target });
            st.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, sc, new object[] { workDir });
            st.InvokeMember("Description", BindingFlags.SetProperty, null, sc, new object[] { desc });
            st.InvokeMember("Save", BindingFlags.InvokeMethod, null, sc, new object[0]);
            Marshal.FinalReleaseComObject(sc);
            Marshal.FinalReleaseComObject(sh);
        }

        static string FormatBytes(long bytes)
        {
            if (bytes <= 0) return "0 B";
            string[] s = { "B", "KB", "MB", "GB", "TB" };
            int i = 0;
            double d = bytes;
            while (d >= 1024 && i < s.Length - 1) { d /= 1024; i++; }
            return $"{d:F1} {s[i]}";
        }

        void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new SizeF(6f, 13f);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ResumeLayout(false);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  CUSTOM CONTROLS
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Custom sidebar navigation button with icon + label + active indicator</summary>
    class NavBtn : Panel
    {
        public string PageId { get; }
        bool _active;
        Label lblIcon, lblText;
        Panel indicator;

        static readonly Color C_HOVER = Color.FromArgb(30, 40, 65);
        static readonly Color C_ACTIVE = Color.FromArgb(25, 35, 58);
        static readonly Color C_NORMAL = Color.FromArgb(15, 20, 35);
        static readonly Color C_IND = Color.FromArgb(56, 139, 253);

        public NavBtn(string icon, string text, string pageId)
        {
            PageId = pageId;
            Size = new Size(194, 44);
            BackColor = C_NORMAL;
            Cursor = Cursors.Hand;

            indicator = new Panel
            {
                Size = new Size(3, 28),
                BackColor = Color.Transparent,
                Location = new Point(0, 8)
            };
            lblIcon = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI", 13f),
                ForeColor = Color.FromArgb(100, 130, 180),
                AutoSize = true,
                Location = new Point(14, 11)
            };
            lblText = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(140, 160, 200),
                AutoSize = true,
                Location = new Point(44, 14)
            };

            Controls.AddRange(new Control[] { indicator, lblIcon, lblText });

            MouseEnter += (s, e) => { if (!_active) BackColor = C_HOVER; };
            MouseLeave += (s, e) => { if (!_active) BackColor = C_NORMAL; };
            foreach (Control c in Controls)
            {
                c.MouseEnter += (s, e) => { if (!_active) BackColor = C_HOVER; };
                c.MouseLeave += (s, e) => { if (!_active) BackColor = C_NORMAL; };
                c.Click += (s, e) => OnClick(e);
            }
        }

        public void SetActive(bool active)
        {
            _active = active;
            BackColor = active ? C_ACTIVE : C_NORMAL;
            indicator.BackColor = active ? C_IND : Color.Transparent;
            lblIcon.ForeColor = active ? Color.FromArgb(56, 139, 253) : Color.FromArgb(100, 130, 180);
            lblText.ForeColor = active ? Color.FromArgb(220, 230, 245) : Color.FromArgb(140, 160, 200);
            lblText.Font = new Font("Segoe UI", 9f, active ? FontStyle.Bold : FontStyle.Regular);
        }
    }

    /// <summary>Dashboard quick-action card</summary>
    class QuickCard : Panel
    {
        Color _accent;
        Label lblIcon, lblText;
        bool _hover;

        public QuickCard(string icon, string text, Color accent)
        {
            _accent = accent;
            Size = new Size(160, 105);
            Margin = new Padding(8);
            BackColor = Color.FromArgb(22, 30, 50);
            Cursor = Cursors.Hand;

            lblIcon = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI Emoji", 22f),
                ForeColor = accent,
                AutoSize = true,
                Location = new Point(12, 12)
            };
            lblText = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 195, 220),
                AutoSize = false,
                Width = 136,
                Height = 40,
                Location = new Point(12, 58)
            };

            Controls.AddRange(new Control[] { lblIcon, lblText });

            MouseEnter += OnHoverEnter; MouseLeave += OnHoverLeave;
            foreach (Control c in Controls) { c.MouseEnter += OnHoverEnter; c.MouseLeave += OnHoverLeave; c.Click += (s, e) => OnClick(e); }
        }

        void OnHoverEnter(object s, EventArgs e) { _hover = true; Invalidate(); }
        void OnHoverLeave(object s, EventArgs e) { _hover = false; Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var p = new Pen(_hover ? _accent : Color.FromArgb(40, 55, 85), _hover ? 2 : 1))
                g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
            if (_hover)
                using (var b = new SolidBrush(Color.FromArgb(20, _accent)))
                    g.FillRectangle(b, ClientRectangle);
        }
    }
}