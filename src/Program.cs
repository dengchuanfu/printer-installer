using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace PrinterInstaller
{
    [DataContract]
    public class PrinterConfig
    {
        [DataMember]
        public string Office { get; set; }

        [DataMember]
        public string PrinterName { get; set; }

        [DataMember]
        public string IpAddress { get; set; }

        [DataMember]
        public string DriverName { get; set; }

        [DataMember]
        public string DriverInfPath { get; set; }
    }

    public class MainForm : Form
    {
        private const string FinanceOfficeName = "财务部";
        private const string FinanceInstallPassword = "970929";
        private const string UnifiedDriverName = "RICOH Aficio MP C4502 PCL 6";
        private const string UnifiedDriverInfPath = "drivers\\Ricoh_MP_C4502_5502_Pcl6\\x64\\OEMSETUP.INF";
        private readonly PictureBox logoBox = new PictureBox();
        private readonly ComboBox officeCombo = new ComboBox();
        private readonly Label infoLabel = new Label();
        private readonly Button installButton = new Button();
        private readonly TextBox logText = new TextBox();
        private List<PrinterConfig> configs = new List<PrinterConfig>();

        public MainForm()
        {
            Text = "打印机一键安装";
            Width = 740;
            Height = 480;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            logoBox.Left = 20;
            logoBox.Top = 12;
            logoBox.Width = 72;
            logoBox.Height = 72;
            logoBox.SizeMode = PictureBoxSizeMode.Zoom;
            TryLoadLogo();

            var topLabel = new Label
            {
                Left = 110,
                Top = 20,
                Width = 120,
                Text = "选择办公室："
            };

            officeCombo.Left = 230;
            officeCombo.Top = 16;
            officeCombo.Width = 280;
            officeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            officeCombo.SelectedIndexChanged += (_, __) => RefreshInfo();

            installButton.Left = 520;
            installButton.Top = 15;
            installButton.Width = 180;
            installButton.Height = 30;
            installButton.Text = "安装所选打印机";
            installButton.BackColor = Color.FromArgb(34, 139, 34);
            installButton.ForeColor = Color.White;
            installButton.FlatStyle = FlatStyle.Flat;
            installButton.FlatAppearance.BorderColor = Color.FromArgb(28, 112, 28);
            installButton.Click += (_, __) => InstallSelectedPrinter();

            infoLabel.Left = 110;
            infoLabel.Top = 60;
            infoLabel.Width = 590;
            infoLabel.Height = 50;

            logText.Left = 20;
            logText.Top = 120;
            logText.Width = 650;
            logText.Height = 300;
            logText.Multiline = true;
            logText.ScrollBars = ScrollBars.Vertical;
            logText.ReadOnly = true;

            Controls.Add(logoBox);
            Controls.Add(topLabel);
            Controls.Add(officeCombo);
            Controls.Add(installButton);
            Controls.Add(infoLabel);
            Controls.Add(logText);

            LoadConfigs();
        }

        private void TryLoadLogo()
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var candidates = new[]
                {
                    Path.Combine(baseDir, "logo.png"),
                    Path.Combine(baseDir, "logo.jpg"),
                    Path.Combine(baseDir, "logo.jpeg")
                };

                var logoPath = candidates.FirstOrDefault(File.Exists);
                if (logoPath == null)
                {
                    logoPath = Directory.GetFiles(baseDir, "*.png").FirstOrDefault()
                        ?? Directory.GetFiles(baseDir, "*.jpg").FirstOrDefault()
                        ?? Directory.GetFiles(baseDir, "*.jpeg").FirstOrDefault();
                }

                if (logoPath != null)
                {
                    using (var img = Image.FromFile(logoPath))
                    {
                        logoBox.Image = (Image)img.Clone();
                    }
                    return;
                }

                // Fallback to embedded logo so UI still has branding when file is missing.
                var asm = Assembly.GetExecutingAssembly();
                var resName = asm.GetManifestResourceNames()
                    .FirstOrDefault(n => n.EndsWith("logo.png", StringComparison.OrdinalIgnoreCase));
                if (resName != null)
                {
                    using (var stream = asm.GetManifestResourceStream(resName))
                    {
                        if (stream != null)
                        {
                            using (var img = Image.FromStream(stream))
                            {
                                logoBox.Image = (Image)img.Clone();
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore logo loading failures and keep UI functional.
            }
        }

        private void LoadConfigs()
        {
            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "printers.json");
                if (!File.Exists(configPath))
                {
                    MessageBox.Show("未找到 printers.json，请确保与 exe 在同一目录。", "配置缺失", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var json = File.ReadAllText(configPath, new UTF8Encoding(true));
                if (!string.IsNullOrWhiteSpace(json) && json.Length > 0 && json[0] == '\uFEFF')
                {
                    json = json.Substring(1);
                }

                var serializer = new JavaScriptSerializer();
                configs = serializer.Deserialize<List<PrinterConfig>>(json) ?? new List<PrinterConfig>();
                NormalizeDrivers(configs);

                officeCombo.Items.Clear();
                foreach (var item in configs)
                {
                    officeCombo.Items.Add(item.Office);
                }

                if (officeCombo.Items.Count > 0)
                {
                    officeCombo.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("读取配置失败：" + ex.Message + "\n请检查 printers.json 格式和编码（UTF-8）。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void NormalizeDrivers(IEnumerable<PrinterConfig> printerConfigs)
        {
            if (printerConfigs == null)
            {
                return;
            }

            foreach (var config in printerConfigs)
            {
                if (config == null)
                {
                    continue;
                }

                config.DriverName = UnifiedDriverName;
                config.DriverInfPath = UnifiedDriverInfPath;
            }
        }

        private void RefreshInfo()
        {
            var config = GetSelectedConfig();
            if (config == null)
            {
                infoLabel.Text = "";
                return;
            }

            infoLabel.Text = "打印机：" + GetTargetPrinterName(config) + "    IP：" + config.IpAddress + "    驱动：" + config.DriverName;
        }

        private PrinterConfig GetSelectedConfig()
        {
            if (officeCombo.SelectedIndex < 0 || officeCombo.SelectedIndex >= configs.Count)
            {
                return null;
            }

            var office = officeCombo.SelectedItem as string;
            return configs.FirstOrDefault(c => c.Office == office);
        }

        private void InstallSelectedPrinter()
        {
            var config = GetSelectedConfig();
            if (config == null)
            {
                MessageBox.Show("请先选择办公室。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                "即将安装：\n" +
                "办公室：" + config.Office + "\n" +
                "打印机：" + GetTargetPrinterName(config) + "\n" +
                "IP：" + config.IpAddress + "\n\n" +
                "安装过程将在后台执行。是否继续？",
                "确认安装",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            if (string.Equals(config.Office, FinanceOfficeName, StringComparison.OrdinalIgnoreCase))
            {
                if (!VerifyFinancePassword())
                {
                    AppendLog("财务部密码校验未通过，已取消安装。");
                    MessageBox.Show("密码错误或已取消，未执行安装。", "验证失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            var logFile = GetLogFilePath();
            var targetPrinterName = GetTargetPrinterName(config);
            AppendLog("开始安装：" + targetPrinterName);
            AppendLog("日志文件：" + logFile);

            try
            {
                var resolvedInfPath = ResolvePathInAppDir(config.DriverInfPath);
                var runtimeConfig = new PrinterConfig
                {
                    Office = config.Office,
                    PrinterName = targetPrinterName,
                    IpAddress = config.IpAddress,
                    DriverName = config.DriverName,
                    DriverInfPath = config.DriverInfPath
                };
                var psScript = BuildInstallScript(runtimeConfig, logFile);
                psScript = psScript.Replace("__DRIVER_INF_PATH__", EscapePs(resolvedInfPath ?? string.Empty));
                var tempScript = Path.Combine(Path.GetTempPath(), "install_printer_" + Guid.NewGuid().ToString("N") + ".ps1");
                // Windows PowerShell 5.1 reads UTF-8 script reliably with BOM.
                // This avoids Chinese printer name mojibake in script literals.
                File.WriteAllText(tempScript, psScript, new UTF8Encoding(true));

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -ExecutionPolicy Bypass -File \"" + tempScript + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                var process = Process.Start(psi);
                if (process != null)
                {
                    process.WaitForExit();
                    AppendLog("安装进程结束，退出码：" + process.ExitCode);
                    if (process.ExitCode == 0)
                    {
                        MessageBox.Show("安装完成。\n请在系统打印机列表确认。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("安装失败，退出码：" + process.ExitCode + "\n请把日志发给 IT。", "失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    AppendLog("无法启动 PowerShell 安装进程。");
                    MessageBox.Show("无法启动安装进程。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                TryDeleteFile(tempScript);
            }
            catch (Exception ex)
            {
                AppendLog("安装异常：" + ex.Message);
                MessageBox.Show("安装异常：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string GetTargetPrinterName(PrinterConfig config)
        {
            var office = (config == null ? string.Empty : config.Office) ?? string.Empty;
            office = office.Trim();
            if (office.Length == 0)
            {
                return "打印机";
            }

            return office + "打印机";
        }

        private static bool VerifyFinancePassword()
        {
            using (var form = new Form())
            using (var label = new Label())
            using (var textBox = new TextBox())
            using (var okButton = new Button())
            using (var cancelButton = new Button())
            {
                form.Text = "财务部验证";
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                form.Width = 360;
                form.Height = 180;
                form.ShowInTaskbar = false;

                label.Left = 20;
                label.Top = 20;
                label.Width = 300;
                label.Text = "请输入财务部安装密码：";

                textBox.Left = 20;
                textBox.Top = 50;
                textBox.Width = 300;
                textBox.UseSystemPasswordChar = true;

                okButton.Text = "确定";
                okButton.Left = 160;
                okButton.Top = 90;
                okButton.Width = 75;
                okButton.DialogResult = DialogResult.OK;

                cancelButton.Text = "取消";
                cancelButton.Left = 245;
                cancelButton.Top = 90;
                cancelButton.Width = 75;
                cancelButton.DialogResult = DialogResult.Cancel;

                form.AcceptButton = okButton;
                form.CancelButton = cancelButton;
                form.Controls.Add(label);
                form.Controls.Add(textBox);
                form.Controls.Add(okButton);
                form.Controls.Add(cancelButton);

                var result = form.ShowDialog();
                if (result != DialogResult.OK)
                {
                    return false;
                }

                return string.Equals(textBox.Text, FinanceInstallPassword, StringComparison.Ordinal);
            }
        }

        private static string ResolvePathInAppDir(string pathValue)
        {
            if (string.IsNullOrWhiteSpace(pathValue))
            {
                return null;
            }

            if (Path.IsPathRooted(pathValue))
            {
                return pathValue;
            }

            return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, pathValue));
        }

        private static string BuildInstallScript(PrinterConfig config, string logFile)
        {
            var sb = new StringBuilder();
            sb.AppendLine("$ErrorActionPreference = 'Stop'");
            sb.AppendLine("$log = '" + EscapePs(logFile) + "'");
            sb.AppendLine("New-Item -ItemType Directory -Force -Path ([System.IO.Path]::GetDirectoryName($log)) | Out-Null");
            sb.AppendLine("function Write-Log($m) { Add-Content -Path $log -Value ((Get-Date -Format 'yyyy-MM-dd HH:mm:ss') + ' ' + $m) }");
            sb.AppendLine("trap {");
            sb.AppendLine("  Write-Log ('ERROR: ' + $_.Exception.Message)");
            sb.AppendLine("  exit 1");
            sb.AppendLine("}");
            sb.AppendLine("Write-Log '==== Start Install ===='");
            sb.AppendLine("$printerName = '" + EscapePs(config.PrinterName) + "'");
            sb.AppendLine("$ip = '" + EscapePs(config.IpAddress) + "'");
            sb.AppendLine("$driverName = '" + EscapePs(config.DriverName) + "'");
            sb.AppendLine("$portName = 'IP_' + $ip");

            if (!string.IsNullOrWhiteSpace(config.DriverInfPath))
            {
                sb.AppendLine("$driverInf = '__DRIVER_INF_PATH__'");
                sb.AppendLine("if (Test-Path $driverInf) {");
                sb.AppendLine("  Write-Log ('Install driver inf: ' + $driverInf)");
                sb.AppendLine("  pnputil.exe /add-driver \"$driverInf\" /install | Out-Null");
                sb.AppendLine("} else {");
                sb.AppendLine("  Write-Log ('Driver inf not found, skip: ' + $driverInf)");
                sb.AppendLine("}");
            }

            sb.AppendLine("if (-not (Get-PrinterPort -Name $portName -ErrorAction SilentlyContinue)) {");
            sb.AppendLine("  Write-Log ('Create port: ' + $portName)");
            sb.AppendLine("  Add-PrinterPort -Name $portName -PrinterHostAddress $ip");
            sb.AppendLine("} else { Write-Log ('Port exists: ' + $portName) }");

            sb.AppendLine("if (-not (Get-PrinterDriver -Name $driverName -ErrorAction SilentlyContinue)) {");
            sb.AppendLine("  Write-Log ('Try add driver by name: ' + $driverName)");
            sb.AppendLine("  Add-PrinterDriver -Name $driverName");
            sb.AppendLine("} else { Write-Log ('Driver exists: ' + $driverName) }");

            sb.AppendLine("if (-not (Get-Printer -Name $printerName -ErrorAction SilentlyContinue)) {");
            sb.AppendLine("  Write-Log ('Add printer: ' + $printerName)");
            sb.AppendLine("  Add-Printer -Name $printerName -DriverName $driverName -PortName $portName");
            sb.AppendLine("} else {");
            sb.AppendLine("  Write-Log ('Printer exists, set current config: ' + $printerName)");
            sb.AppendLine("  Set-Printer -Name $printerName -DriverName $driverName -PortName $portName");
            sb.AppendLine("}");

            sb.AppendLine("if (Get-Command Set-PrintConfiguration -ErrorAction SilentlyContinue) {");
            sb.AppendLine("  Write-Log ('Set default print mode to black and white: ' + $printerName)");
            sb.AppendLine("  Set-PrintConfiguration -PrinterName $printerName -Color $false");
            sb.AppendLine("} else {");
            sb.AppendLine("  Write-Log 'Set-PrintConfiguration not available, skip black and white default setting.'");
            sb.AppendLine("}");

            sb.AppendLine("Write-Log '==== Install Success ===='");
            sb.AppendLine("exit 0");

            return sb.ToString();
        }

        private static string EscapePs(string value)
        {
            return (value ?? string.Empty).Replace("'", "''");
        }

        private static string GetLogFilePath()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "PrinterInstaller", "logs");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "install-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log");
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
            }
        }

        private void AppendLog(string message)
        {
            var line = DateTime.Now.ToString("HH:mm:ss") + " " + message + Environment.NewLine;
            logText.AppendText(line);
        }
    }

    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
