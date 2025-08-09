using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Npgsql;
using Newtonsoft.Json;
using System.Configuration;
using System.Security.Cryptography;
using System.Diagnostics;

namespace NexusUpdate
{
    public class UpdateManifest
    {
        public string LatestVersion { get; set; }
        public string ReleaseNotesUrl { get; set; }
        public List<ManifestFile> Files { get; set; }
    }

    public class ManifestFile
    {
        public string Path { get; set; }
        public string Hash { get; set; }
        public long Size { get; set; }
        public string DownloadUrl { get; set; }
        public string Description { get; set; }
    }

    internal class Program
    {
        static async Task Main(string[] args)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["NexusSalesConnection"].ConnectionString; // Replace with your actual connection string
            string updateManifestUrl = ConfigurationManager.AppSettings["UpdateManifestUrl"];

            // 1. Check update deadline from DB
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    using (var cmd = new NpgsqlCommand("SELECT deadline_utc, message FROM update_deadline WHERE is_active = TRUE LIMIT 1", conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var deadline = reader.GetDateTime(0);
                            var message = reader.IsDBNull(1) ? null : reader.GetString(1);
                            if (DateTime.UtcNow > deadline)
                            {
                                Console.WriteLine(message ?? "Update deadline has passed. Update is now mandatory.");
                                Logger.Log("INFO", "Deadline", "Main", "Update deadline has passed. Update is now mandatory.");
                                // Proceed to update logic, do not allow exit
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("FATAL", "Deadline", "Main", "Failed to check update deadline.", ex, "UPD-DB-001");
                return;
            }

            // 2. Download and validate update manifest
            UpdateManifest manifest = null;
            try
            {
                string manifestJson = await DownloadManifestAsync(updateManifestUrl);
                manifest = ParseManifest(manifestJson);
                Logger.Log("INFO", "Manifest", "Main", "Update manifest downloaded and parsed successfully.");
            }
            catch (Exception ex)
            {
                Logger.Log("FATAL", "Manifest", "Main", "Failed to download or parse update manifest.", ex, "UPD-MAN-001", new { updateManifestUrl });
                Console.WriteLine("Failed to download or parse update manifest. See log for details.");
                return;
            }

            // 3. Compare local version to manifest version
            try
            {
                string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NexusSales.UI.exe");
                Version localVersion = GetLocalVersion(exePath);
                Version onlineVersion = new Version(manifest.LatestVersion);

                Logger.Log("INFO", "Version", "Main", $"Local version: {localVersion}, Online version: {onlineVersion}");

                if (onlineVersion > localVersion)
                {
                    Console.WriteLine("Update required. Proceeding...");
                    Logger.Log("INFO", "Version", "Main", "Update required. Proceeding...");
                    // Continue with file download/validation logic
                    bool updateSuccess = await DownloadAndReplaceFilesAsync(manifest);
                    if (!updateSuccess)
                    {
                        return;
                    }
                }
                else
                {
                    Console.WriteLine("No update required.");
                    Logger.Log("INFO", "Version", "Main", "No update required.");
                }
            }
            catch (Exception ex)
            {
                Logger.Log("FATAL", "Version", "Main", "Failed to compare versions.", ex, "UPD-VER-001");
                Console.WriteLine("Failed to compare versions. See log for details.");
                return;
            }
        }

        private static async Task<string> DownloadManifestAsync(string manifestUrl)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    var response = await client.GetAsync(manifestUrl);
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync();
                }
                catch (Exception ex)
                {
                    Logger.Log("FATAL", "Network", "DownloadManifestAsync", $"Failed to download manifest from {manifestUrl}", ex, "UPD-NET-001", new { manifestUrl });
                    throw;
                }
            }
        }

        private static UpdateManifest ParseManifest(string manifestJson)
        {
            try
            {
                return JsonConvert.DeserializeObject<UpdateManifest>(manifestJson);
            }
            catch (Exception ex)
            {
                Logger.Log("FATAL", "Manifest", "ParseManifest", "Failed to parse update manifest JSON.", ex, "UPD-MAN-002");
                throw;
            }
        }

        private static Version GetLocalVersion(string exePath)
        {
            try
            {
                var assembly = Assembly.LoadFrom(exePath);
                return assembly.GetName().Version;
            }
            catch (Exception ex)
            {
                Logger.Log("FATAL", "Version", "GetLocalVersion", $"Failed to get version from {exePath}", ex, "UPD-VER-002", new { exePath });
                throw;
            }
        }

        private static async Task<bool> DownloadAndReplaceFilesAsync(UpdateManifest manifest)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "NexusUpdate_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                foreach (var file in manifest.Files)
                {
                    string tempFilePath = Path.Combine(tempDir, file.Path.Replace('/', Path.DirectorySeparatorChar));
                    Directory.CreateDirectory(Path.GetDirectoryName(tempFilePath));

                    Logger.Log("INFO", "FileDownload", "DownloadAndReplaceFilesAsync", $"Downloading {file.Path}...");
                    using (var client = new HttpClient())
                    using (var response = await client.GetAsync(file.DownloadUrl))
                    {
                        response.EnsureSuccessStatusCode();
                        using (var fs = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await response.Content.CopyToAsync(fs);
                        }
                    }

                    // Verify SHA256
                    Logger.Log("INFO", "FileVerify", "DownloadAndReplaceFilesAsync", $"Verifying {file.Path}...");
                    string actualHash = ComputeSHA256(tempFilePath);
                    if (!string.Equals(actualHash, file.Hash, StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.Log("FATAL", "FileVerify", "DownloadAndReplaceFilesAsync", $"Hash mismatch for {file.Path}. Expected: {file.Hash}, Actual: {actualHash}", null, "UPD-FILE-001", new { file.Path });
                        Console.WriteLine($"Hash mismatch for {file.Path}. Update aborted.");
                        return false;
                    }
                }

                // Attempt to gracefully close NexusSales.UI.exe
                Logger.Log("INFO", "Process", "DownloadAndReplaceFilesAsync", "Checking for running NexusSales.UI.exe...");
                foreach (var proc in Process.GetProcessesByName("NexusSales.UI"))
                {
                    try
                    {
                        Logger.Log("INFO", "Process", "DownloadAndReplaceFilesAsync", "Attempting to close NexusSales.UI.exe...");
                        proc.CloseMainWindow();
                        if (!proc.WaitForExit(5000))
                        {
                            Logger.Log("WARNING", "Process", "DownloadAndReplaceFilesAsync", "Forcing NexusSales.UI.exe to close.");
                            proc.Kill();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log("ERROR", "Process", "DownloadAndReplaceFilesAsync", "Failed to close NexusSales.UI.exe.", ex, "UPD-PROC-001");
                    }
                }

                // Replace files
                string installDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Nexus");
                Directory.CreateDirectory(installDir);
                foreach (var file in manifest.Files)
                {
                    string tempFilePath = Path.Combine(tempDir, file.Path.Replace('/', Path.DirectorySeparatorChar));
                    string destFilePath = Path.Combine(installDir, file.Path.Replace('/', Path.DirectorySeparatorChar));
                    Directory.CreateDirectory(Path.GetDirectoryName(destFilePath));
                    Logger.Log("INFO", "FileReplace", "DownloadAndReplaceFilesAsync", $"Replacing {file.Path}...");
                    File.Copy(tempFilePath, destFilePath, true);
                }

                Logger.Log("INFO", "Update", "DownloadAndReplaceFilesAsync", "All files updated successfully.");
                Console.WriteLine("Update completed successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log("FATAL", "Update", "DownloadAndReplaceFilesAsync", "Update failed.", ex, "UPD-GEN-001");
                Console.WriteLine("Update failed. See log for details.");
                return false;
            }
            finally
            {
                // Clean up temp files
                try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { }
            }
        }

        private static string ComputeSHA256(string filePath)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                var hash = sha.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }

    public static class Logger
    {
        public static bool DebugMode { get; set; } = false;
        public static void Log(string level, string module, string method, string message, Exception ex = null, string errorCode = null, object context = null)
        {
            var logLine = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} [{level}] {module}.{method} {message}";
            if (!string.IsNullOrEmpty(errorCode)) logLine += $" [ErrorCode: {errorCode}]";
            if (context != null) logLine += $" [Context: {JsonConvert.SerializeObject(context)}]";
            if (ex != null) logLine += $"\nException: {ex.Message}\nStackTrace: {ex.StackTrace}";
            Console.WriteLine(logLine);
            File.AppendAllText("NexusUpdate.log", logLine + Environment.NewLine);
        }
    }
}