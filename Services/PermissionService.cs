using ClientProManager.Models;

namespace ClientProManager.Services;

public interface IPermissionService
{
    bool CanAccess(string moduleKey);
    bool CanDeleteImportantRecords();
    bool CanExportReports();
    bool CanBackupRestore();
}

public class PermissionService(ISessionService sessionService) : IPermissionService
{
    public bool CanAccess(string moduleKey)
    {
        var role = sessionService.CurrentUser?.Role;
        if (role == UserRoles.Admin)
        {
            return true;
        }

        if (role == UserRoles.Accountant)
        {
            return moduleKey is "Dashboard" or "Clients" or "Invoices" or "Payments" or "Expenses" or "Reports" or "Settings";
        }

        if (role == UserRoles.Staff)
        {
            return moduleKey is "Dashboard" or "Clients" or "Products" or "Quotations" or "Invoices" or "Payments" or "Settings";
        }

        return false;
    }

    public bool CanDeleteImportantRecords() => sessionService.CurrentUser?.Role == UserRoles.Admin;
    public bool CanExportReports() => sessionService.CurrentUser?.Role is UserRoles.Admin or UserRoles.Accountant;
    public bool CanBackupRestore() => sessionService.CurrentUser?.Role == UserRoles.Admin;
}
