using System.Security.Cryptography;
using System.Text;

namespace FsoftERP.KontrolServis.Security;

public static class SecurityHelper
{
    private const string EncPrefix = "ENC(";
    private const string EncSuffix = ")";

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
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] cipherBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.LocalMachine);
            return $"{EncPrefix}{Convert.ToBase64String(cipherBytes)}{EncSuffix}";
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

        try
        {
            var trimmed = cipherText.Trim();
            string b64 = trimmed[EncPrefix.Length..^EncSuffix.Length];
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
