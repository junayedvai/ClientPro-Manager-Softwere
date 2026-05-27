using System.IO;
using ClientProManager.Data;
using ClientProManager.Helpers;
using ClientProManager.Models;
using Microsoft.EntityFrameworkCore;

namespace ClientProManager.Services;

public interface IBackupService
{
    Task<string> CreateBackupAsync(string backupType = "Manual");
    Task RestoreBackupAsync(string backupFilePath);
    Task<string> CloudBackupPlaceholderAsync();
    Task<List<BackupLog>> GetBackupLogsAsync();
}

public class BackupService(
    IDbContextFactory<ClientProDbContext> dbFactory,
    ISessionService sessionService,
    IActivityLogService activityLogService) : IBackupService
{
    public async Task<string> CreateBackupAsync(string backupType = "Manual")
    {
        AppPaths.EnsureFolders();

        if (!File.Exists(AppPaths.DatabasePath))
        {
            await using var initDb = await dbFactory.CreateDbContextAsync();
            await initDb.Database.EnsureCreatedAsync();
        }

        var fileName = $"ClientPro_Backup_{DateTime.Now:yyyy-MM-dd_HHmm}.db";
        var destination = Path.Combine(AppPaths.BackupFolder, fileName);
        File.Copy(AppPaths.DatabasePath, destination, overwrite: true);

        await using var db = await dbFactory.CreateDbContextAsync();
        db.BackupLogs.Add(new BackupLog
        {
            BackupFilePath = destination,
            BackupType = backupType,
            Status = "Success",
            CreatedByUserId = sessionService.CurrentUser?.Id,
            CreatedAt = DateTime.Now
        });
        await db.SaveChangesAsync();
        await activityLogService.LogAsync("Backup Database", "BackupLog", null, destination);
        return destination;
    }

    public async Task RestoreBackupAsync(string backupFilePath)
    {
        if (string.IsNullOrWhiteSpace(backupFilePath) || !File.Exists(backupFilePath))
        {
            throw new InvalidOperationException("Backup file was not found.");
        }

        File.Copy(backupFilePath, AppPaths.DatabasePath, overwrite: true);
        await activityLogService.LogAsync("Restore Database", "Backup", null, backupFilePath);
    }

    public Task<string> CloudBackupPlaceholderAsync()
    {
        return Task.FromResult("Cloud backup placeholder is ready for future Google Drive/OneDrive integration.");
    }

    public async Task<List<BackupLog>> GetBackupLogsAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.BackupLogs.OrderByDescending(x => x.CreatedAt).AsNoTracking().ToListAsync();
    }
}
