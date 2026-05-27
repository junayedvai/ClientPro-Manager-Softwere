using ClientProManager.Data;
using ClientProManager.Models;
using ClientProManager.Services.Security;
using Microsoft.EntityFrameworkCore;

namespace ClientProManager.Services;

public sealed record LoginResult(bool Success, string Message, User? User);

public interface IAuthenticationService
{
    Task<LoginResult> LoginAsync(string username, string password);
    Task ChangePasswordAsync(int userId, string currentPassword, string newPassword);
}

public class AuthenticationService(
    IDbContextFactory<ClientProDbContext> dbFactory,
    IPasswordHasher passwordHasher,
    ISessionService sessionService,
    IActivityLogService activityLogService) : IAuthenticationService
{
    public async Task<LoginResult> LoginAsync(string username, string password)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var user = await db.Users.FirstOrDefaultAsync(x => x.Username == username.Trim());

        if (user is null || !user.IsActive)
        {
            return new LoginResult(false, "Invalid username or inactive account.", null);
        }

        if (!passwordHasher.VerifyPassword(password, user.PasswordHash))
        {
            return new LoginResult(false, "Invalid username or password.", null);
        }

        user.LastLoginAt = DateTime.Now;
        await db.SaveChangesAsync();

        sessionService.CurrentUser = user;
        await activityLogService.LogAsync("Login", "User", user.Id, $"{user.Username} logged in.");

        return new LoginResult(true, user.MustChangePassword ? "Login successful. Please change the default password soon." : "Login successful.", user);
    }

    public async Task ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        if (newPassword.Length < 6)
        {
            throw new InvalidOperationException("Password must be at least 6 characters.");
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var user = await db.Users.FirstAsync(x => x.Id == userId);
        if (!passwordHasher.VerifyPassword(currentPassword, user.PasswordHash))
        {
            throw new InvalidOperationException("Current password is incorrect.");
        }

        user.PasswordHash = passwordHasher.HashPassword(newPassword);
        user.MustChangePassword = false;
        user.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync();
        await activityLogService.LogAsync("Change Password", "User", user.Id, "Password changed.");
    }
}
