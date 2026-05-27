# ClientPro Manager

Windows Desktop Client, Invoice & Service Management System.

ClientPro Manager is an offline-first WPF desktop application for small businesses, agencies, repair shops, construction teams, suppliers, freelancers, and local offices that need to replace spreadsheets with a structured client, invoice, payment, service, expense, and reporting system.

## Technology Stack

- C# and .NET 8
- WPF desktop UI
- MVVM architecture
- SQLite local database
- Entity Framework Core
- QuestPDF for PDF exports
- ClosedXML for Excel exports
- Custom CSV export
- Microsoft.Extensions.Hosting dependency injection

## Current Functional Scope

- Login screen with seeded admin user
- Password hashing with PBKDF2
- Role-aware navigation and permissions
- Activity log for important actions
- Client CRUD with search/filter and profile totals
- Product/service CRUD
- Quotation creation, calculations, PDF export, and conversion to invoice
- Invoice creation, calculations, BDT totals, and PDF export
- Payment entry against invoices with due/status updates
- Expense management
- Dashboard summary cards and recent/pending/overdue tables
- Reports with PDF, Excel, and CSV export
- User management, disable/reset password
- Company settings
- Manual backup and restore workflow
- Cloud backup placeholder for future Google Drive/OneDrive integration

## How To Run

From the solution folder:

```powershell
dotnet restore
dotnet build
dotnet run --project .\ClientProManager\ClientProManager.csproj
```

## Default Admin Login

- Username: `admin`
- Password: `admin123`
- Role: `Admin`

The seeded admin is marked for password-change recommendation after first login.

## Database Location

The SQLite database is created automatically on first run:

```text
%LOCALAPPDATA%\ClientProManager\clientpro.db
```

Backups are stored in:

```text
%LOCALAPPDATA%\ClientProManager\Backups
```

Exports are stored in:

```text
%LOCALAPPDATA%\ClientProManager\Exports
```

## Backup And Restore

Open `Backup/Restore` as an admin user.

- `Create Backup` copies the SQLite database to the local backup folder.
- Backup file names use the format `ClientPro_Backup_yyyy-MM-dd_HHmm.db`.
- Restore requires an admin user and a backup file path.
- Restart the app after restore so the running UI reloads the restored database.

## Entity Framework Migrations

The first version uses `EnsureCreated()` for fast local startup. To switch to migrations later:

```powershell
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate --project .\ClientProManager\ClientProManager.csproj
dotnet ef database update --project .\ClientProManager\ClientProManager.csproj
```

After enabling migrations, replace the initializer's `EnsureCreatedAsync()` call with `MigrateAsync()`.

## Publish Windows App

Framework-dependent publish:

```powershell
dotnet publish .\ClientProManager\ClientProManager.csproj -c Release -r win-x64 --self-contained false
```

Self-contained publish:

```powershell
dotnet publish .\ClientProManager\ClientProManager.csproj -c Release -r win-x64 --self-contained true
```

Inno Setup can be added later to package the publish output into a Windows installer.

## Development Phases

Phase 1 is implemented: WPF solution, MVVM folders, EF Core SQLite, models, DbContext, seed data, login, and main shell.

Phase 2 is implemented: client management, product/service management, and company settings.

Phase 3 is implemented: quotations, invoices, payments, totals, due tracking, and conversion.

Phase 4 is implemented: expenses and dashboard summaries.

Phase 5 is implemented as a first pass: PDF, Excel, and CSV exports for invoices, quotations, receipts, and reports.

Phase 6 is implemented as a first pass: users, roles, permissions, activity logs, backup, and restore.

## Future Improvements

- Add full edit dialogs with richer validation visuals.
- Add database encryption using SQLCipher or an encrypted SQLite provider.
- Add automatic daily backup scheduler and retention policy.
- Add print preview windows for invoices and quotations.
- Add file attachment copy/storage for client documents.
- Add auto logout timer.
- Add granular staff permissions.
- Add Inno Setup installer script.
- Add unit tests for calculations, permissions, and payment updates.
