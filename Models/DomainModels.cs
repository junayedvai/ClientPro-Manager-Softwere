using System.ComponentModel.DataAnnotations;

namespace ClientProManager.Models;

public static class UserRoles
{
    public const string Admin = "Admin";
    public const string Staff = "Staff";
    public const string Accountant = "Accountant";

    public static readonly string[] All = [Admin, Staff, Accountant];
}

public static class InvoiceStatuses
{
    public const string Paid = "Paid";
    public const string Unpaid = "Unpaid";
    public const string Partial = "Partial";
    public const string Overdue = "Overdue";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All = [Paid, Unpaid, Partial, Overdue, Cancelled];
}

public static class QuotationStatuses
{
    public const string Draft = "Draft";
    public const string Sent = "Sent";
    public const string Accepted = "Accepted";
    public const string Rejected = "Rejected";
    public const string Converted = "Converted";

    public static readonly string[] All = [Draft, Sent, Accepted, Rejected, Converted];
}

public static class PaymentMethods
{
    public static readonly string[] All =
    [
        "Cash",
        "Bank Transfer",
        "bKash",
        "Nagad",
        "Rocket",
        "Cheque",
        "Card",
        "Other"
    ];
}

public static class ExpenseCategories
{
    public static readonly string[] All =
    [
        "Office Rent",
        "Salary",
        "Transport",
        "Internet",
        "Utility Bill",
        "Raw Materials",
        "Software/Tools",
        "Maintenance",
        "Other"
    ];
}

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(60)]
    public string Username { get; set; } = string.Empty;

    [MaxLength(160)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(40)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string Role { get; set; } = UserRoles.Staff;

    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public DateTime? LastLoginAt { get; set; }
}

public class Client
{
    public int Id { get; set; }

    [Required, MaxLength(160)]
    public string ClientName { get; set; } = string.Empty;

    [MaxLength(160)]
    public string CompanyName { get; set; } = string.Empty;

    [MaxLength(40)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(160)]
    public string Email { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;
    public string BusinessType { get; set; } = string.Empty;
    public string ClientCategory { get; set; } = "Regular";
    public string Notes { get; set; } = string.Empty;
    public string PreviousWorkHistory { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = "Due";
    public decimal TotalDue { get; set; }
    public DateTime? LastServiceDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;

    public ICollection<ClientDocument> Documents { get; set; } = [];
    public ICollection<Invoice> Invoices { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
    public ICollection<Quotation> Quotations { get; set; } = [];
}

public class ClientDocument
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.Now;

    public Client? Client { get; set; }
}

public class ProductService
{
    public int Id { get; set; }

    [Required, MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(60)]
    public string Code { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Unit { get; set; } = "Unit";
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public string MaterialType { get; set; } = string.Empty;
    public bool IsService { get; set; } = true;
    public bool IsProduct { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;
}

public class Quotation
{
    public int Id { get; set; }

    [Required, MaxLength(60)]
    public string QuotationNumber { get; set; } = string.Empty;

    public int ClientId { get; set; }
    public DateTime QuotationDate { get; set; } = DateTime.Today;
    public DateTime ValidUntil { get; set; } = DateTime.Today.AddDays(15);
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public string TermsAndConditions { get; set; } = string.Empty;
    public string Status { get; set; } = QuotationStatuses.Draft;
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public Client? Client { get; set; }
    public User? CreatedByUser { get; set; }
    public ICollection<QuotationItem> Items { get; set; } = [];
}

public class QuotationItem
{
    public int Id { get; set; }
    public int QuotationId { get; set; }
    public int ProductServiceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public string Unit { get; set; } = "Unit";
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal LineTotal { get; set; }

    public Quotation? Quotation { get; set; }
    public ProductService? ProductService { get; set; }
}

public class Invoice
{
    public int Id { get; set; }

    [Required, MaxLength(60)]
    public string InvoiceNumber { get; set; } = string.Empty;

    public int ClientId { get; set; }
    public DateTime InvoiceDate { get; set; } = DateTime.Today;
    public DateTime DueDate { get; set; } = DateTime.Today.AddDays(7);
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DueAmount { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string Status { get; set; } = InvoiceStatuses.Unpaid;
    public string Notes { get; set; } = string.Empty;
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public Client? Client { get; set; }
    public User? CreatedByUser { get; set; }
    public ICollection<InvoiceItem> Items { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
}

public class InvoiceItem
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public int ProductServiceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public string Unit { get; set; } = "Unit";
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal LineTotal { get; set; }

    public Invoice? Invoice { get; set; }
    public ProductService? ProductService { get; set; }
}

public class Payment
{
    public int Id { get; set; }

    [Required, MaxLength(60)]
    public string PaymentNumber { get; set; } = string.Empty;

    public int InvoiceId { get; set; }
    public int ClientId { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public string PaymentMethod { get; set; } = "Cash";
    public decimal PaidAmount { get; set; }
    public decimal DueAmountAfterPayment { get; set; }
    public string TransactionNote { get; set; } = string.Empty;
    public int ReceivedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public Invoice? Invoice { get; set; }
    public Client? Client { get; set; }
    public User? ReceivedByUser { get; set; }
}

public class Expense
{
    public int Id { get; set; }
    public string ExpenseType { get; set; } = string.Empty;
    public string Category { get; set; } = "Other";
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; } = DateTime.Today;
    public string PaidBy { get; set; } = "Cash";
    public string Note { get; set; } = string.Empty;
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public User? CreatedByUser { get; set; }
}

public class ActivityLog
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public User? User { get; set; }
}

public class CompanySetting
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = "ClientPro Manager";
    public string OwnerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string LogoPath { get; set; } = string.Empty;
    public string Currency { get; set; } = "BDT";
    public string TaxName { get; set; } = "VAT";
    public decimal DefaultTaxRate { get; set; } = 5;
    public string InvoicePrefix { get; set; } = "INV";
    public string QuotationPrefix { get; set; } = "QT";
    public string PaymentPrefix { get; set; } = "PAY";
    public string TermsAndConditions { get; set; } = "Payment is due by the document due date. Thank you for your business.";
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public class BackupLog
{
    public int Id { get; set; }
    public string BackupFilePath { get; set; } = string.Empty;
    public string BackupType { get; set; } = "Manual";
    public string Status { get; set; } = "Success";
    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public User? CreatedByUser { get; set; }
}
