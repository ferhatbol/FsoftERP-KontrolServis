using System.Security.Cryptography;
using System.Text;

namespace FsoftERP.KontrolServis.Security;

public static class SecurityHelper
{
    private const string EncPrefix = "ENC(";
    private const string EncSuffix = ")";

    // FsoftERP Kontrol Servisi için AES-256 Anahtarı ve IV
    private static readonly byte[] Key = SHA256.HashData(Encoding.UTF8.GetBytes("FsoftERP_KontrolServis_SecKey_2026!#"));
    private static readonly byte[] Iv = MD5.HashData(Encoding.UTF8.GetBytes("FsoftERP_IV_2026"));

    public static bool IsEncrypted(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;
        var trimmed = input.Trim();
        return trimmed.StartsWith(EncPrefix, StringComparison.OrdinalIgnoreCase) &&
               trimmed.EndsWith(EncSuffix, StringComparison.OrdinalIgnoreCase);
    }

    public static string Protect(string? plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return string.Empty;
        if (IsEncrypted(plainText)) return plainText!;

        try
        {
            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = Iv;

            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                cs.Write(plainBytes, 0, plainBytes.Length);
                cs.FlushFinalBlock();
            }

            string b64 = Convert.ToBase64String(ms.ToArray());
            return $"{EncPrefix}{b64}{EncSuffix}";
        }
        catch
        {
            return plainText;
        }
    }

    public static string Unprotect(string? cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return string.Empty;
        if (!IsEncrypted(cipherText)) return cipherText!;

        string trimmed = cipherText.Trim();
        string b64 = trimmed[EncPrefix.Length..^EncSuffix.Length];

        // 1. Önce AES-256 ile çözmeyi dene (Makineler arası taşınabilir)
        try
        {
            byte[] cipherBytes = Convert.FromBase64String(b64);
            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = Iv;

            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
            {
                cs.Write(cipherBytes, 0, cipherBytes.Length);
                cs.FlushFinalBlock();
            }

            return Encoding.UTF8.GetString(ms.ToArray());
        }
        catch
        {
            // 2. Geriye dönük uyumluluk: Eski DPAPI LocalMachine ile şifrelenmiş olabilir
            try
            {
                byte[] cipherBytes = Convert.FromBase64String(b64);
                byte[] plainBytes = ProtectedData.Unprotect(cipherBytes, null, DataProtectionScope.LocalMachine);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                return cipherText;
            }
        }
    }
}
