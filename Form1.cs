// ══════════════════════════════════════════════════════════════════════════════
//  IT Support Tools v3.0  —  Form1.cs
//  .NET Framework 4.8 | WinForms | Requires Administrator
//  TẤT CẢ chức năng hoạt động thực tế — Driver Scanner Built-in
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

        const int SIDEBAR_W = 215;
        const int HEADER_H  = 62;
        const int FOOTER_H  = 185;

        Panel pnlSidebar, pnlHeader, pnlContent, pnlFooter;
        RichTextBox rtbLog;
        ProgressBar pbMain;
        Label lblTask, lblClock, lblSysName, lblSysOS, lblSysIP, lblSysRAM, lblDriverStatus;
        System.Windows.Forms.Timer tmrClock;
        ListView lvDrivers;

        Dictionary<string, Panel> pages  = new Dictionary<string, Panel>();
        List<NavBtn> navBtns = new List<NavBtn>();

        public Form1()
        {
            InitializeComponent();
            BuildUI();
            LoadSysInfoAsync();
            ShowPage("dashboard");
            StartClock();
            Log("IT Support Tools v3.0 — San sang.", C_GREEN);
            Log("Dang chay voi quyen Administrator.", C_ACCENT);
        }

        // BUILD UI
        void BuildUI()
        {
            this.Text = "IT Support Tools v3.0";
            this.Size = new Size(1150, 800);
            this.MinimumSize = new Size(950, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = C_DARK;
            this.Font = new Font("Segoe UI", 9f);
            this.DoubleBuffered = true;
            try {
                string ico = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "logo.ico");
                this.Icon = File.Exists(ico) ? new Icon(ico) : SystemIcons.Shield;
            } catch { this.Icon = SystemIcons.Shield; }
            BuildHeader(); BuildSidebar(); BuildContent(); BuildFooter(); BuildAllPages();
            this.Resize += (s, e) => DoLayout();
            DoLayout();
        }

        void BuildHeader()
        {
            pnlHeader = new Panel { BackColor = C_SIDEBAR };
            pnlHeader.Paint += (s, e) => {
                using (var p = new Pen(C_ACCENT, 2))
                    e.Graphics.DrawLine(p, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
            };
            var pbLogo = new PictureBox { Size = new Size(42, 42), Location = new Point(10, 9), SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Transparent };
            try {
                string ico = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "logo.ico");
                if (File.Exists(ico)) pbLogo.Image = new Icon(ico, 42, 42).ToBitmap();
                else pbLogo.Visible = false;
            } catch { pbLogo.Visible = false; }
            var lblTitle = new Label { Text = "IT Support Tools", Font = new Font("Segoe UI", 14f, FontStyle.Bold), ForeColor = C_TEXT, AutoSize = true, Location = new Point(62, 8) };
            var lblSub   = new Label { Text = "v3.0  -  Administrator Mode", Font = new Font("Segoe UI", 7.5f), ForeColor = C_SUBTEXT, AutoSize = true, Location = new Point(62, 36) };
            lblClock     = new Label { Text = "", Font = new Font("Consolas", 11f, FontStyle.Bold), ForeColor = C_ACCENT, AutoSize = true };
            pnlHeader.Controls.AddRange(new Control[] { pbLogo, lblTitle, lblSub, lblClock });
            this.Controls.Add(pnlHeader);
        }

        void BuildSidebar()
        {
            pnlSidebar = new Panel { BackColor = C_SIDEBAR };
            pnlSidebar.Paint += (s, e) => {
                using (var p = new Pen(C_BORDER, 1))
                    e.Graphics.DrawLine(p, pnlSidebar.Width - 1, 0, pnlSidebar.Width - 1, pnlSidebar.Height);
            };
            var items = new[] {
                ("dashboard"," Dashboard"),
                ("drivers",  " Driver Suite"),
                ("software", " Phan Mem"),
                ("optimize", " Toi Uu He Thong"),
                ("network",  " Mang & Ket Noi"),
                ("sysinfo",  " Thong Tin He Thong"),
            };
            string[] icons = { "🏠","🖥","📦","🔧","🌐","ℹ" };
            int y = 14, i2 = 0;
            foreach (var (id, lbl) in items) {
                var btn = new NavBtn(icons[i2++], lbl, id) { Location = new Point(8, y) };
                btn.Click += (s, e) => ShowPage(((NavBtn)s).PageId);
                navBtns.Add(btn); pnlSidebar.Controls.Add(btn); y += 50;
            }
            lblSysName = SL("Thong tin may...", new Point(8, 2));
            lblSysOS   = SL("He dieu hanh...", new Point(8, 20));
            lblSysIP   = SL("IP: ...", new Point(8, 38));
            lblSysRAM  = SL("RAM: ...", new Point(8, 56));
            var pMini = new Panel { BackColor = Color.FromArgb(12,17,30), Size = new Size(SIDEBAR_W-2,78), Name="miniSys" };
            pMini.Controls.AddRange(new Control[]{ lblSysName, lblSysOS, lblSysIP, lblSysRAM });
            pnlSidebar.Controls.Add(pMini);
            this.Controls.Add(pnlSidebar);
        }

        Label SL(string t, Point p) => new Label { Text=t, ForeColor=C_SUBTEXT, Font=new Font("Segoe UI",7.5f), AutoSize=false, Width=SIDEBAR_W-18, Height=17, Location=p };

        void BuildContent() { pnlContent = new Panel { BackColor = C_PANEL }; this.Controls.Add(pnlContent); }

        void BuildFooter()
        {
            pnlFooter = new Panel { BackColor = C_LOG_BG };
            pnlFooter.Paint += (s, e) => { using (var p = new Pen(C_BORDER)) e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0); };
            pbMain  = new ProgressBar { Minimum=0, Maximum=100, Value=0, Style=ProgressBarStyle.Continuous, Height=4, Name="pb" };
            lblTask = new Label { Text="", ForeColor=C_SUBTEXT, Font=new Font("Segoe UI",7.5f), AutoSize=true, Name="lt" };
            var lh  = new Label { Text="  Console Log", Font=new Font("Segoe UI",8.5f,FontStyle.Bold), ForeColor=C_ACCENT, AutoSize=true, Name="lh" };
            var bc  = new Button { Text="Clear", BackColor=Color.FromArgb(40,55,80), ForeColor=Color.FromArgb(160,175,210), FlatStyle=FlatStyle.Flat, Font=new Font("Segoe UI",7.5f), Size=new Size(60,22), Cursor=Cursors.Hand, Name="bc" };
            bc.FlatAppearance.BorderSize = 0; bc.Click += (s,e) => rtbLog.Clear();
            rtbLog = new RichTextBox { ReadOnly=true, BackColor=C_LOG_BG, ForeColor=C_TEXT, Font=new Font("Consolas",8.5f), BorderStyle=BorderStyle.None, ScrollBars=RichTextBoxScrollBars.Vertical, DetectUrls=false };
            pnlFooter.Controls.AddRange(new Control[]{ pbMain, lblTask, lh, bc, rtbLog });
            this.Controls.Add(pnlFooter);
        }

        void DoLayout()
        {
            int W=this.ClientSize.Width, H=this.ClientSize.Height;
            pnlHeader.SetBounds(0,0,W,HEADER_H);
            if (lblClock != null) lblClock.Location = new Point(W-lblClock.Width-16, 20);
            pnlSidebar.SetBounds(0,HEADER_H,SIDEBAR_W,H-HEADER_H-FOOTER_H);
            var mini=pnlSidebar.Controls["miniSys"];
            if(mini!=null) mini.Location=new Point(0,pnlSidebar.Height-mini.Height-4);
            foreach(var nb in navBtns) nb.Width=SIDEBAR_W-16;
            pnlContent.SetBounds(SIDEBAR_W,HEADER_H,W-SIDEBAR_W,H-HEADER_H-FOOTER_H);
            foreach(var pg in pages.Values) pg.Size=pnlContent.ClientSize;
            pnlFooter.SetBounds(0,H-FOOTER_H,W,FOOTER_H);
            var pb_=pnlFooter.Controls["pb"] as ProgressBar;
            var lt_=pnlFooter.Controls["lt"] as Label;
            var bc_=pnlFooter.Controls["bc"] as Control;
            var lh_=pnlFooter.Controls["lh"] as Control;
            if(pb_!=null) pb_.SetBounds(0,0,pnlFooter.Width,4);
            if(lt_!=null) lt_.Location=new Point(pnlFooter.Width/2-100,6);
            if(bc_!=null) bc_.Location=new Point(pnlFooter.Width-bc_.Width-4,2);
            if(lh_!=null) lh_.Location=new Point(4,6);
            rtbLog.SetBounds(4,26,pnlFooter.Width-8,FOOTER_H-30);
        }

        void BuildAllPages()
        {
            pages["dashboard"]=BuildDashboard();
            pages["drivers"]  =BuildDriverPage();
            pages["software"] =BuildSoftwarePage();
            pages["optimize"] =BuildOptimizePage();
            pages["network"]  =BuildNetworkPage();
            pages["sysinfo"]  =BuildSysInfoPage();
            foreach(var pg in pages.Values){ pg.Visible=false; pg.Dock=DockStyle.Fill; pg.BackColor=C_PANEL; pnlContent.Controls.Add(pg); }
        }

        void ShowPage(string id)
        {
            if(!pages.ContainsKey(id)) return;
            foreach(var pg in pages.Values) pg.Visible=false;
            pages[id].Visible=true;
            foreach(var nb in navBtns) nb.SetActive(nb.PageId==id);
        }

        // DASHBOARD
        Panel BuildDashboard()
        {
            var pg=new Panel{AutoScroll=true};
            var flow=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoScroll=true,FlowDirection=FlowDirection.LeftToRight,WrapContents=true,Padding=new Padding(16)};
            var cards=new[]{
                ("🖥","Quet Driver\nWindows",     C_ACCENT, (Action)(()=>ShowPage("drivers"))),
                ("🟢","Cai Chrome\nSilent",        C_GREEN,  ()=>Task.Run(async()=>await InstallDirect("Chrome","https://dl.google.com/chrome/install/latest/chrome_installer.exe",Path.Combine(Path.GetTempPath(),"ChromeSetup.exe"),"/silent /install"))),
                ("🔑","Kiem Tra\nBan Quyen",       C_ACCENT2,()=>BtnActivation_Click(null,null)),
                ("🖨","Reset Print\nSpooler",      C_YELLOW, ()=>BtnSpooler_Click(null,null)),
                ("🌐","Flush DNS\nCache",           C_ACCENT, ()=>QuickFlushDns()),
                ("🗑","Xoa\nTemp Files",            C_RED,    ()=>BtnCleanTemp_Click(null,null)),
            };
            foreach(var(ico,lbl,clr,act) in cards){ var c2=new QuickCard(ico,lbl,clr); c2.Click+=(s,e2)=>act(); flow.Controls.Add(c2); }
            var pi=new Panel{Width=920,Height=130,Margin=new Padding(0,8,0,0)};
            pi.Paint+=(s,e)=>DrawCard(e.Graphics,pi.ClientRectangle,"Huong Dan Nhanh");
            var lg=new Label{Text="1. Tab 'Driver Suite' -> Quet thiet bi -> Cai driver bi loi truc tiep.\r\n2. Tab 'Phan Mem' -> Cai Chrome, EVKey, Office 365, 7-Zip, VLC mot click.\r\n3. Tab 'Toi Uu' -> Don temp, tat BitLocker, fix may in, SFC scan.\r\n4. Tab 'Mang' -> Ping test, flush DNS, reset TCP/IP, xem IP config.\r\n5. Tab 'He Thong' -> Xem CPU/RAM/Disk, kiem tra ban quyen Windows.",ForeColor=C_SUBTEXT,Font=new Font("Segoe UI",8.5f),Location=new Point(12,28),AutoSize=false,Width=890,Height=90};
            pi.Controls.Add(lg); flow.Controls.Add(pi); pg.Controls.Add(flow);
            return pg;
        }

        // DRIVER SUITE - BUILT-IN SCANNER
        Panel BuildDriverPage()
        {
            var pg=new Panel{AutoScroll=false};
            var lblT=new Label{Text="Driver Suite - Quet & Cai Driver (Built-in)",Font=new Font("Segoe UI",13f,FontStyle.Bold),ForeColor=C_TEXT,AutoSize=true,Location=new Point(16,14)};
            var line=new Panel{BackColor=C_ACCENT,Location=new Point(16,42),Height=2,Name="dl"};
            var flow=new FlowLayoutPanel{Location=new Point(16,54),Height=44,AutoSize=true,FlowDirection=FlowDirection.LeftToRight,WrapContents=false};
            var b1=MkBtn("Quet Driver",C_ACCENT);   b1.Click+=BtnScanDrivers_Click;
            var b2=MkBtn("Cai Driver Loi",C_RED);   b2.Click+=BtnFixDrivers_Click;
            var b3=MkBtn("Windows Update",C_GREEN);  b3.Click+=BtnWinUpdateDriver_Click;
            var b4=MkBtn("Xuat Bao Cao",C_ACCENT2); b4.Click+=BtnExportDrivers_Click;
            flow.Controls.AddRange(new Control[]{b1,b2,b3,b4});
            lblDriverStatus=new Label{Text="Nhan 'Quet Driver' de bat dau...",ForeColor=C_SUBTEXT,Font=new Font("Segoe UI",9f),AutoSize=true,Location=new Point(16,104)};
            lvDrivers=new ListView{Location=new Point(16,128),View=View.Details,FullRowSelect=true,GridLines=true,BackColor=Color.FromArgb(18,25,42),ForeColor=C_TEXT,Font=new Font("Segoe UI",8.5f),BorderStyle=BorderStyle.None,Name="lvd"};
            lvDrivers.Columns.Add("Thiet Bi",310); lvDrivers.Columns.Add("Trang Thai",120); lvDrivers.Columns.Add("Nha San Xuat",180); lvDrivers.Columns.Add("Driver Ver",140); lvDrivers.Columns.Add("Ngay Driver",110);
            pg.Controls.AddRange(new Control[]{lblT,line,flow,lblDriverStatus,lvDrivers});
            pg.Resize+=(s,e)=>{ line.Width=pg.Width-32; lvDrivers.Size=new Size(pg.Width-32,pg.Height-158); };
            return pg;
        }

        async void BtnScanDrivers_Click(object s, EventArgs e)
        {
            await RunGuarded("Quet Driver", async () => {
                SafeInvoke(()=>{ lvDrivers.Items.Clear(); lblDriverStatus.Text="Dang quet driver..."; lblDriverStatus.ForeColor=C_YELLOW; });
                Log("Quet thiet bi qua WMI Win32_PnPEntity + Win32_PnPSignedDriver...", C_YELLOW);
                var results=new List<DriverItem>();
                await Task.Run(()=>{
                    // Pass 1: Get all devices with error codes
                    using(var m=new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity"))
                        foreach(ManagementObject mo in m.Get())
                            try{
                                int err=Convert.ToInt32(mo["ConfigManagerErrorCode"]??0);
                                string name=mo["Name"]?.ToString()??"";
                                string mfr =mo["Manufacturer"]?.ToString()??"";
                                string cls =mo["PNPClass"]?.ToString()??"";
                                if(string.IsNullOrEmpty(name)||name.StartsWith("PCI Data")||cls=="System") continue;
                                results.Add(new DriverItem{Name=name,Mfr=mfr,Status=err==0?"OK":ErrDesc(err),HasErr=err!=0,PnpClass=cls});
                            }catch{}
                    // Pass 2: Enrich with driver version/date from signed drivers
                    using(var m=new ManagementObjectSearcher("SELECT * FROM Win32_PnPSignedDriver WHERE DeviceName IS NOT NULL"))
                        foreach(ManagementObject mo in m.Get())
                            try{
                                string dn=mo["DeviceName"]?.ToString()??"";
                                string ver=mo["DriverVersion"]?.ToString()??"";
                                string rawDate=mo["DriverDate"]?.ToString()??"";
                                string mfr=mo["Manufacturer"]?.ToString()??"";
                                string date="";
                                if(rawDate.Length>=8) try{date=ManagementDateTimeConverter.ToDateTime(rawDate).ToString("dd/MM/yyyy");}catch{}
                                var found=results.Find(r=>r.Name==dn);
                                if(found!=null){found.Ver=ver;found.Date=date;if(!string.IsNullOrEmpty(mfr))found.Mfr=mfr;}
                            }catch{}
                });
                int errCnt=0;
                SafeInvoke(()=>{
                    lvDrivers.Items.Clear();
                    results.Sort((a,b2)=>b2.HasErr.CompareTo(a.HasErr));
                    foreach(var r in results){
                        var item=new ListViewItem(r.Name);
                        item.SubItems.Add(r.Status); item.SubItems.Add(r.Mfr); item.SubItems.Add(r.Ver); item.SubItems.Add(r.Date);
                        item.ForeColor=r.HasErr?C_RED:(r.Status=="OK"?C_GREEN:C_TEXT);
                        item.BackColor=r.HasErr?Color.FromArgb(40,15,15):Color.FromArgb(18,25,42);
                        lvDrivers.Items.Add(item);
                        if(r.HasErr) errCnt++;
                    }
                    lblDriverStatus.Text=errCnt>0?$"Tim thay {errCnt} driver loi / {results.Count} tong - Nhan 'Cai Driver Loi' de sua":$"Tat ca {results.Count} driver hoat dong binh thuong";
                    lblDriverStatus.ForeColor=errCnt>0?C_RED:C_GREEN;
                });
                Log($"Quet xong: {results.Count} driver, {errCnt} loi.",errCnt>0?C_YELLOW:C_GREEN);
            });
        }

        async void BtnFixDrivers_Click(object s, EventArgs e)
        {
            await RunGuarded("Cai Driver Loi", async () => {
                Log("pnputil /scan-devices - Windows tu tim driver phu hop...", C_YELLOW);
                await RunProcAsync("pnputil.exe","/scan-devices");
                Log("Trigger Windows Update tim driver moi...", C_YELLOW);
                int code=await RunProcAsync("powershell.exe",
                    "-NonInteractive -NoProfile -Command \"$s=New-Object -ComObject Microsoft.Update.Session;$q=$s.CreateUpdateSearcher().Search(\\\"Type='Driver' AND IsInstalled=0\\\");Write-Host \\\"Found: $($q.Updates.Count) driver(s)\\\";if($q.Updates.Count -gt 0){$c=New-Object -ComObject Microsoft.Update.UpdateColl;foreach($u in $q.Updates){$c.Add($u)|Out-Null};$i=$s.CreateUpdateInstaller();$i.Updates=$c;$r=$i.Install();Write-Host \\\"Result:$($r.ResultCode)\\\"}\"");
                Log(code==0?"Driver da duoc cap nhat qua Windows Update.":"Hoan thanh voi code: "+code,C_GREEN);
                await Task.Delay(1000);
                BtnScanDrivers_Click(null,null);
            });
        }

        async void BtnWinUpdateDriver_Click(object s, EventArgs e)
        {
            await RunGuarded("Windows Update",async()=>{
                await Task.Run(()=>Process.Start("ms-settings:windowsupdate-optionalupdates"));
                Log("Da mo Windows Update -> Optional Updates (Driver updates).",C_GREEN);
            });
        }

        async void BtnExportDrivers_Click(object s, EventArgs e)
        {
            await RunGuarded("Xuat Bao Cao Driver",async()=>{
                string path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),"DriverReport.txt");
                var sb=new StringBuilder($"=== DRIVER REPORT - {DateTime.Now} ===\r\n\r\n");
                string pnp=await CaptureAsync("pnputil.exe","/enum-drivers");
                sb.AppendLine(pnp);
                if(lvDrivers.Items.Count>0){
                    sb.AppendLine("\r\n=== DEVICE STATUS ===");
                    foreach(ListViewItem item in lvDrivers.Items)
                        sb.AppendLine($"[{item.SubItems[1].Text}] {item.Text} | {item.SubItems[2].Text} | v{item.SubItems[3].Text} | {item.SubItems[4].Text}");
                }
                File.WriteAllText(path,sb.ToString(),Encoding.UTF8);
                Log($"Bao cao luu tai: {path}",C_GREEN);
                Process.Start(new ProcessStartInfo("notepad.exe",path){UseShellExecute=true});
            });
        }

        static string ErrDesc(int code){ switch(code){case 1:return "Loi cau hinh";case 3:return "Driver hong";case 10:return "Khong khoi dong";case 12:return "Xung dot tai nguyen";case 14:return "Can restart";case 18:return "Can cai lai";case 22:return "Bi tat";case 28:return "Khong co driver";case 31:return "Khong hoat dong";case 43:return "Thiet bi loi";default:return $"Loi #{code}";} }
        class DriverItem { public string Name="",Mfr="",Ver="",Date="",Status="",PnpClass=""; public bool HasErr; }

        // SOFTWARE — DIRECT DOWNLOAD, KHONG CAN WINGET
        Panel BuildSoftwarePage()
        {
            var pg=PageScroll("📦  Phan Mem — Cai Dat Truc Tiep (Khong can Winget)");
            var flow=GetPageFlow(pg);

            var g1=AddGrp(flow,"Google Chrome","Tai truc tiep tu dl.google.com, cai ngam hoan toan. Khong can Winget.");
            AddBtn(AddRow(g1),"⬇  Cai Google Chrome",C_GREEN,async(s,e)=>
                await InstallDirect("Chrome","https://dl.google.com/chrome/install/latest/chrome_installer.exe",
                    Path.Combine(Path.GetTempPath(),"ChromeSetup.exe"),"/silent /install"));

            var g2=AddGrp(flow,"Mozilla Firefox","Tai tu Mozilla chinh thuc, cai ngam. Trinh duyet bao mat cao.");
            AddBtn(AddRow(g2),"⬇  Cai Mozilla Firefox",Color.FromArgb(220,80,0),async(s,e)=>
                await InstallDirect("Firefox","https://download.mozilla.org/?product=firefox-latest&os=win64&lang=vi",
                    Path.Combine(Path.GetTempPath(),"FirefoxSetup.exe"),"-ms"));

            var g3=AddGrp(flow,"7-Zip","Giai nen manh me 100+ dinh dang. Tai tu 7-zip.org chinh thuc, cai ngam.");
            AddBtn(AddRow(g3),"⬇  Cai 7-Zip",Color.FromArgb(0,150,110),async(s,e)=>
                await InstallDirect("7-Zip","https://7-zip.org/a/7z2301-x64.exe",
                    Path.Combine(Path.GetTempPath(),"7zSetup.exe"),"/S"));

            var g4=AddGrp(flow,"VLC Media Player","Phat video/audio moi dinh dang. Tai tu videolan.org chinh thuc.");
            AddBtn(AddRow(g4),"⬇  Cai VLC",Color.FromArgb(220,80,0),async(s,e)=>
                await InstallDirect("VLC","https://download.videolan.org/pub/videolan/vlc/last/win64/vlc-3.0.20-win64.exe",
                    Path.Combine(Path.GetTempPath(),"VLCSetup.exe"),"/S /L=1066"));

            var g5=AddGrp(flow,"Notepad++ 8.6","Trinh soan thao code nhe va manh. Tai tu GitHub release chinh thuc.");
            AddBtn(AddRow(g5),"⬇  Cai Notepad++",Color.FromArgb(0,130,200),async(s,e)=>
                await InstallDirect("Notepad++","https://github.com/notepad-plus-plus/notepad-plus-plus/releases/download/v8.6/npp.8.6.Installer.x64.exe",
                    Path.Combine(Path.GetTempPath(),"NppSetup.exe"),"/S"));

            var g6=AddGrp(flow,"Zoom Meetings","Hop truc tuyen pho bien. Tai truc tiep tu zoom.us chinh thuc.");
            AddBtn(AddRow(g6),"⬇  Cai Zoom",Color.FromArgb(45,140,255),async(s,e)=>
                await InstallDirect("Zoom","https://zoom.us/client/latest/ZoomInstallerFull.exe",
                    Path.Combine(Path.GetTempPath(),"ZoomSetup.exe"),"/quiet /norestart"));

            var g7=AddGrp(flow,"WinRAR 7.01","Giai nen RAR/ZIP/7Z. Tai truc tiep tu win-rar.com chinh thuc.");
            AddBtn(AddRow(g7),"⬇  Cai WinRAR",Color.FromArgb(140,80,20),async(s,e)=>
                await InstallDirect("WinRAR","https://www.win-rar.com/fileadmin/winrar-versions/winrar/winrar-x64-701.exe",
                    Path.Combine(Path.GetTempPath(),"WinRARSetup.exe"),"/S"));

            var ge=AddGrp(flow,"EVKey — Go Tieng Viet","Tai phien ban moi nhat tu GitHub, giai nen + tao shortcut Desktop.");
            AddBtn(AddRow(ge),"⬇  Cai EVKey",C_ACCENT,BtnEVKey_Click);

            var guk=AddGrp(flow,"UniKey — Go Tieng Viet","Tai tu unikey.org chinh thuc, giai nen + tao shortcut Desktop.");
            AddBtn(AddRow(guk),"⬇  Cai UniKey",Color.FromArgb(0,100,180),BtnUniKey_Click);

            var go=AddGrp(flow,"Microsoft Office 365 ProPlus","Tu tai ODT tu Microsoft, tao config.xml (Word+Excel+PPT, VI+EN), cai ngam ~4GB.");
            AddBtn(AddRow(go),"⬇  Deploy Office 365",C_ACCENT2,BtnOffice_Click);

            return pg;
        }

        // ENGINE CAI PHAN MEM — TAI + CHAY INSTALLER TRUC TIEP
        async Task InstallDirect(string name, string url, string localPath, string args)
        {
            await RunGuarded($"Cai {name}", async () => {
                Log($"Dang tai {name}...", C_YELLOW);
                Log($"  URL: {url}", C_SUBTEXT);
                await DownloadAsync(url, localPath);
                Log($"Dang cai {name} (installer dang chay nen)...", C_YELLOW);
                int code = await Task.Run(() => {
                    using (var p = new Process()) {
                        p.StartInfo = new ProcessStartInfo {
                            FileName        = localPath,
                            Arguments       = args,
                            UseShellExecute = true,
                            Verb            = "runas",
                            WindowStyle     = ProcessWindowStyle.Minimized
                        };
                        p.Start(); p.WaitForExit();
                        return p.ExitCode;
                    }
                });
                try { File.Delete(localPath); } catch { }
                if (code == 0 || code == 3010)
                    Log($"✔ {name} da cai xong!" + (code==3010?" (Nen restart may de hoan tat)":""), C_GREEN);
                else if (code == 1638 || code == 1602)
                    Log($"⚠ {name} da duoc cai roi (code {code}).", C_YELLOW);
                else
                    Log($"⚠ Installer tra ve {code} — {name} co the da cai xong hoac can chay tay.", C_YELLOW);
            });
        }

        async void BtnEVKey_Click(object s, EventArgs e)
        {
            await RunGuarded("Cai EVKey",async()=>{
                Log("Query GitHub API cho EVKey moi nhat...",C_YELLOW);
                string json=await GetStrAsync("https://api.github.com/repos/lamquangminh/EVKey/releases/latest");
                string zipUrl=ParseGhAsset(json,".zip");
                if(string.IsNullOrEmpty(zipUrl)) throw new Exception("Khong tim thay .zip trong EVKey release.");
                string tmp=Path.Combine(Path.GetTempPath(),"EVKey_latest.zip");
                await DownloadAsync(zipUrl,tmp);
                const string dest=@"C:\Program Files\EVKey";
                if(Directory.Exists(dest)) Directory.Delete(dest,true);
                Directory.CreateDirectory(dest);
                await Task.Run(()=>ZipFile.ExtractToDirectory(tmp,dest));
                File.Delete(tmp);
                string[] exes=Directory.GetFiles(dest,"EVKey*.exe",SearchOption.AllDirectories);
                string exePath=exes.Length>0?exes[0]:Path.Combine(dest,"EVKey.exe");
                await Task.Run(()=>CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),"EVKey.lnk"),exePath,dest,"EVKey - Go Tieng Viet"));
                Log("EVKey cai xong + shortcut Desktop.",C_GREEN);
            });
        }

        async void BtnUniKey_Click(object s, EventArgs e)
        {
            await RunGuarded("Cai UniKey",async()=>{
                const string url="https://www.unikey.org/assets/unikey/unikey4.3RC5-140925-win64.zip";
                const string dest=@"C:\Program Files\UniKey";
                string tmp=Path.Combine(Path.GetTempPath(),"UniKey.zip");
                await DownloadAsync(url,tmp);
                if(Directory.Exists(dest)) Directory.Delete(dest,true);
                Directory.CreateDirectory(dest);
                await Task.Run(()=>ZipFile.ExtractToDirectory(tmp,dest));
                File.Delete(tmp);
                string[] exes=Directory.GetFiles(dest,"*.exe",SearchOption.AllDirectories);
                if(exes.Length==0) throw new Exception("Khong tim thay EXE UniKey.");
                await Task.Run(()=>CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),"UniKey.lnk"),exes[0],dest,"UniKey - Go Tieng Viet"));
                Log("UniKey cai xong + shortcut Desktop.",C_GREEN);
            });
        }

        async void BtnOffice_Click(object s, EventArgs e)
        {
            await RunGuarded("Deploy Office 365",async()=>{
                const string odtUrl="https://download.microsoft.com/download/2/7/A/27AF1BE6-DD20-4CB4-B154-EBAB8A7D4A7E/officedeploymenttool_18129-20030.exe";
                const string dir=@"C:\IT_Tools\ODT";
                Directory.CreateDirectory(dir);
                string odtExe=Path.Combine(dir,"ODTSetup.exe");
                Log("Tai Office Deployment Tool...",C_YELLOW);
                await DownloadAsync(odtUrl,odtExe);
                await RunProcAsync(odtExe,$"/quiet /extract:\"{dir}\"");
                string cfg=Path.Combine(dir,"configuration.xml");
                File.WriteAllText(cfg,"<Configuration><Add OfficeClientEdition=\"64\" Channel=\"Current\"><Product ID=\"O365ProPlusRetail\"><Language ID=\"vi-vn\"/><Language ID=\"en-us\"/><ExcludeApp ID=\"Access\"/><ExcludeApp ID=\"Groove\"/><ExcludeApp ID=\"Lync\"/><ExcludeApp ID=\"OneDrive\"/><ExcludeApp ID=\"OneNote\"/><ExcludeApp ID=\"Outlook\"/><ExcludeApp ID=\"Publisher\"/><ExcludeApp ID=\"Teams\"/></Product></Add><Display Level=\"None\" AcceptEULA=\"TRUE\"/><Property Name=\"AUTOACTIVATE\" Value=\"1\"/></Configuration>",Encoding.UTF8);
                Log("configuration.xml da tao (Word+Excel+PPT, VI+EN).",C_GREEN);
                string setup=Path.Combine(dir,"setup.exe");
                if(!File.Exists(setup)) throw new FileNotFoundException("setup.exe khong tim thay.",setup);
                Log("Cai Office 365 - tai ~4GB, co the mat 20-60 phut...",C_YELLOW);
                int code=await RunProcAsync(setup,$"/configure \"{cfg}\"");
                Log(code==0?"Office 365 ProPlus cai xong!":$"setup.exe tra ve {code}. Xem log: %temp%\\Microsoft Office\\",code==0?C_GREEN:C_YELLOW);
            });
        }

        // OPTIMIZE
        Panel BuildOptimizePage()
        {
            var pg=PageScroll("Toi Uu He Thong");
            var flow=GetPageFlow(pg);
            var g1=AddGrp(flow,"BitLocker - Tat Ma Hoa O C:","Tat BitLocker, qua trinh giai ma chay nen.");
            AddBtn(AddRow(g1),"Tat BitLocker C:",C_RED,BtnBitLocker_Click);
            var g2=AddGrp(flow,"Print Spooler - Fix Loi May In","Dung->Xoa job ket->Khoi dong lai. Giai quyet 99% loi may in.");
            AddBtn(AddRow(g2),"Reset Print Spooler",C_YELLOW,BtnSpooler_Click);
            var g3=AddGrp(flow,"Don File Rac - Temp Cleaner","Xoa %TEMP%, Windows\\Temp, Prefetch.");
            AddBtn(AddRow(g3),"Xoa Temp Files",C_RED,BtnCleanTemp_Click);
            var g4=AddGrp(flow,"System File Checker (SFC)","Quet va sua file he thong bi loi/thieu. Mat 10-15 phut.");
            AddBtn(AddRow(g4),"Chay SFC /scannow",Color.FromArgb(0,160,100),async(s,e)=>await RunGuarded("SFC",async()=>{
                Log("sfc /scannow - cho 10-15 phut...",C_YELLOW);
                int c=await RunProcAsync("sfc.exe","/scannow");
                Log(c==0?"SFC hoan thanh.":"SFC tra ve "+c+" - xem C:\\Windows\\Logs\\CBS\\CBS.log",c==0?C_GREEN:C_YELLOW);
            }));
            var g5=AddGrp(flow,"DISM - Repair Windows Image","Sua Windows image bi hong. Chay sau SFC neu bao loi.");
            AddBtn(AddRow(g5),"DISM /RestoreHealth",Color.FromArgb(100,60,200),async(s,e)=>await RunGuarded("DISM",async()=>{
                Log("DISM /RestoreHealth - cho 15-30 phut...",C_YELLOW);
                int c=await RunProcAsync("dism.exe","/Online /Cleanup-Image /RestoreHealth");
                Log(c==0?"DISM hoan thanh, Windows image da sua.":"DISM tra ve "+c,c==0?C_GREEN:C_YELLOW);
            }));
            var g6=AddGrp(flow,"Disk Cleanup","Mo Disk Cleanup o C: - xoa file he thong, WinSxS cu.");
            AddBtn(AddRow(g6),"Mo Disk Cleanup",Color.FromArgb(0,140,190),async(s,e)=>await RunGuarded("Disk Cleanup",async()=>{
                await Task.Run(()=>Process.Start(new ProcessStartInfo("cleanmgr.exe","/d C:"){UseShellExecute=true}));
                Log("Disk Cleanup da mo.",C_GREEN);
            }));
            var g7=AddGrp(flow,"Windows Update","Mo Windows Update kiem tra ban va moi nhat.");
            AddBtn(AddRow(g7),"Mo Windows Update",C_ACCENT,async(s,e)=>await RunGuarded("WinUpdate",async()=>{
                await Task.Run(()=>Process.Start("ms-settings:windowsupdate"));
                Log("Da mo Windows Update.",C_GREEN);
            }));
            return pg;
        }

        async void BtnBitLocker_Click(object s, EventArgs e)
        {
            if(MessageBox.Show("Tat BitLocker tren C:?\nQua trinh giai ma chay nen, may van dung binh thuong.","Xac Nhan",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes) return;
            await RunGuarded("Tat BitLocker",async()=>{
                int c=await RunProcAsync("powershell.exe","-NonInteractive -NoProfile -Command \"Disable-BitLocker -MountPoint 'C:'\"");
                Log(c==0?"Giai ma BitLocker bat dau. Dung 'manage-bde -status C:' theo doi.":"Loi: "+c+" - BitLocker co the chua bat.",c==0?C_GREEN:C_YELLOW);
            });
        }

        async void BtnSpooler_Click(object s, EventArgs e)
        {
            await RunGuarded("Reset Print Spooler",async()=>{
                await RunProcAsync("net","stop spooler");
                await Task.Run(()=>{ const string d=@"C:\Windows\System32\spool\PRINTERS"; if(Directory.Exists(d)) foreach(string f in Directory.GetFiles(d)) try{File.Delete(f);}catch{} });
                Log("Da xoa print jobs ket.",C_GREEN);
                int c=await RunProcAsync("net","start spooler");
                Log(c==0?"Print Spooler reset thanh cong!":"Loi: "+c,c==0?C_GREEN:C_RED);
            });
        }

        async void BtnCleanTemp_Click(object s, EventArgs e)
        {
            await RunGuarded("Xoa Temp Files",async()=>{
                long total=0;
                var dirs=new[]{Environment.GetEnvironmentVariable("TEMP"),@"C:\Windows\Temp",@"C:\Windows\Prefetch"};
                await Task.Run(()=>{ foreach(string d in dirs){ if(!Directory.Exists(d)) continue; foreach(string f in Directory.GetFiles(d,"*",SearchOption.TopDirectoryOnly)) try{var fi=new FileInfo(f);total+=fi.Length;File.Delete(f);}catch{} foreach(string sub in Directory.GetDirectories(d)) try{Directory.Delete(sub,true);}catch{} } });
                Log($"Da don {FormatBytes(total)} - Temp, Windows\\Temp, Prefetch.",C_GREEN);
            });
        }

        // NETWORK
        Panel BuildNetworkPage()
        {
            var pg=PageScroll("Mang & Ket Noi");
            var flow=GetPageFlow(pg);
            var g1=AddGrp(flow,"Flush DNS Cache","Xoa cache DNS - giai quyet loi khong vao duoc web.");
            AddBtn(AddRow(g1),"Flush DNS",C_ACCENT,async(s,e)=>await QuickFlushDns());
            var g2=AddGrp(flow,"Reset TCP/IP & Winsock","Dat lai mang TCP/IP + Winsock ve mac dinh. Can restart sau.");
            AddBtn(AddRow(g2),"Reset TCP/IP + Winsock",C_RED,async(s,e)=>{
                if(MessageBox.Show("Reset TCP/IP va Winsock?\nMay can RESTART sau.","Xac Nhan",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes) return;
                await RunGuarded("Reset Network",async()=>{ await RunProcAsync("netsh","int ip reset"); await RunProcAsync("netsh","winsock reset"); Log("TCP/IP & Winsock da reset. Vui long RESTART may.",C_GREEN); });
            });
            var g3=AddGrp(flow,"Ping Test - Kiem Tra Internet","Ping Google DNS, Cloudflare, Google.com, Facebook.");
            AddBtn(AddRow(g3),"Chay Ping Test",C_GREEN,async(s,e)=>await RunGuarded("Ping Test",async()=>{
                var hosts=new[]{("Google DNS","8.8.8.8"),("Cloudflare","1.1.1.1"),("Google.com","google.com"),("Facebook","facebook.com")};
                foreach(var(name,host) in hosts){ using(var p=new Ping()){ try{ var r=await Task.Run(()=>p.Send(host,2000)); Log(r.Status==IPStatus.Success?$"  OK  {name,-15} ({host,-15}) - {r.RoundtripTime}ms":$"  FAIL {name,-15} ({host,-15}) - {r.Status}",r.Status==IPStatus.Success?C_GREEN:C_RED); }catch(Exception ex){Log($"  ERR {name} - {ex.Message}",C_RED);} } }
            }));
            var g4=AddGrp(flow,"IP Configuration","Hien thi IP, Gateway, DNS, MAC address.");
            AddBtn(AddRow(g4),"Xem IP Config /all",C_ACCENT,async(s,e)=>await RunGuarded("IP Config",async()=>{ string o=await CaptureAsync("ipconfig.exe","/all"); Log("-- IP Configuration --",C_ACCENT); Log(o,C_TEXT); }));
            var g5=AddGrp(flow,"Release & Renew IP","Tra IP hien tai va xin IP moi tu DHCP server.");
            AddBtn(AddRow(g5),"Release & Renew IP",C_YELLOW,async(s,e)=>await RunGuarded("Renew IP",async()=>{ await RunProcAsync("ipconfig.exe","/release"); await RunProcAsync("ipconfig.exe","/renew"); Log("IP moi da duoc cap tu DHCP.",C_GREEN); }));
            var g6=AddGrp(flow,"Speed Test - Kiem Tra Toc Do","Mo SpeedTest.net tren trinh duyet.");
            AddBtn(AddRow(g6),"Mo SpeedTest.net",Color.FromArgb(0,180,140),async(s,e)=>await RunGuarded("SpeedTest",async()=>{ await Task.Run(()=>Process.Start(new ProcessStartInfo("https://www.speedtest.net"){UseShellExecute=true})); Log("Da mo SpeedTest.net.",C_GREEN); }));
            return pg;
        }

        async Task QuickFlushDns() { await RunGuarded("Flush DNS",async()=>{ await RunProcAsync("ipconfig.exe","/flushdns"); Log("DNS cache da xoa sach.",C_GREEN); }); }

        // SYSTEM INFO
        Panel BuildSysInfoPage()
        {
            var pg=PageScroll("Thong Tin He Thong");
            var flow=GetPageFlow(pg);
            var g1=AddGrp(flow,"Kich Hoat Windows","Kiem tra trang thai ban quyen Windows.");
            AddBtn(AddRow(g1),"Kiem Tra Kich Hoat",Color.FromArgb(80,60,200),BtnActivation_Click);
            var g2=AddGrp(flow,"Thong So CPU / RAM / GPU / Mainboard","Hien thi thong so phan cung qua WMI.");
            AddBtn(AddRow(g2),"Xem Thong So Phan Cung",C_ACCENT,BtnHWInfo_Click);
            var g3=AddGrp(flow,"Dung Luong Tat Ca O Dia","Hien dung luong da dung/con trong kem progress bar.");
            AddBtn(AddRow(g3),"Xem Dung Luong O Dia",C_GREEN,BtnDisk_Click);
            var g4=AddGrp(flow,"Task Manager & Startup","Mo Task Manager de quan ly tien trinh va startup.");
            AddBtn(AddRow(g4),"Mo Task Manager",C_YELLOW,async(s,e)=>await RunGuarded("Task Manager",async()=>{ await Task.Run(()=>Process.Start(new ProcessStartInfo("taskmgr.exe"){UseShellExecute=true})); Log("Task Manager da mo.",C_GREEN); }));
            var g5=AddGrp(flow,"Event Viewer - Log He Thong","Xem log loi BSOD, crash, hardware failures.");
            AddBtn(AddRow(g5),"Mo Event Viewer",C_ACCENT2,async(s,e)=>await RunGuarded("Event Viewer",async()=>{ await Task.Run(()=>Process.Start(new ProcessStartInfo("eventvwr.msc"){UseShellExecute=true})); Log("Event Viewer da mo.",C_GREEN); }));
            var g6=AddGrp(flow,"msinfo32 - System Information","Xem toan bo thong tin he thong chi tiet.");
            AddBtn(AddRow(g6),"Mo System Information",C_ACCENT,async(s,e)=>await RunGuarded("msinfo32",async()=>{ await Task.Run(()=>Process.Start(new ProcessStartInfo("msinfo32.exe"){UseShellExecute=true})); Log("System Information da mo.",C_GREEN); }));
            return pg;
        }

        async void BtnActivation_Click(object s, EventArgs e)
        {
            await RunGuarded("Kiem Tra Kich Hoat",async()=>{
                string r=await CaptureAsync("cscript.exe",@"//Nologo C:\Windows\System32\slmgr.vbs /dli");
                Log("-- Trang Thai Kich Hoat Windows --",C_ACCENT); Log(r,C_TEXT);
            });
        }

        async void BtnHWInfo_Click(object s, EventArgs e)
        {
            await RunGuarded("Thong So Phan Cung",async()=>{
                var sb=new StringBuilder();
                await Task.Run(()=>{
                    sb.AppendLine("-- CPU --");
                    using(var m=new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
                        foreach(ManagementObject mo in m.Get())
                            sb.AppendLine($"  {mo["Name"]}  |  Cores: {mo["NumberOfCores"]}  |  Threads: {mo["ThreadCount"]}  |  {mo["MaxClockSpeed"]} MHz");
                    sb.AppendLine("\n-- RAM --");
                    using(var m=new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory"))
                        foreach(ManagementObject mo in m.Get())
                            sb.AppendLine($"  Slot: {mo["DeviceLocator"]}  |  {FormatBytes(Convert.ToInt64(mo["Capacity"]))}  |  {mo["Speed"]} MHz");
                    sb.AppendLine("\n-- GPU --");
                    using(var m=new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
                        foreach(ManagementObject mo in m.Get())
                            sb.AppendLine($"  {mo["Name"]}  |  RAM: {FormatBytes(Convert.ToInt64(mo["AdapterRAM"]??0L))}  |  {mo["CurrentHorizontalResolution"]}x{mo["CurrentVerticalResolution"]}");
                    sb.AppendLine("\n-- Mainboard --");
                    using(var m=new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard"))
                        foreach(ManagementObject mo in m.Get())
                            sb.AppendLine($"  {mo["Manufacturer"]} {mo["Product"]}  |  S/N: {mo["SerialNumber"]}");
                    sb.AppendLine("\n-- BIOS --");
                    using(var m=new ManagementObjectSearcher("SELECT * FROM Win32_BIOS"))
                        foreach(ManagementObject mo in m.Get())
                            sb.AppendLine($"  {mo["Manufacturer"]}  |  Ver: {mo["SMBIOSBIOSVersion"]}  |  S/N: {mo["SerialNumber"]}");
                    sb.AppendLine("\n-- O Dia --");
                    using(var m=new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive"))
                        foreach(ManagementObject mo in m.Get())
                            sb.AppendLine($"  {mo["Model"]}  |  {FormatBytes(Convert.ToInt64(mo["Size"]??0L))}  |  {mo["MediaType"]}");
                });
                Log("-- Thong So Phan Cung Day Du --",C_ACCENT); Log(sb.ToString(),C_TEXT);
            });
        }

        async void BtnDisk_Click(object s, EventArgs e)
        {
            await RunGuarded("Dung Luong O Dia",async()=>{
                await Task.Run(()=>{
                    Log("-- Dung Luong O Dia --",C_ACCENT);
                    foreach(var d in DriveInfo.GetDrives()){
                        if(!d.IsReady) continue;
                        double pct=(double)(d.TotalSize-d.AvailableFreeSpace)/d.TotalSize*100;
                        string bar=new string((char)9608,(int)(pct/5))+new string((char)9617,20-(int)(pct/5));
                        Log($"  {d.Name}  [{bar}] {pct:F1}%  Da dung: {FormatBytes(d.TotalSize-d.AvailableFreeSpace)} / {FormatBytes(d.TotalSize)}",pct>90?C_RED:pct>70?C_YELLOW:C_GREEN);
                    }
                });
            });
        }

        // SIDEBAR SYS INFO
        async void LoadSysInfoAsync()
        {
            await Task.Run(()=>{
                try{
                    string pc=Environment.MachineName,os="",ram="",ip="";
                    using(var m=new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem")) foreach(ManagementObject mo in m.Get()) os=mo["Caption"]?.ToString()?.Replace("Microsoft ","")??"";;
                    using(var m=new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem")) foreach(ManagementObject mo in m.Get()) ram=FormatBytes(Convert.ToInt64(mo["TotalVisibleMemorySize"])*1024)+" RAM";
                    foreach(var ni in NetworkInterface.GetAllNetworkInterfaces()){ if(ni.OperationalStatus!=OperationalStatus.Up||ni.NetworkInterfaceType==NetworkInterfaceType.Loopback) continue; foreach(var ua in ni.GetIPProperties().UnicastAddresses) if(ua.Address.AddressFamily==System.Net.Sockets.AddressFamily.InterNetwork){ip=ua.Address.ToString();break;} if(!string.IsNullOrEmpty(ip)) break; }
                    SafeInvoke(()=>{ lblSysName.Text=$"PC: {pc}"; lblSysOS.Text=$"OS: {(os.Length>22?os.Substring(0,22)+"...":os)}"; lblSysIP.Text=$"IP: {(string.IsNullOrEmpty(ip)?"No network":ip)}"; lblSysRAM.Text=$"RAM: {ram}"; });
                }catch{}
            });
        }

        void StartClock()
        {
            tmrClock=new System.Windows.Forms.Timer{Interval=1000};
            tmrClock.Tick+=(s,e)=>{ lblClock.Text=DateTime.Now.ToString("HH:mm:ss  dd/MM/yyyy"); SafeInvoke(()=>lblClock.Location=new Point(this.ClientSize.Width-lblClock.Width-16,20)); };
            tmrClock.Start();
        }

        // UI HELPERS
        Panel PageScroll(string title)
        {
            var pg=new Panel{AutoScroll=true};
            var lbl=new Label{Text=title,Font=new Font("Segoe UI",13f,FontStyle.Bold),ForeColor=C_TEXT,AutoSize=true,Location=new Point(16,14)};
            var line=new Panel{BackColor=C_ACCENT,Location=new Point(16,42),Height=2,Name="ptl"};
            pg.Controls.AddRange(new Control[]{lbl,line});
            pg.Resize+=(s,e)=>{ var l=pg.Controls["ptl"]; if(l!=null) l.Width=pg.Width-32; };
            return pg;
        }

        FlowLayoutPanel GetPageFlow(Panel pg)
        {
            var f=new FlowLayoutPanel{Location=new Point(0,56),AutoSize=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=false,Name="mf"};
            pg.Controls.Add(f);
            pg.Resize+=(s,e)=>{ var fl=pg.Controls["mf"] as FlowLayoutPanel; if(fl!=null) fl.Width=pg.Width; };
            return f;
        }

        GroupBox AddGrp(FlowLayoutPanel parent, string title, string desc)
        {
            var g=new GroupBox{Text=title,Font=new Font("Segoe UI",9f,FontStyle.Bold),ForeColor=C_ACCENT,BackColor=C_CARD,Margin=new Padding(16,8,16,0),Width=parent.Width-40,Height=88};
            g.Paint+=(s,e)=>{ using(var p=new Pen(C_BORDER)) e.Graphics.DrawRectangle(p,0,0,g.Width-1,g.Height-1); };
            var lb=new Label{Text=desc,ForeColor=C_SUBTEXT,Font=new Font("Segoe UI",8f),AutoSize=false,Location=new Point(10,20),Width=g.Width-20,Height=18};
            g.Controls.Add(lb);
            parent.Controls.Add(g);
            parent.Resize+=(s,e)=>{ g.Width=parent.Width-40; lb.Width=g.Width-20; };
            return g;
        }

        FlowLayoutPanel AddRow(GroupBox g)
        {
            var f=new FlowLayoutPanel{FlowDirection=FlowDirection.LeftToRight,AutoSize=true,Location=new Point(10,44),WrapContents=false};
            g.Controls.Add(f); g.Height=90;
            return f;
        }

        void AddBtn(FlowLayoutPanel row, string text, Color color, EventHandler handler)
        {
            var btn=new Button{Text=text,BackColor=color,ForeColor=Color.White,FlatStyle=FlatStyle.Flat,Font=new Font("Segoe UI",9f,FontStyle.Bold),Size=new Size(220,34),Cursor=Cursors.Hand,Margin=new Padding(0,0,8,0)};
            btn.FlatAppearance.BorderSize=0; btn.Click+=handler;
            btn.MouseEnter+=(s,e)=>btn.BackColor=ControlPaint.Light(color,0.15f);
            btn.MouseLeave+=(s,e)=>btn.BackColor=color;
            row.Controls.Add(btn);
        }

        static Button MkBtn(string text, Color color)
        {
            var b=new Button{Text=text,BackColor=color,ForeColor=Color.White,FlatStyle=FlatStyle.Flat,Font=new Font("Segoe UI",9f,FontStyle.Bold),Size=new Size(180,34),Cursor=Cursors.Hand,Margin=new Padding(0,0,8,0)};
            b.FlatAppearance.BorderSize=0;
            b.MouseEnter+=(s,e)=>b.BackColor=ControlPaint.Light(color,0.15f);
            b.MouseLeave+=(s,e)=>b.BackColor=color;
            return b;
        }

        static void DrawCard(Graphics g, Rectangle r, string title)
        {
            g.SmoothingMode=SmoothingMode.AntiAlias;
            using(var b=new SolidBrush(Color.FromArgb(26,35,58))) g.FillRectangle(b,r);
            using(var p=new Pen(Color.FromArgb(40,55,85))) g.DrawRectangle(p,r.X,r.Y,r.Width-1,r.Height-1);
            using(var f=new Font("Segoe UI",9f,FontStyle.Bold)) using(var b=new SolidBrush(Color.FromArgb(56,139,253))) g.DrawString(title,f,b,new PointF(12,8));
        }

        // ASYNC INFRA
        async Task RunGuarded(string name, Func<Task> action)
        {
            Log($"\n[{DateTime.Now:HH:mm:ss}] {name} bat dau...",C_ACCENT);
            SetProg(0,name+"...");
            try{ await action(); SetProg(100,name+" xong."); Log($"[{DateTime.Now:HH:mm:ss}] {name} hoan thanh.\n",C_GREEN); }
            catch(Exception ex){ SetProg(0,""); Log($"[{DateTime.Now:HH:mm:ss}] LOI {name}: {ex.Message}\n",C_RED); MessageBox.Show($"Loi trong '{name}':\r\n\r\n{ex.Message}","Loi",MessageBoxButtons.OK,MessageBoxIcon.Error); }
        }

        async Task DownloadAsync(string url, string dest)
        {
            using(var wc=new WebClient()){
                wc.Headers["User-Agent"]="ITSupportTools/3.0";
                wc.DownloadProgressChanged+=(s,e)=>SafeInvoke(()=>SetProg(e.ProgressPercentage,$"Dang tai... {e.ProgressPercentage}%  ({e.BytesReceived/1024:N0} KB)"));
                await wc.DownloadFileTaskAsync(new Uri(url),dest);
            }
            SetProg(100,"Tai xong."); Log($"  Da luu: {dest}",C_GREEN);
        }

        async Task<string> GetStrAsync(string url)
        {
            using(var wc=new WebClient()){ wc.Headers["User-Agent"]="ITSupportTools/3.0"; return await wc.DownloadStringTaskAsync(url); }
        }

        Task<int> RunProcAsync(string exe, string args)
        {
            return Task.Run(()=>{
                using(var p=new Process()){
                    p.StartInfo=new ProcessStartInfo{FileName=exe,Arguments=args,UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
                    p.OutputDataReceived+=(s,e)=>{if(e.Data!=null) SafeInvoke(()=>Log("  "+e.Data,Color.FromArgb(160,175,200)));};
                    p.ErrorDataReceived +=(s,e)=>{if(e.Data!=null) SafeInvoke(()=>Log("  "+e.Data,C_RED));};
                    p.Start(); p.BeginOutputReadLine(); p.BeginErrorReadLine(); p.WaitForExit();
                    return p.ExitCode;
                }
            });
        }

        Task<string> CaptureAsync(string exe, string args)
        {
            return Task.Run(()=>{
                using(var p=new Process()){
                    p.StartInfo=new ProcessStartInfo{FileName=exe,Arguments=args,UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
                    p.Start(); string o=p.StandardOutput.ReadToEnd(); string er=p.StandardError.ReadToEnd(); p.WaitForExit();
                    return string.IsNullOrWhiteSpace(o)?er:o;
                }
            });
        }

        void Log(string msg, Color color)
        {
            SafeInvoke(()=>{ rtbLog.SelectionStart=rtbLog.TextLength; rtbLog.SelectionLength=0; rtbLog.SelectionColor=color; rtbLog.AppendText(msg+"\n"); rtbLog.SelectionColor=rtbLog.ForeColor; rtbLog.ScrollToCaret(); });
        }

        void SetProg(int pct, string msg) { SafeInvoke(()=>{ pbMain.Value=Math.Max(0,Math.Min(100,pct)); lblTask.Text=msg; }); }
        void SafeInvoke(Action a){ if(this.InvokeRequired) this.BeginInvoke(a); else a(); }

        // UTILITIES
        static string ParseGhAsset(string json, string ext)
        {
            int idx=0;
            while(true){ idx=json.IndexOf("browser_download_url",idx); if(idx<0) break; int s2=json.IndexOf('"',idx+22)+1; int e2=json.IndexOf('"',s2); if(s2<0||e2<0) break; string url=json.Substring(s2,e2-s2); if(url.EndsWith(ext,StringComparison.OrdinalIgnoreCase)) return url; idx=e2; }
            return null;
        }

        static void CreateShortcut(string lnkPath, string target, string workDir, string desc)
        {
            Type t=Type.GetTypeFromProgID("WScript.Shell"); object sh=Activator.CreateInstance(t);
            object sc=t.InvokeMember("CreateShortcut",BindingFlags.InvokeMethod,null,sh,new object[]{lnkPath}); Type st=sc.GetType();
            st.InvokeMember("TargetPath",      BindingFlags.SetProperty,null,sc,new object[]{target});
            st.InvokeMember("WorkingDirectory",BindingFlags.SetProperty,null,sc,new object[]{workDir});
            st.InvokeMember("Description",     BindingFlags.SetProperty,null,sc,new object[]{desc});
            st.InvokeMember("Save",            BindingFlags.InvokeMethod,null,sc,new object[0]);
            Marshal.FinalReleaseComObject(sc); Marshal.FinalReleaseComObject(sh);
        }

        static string FormatBytes(long bytes)
        {
            if(bytes<=0) return "0 B"; string[] s={"B","KB","MB","GB","TB"}; int i=0; double d=bytes;
            while(d>=1024&&i<s.Length-1){d/=1024;i++;} return $"{d:F1} {s[i]}";
        }

        void InitializeComponent()
        {
            this.SuspendLayout(); this.AutoScaleDimensions=new SizeF(6f,13f); this.AutoScaleMode=AutoScaleMode.Font; this.ResumeLayout(false);
        }
    }

    class NavBtn : Panel
    {
        public string PageId{get;}
        bool _active; Label lblI,lblT; Panel ind;
        static readonly Color CH=Color.FromArgb(30,40,65),CA=Color.FromArgb(25,35,58),CN=Color.FromArgb(15,20,35);
        public NavBtn(string icon,string text,string pageId)
        {
            PageId=pageId; Size=new Size(199,44); BackColor=CN; Cursor=Cursors.Hand;
            ind =new Panel{Size=new Size(3,28),BackColor=Color.Transparent,Location=new Point(0,8)};
            lblI=new Label{Text=icon,Font=new Font("Segoe UI Emoji",13f),ForeColor=Color.FromArgb(100,130,180),AutoSize=true,Location=new Point(14,11)};
            lblT=new Label{Text=text,Font=new Font("Segoe UI",9f),ForeColor=Color.FromArgb(140,160,200),AutoSize=true,Location=new Point(44,14)};
            Controls.AddRange(new Control[]{ind,lblI,lblT});
            MouseEnter+=(s,e)=>{if(!_active)BackColor=CH;};
            MouseLeave+=(s,e)=>{if(!_active)BackColor=CN;};
            foreach(Control c in Controls){c.MouseEnter+=(s,e)=>{if(!_active)BackColor=CH;};c.MouseLeave+=(s,e)=>{if(!_active)BackColor=CN;};c.Click+=(s,e2)=>OnClick(e2);}
        }
        public void SetActive(bool a)
        {
            _active=a; BackColor=a?CA:CN;
            ind.BackColor=a?Color.FromArgb(56,139,253):Color.Transparent;
            lblI.ForeColor=a?Color.FromArgb(56,139,253):Color.FromArgb(100,130,180);
            lblT.ForeColor=a?Color.FromArgb(220,230,245):Color.FromArgb(140,160,200);
            lblT.Font=new Font("Segoe UI",9f,a?FontStyle.Bold:FontStyle.Regular);
        }
    }

    class QuickCard : Panel
    {
        Color _ac; bool _hov; Label lblI,lblT;
        public QuickCard(string icon,string text,Color accent)
        {
            _ac=accent; Size=new Size(165,108); Margin=new Padding(8); BackColor=Color.FromArgb(22,30,50); Cursor=Cursors.Hand;
            lblI=new Label{Text=icon,Font=new Font("Segoe UI Emoji",22f),ForeColor=accent,AutoSize=true,Location=new Point(12,12)};
            lblT=new Label{Text=text,Font=new Font("Segoe UI",8.5f,FontStyle.Bold),ForeColor=Color.FromArgb(180,195,220),AutoSize=false,Width=140,Height=42,Location=new Point(12,60)};
            Controls.AddRange(new Control[]{lblI,lblT});
            MouseEnter+=OnE; MouseLeave+=OnL;
            foreach(Control c in Controls){c.MouseEnter+=OnE;c.MouseLeave+=OnL;c.Click+=(s,e2)=>OnClick(e2);}
        }
        void OnE(object s,EventArgs e){_hov=true;Invalidate();}
        void OnL(object s,EventArgs e){_hov=false;Invalidate();}
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            using(var p=new Pen(_hov?_ac:Color.FromArgb(40,55,85),_hov?2:1)) e.Graphics.DrawRectangle(p,0,0,Width-1,Height-1);
            if(_hov) using(var b=new SolidBrush(Color.FromArgb(20,_ac))) e.Graphics.FillRectangle(b,ClientRectangle);
        }
    }
}
