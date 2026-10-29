using System.Diagnostics;
using FsoftERP.KontrolServis.Configuration;

namespace FsoftERP.KontrolServis.Services;

public static class ServiceManager
{
    public static (bool Installed, bool Running, string StatusText) GetServiceStatus(string serviceName)
    {
        try
        {
            var psi = new ProcessStartInfo("sc.exe", $"query \"{serviceName}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc == null) return (false, false, "Durum alınamadı");

            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

            if (output.Contains("1060") || output.Contains("does not exist") || output.Contains("belirtilen hizmet mevcut değil"))
            {
                return (false, false, "Kurulu Değil");
            }

            if (output.Contains("STATE") && output.Contains("RUNNING"))
            {
                return (true, true, "Çalışıyor (RUNNING)");
            }

            if (output.Contains("STATE") && output.Contains("STOPPED"))
            {
                return (true, false, "Durduruldu (STOPPED)");
            }

            if (output.Contains("STATE") && output.Contains("START_PENDING"))
            {
                return (true, true, "Başlatılıyor (START_PENDING)");
            }

            if (output.Contains("STATE") && output.Contains("STOP_PENDING"))
            {
                return (true, false, "Durduruluyor (STOP_PENDING)");
            }

            return (true, false, "Yüklü");
        }
        catch (Exception ex)
        {
            return (false, false, $"Hata: {ex.Message}");
        }
    }

    public static (bool Success, string Message) InstallService(AppConfig config)
    {
        string exePath = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "FsoftERPSKWS.exe");
        string binPath = $"\"{exePath}\" --service";
        string serviceName = config.Service.ServiceName;
        string displayName = config.Service.DisplayName;
        string description = config.Service.Description;

        try
        {
            // 1. Önce varsa durdur
            RunScCommand($"stop \"{serviceName}\"");
            Thread.Sleep(500);

            // 2. sc create
            string createArgs = $"create \"{serviceName}\" binPath= \"{binPath}\" start= auto DisplayName= \"{displayName}\"";
            var createRes = RunScCommand(createArgs);
            if (!createRes.Success && !createRes.Output.Contains("already exists"))
            {
                return (false, $"Servis oluşturulamadı: {createRes.Output}");
            }

            // 3. sc description
            RunScCommand($"description \"{serviceName}\" \"{description}\"");

            // 4. sc failure (Hata durumunda 1 dk sonra yeniden başlat)
            RunScCommand($"failure \"{serviceName}\" reset= 86400 actions= restart/60000/restart/60000/restart/60000");

            AppLogger.Success($"Windows Servisi başarıyla kuruldu: {serviceName}");
            return (true, "Windows Servisi başarıyla kuruldu ve 'Otomatik' olarak ayarlandı. ✅");
        }
        catch (Exception ex)
        {
            return (false, $"Servis kurulumunda hata: {ex.Message}");
        }
    }

    public static (bool Success, string Message) UninstallService(string serviceName)
    {
        try
        {
            RunScCommand($"stop \"{serviceName}\"");
            Thread.Sleep(1000);

            var res = RunScCommand($"delete \"{serviceName}\"");
            if (res.Success || res.Output.Contains("SUCCESS"))
            {
                AppLogger.Info($"Windows Servisi kaldırıldı: {serviceName}");
                return (true, "Windows Servisi başarıyla kaldırıldı. ✅");
            }

            return (false, $"Servis kaldırılamadı: {res.Output}");
        }
        catch (Exception ex)
        {
            return (false, $"Servis kaldırılırken hata: {ex.Message}");
        }
    }

    public static (bool Success, string Message) StartService(string serviceName)
    {
        try
        {
            var res = RunScCommand($"start \"{serviceName}\"");
            if (res.Success || res.Output.Contains("START_PENDING") || res.Output.Contains("RUNNING"))
            {
                AppLogger.Info($"Windows Servisi başlatıldı: {serviceName}");
                return (true, "Windows Servisi başlatıldı. ✅");
            }

            return (false, $"Servis başlatılamadı: {res.Output}");
        }
        catch (Exception ex)
        {
            return (false, $"Servis başlatılırken hata: {ex.Message}");
        }
    }

    public static (bool Success, string Message) StopService(string serviceName)
    {
        try
        {
            var res = RunScCommand($"stop \"{serviceName}\"");
            if (res.Success || res.Output.Contains("STOP_PENDING") || res.Output.Contains("STOPPED"))
            {
                AppLogger.Info($"Windows Servisi durduruldu: {serviceName}");
                return (true, "Windows Servisi durduruldu. ⏹️");
            }

            return (false, $"Servis durdurulamadı: {res.Output}");
        }
        catch (Exception ex)
        {
            return (false, $"Servis durdurulurken hata: {ex.Message}");
        }
    }

    private static (bool Success, string Output) RunScCommand(string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo("sc.exe", arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc == null) return (false, "sc.exe başlatılamadı");

            string output = proc.StandardOutput.ReadToEnd() + " " + proc.StandardError.ReadToEnd();
            proc.WaitForExit();
            return (proc.ExitCode == 0, output.Trim());
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
