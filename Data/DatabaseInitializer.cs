using ClientProManager.Helpers;
using ClientProManager.Models;
using ClientProManager.Services.Security;
using Microsoft.EntityFrameworkCore;

namespace ClientProManager.Data;

public interface IDatabaseInitializer
{
    Task InitializeAsync();
}

public class DatabaseInitializer(IDbContextFactory<ClientProDbContext> dbFactory, IPasswordHasher passwordHasher) : IDatabaseInitializer
{
    public async Task InitializeAsync()
    {
        AppPaths.EnsureFolders();

        await using var db = await dbFactory.CreateDbContextAsync();
        await db.Database.EnsureCreatedAsync();

        var adminUser = await db.Users.FirstOrDefaultAsync(x => x.Username == "admin");
        if (adminUser is null)
        {
            adminUser = new User
            {
                FullName = "System Administrator",
                Username = "admin",
                Email = "admin@clientpro.local",
                Phone = "",
                PasswordHash = passwordHasher.HashPassword("admin123"),
                Role = UserRoles.Admin,
                MustChangePassword = true,
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            db.Users.Add(adminUser);
            await db.SaveChangesAsync();
        }

        if (!await db.CompanySettings.AnyAsync())
        {
            db.CompanySettings.Add(new CompanySetting
            {
                CompanyName = "ClientPro Manager",
                OwnerName = "Business Owner",
                Phone = "017XXXXXXXX",
                Email = "info@example.com",
                Address = "Dhaka, Bangladesh",
                Website = "https://example.com",
                Currency = "BDT",
                TaxName = "VAT",
                DefaultTaxRate = 5,
                InvoicePrefix = "INV",
                QuotationPrefix = "QT",
                PaymentPrefix = "PAY",
                TermsAndConditions = "Payment is due by the document due date. Goods and services once delivered are subject to agreed business terms."
            });
        }

        if (!await db.Clients.AnyAsync())
        {
            db.Clients.Add(new Client
            {
                ClientName = "Rahim Traders",
                CompanyName = "Rahim Trading Agency",
                Phone = "017XXXXXXXX",
                Email = "rahim@example.com",
                Address = "Dhaka, Bangladesh",
                BusinessType = "Trading",
                ClientCategory = "Regular",
                Notes = "Seed client for demo records.",
                PreviousWorkHistory = "Website maintenance and business support.",
                PaymentStatus = "Due",
                TotalDue = 15000,
                LastServiceDate = new DateTime(2026, 5, 20),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsActive = true
            });
        }

        if (!await db.ProductServices.AnyAsync())
        {
            db.ProductServices.AddRange(
                new ProductService
                {
                    Name = "Website Maintenance",
                    Code = "WEB-MNT-001",
                    Category = "IT Service",
                    Description = "Monthly website update and support",
                    Unit = "Month",
                    UnitPrice = 5000,
                    TaxRate = 5,
                    IsService = true,
                    IsProduct = false,
                    IsActive = true
                },
                new ProductService
                {
                    Name = "Building Design Consultancy",
                    Code = "BLD-DSN-001",
                    Category = "Construction",
                    Description = "Building design and consultancy",
                    Unit = "Square Feet",
                    UnitPrice = 250,
                    TaxRate = 5,
                    MaterialType = "Cement, Rod, Brick",
                    IsService = true,
                    IsProduct = false,
                    IsActive = true
                });
        }

        if (!await db.Expenses.AnyAsync())
        {
            db.Expenses.Add(new Expense
            {
                ExpenseType = "Office Rent",
                Category = "Office Rent",
                Amount = 25000,
                ExpenseDate = new DateTime(2026, 5, 27),
                PaidBy = "Cash",
                Note = "Monthly office rent",
                CreatedByUserId = adminUser.Id,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });
        }

        await db.SaveChangesAsync();
    }
}
