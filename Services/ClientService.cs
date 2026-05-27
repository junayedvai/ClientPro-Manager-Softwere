using ClientProManager.Data;
using ClientProManager.Models;
using Microsoft.EntityFrameworkCore;

namespace ClientProManager.Services;

public interface IClientService
{
    Task<List<Client>> GetClientsAsync(string? search = null, string? category = null, string? paymentStatus = null);
    Task<Client?> GetClientProfileAsync(int clientId);
    Task SaveClientAsync(Client client);
    Task DisableClientAsync(int clientId);
    Task RecalculateClientDueAsync(int clientId);
}

public class ClientService(
    IDbContextFactory<ClientProDbContext> dbFactory,
    IActivityLogService activityLogService) : IClientService
{
    public async Task<List<Client>> GetClientsAsync(string? search = null, string? category = null, string? paymentStatus = null)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var query = db.Clients.AsNoTracking().Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.ClientName.Contains(term) ||
                x.CompanyName.Contains(term) ||
                x.Phone.Contains(term) ||
                x.Email.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(category) && category != "All")
        {
            query = query.Where(x => x.ClientCategory == category);
        }

        if (!string.IsNullOrWhiteSpace(paymentStatus) && paymentStatus != "All")
        {
            query = query.Where(x => x.PaymentStatus == paymentStatus);
        }

        return await query.OrderBy(x => x.ClientName).ToListAsync();
    }

    public async Task<Client?> GetClientProfileAsync(int clientId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Clients
            .Include(x => x.Documents)
            .Include(x => x.Invoices)
            .Include(x => x.Payments)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == clientId);
    }

    public async Task SaveClientAsync(Client client)
    {
        if (string.IsNullOrWhiteSpace(client.ClientName))
        {
            throw new InvalidOperationException("Client name is required.");
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        client.UpdatedAt = DateTime.Now;

        if (client.Id == 0)
        {
            client.CreatedAt = DateTime.Now;
            db.Clients.Add(client);
            await db.SaveChangesAsync();
            await activityLogService.LogAsync("Create Client", "Client", client.Id, client.ClientName);
        }
        else
        {
            db.Clients.Update(client);
            await db.SaveChangesAsync();
            await activityLogService.LogAsync("Edit Client", "Client", client.Id, client.ClientName);
        }
    }

    public async Task DisableClientAsync(int clientId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var client = await db.Clients.FirstAsync(x => x.Id == clientId);
        client.IsActive = false;
        client.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync();
        await activityLogService.LogAsync("Disable Client", "Client", client.Id, client.ClientName);
    }

    public async Task RecalculateClientDueAsync(int clientId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var client = await db.Clients.FirstOrDefaultAsync(x => x.Id == clientId);
        if (client is null)
        {
            return;
        }

        client.TotalDue = await db.Invoices
            .Where(x => x.ClientId == clientId && x.Status != InvoiceStatuses.Cancelled)
            .SumAsync(x => x.DueAmount);

        client.PaymentStatus = client.TotalDue <= 0 ? "Paid" : "Due";
        client.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync();
    }
}
