using ClientProManager.Data;
using ClientProManager.Models;
using Microsoft.EntityFrameworkCore;

namespace ClientProManager.Services;

public interface IActivityLogService
{
    Task LogAsync(string action, string entityName = "", int? entityId = null, string description = "");
    Task<List<ActivityLog>> GetRecentAsync(int take = 100);
}

public class ActivityLogService(IDbContextFactory<ClientProDbContext> dbFactory, ISessionService sessionService) : IActivityLogService
{
    public async Task LogAsync(string action, string entityName = "", int? entityId = null, string description = "")
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.ActivityLogs.Add(new ActivityLog
        {
            UserId = sessionService.CurrentUser?.Id,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Description = description,
            CreatedAt = DateTime.Now
        });
        await db.SaveChangesAsync();
    }

    public async Task<List<ActivityLog>> GetRecentAsync(int take = 100)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.ActivityLogs
            .Include(x => x.User)
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .AsNoTracking()
            .ToListAsync();
    }
}
