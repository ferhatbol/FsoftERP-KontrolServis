using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using FsoftERP.KontrolServis.Security;
using Microsoft.Data.SqlClient;

namespace FsoftERP.KontrolServis.Configuration;

public enum SpScheduleType
{
    SpecificTimes = 0,    // Belirtilen Saatlerde (Örn: 12:00, 19:00)
    IntervalMinutes = 1   // Belirli Dakika Aralıklarında (Örn: 3 dakikada bir)
}

public enum SpParamType
{
    String = 0,      // Metin (VarChar)
    Int = 1,         // Tamsayı (Int)
    Decimal = 2,     // Ondalıklı Sayı (Decimal)
    Boolean = 3,     // Bit / Mantıksal (True/False - 1/0)
    DateTime = 4     // Tarih / Saat (DateTime)
}

public class SpParameterConfig
{
    public string Name { get; set; } = "";
    public bool IsNull { get; set; } = true; // true ise doğrudan NULL (DBNull.Value) atanır
    public SpParamType DataType { get; set; } = SpParamType.String;
    public string? Value { get; set; } = null; // Sabit değer (IsNull false ise kullanılır)

    public string GetDisplaySummary()
    {
        string pName = string.IsNullOrWhiteSpace(Name) ? "@Param" : (Name.StartsWith("@") ? Name : "@" + Name);
        if (IsNull)
        {
            return $"{pName} = NULL";
        }

        if (DataType == SpParamType.String || DataType == SpParamType.DateTime)
        {
            return $"{pName} = '{Value}'";
        }

        return $"{pName} = {Value}";
    }

    public SpParameterConfig Clone()
    {
        return new SpParameterConfig
        {
            Name = Name,
            IsNull = IsNull,
            DataType = DataType,
            Value = Value
        };
    }
}

public class SpTaskConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Yeni Görev";
    public string ProcedureName { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public SpScheduleType ScheduleType { get; set; } = SpScheduleType.IntervalMinutes;
    public int IntervalMinutes { get; set; } = 3;
    public List<string> ExecutionTimes { get; set; } = new() { "12:00", "19:00" };
    public List<SpParameterConfig> Parameters { get; set; } = new();
    public int CommandTimeoutSeconds { get; set; } = 1000;

    [JsonIgnore]
    public DateTime? LastRunTime { get; set; }
    [JsonIgnore]
    public string? LastRunStatus { get; set; }
    [JsonIgnore]
    public TimeSpan? LastRunDuration { get; set; }

    public string GetScheduleSummary()
    {
        if (ScheduleType == SpScheduleType.IntervalMinutes)
        {
            return $"⏱️ Her {IntervalMinutes} dakikada bir";
        }
        else
        {
            string times = ExecutionTimes != null && ExecutionTimes.Count > 0 
                ? string.Join(", ", ExecutionTimes.OrderBy(t => t)) 
                : "Saat belirtilmedi";
            return $"🕒 Günde {ExecutionTimes?.Count ?? 0} kez: {times}";
        }
    }

    public string GetParametersSummary()
    {
        if (Parameters == null || Parameters.Count == 0) return "(Parametresiz)";
        return string.Join(", ", Parameters.Select(p => p.GetDisplaySummary()));
    }

    public string GetSqlPreview()
    {
        string sp = string.IsNullOrWhiteSpace(ProcedureName) ? "SP_ADI" : ProcedureName;
        if (Parameters == null || Parameters.Count == 0) return $"EXEC {sp}";
        return $"EXEC {sp} {string.Join(", ", Parameters.Select(p => p.GetDisplaySummary()))}";
    }

    public SpTaskConfig Clone()
    {
        return new SpTaskConfig
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = Name + " (Kopya)",
            ProcedureName = ProcedureName,
            Enabled = Enabled,
            ScheduleType = ScheduleType,
            IntervalMinutes = IntervalMinutes,
            ExecutionTimes = new List<string>(ExecutionTimes),
            Parameters = Parameters.Select(p => p.Clone()).ToList(),
            CommandTimeoutSeconds = CommandTimeoutSeconds
        };
    }
}

public class AppConfig
{
    public DatabaseConfig Database { get; set; } = new();
    public List<SpTaskConfig> Tasks { get; set; } = new();
    public ServiceConfig Service { get; set; } = new();
    public LoggingConfig Logging { get; set; } = new();

