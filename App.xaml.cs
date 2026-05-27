using System.Windows;
using ClientProManager.Data;
using ClientProManager.Helpers;
using ClientProManager.Services;
using ClientProManager.Services.Security;
using ClientProManager.ViewModels;
using ClientProManager.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuestPDF.Infrastructure;

namespace ClientProManager;

public partial class App : Application
{
    public static IHost AppHost { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        QuestPDF.Settings.License = LicenseType.Community;
        AppPaths.EnsureFolders();

        AppHost = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddDbContextFactory<ClientProDbContext>(options => options.UseSqlite(AppPaths.ConnectionString));

                services.AddSingleton<IPasswordHasher, PasswordHasher>();
                services.AddSingleton<ISessionService, SessionService>();
                services.AddSingleton<IDialogService, DialogService>();
                services.AddSingleton<IPermissionService, PermissionService>();
                services.AddTransient<IActivityLogService, ActivityLogService>();
                services.AddTransient<IAuthenticationService, AuthenticationService>();
                services.AddTransient<IDatabaseInitializer, DatabaseInitializer>();
                services.AddTransient<IClientService, ClientService>();
                services.AddTransient<IProductCatalogService, ProductCatalogService>();
                services.AddTransient<ICompanySettingsService, CompanySettingsService>();
                services.AddTransient<IQuotationService, QuotationService>();
                services.AddTransient<IInvoiceService, InvoiceService>();
                services.AddTransient<IPaymentService, PaymentService>();
                services.AddTransient<IExpenseService, ExpenseService>();
                services.AddTransient<IDashboardService, DashboardService>();
                services.AddTransient<IReportService, ReportService>();
                services.AddTransient<IExportService, ExportService>();
                services.AddTransient<IBackupService, BackupService>();
                services.AddTransient<IUserService, UserService>();

                services.AddTransient<LoginViewModel>();
                services.AddTransient<MainViewModel>();
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<ClientsViewModel>();
                services.AddTransient<ProductServicesViewModel>();
                services.AddTransient<QuotationsViewModel>();
                services.AddTransient<InvoicesViewModel>();
                services.AddTransient<PaymentsViewModel>();
                services.AddTransient<ExpensesViewModel>();
                services.AddTransient<ReportsViewModel>();
                services.AddTransient<UsersViewModel>();
                services.AddTransient<BackupViewModel>();
                services.AddTransient<SettingsViewModel>();

                services.AddTransient<LoginWindow>();
                services.AddTransient<MainWindow>();
            })
            .Build();

        await AppHost.StartAsync();
        await AppHost.Services.GetRequiredService<IDatabaseInitializer>().InitializeAsync();

        var loginWindow = AppHost.Services.GetRequiredService<LoginWindow>();
        loginWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (AppHost is not null)
        {
            await AppHost.StopAsync(TimeSpan.FromSeconds(5));
            AppHost.Dispose();
        }

        base.OnExit(e);
    }
}
