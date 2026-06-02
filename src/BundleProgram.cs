using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace PrinterInstallerBundle
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                var baseTemp = Path.Combine(Path.GetTempPath(), "PrinterInstallerBundle");
                Directory.CreateDirectory(baseTemp);
                var workDir = Path.Combine(baseTemp, Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(workDir);

                ExtractPayload(workDir);

                var installerPath = Path.Combine(workDir, "PrinterInstaller.exe");
                if (!File.Exists(installerPath))
                {
                    MessageBox.Show("安装器缺失，无法继续。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = installerPath,
                    UseShellExecute = true
                });

                if (process == null)
                {
                    MessageBox.Show("无法启动安装器。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Cleanup after installer exits.
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        process.WaitForExit();
                        Thread.Sleep(1500);
                        if (Directory.Exists(workDir))
                        {
                            Directory.Delete(workDir, true);
                        }
                    }
                    catch
                    {
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("启动失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void ExtractPayload(string workDir)
        {
            var asm = Assembly.GetExecutingAssembly();
            var resourceName = asm.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("payload.zip", StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrEmpty(resourceName))
            {
                throw new InvalidOperationException("未找到内置安装包资源。");
            }

            var zipPath = Path.Combine(workDir, "payload.zip");
            using (var stream = asm.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException("无法读取内置安装包资源。");
                }

                using (var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write))
                {
                    stream.CopyTo(fs);
                }
            }

            ZipFile.ExtractToDirectory(zipPath, workDir);
            File.Delete(zipPath);
        }
    }
}
