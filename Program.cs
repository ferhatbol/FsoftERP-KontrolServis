using System.Runtime.InteropServices;
using FsoftERP.KontrolServis.Configuration;
using FsoftERP.KontrolServis.Services;
using FsoftERP.KontrolServis.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FsoftERP.KontrolServis;

internal static class Program
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(int dwProcessId);
    private const int ATTACH_PARENT_PROCESS = -1;

    [STAThread]
    private static int Main(string[] args)
    {
        var config = AppConfig.Load();
        AppLogger.Initialize(config.Logging.LogDirectory);

        // 1. Windows Servis Modu
        if (args.Contains("--service", StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var builder = Host.CreateApplicationBuilder(args);
                builder.Services.AddWindowsService(options =>
                {
                    options.ServiceName = config.Service.ServiceName;
                });
                builder.Services.AddHostedService<KontrolBackgroundService>();

                var host = builder.Build();
                host.Run();
                return 0;
            }
            catch (Exception ex)
            {
                AppLogger.Error("Servis başlatılırken ölümcül hata", ex);
                return 1;
            }
        }

        // Konsol komutları için üst konsola bağlan
        if (args.Length > 0 && !args.Contains("--gui", StringComparer.OrdinalIgnoreCase) && !args.Contains("--config", StringComparer.OrdinalIgnoreCase))
        {
            if (AttachConsole(ATTACH_PARENT_PROCESS))
            {
                var stdOut = Console.OpenStandardOutput();
                Console.SetOut(new StreamWriter(stdOut) { AutoFlush = true });
                var stdErr = Console.OpenStandardError();
                Console.SetError(new StreamWriter(stdErr) { AutoFlush = true });
            }
        }

        // 2. Servis Kurulum / Kaldırma / Başlatma / Durdurma Komutları
        if (args.Contains("--install", StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine("FsoftERP Kontrol Servisi kuruluyor...");
            var res = ServiceManager.InstallService(config);
            Console.WriteLine(res.Message);
            return res.Success ? 0 : 1;
        }

        if (args.Contains("--uninstall", StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine("FsoftERP Kontrol Servisi kaldırılıyor...");
            var res = ServiceManager.UninstallService(config.Service.ServiceName);
            Console.WriteLine(res.Message);
            return res.Success ? 0 : 1;
        }

        if (args.Contains("--start", StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine("FsoftERP Kontrol Servisi başlatılıyor...");
            var res = ServiceManager.StartService(config.Service.ServiceName);
            Console.WriteLine(res.Message);
            return res.Success ? 0 : 1;
        }

        if (args.Contains("--stop", StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine("FsoftERP Kontrol Servisi durduruluyor...");
            var res = ServiceManager.StopService(config.Service.ServiceName);
            Console.WriteLine(res.Message);
            return res.Success ? 0 : 1;
        }

        // 3. Tek Seferlik Manuel Test Çalıştırma Komutları
        if (args.Contains("--run-all", StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine($"Toplam {config.Tasks.Count} görev çalıştırılıyor...");
            var sql = new SqlService(config);
            foreach (var task in config.Tasks.Where(t => t.Enabled))
            {
                Console.WriteLine($"-> {task.ProcedureName}...");
                var res = sql.ExecuteTaskAsync(task).GetAwaiter().GetResult();
                Console.WriteLine($"   {res.Message}");
            }
            return 0;
        }

        var runArg = args.FirstOrDefault(a => a.StartsWith("--run-sp=", StringComparison.OrdinalIgnoreCase));
        if (runArg != null)
        {
            string spName = runArg.Substring("--run-sp=".Length).Trim();
            var task = config.Tasks.FirstOrDefault(t => string.Equals(t.ProcedureName, spName, StringComparison.OrdinalIgnoreCase))
                       ?? new SpTaskConfig { Name = spName, ProcedureName = spName, CommandTimeoutSeconds = 1000 };

            Console.WriteLine($"{spName} stored procedure çalıştırılıyor...");
            var sql = new SqlService(config);
            var res = sql.ExecuteTaskAsync(task).GetAwaiter().GetResult();
            Console.WriteLine(res.Message);
            return res.Success ? 0 : 1;
        }

        if (args.Contains("--help", StringComparer.OrdinalIgnoreCase) || args.Contains("-h", StringComparer.OrdinalIgnoreCase))
        {
            string ver = typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.4.0";
            Console.WriteLine($"FsoftERP Kontrol Servisi (FsoftERPSKWS) v{ver}");
            Console.WriteLine("Kullanım:");
            Console.WriteLine("  FsoftERPSKWS.exe                     : Görsel Ayar ve Yönetim Panelini açar (GUI)");
            Console.WriteLine("  FsoftERPSKWS.exe --service           : Windows Servis modunda arka planda çalıştırır");
            Console.WriteLine("  FsoftERPSKWS.exe --install           : Windows Servisini sisteme otomatik başlangıçlı kurar");
            Console.WriteLine("  FsoftERPSKWS.exe --uninstall         : Windows Servisini sistemden kaldırır");
            Console.WriteLine("  FsoftERPSKWS.exe --start             : Windows Servisini başlatır");
            Console.WriteLine("  FsoftERPSKWS.exe --stop              : Windows Servisini durdurur");
            Console.WriteLine("  FsoftERPSKWS.exe --run-all           : Aktif tüm Stored Procedure'leri hemen çalıştırır");
            Console.WriteLine("  FsoftERPSKWS.exe --run-sp=<SP_ADI>   : Belirtilen Stored Procedure'ü hemen çalıştırır");
            return 0;
        }

        // 4. Varsayılan: Windows Forms Görsel Arayüzü (GUI)
        ApplicationConfiguration.Initialize();
        Application.Run(new MainConfigForm());
        return 0;
    }
}
