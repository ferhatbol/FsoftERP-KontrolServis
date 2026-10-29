using System.Collections.Concurrent;
using FsoftERP.KontrolServis.Configuration;
using Microsoft.Extensions.Hosting;

namespace FsoftERP.KontrolServis.Services;

public class KontrolBackgroundService : BackgroundService
{
    private AppConfig _config;
    private SqlService _sqlService;
    private readonly HashSet<string> _executedTimeSlots = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastIntervalRunTimes = new();
    private readonly ConcurrentDictionary<string, bool> _currentlyExecutingTasks = new();
    private DateTime _lastConfigReloadTime = DateTime.MinValue;

    public KontrolBackgroundService()
    {
        _config = AppConfig.Load();
        _sqlService = new SqlService(_config);
        AppLogger.Initialize(_config.Logging.LogDirectory);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string version = typeof(KontrolBackgroundService).Assembly.GetName().Version?.ToString() ?? "1.0.3.0";
        AppLogger.Info("=================================================");
        AppLogger.Info($"FsoftERP Kontrol Servisi (FsoftERPSKWS) Başlatıldı. v{version}");
        AppLogger.Info($"Veritabanı: {_config.Database.Server} / {_config.Database.Database}");
        AppLogger.Info($"Tanımlı Toplam SP Görev Sayısı: {_config.Tasks.Count}");
        foreach (var t in _config.Tasks)
        {
            AppLogger.Info($" - [{t.ProcedureName}] {(t.Enabled ? "AKTİF" : "PASİF")} -> {t.GetScheduleSummary()}");
        }
        AppLogger.Info("=================================================");

        // Başlangıç veritabanı testi
        var connTest = await _sqlService.TestConnectionAsync(stoppingToken);
        if (connTest.Success)
        {
            AppLogger.Success($"Başlangıç Veritabanı Testi: {connTest.Message}");
        }
        else
        {
            AppLogger.Warn($"Başlangıç Veritabanı Uyarısı: {connTest.Message} (Bağlantı sağlandığında görevler çalışacaktır)");
        }

        // Ana döngü
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                CheckAndReloadConfig();

                var now = DateTime.Now;
                string currentHm = now.ToString("HH:mm");
                string todayStr = now.ToString("yyyyMMdd");

                // Eski günün zaman kayıtlarını temizle
                _executedTimeSlots.RemoveWhere(s => !s.Contains($"_{todayStr}_"));

                foreach (var task in _config.Tasks)
                {
                    if (!task.Enabled || string.IsNullOrWhiteSpace(task.ProcedureName))
                        continue;

                    // Bu görev şu anda çalışıyorsa çakışmayı önlemek için atla
                    if (_currentlyExecutingTasks.TryGetValue(task.Id, out bool isRunning) && isRunning)
                        continue;

                    bool shouldRun = false;
                    string triggerReason = "";

                    if (task.ScheduleType == SpScheduleType.IntervalMinutes)
                    {
                        int interval = Math.Max(1, task.IntervalMinutes);
                        DateTime lastRun = _lastIntervalRunTimes.GetOrAdd(task.Id, DateTime.MinValue);

                        if (now - lastRun >= TimeSpan.FromMinutes(interval))
                        {
                            shouldRun = true;
                            triggerReason = $"Periyodik ({interval} dk aralık)";
                            _lastIntervalRunTimes[task.Id] = now;
                        }
                    }
                    else if (task.ScheduleType == SpScheduleType.SpecificTimes)
                    {
                        if (task.ExecutionTimes != null && task.ExecutionTimes.Count > 0)
                        {
                            foreach (var rawTime in task.ExecutionTimes)
                            {
                                string targetTime = NormalizeTime(rawTime);
                                if (string.Equals(currentHm, targetTime, StringComparison.OrdinalIgnoreCase))
                                {
                                    string slotKey = $"{task.Id}_{todayStr}_{targetTime}";
                                    if (!_executedTimeSlots.Contains(slotKey))
                                    {
                                        _executedTimeSlots.Add(slotKey);
                                        shouldRun = true;
                                        triggerReason = $"Planlanan Saat ({targetTime})";
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (shouldRun)
                    {
                        var taskToRun = task;
                        _currentlyExecutingTasks[taskToRun.Id] = true;

                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                AppLogger.Info($"[TETİKLENDİ] Görev: '{taskToRun.Name}' | Sebep: {triggerReason}");
                                await _sqlService.ExecuteTaskAsync(taskToRun, stoppingToken);
                            }
                            catch (Exception ex)
                            {
                                AppLogger.Error($"Görev çalıştırma hatası: {taskToRun.Name}", ex);
                            }
                            finally
                            {
                                _currentlyExecutingTasks[taskToRun.Id] = false;
                            }
                        }, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("Servis ana döngüsünde hata", ex);
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        AppLogger.Info("FsoftERP Kontrol Servisi durduruldu.");
    }

    private void CheckAndReloadConfig()
    {
        if (DateTime.Now - _lastConfigReloadTime < TimeSpan.FromSeconds(30)) return;
        _lastConfigReloadTime = DateTime.Now;

        try
        {
            string cfgPath = AppConfig.DefaultConfigPath;
            if (File.Exists(cfgPath))
            {
                var newCfg = AppConfig.Load(cfgPath);
                _config = newCfg;
                _sqlService = new SqlService(newCfg);
            }
        }
        catch { }
    }

    private static string NormalizeTime(string timeStr)
    {
        if (string.IsNullOrWhiteSpace(timeStr)) return string.Empty;
        var parts = timeStr.Trim().Split(':');
        if (parts.Length >= 2 && int.TryParse(parts[0], out int h) && int.TryParse(parts[1], out int m))
        {
            return $"{h:D2}:{m:D2}";
        }
        return timeStr.Trim();
    }
}
