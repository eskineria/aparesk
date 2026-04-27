namespace Aparesk.Eskineria.Core.Auth.Constants;

public static class Permissions
{
    public const string ClaimType = "permission";
    public const string AdminRole = "Admin";
    public const string ManagerRole = "Manager";
    public const string UserRole = "User";

    public const string DashboardView = "Dashboard.View";

    // Screen-level permission names used by startup seeding.
    public const string UsersRead = "Users.Read";
    public const string UsersManage = "Users.Manage";
    public const string RolesRead = "Roles.Read";
    public const string RolesManage = "Roles.Manage";
    public const string SettingsRead = "Settings.Read";
    public const string SettingsManage = "Settings.Manage";
    public const string ComplianceRead = "Compliance.Read";
    public const string ComplianceManage = "Compliance.Manage";
    public const string LocalizationRead = "Localization.Read";
    public const string LocalizationManage = "Localization.Manage";
    public const string AuditRead = "Audit.Read";
    public const string EmailRead = "Email.Read";
    public const string EmailManage = "Email.Manage";

    // Management Permissions
    public const string SitesRead = "Sites.Read";
    public const string SitesManage = "Sites.Manage";
    public const string BlocksRead = "Blocks.Read";
    public const string BlocksManage = "Blocks.Manage";
    public const string UnitsRead = "Units.Read";
    public const string UnitsManage = "Units.Manage";
    public const string ResidentsRead = "Residents.Read";
    public const string ResidentsManage = "Residents.Manage";
}
