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

    public async Task<(bool Success, string Message, TimeSpan Duration)> ExecuteTaskAsync(SpTaskConfig task, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(task.ProcedureName))
        {
            return (false, "Stored Procedure adı belirtilmemiş!", TimeSpan.Zero);
        }

        var sw = Stopwatch.StartNew();
        string spName = task.ProcedureName.Trim();
        string taskDisplayName = string.IsNullOrWhiteSpace(task.Name) ? spName : task.Name;

        AppLogger.Info($"[BAŞLADI] Görev: '{taskDisplayName}' ({spName}) çalıştırılıyor...");

        try
        {
            string conStr = _config.Database.BuildConnectionString();
            await using var conn = new SqlConnection(conStr);
            await conn.OpenAsync(cancellationToken);

            await using var cmd = new SqlCommand(spName, conn)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = Math.Max(10, task.CommandTimeoutSeconds)
            };

            // Parametreleri ekle
            if (task.Parameters != null && task.Parameters.Count > 0)
            {
                foreach (var p in task.Parameters)
                {
                    if (string.IsNullOrWhiteSpace(p.Name)) continue;
                    string pName = p.Name.Trim();
                    if (!pName.StartsWith("@")) pName = "@" + pName;

                    var sqlParam = cmd.Parameters.Add(pName, SqlDbType.VarChar);
                    if (string.IsNullOrWhiteSpace(p.Value) || string.Equals(p.Value, "NULL", StringComparison.OrdinalIgnoreCase))
                    {
                        sqlParam.Value = DBNull.Value;
                    }
                    else
                    {
                        sqlParam.Value = p.Value.Trim();
                    }
                }
            }

            int rowsAffected = await cmd.ExecuteNonQueryAsync(cancellationToken);
            sw.Stop();

            task.LastRunTime = DateTime.Now;
            task.LastRunStatus = "Başarılı";
            task.LastRunDuration = sw.Elapsed;

            string msg = $"'{taskDisplayName}' ({spName}) başarıyla tamamlandı. (Süre: {sw.ElapsedMilliseconds} ms, Etkilenen Satır: {rowsAffected})";
            AppLogger.Success($"[TAMAMLANDI] {msg}");
            return (true, msg, sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            task.LastRunTime = DateTime.Now;
            task.LastRunStatus = $"Hata: {ex.Message}";
            task.LastRunDuration = sw.Elapsed;

            string errorMsg = $"'{taskDisplayName}' ({spName}) çalıştırılırken hata oluştu: {ex.Message}";
            AppLogger.Error(errorMsg, ex);
            return (false, errorMsg, sw.Elapsed);
        }
    }
}
