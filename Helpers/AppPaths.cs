using System.IO;

namespace ClientProManager.Helpers;

public static class AppPaths
{
    public static string AppDataFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ClientProManager");

    public static string DatabasePath => Path.Combine(AppDataFolder, "clientpro.db");
    public static string BackupFolder => Path.Combine(AppDataFolder, "Backups");
    public static string ExportFolder => Path.Combine(AppDataFolder, "Exports");
    public static string ConnectionString => $"Data Source={DatabasePath}";

    public static void EnsureFolders()
    {
        Directory.CreateDirectory(AppDataFolder);
        Directory.CreateDirectory(BackupFolder);
        Directory.CreateDirectory(ExportFolder);
    }
}
