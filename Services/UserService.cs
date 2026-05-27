using ClientProManager.Data;
using ClientProManager.Models;
using ClientProManager.Services.Security;
using Microsoft.EntityFrameworkCore;

namespace ClientProManager.Services;

public interface IUserService
{
    Task<List<User>> GetUsersAsync();
    Task SaveUserAsync(User user, string? password);
    Task DisableUserAsync(int id);
    Task ResetPasswordAsync(int id, string newPassword);
}

public class UserService(
    IDbContextFactory<ClientProDbContext> dbFactory,
    IPasswordHasher passwordHasher,
    IActivityLogService activityLogService) : IUserService
{
    public async Task<List<User>> GetUsersAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Users.AsNoTracking().OrderBy(x => x.Username).ToListAsync();
    }

    public async Task SaveUserAsync(User user, string? password)
    {
        if (string.IsNullOrWhiteSpace(user.FullName) || string.IsNullOrWhiteSpace(user.Username))
        {
            throw new InvalidOperationException("Full name and username are required.");
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var duplicate = await db.Users.AnyAsync(x => x.Username == user.Username && x.Id != user.Id);
        if (duplicate)
        {
            throw new InvalidOperationException("Username must be unique.");
        }

        user.UpdatedAt = DateTime.Now;
        if (user.Id == 0)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("Password is required for new users.");
            }

            user.PasswordHash = passwordHasher.HashPassword(password);
            user.MustChangePassword = true;
            user.CreatedAt = DateTime.Now;
            db.Users.Add(user);
        }
        else
        {
            var existing = await db.Users.FirstAsync(x => x.Id == user.Id);
            existing.FullName = user.FullName;
            existing.Username = user.Username;
            existing.Email = user.Email;
            existing.Phone = user.Phone;
            existing.Role = user.Role;
            existing.IsActive = user.IsActive;
            existing.UpdatedAt = DateTime.Now;
        }

        await db.SaveChangesAsync();
        await activityLogService.LogAsync("Save User", "User", user.Id, user.Username);
    }

    public async Task DisableUserAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var user = await db.Users.FirstAsync(x => x.Id == id);
        user.IsActive = false;
        user.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync();
        await activityLogService.LogAsync("Disable User", "User", id, user.Username);
    }

    public async Task ResetPasswordAsync(int id, string newPassword)
    {
        if (newPassword.Length < 6)
        {
            throw new InvalidOperationException("Password must be be at least 6 characters.");
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var user = await db.Users.FirstAsync(x => x.Id == id);
        user.PasswordHash = passwordHasher.HashPassword(newPassword);
        user.MustChangePassword = true;
        user.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync();
        await activityLogService.LogAsync("Reset Password", "User", id, user.Username);
    }
}
