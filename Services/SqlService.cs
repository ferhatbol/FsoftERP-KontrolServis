using System.Data;
using System.Diagnostics;
using FsoftERP.KontrolServis.Configuration;
using Microsoft.Data.SqlClient;

namespace FsoftERP.KontrolServis.Services;

public class SqlService
{
    private readonly AppConfig _config;

    public SqlService(AppConfig config)
    {
        _config = config;
    }

    public async Task<(bool Success, string Message, TimeSpan Duration)> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            string conStr = _config.Database.BuildConnectionString();
            await using var conn = new SqlConnection(conStr);
            await conn.OpenAsync(cancellationToken);

            await using var cmd = new SqlCommand("SELECT @@SERVERNAME, DB_NAME(), @@VERSION", conn);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                string server = reader.GetString(0);
                string db = reader.GetString(1);
                sw.Stop();
                return (true, $"Bağlantı Başarılı! Sunucu: {server}, Veritabanı: {db} ({sw.ElapsedMilliseconds} ms)", sw.Elapsed);
            }

            sw.Stop();
            return (true, $"Bağlantı Başarılı ({sw.ElapsedMilliseconds} ms)", sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return (false, $"Bağlantı Hatası: {ex.Message}", sw.Elapsed);
        }
    }

    public async Task<(bool Success, string Message, TimeSpan Duration)> ExecuteDepoStokTarihceAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        string procName = "Sp_Set_DepoStokTarihce";
        AppLogger.Info($"[BAŞLADI] {procName} çalıştırılıyor...");

        try
        {
            string conStr = _config.Database.BuildConnectionString();
            await using var conn = new SqlConnection(conStr);
            await conn.OpenAsync(cancellationToken);

            await using var cmd = new SqlCommand(procName, conn)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = _config.DepoStokTarihce.CommandTimeoutSeconds
            };

            int rowsAffected = await cmd.ExecuteNonQueryAsync(cancellationToken);
            sw.Stop();

            string msg = $"{procName} başarıyla tamamlandı. (Süre: {sw.ElapsedMilliseconds} ms, Etkilenen Satır: {rowsAffected})";
            AppLogger.Success($"[TAMAMLANDI] {msg}");
            return (true, msg, sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            string errorMsg = $"{procName} çalıştırılırken hata: {ex.Message}";
            AppLogger.Error(errorMsg, ex);
            return (false, errorMsg, sw.Elapsed);
        }
    }

    public async Task<(bool Success, string Message, TimeSpan Duration)> ExecuteSparisDurumKontrolAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        string procName = "Sp_Set_SparisDurumKontrol";
        string? siparisNo = _config.SparisDurumKontrol.SiparisNo;
        AppLogger.Info($"[BAŞLADI] {procName} çalıştırılıyor... (SiparisNo: {(string.IsNullOrEmpty(siparisNo) ? "TÜMÜ / NULL" : siparisNo)})");

        try
        {
            string conStr = _config.Database.BuildConnectionString();
            await using var conn = new SqlConnection(conStr);
            await conn.OpenAsync(cancellationToken);

            await using var cmd = new SqlCommand(procName, conn)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = _config.SparisDurumKontrol.CommandTimeoutSeconds
            };

            var pSiparis = cmd.Parameters.Add("@SiparisNo", SqlDbType.VarChar);
            if (string.IsNullOrWhiteSpace(siparisNo))
            {
                pSiparis.Value = DBNull.Value;
            }
            else
            {
                pSiparis.Value = siparisNo.Trim();
            }

            int rowsAffected = await cmd.ExecuteNonQueryAsync(cancellationToken);
            sw.Stop();

            string msg = $"{procName} başarıyla tamamlandı. (Süre: {sw.ElapsedMilliseconds} ms, Etkilenen Satır: {rowsAffected})";
            AppLogger.Success($"[TAMAMLANDI] {msg}");
            return (true, msg, sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            string errorMsg = $"{procName} çalıştırılırken hata: {ex.Message}";
            AppLogger.Error(errorMsg, ex);
            return (false, errorMsg, sw.Elapsed);
        }
    }
}
