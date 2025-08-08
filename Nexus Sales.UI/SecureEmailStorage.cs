using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public static class SecureEmailStorage
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "NexusSales", "user_email.dat");

    public static void SaveEmail(string email)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
        var data = Encoding.UTF8.GetBytes(email);
        var encrypted = System.Security.Cryptography.ProtectedData.Protect(data, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
        File.WriteAllBytes(FilePath, encrypted);
    }

    public static string LoadEmail()
    {
        if (!File.Exists(FilePath)) return null;
        var encrypted = File.ReadAllBytes(FilePath);
        var decrypted = System.Security.Cryptography.ProtectedData.Unprotect(encrypted, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(decrypted);
    }

    public static void ClearEmail()
    {
        if (File.Exists(FilePath))
            File.Delete(FilePath);
    }
}