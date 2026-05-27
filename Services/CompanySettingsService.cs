using ClientProManager.Data;
using ClientProManager.Models;
using Microsoft.EntityFrameworkCore;

namespace ClientProManager.Services;

public interface ICompanySettingsService
{
    Task<CompanySetting> GetAsync();
    Task SaveAsync(CompanySetting settings);
}

public class CompanySettingsService(
    IDbContextFactory<ClientProDbContext> dbFactory,
    IActivityLogService activityLogService) : ICompanySettingsService
{
    public async Task<CompanySetting> GetAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var settings = await db.CompanySettings.AsNoTracking().FirstOrDefaultAsync();
        return settings ?? new CompanySetting();
    }

    public async Task SaveAsync(CompanySetting settings)
    {
        if (string.IsNullOrWhiteSpace(settings.CompanyName))
        {
            throw new InvalidOperationException("Company name is required.");
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        settings.UpdatedAt = DateTime.Now;
        if (settings.Id == 0)
        {
            db.CompanySettings.Add(settings);
        }
        else
        {
            db.CompanySettings.Update(settings);
        }

        await db.SaveChangesAsync();
        await activityLogService.LogAsync("Save Company Settings", "CompanySettings", settings.Id, settings.CompanyName);
    }
}
