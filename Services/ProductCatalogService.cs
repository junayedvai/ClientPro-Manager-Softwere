using ClientProManager.Data;
using ClientProManager.Models;
using Microsoft.EntityFrameworkCore;

namespace ClientProManager.Services;

public interface IProductCatalogService
{
    Task<List<ProductService>> GetProductsAsync(string? search = null, string? category = null);
    Task<List<string>> GetCategoriesAsync();
    Task SaveProductAsync(ProductService item);
    Task DisableProductAsync(int id);
}

public class ProductCatalogService(
    IDbContextFactory<ClientProDbContext> dbFactory,
    IActivityLogService activityLogService) : IProductCatalogService
{
    public async Task<List<ProductService>> GetProductsAsync(string? search = null, string? category = null)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var query = db.ProductServices.AsNoTracking().Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Name.Contains(term) || x.Code.Contains(term) || x.Category.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(category) && category != "All")
        {
            query = query.Where(x => x.Category == category);
        }

        return await query.OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<List<string>> GetCategoriesAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var categories = await db.ProductServices
            .Where(x => x.IsActive && x.Category != "")
            .Select(x => x.Category)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();
        categories.Insert(0, "All");
        return categories;
    }

    public async Task SaveProductAsync(ProductService item)
    {
        if (string.IsNullOrWhiteSpace(item.Name))
        {
            throw new InvalidOperationException("Name is required.");
        }

        if (string.IsNullOrWhiteSpace(item.Code))
        {
            throw new InvalidOperationException("Code is required.");
        }

        if (item.UnitPrice < 0)
        {
            throw new InvalidOperationException("Unit price cannot be negative.");
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var duplicateCode = await db.ProductServices.AnyAsync(x => x.Code == item.Code && x.Id != item.Id);
        if (duplicateCode)
        {
            throw new InvalidOperationException("Product/service code must be unique.");
        }

        item.UpdatedAt = DateTime.Now;
        if (item.Id == 0)
        {
            item.CreatedAt = DateTime.Now;
            db.ProductServices.Add(item);
            await db.SaveChangesAsync();
            await activityLogService.LogAsync("Create Product/Service", "ProductService", item.Id, item.Name);
        }
        else
        {
            db.ProductServices.Update(item);
            await db.SaveChangesAsync();
            await activityLogService.LogAsync("Edit Product/Service", "ProductService", item.Id, item.Name);
        }
    }

    public async Task DisableProductAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var item = await db.ProductServices.FirstAsync(x => x.Id == id);
        item.IsActive = false;
        item.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync();
        await activityLogService.LogAsync("Disable Product/Service", "ProductService", item.Id, item.Name);
    }
}
