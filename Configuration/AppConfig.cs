using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using FsoftERP.KontrolServis.Security;
using Microsoft.Data.SqlClient;

namespace FsoftERP.KontrolServis.Configuration;

public class AppConfig
{
    public DatabaseConfig Database { get; set; } = new();
    public DepoStokTarihceConfig DepoStokTarihce { get; set; } = new();
    public SparisDurumKontrolConfig SparisDurumKontrol { get; set; } = new();
    public ServiceConfig Service { get; set; } = new();
    public LoggingConfig Logging { get; set; } = new();

    public static string DefaultConfigPath => Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    public static AppConfig Load(string? path = null)
    {
        string filePath = path ?? DefaultConfigPath;
        if (!File.Exists(filePath))
        {
            var newConfig = new AppConfig();
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
            return config;
        }
        catch
        {
            return new AppConfig();
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

        // Şifreleri kaydederken DPAPI ile koru
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

public class DepoStokTarihceConfig
{
    public bool Enabled { get; set; } = true;
    public List<string> ExecutionTimes { get; set; } = new() { "12:00", "19:00" };
    public int CommandTimeoutSeconds { get; set; } = 1000;
}

public class SparisDurumKontrolConfig
{
    public bool Enabled { get; set; } = true;
    public int IntervalMinutes { get; set; } = 3;
    public string? SiparisNo { get; set; } = null;
    public int CommandTimeoutSeconds { get; set; } = 1000;
}

public class ServiceConfig
{
    public string ServiceName { get; set; } = "FsoftERPSKWS";
    public string DisplayName { get; set; } = "FsoftERP Kontrol Servisi";
    public string Description { get; set; } = "FsoftERP Depo Stok Tarihçe ve Sipariş Durum Kontrol Windows Servisi";
}

public class LoggingConfig
{
    public string LogDirectory { get; set; } = "logs";
    public int RetentionDays { get; set; } = 30;
}
