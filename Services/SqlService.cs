using System.Data;
using System.Diagnostics;
using System.Globalization;
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

    public async Task<List<SpParameterConfig>> GetSpParametersAsync(string procedureName, CancellationToken cancellationToken = default)
    {
        var list = new List<SpParameterConfig>();
        if (string.IsNullOrWhiteSpace(procedureName)) return list;

        string cleanSp = procedureName.Trim();
        if (cleanSp.StartsWith("dbo.", StringComparison.OrdinalIgnoreCase))
        {
            cleanSp = cleanSp.Substring(4);
        }

        try
        {
            string conStr = _config.Database.BuildConnectionString();
            await using var conn = new SqlConnection(conStr);
            await conn.OpenAsync(cancellationToken);

            const string sql = @"
                SELECT 
                    p.name AS ParameterName,
                    t.name AS TypeName,
                    p.is_output AS IsOutput
                FROM sys.parameters p
                INNER JOIN sys.types t ON p.user_type_id = t.user_type_id
                WHERE p.object_id = OBJECT_ID(@spName)
                ORDER BY p.parameter_id;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@spName", cleanSp);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                string pName = reader.IsDBNull(0) ? "" : reader.GetString(0);
                if (string.IsNullOrWhiteSpace(pName)) continue; // ignore return value parameter

                string typeName = reader.IsDBNull(1) ? "" : reader.GetString(1).ToLowerInvariant();

                SpParamType paramType = typeName switch
                {
                    "int" or "bigint" or "smallint" or "tinyint" => SpParamType.Int,
                    "decimal" or "numeric" or "money" or "smallmoney" or "float" or "real" => SpParamType.Decimal,
                    "bit" => SpParamType.Boolean,
                    "datetime" or "datetime2" or "date" or "smalldatetime" or "time" => SpParamType.DateTime,
                    _ => SpParamType.String
                };

                list.Add(new SpParameterConfig
                {
                    Name = pName,
                    DataType = paramType,
                    IsNull = true,
                    Value = null
                });
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error($"'{procedureName}' parametreleri veritabanından çekilirken hata oluştu: {ex.Message}", ex);
            throw;
        }

        return list;
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
        string sqlPreview = task.GetSqlPreview();

        AppLogger.Info($"[BAŞLADI] Görev: '{taskDisplayName}' ({sqlPreview}) çalıştırılıyor...");

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

                    SqlDbType dbType = p.DataType switch
                    {
                        SpParamType.Int => SqlDbType.Int,
                        SpParamType.Decimal => SqlDbType.Decimal,
                        SpParamType.Boolean => SqlDbType.Bit,
                        SpParamType.DateTime => SqlDbType.DateTime,
                        _ => SqlDbType.VarChar
                    };

                    SqlParameter sqlParam = cmd.Parameters.Add(pName, dbType);

                    // NULL Kontrolü
                    if (p.IsNull || string.IsNullOrWhiteSpace(p.Value) || string.Equals(p.Value, "NULL", StringComparison.OrdinalIgnoreCase))
                    {
                        sqlParam.Value = DBNull.Value;
                    }
                    else
                    {
                        // Sabit Değer Kontrolü & Tip Dönüşümü
                        string rawVal = p.Value.Trim();
                        switch (p.DataType)
                        {
                            case SpParamType.Int:
                                if (int.TryParse(rawVal, out int intVal))
                                {
                                    sqlParam.Value = intVal;
                                }
                                else
                                {
                                    sqlParam.Value = DBNull.Value;
                                }
                                break;

                            case SpParamType.Decimal:
                                if (decimal.TryParse(rawVal, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal decVal) ||
                                    decimal.TryParse(rawVal, out decVal))
                                {
                                    sqlParam.Value = decVal;
                                }
                                else
                                {
                                    sqlParam.Value = DBNull.Value;
                                }
                                break;

                            case SpParamType.Boolean:
                                bool bVal = rawVal == "1" || string.Equals(rawVal, "true", StringComparison.OrdinalIgnoreCase);
                                sqlParam.Value = bVal;
                                break;

                            case SpParamType.DateTime:
                                if (string.Equals(rawVal, "NOW", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(rawVal, "BUGUN", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(rawVal, "TODAY", StringComparison.OrdinalIgnoreCase))
                                {
                                    sqlParam = cmd.Parameters.Add(pName, SqlDbType.DateTime);
                                    sqlParam.Value = DateTime.Now;
                                }
                                else if (DateTime.TryParse(rawVal, out DateTime dtVal))
                                {
                                    sqlParam = cmd.Parameters.Add(pName, SqlDbType.DateTime);
                                    sqlParam.Value = dtVal;
                                }
                                else
                                {
                                    sqlParam = cmd.Parameters.Add(pName, SqlDbType.VarChar);
                                    sqlParam.Value = rawVal;
                                }
                                break;

                            default: // String
                                sqlParam = cmd.Parameters.Add(pName, SqlDbType.VarChar);
                                sqlParam.Value = rawVal;
                                break;
                        }
                    }
                }
            }

            int rowsAffected = await cmd.ExecuteNonQueryAsync(cancellationToken);
            sw.Stop();

            task.LastRunTime = DateTime.Now;
            task.LastRunStatus = "Başarılı";
            task.LastRunDuration = sw.Elapsed;

            string msg = $"'{taskDisplayName}' ({sqlPreview}) başarıyla tamamlandı. (Süre: {sw.ElapsedMilliseconds} ms, Etkilenen Satır: {rowsAffected})";
            AppLogger.Success($"[TAMAMLANDI] {msg}");
            return (true, msg, sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            task.LastRunTime = DateTime.Now;
            task.LastRunStatus = $"Hata: {ex.Message}";
            task.LastRunDuration = sw.Elapsed;

            string errorMsg = $"'{taskDisplayName}' ({sqlPreview}) çalıştırılırken hata: {ex.Message}";
            AppLogger.Error(errorMsg, ex);
            return (false, errorMsg, sw.Elapsed);
        }
    }
}
