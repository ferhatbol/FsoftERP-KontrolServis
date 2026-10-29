using FsoftERP.KontrolServis.Configuration;
using Microsoft.Extensions.Hosting;

namespace FsoftERP.KontrolServis.Services;

public class KontrolBackgroundService : BackgroundService
{
    private AppConfig _config;
    private SqlService _sqlService;
    private readonly HashSet<string> _executedDepoSlots = new();
    private DateTime _lastSparisExecutionTime = DateTime.MinValue;
    private DateTime _lastConfigReloadTime = DateTime.MinValue;

    public KontrolBackgroundService()
    {
        _config = AppConfig.Load();
        _sqlService = new SqlService(_config);
        AppLogger.Initialize(_config.Logging.LogDirectory);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        AppLogger.Info("=================================================");
        AppLogger.Info("FsoftERP Kontrol Servisi (FsoftERPSKWS) Başlatıldı.");
        AppLogger.Info($"Veritabanı: {_config.Database.Server} / {_config.Database.Database}");
        AppLogger.Info($"Depo Stok Tarihçe: {(_config.DepoStokTarihce.Enabled ? "AKTİF (" + string.Join(", ", _config.DepoStokTarihce.ExecutionTimes) + ")" : "PASİF")}");
        AppLogger.Info($"Sipariş Durum Kontrol: {(_config.SparisDurumKontrol.Enabled ? "AKTİF (" + _config.SparisDurumKontrol.IntervalMinutes + " dakikada bir)" : "PASİF")}");
        AppLogger.Info("=================================================");

        // İlk başlangıçta veritabanı bağlantı testi yap
        var connTest = await _sqlService.TestConnectionAsync(stoppingToken);
        if (connTest.Success)
        {
            AppLogger.Success($"Başlangıç Veritabanı Testi: {connTest.Message}");
        }
        else
        {
            AppLogger.Warn($"Başlangıç Veritabanı Uyarısı: {connTest.Message} (Servis çalışmaya devam ediyor, bağlantı gelince yeniden denenecektir)");
        }

        // Ana çalışma döngüsü (15 saniyede bir kontrol eder)
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Ayar dosyasını her 30 saniyede bir kontrol edip yenilikleri al
                CheckAndReloadConfig();

                var now = DateTime.Now;

                // 1. Depo Stok Tarihçe Kontrolü (Belirtilen Saatlerde)
                await CheckAndRunDepoStokTarihceAsync(now, stoppingToken);

                // 2. Sipariş Durum Kontrolü (Belirtilen Dakika Aralıklarıyla)
                await CheckAndRunSparisDurumKontrolAsync(now, stoppingToken);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Servis ana döngüsünde beklenmeyen hata", ex);
            }

            // 15 saniye bekle
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

    private async Task CheckAndRunDepoStokTarihceAsync(DateTime now, CancellationToken stoppingToken)
    {
        if (!_config.DepoStokTarihce.Enabled) return;
        if (_config.DepoStokTarihce.ExecutionTimes == null || _config.DepoStokTarihce.ExecutionTimes.Count == 0) return;

        string currentHm = now.ToString("HH:mm");
        string todayStr = now.ToString("yyyyMMdd");

        // Dünden kalan eski kayıtları temizle
        _executedDepoSlots.RemoveWhere(slot => !slot.StartsWith(todayStr));

        foreach (var rawTime in _config.DepoStokTarihce.ExecutionTimes)
        {
            string targetTime = NormalizeTime(rawTime);
            if (string.Equals(currentHm, targetTime, StringComparison.OrdinalIgnoreCase))
            {
                string slotKey = $"{todayStr}_{targetTime}";
                if (!_executedDepoSlots.Contains(slotKey))
                {
                    _executedDepoSlots.Add(slotKey);
                    AppLogger.Info($"[ZAMANLAMA] Depo Stok Tarihçe saati geldi ({targetTime}). Stored procedure tetikleniyor...");

                    _ = Task.Run(async () =>
                    {
                        await _sqlService.ExecuteDepoStokTarihceAsync(CancellationToken.None);
                    }, stoppingToken);
                }
            }
        }
    }

    private async Task CheckAndRunSparisDurumKontrolAsync(DateTime now, CancellationToken stoppingToken)
    {
        if (!_config.SparisDurumKontrol.Enabled) return;

        int intervalMinutes = Math.Max(1, _config.SparisDurumKontrol.IntervalMinutes);
        var elapsed = now - _lastSparisExecutionTime;

        if (elapsed.TotalMinutes >= intervalMinutes)
        {
            _lastSparisExecutionTime = now;
            AppLogger.Info($"[PERİYODİK] Sipariş Durum Kontrol periyodu geldi ({intervalMinutes} dk aralık). Stored procedure tetikleniyor...");

            await _sqlService.ExecuteSparisDurumKontrolAsync(stoppingToken);
        }
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