    public static string DefaultConfigPath => Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    public static AppConfig Load(string? path = null)
    {
        string filePath = path ?? DefaultConfigPath;
        if (!File.Exists(filePath))
        {
            var newConfig = CreateDefaultConfig();
            Save(filePath, newConfig);
            return newConfig;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };
            var config = JsonSerializer.Deserialize<AppConfig>(json, options) ?? new AppConfig();

            if (config.Tasks == null || config.Tasks.Count == 0)
            {
                config.Tasks = CreateDefaultTasks();
                Save(filePath, config);
            }

            return config;
        }
        catch
        {
            return CreateDefaultConfig();
        }
    }

    public static void Save(string? path, AppConfig config)
    {
        string filePath = path ?? DefaultConfigPath;
        string? dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        if (!string.IsNullOrEmpty(config.Database.Password) && !SecurityHelper.IsEncrypted(config.Database.Password))
        {
            config.Database.Password = SecurityHelper.Protect(config.Database.Password);
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        string json = JsonSerializer.Serialize(config, options);
        string tmp = filePath + ".tmp";
        File.WriteAllText(tmp, json);
        File.Move(tmp, filePath, overwrite: true);
    }

    private static AppConfig CreateDefaultConfig()
    {
        return new AppConfig
        {
            Database = new DatabaseConfig(),
            Tasks = CreateDefaultTasks(),
            Service = new ServiceConfig(),
            Logging = new LoggingConfig()
        };
    }

    public static List<SpTaskConfig> CreateDefaultTasks()
    {
        return new List<SpTaskConfig>
        {
            new SpTaskConfig
            {
                Id = "task_depo_stok",
                Name = "Depo Stok Tarihçe Güncelleme",
                ProcedureName = "Sp_Set_DepoStokTarihce",
                Enabled = true,
                ScheduleType = SpScheduleType.SpecificTimes,
                ExecutionTimes = new List<string> { "12:00", "19:00" },
                Parameters = new List<SpParameterConfig>(),
                CommandTimeoutSeconds = 1000
            },
            new SpTaskConfig
            {
                Id = "task_siparis_durum",
                Name = "Sipariş Durum Kontrol",
                ProcedureName = "Sp_Set_SparisDurumKontrol",
                Enabled = true,
                ScheduleType = SpScheduleType.IntervalMinutes,
                IntervalMinutes = 3,
                Parameters = new List<SpParameterConfig>
                {
                    new SpParameterConfig { Name = "@SiparisNo", IsNull = true, DataType = SpParamType.String, Value = null }
                },
                CommandTimeoutSeconds = 1000
            },
            new SpTaskConfig
            {
                Id = "task_satinalma_durum",
                Name = "Satınalma Durum Kontrol",
                ProcedureName = "Sp_Set_SatınalmaDurumKontrol",
                Enabled = true,
                ScheduleType = SpScheduleType.SpecificTimes,
                ExecutionTimes = new List<string> { "12:00" },
                Parameters = new List<SpParameterConfig>(),
                CommandTimeoutSeconds = 1000
            }
        };
    }
}

public class DatabaseConfig
{
    public string Server { get; set; } = "localhost";
    public string Database { get; set; } = "FsoftERP";
    public bool UseWindowsAuth { get; set; } = false;
    public string UserId { get; set; } = "sa";
    public string Password { get; set; } = "";
    public int ConnectionTimeoutSeconds { get; set; } = 30;
    public bool TrustServerCertificate { get; set; } = true;

    [JsonIgnore]
    public string PlainPassword => SecurityHelper.Unprotect(Password);

    public string BuildConnectionString()
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = Server,
            InitialCatalog = Database,
            ConnectTimeout = ConnectionTimeoutSeconds,
            TrustServerCertificate = TrustServerCertificate
        };

        if (UseWindowsAuth)
        {
            builder.IntegratedSecurity = true;
        }
        else
        {
            builder.IntegratedSecurity = false;
            builder.UserID = UserId;
            builder.Password = PlainPassword;
        }

        return builder.ConnectionString;
    }
}

public class ServiceConfig
{
    public string ServiceName { get; set; } = "FsoftERPSKWS";
    public string DisplayName { get; set; } = "FsoftERP Kontrol Servisi";
    public string Description { get; set; } = "FsoftERP Stored Procedure Otomasyon ve Kontrol Windows Servisi";
}

public class LoggingConfig
{
    public string LogDirectory { get; set; } = "logs";
    public int RetentionDays { get; set; } = 30;
}
