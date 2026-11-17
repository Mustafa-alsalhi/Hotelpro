namespace HotelPro.Domain.Constants;

public static class RoleNames
{
    public const string SuperAdmin = "SuperAdmin";
    public const string HotelManager = "HotelManager";
    public const string Receptionist = "Receptionist";
    public const string Accountant = "Accountant";
    public const string HousekeepingSupervisor = "HousekeepingSupervisor";
    public const string HousekeepingStaff = "HousekeepingStaff";
    public const string MaintenanceManager = "MaintenanceManager";
    public const string MaintenanceTechnician = "MaintenanceTechnician";
    public const string ServiceStaff = "ServiceStaff";

    public static readonly string[] AllRoles =
    [
        SuperAdmin,
        HotelManager,
        Receptionist,
        Accountant,
        HousekeepingSupervisor,
        HousekeepingStaff,
        MaintenanceManager,
        MaintenanceTechnician,
        ServiceStaff
    ];
}

public static class PermissionsCatalog
{
    public record PermissionDefinition(string Code, string Module, string Action, string Description);

    public static class Dashboard
    {
        public const string View = "Dashboard.View";
    }

    public static class Users
    {
        public const string View = "Users.View";
        public const string Create = "Users.Create";
        public const string Edit = "Users.Edit";
        public const string Delete = "Users.Delete";
        public const string ManageUsers = "Users.ManageUsers";
    }

    public static class Roles
    {
        public const string View = "Roles.View";
        public const string Create = "Roles.Create";
        public const string Edit = "Roles.Edit";
        public const string Delete = "Roles.Delete";
    }

    public static class Settings
    {
        public const string View = "Settings.View";
        public const string ManageSettings = "Settings.ManageSettings";
    }

    public static class Rooms
    {
        public const string View = "Rooms.View";
        public const string Create = "Rooms.Create";
        public const string Edit = "Rooms.Edit";
        public const string Delete = "Rooms.Delete";
    }

    public static class Rates
    {
        public const string View = "Rates.View";
        public const string Create = "Rates.Create";
        public const string Edit = "Rates.Edit";
        public const string Delete = "Rates.Delete";
    }

    public static class Guests
    {
        public const string View = "Guests.View";
        public const string Create = "Guests.Create";
        public const string Edit = "Guests.Edit";
        public const string Delete = "Guests.Delete";
    }

    public static class Reservations
    {
        public const string View = "Reservations.View";
        public const string Create = "Reservations.Create";
        public const string Edit = "Reservations.Edit";
        public const string Approve = "Reservations.Approve";
        public const string Cancel = "Reservations.Cancel";
        public const string CheckIn = "Reservations.CheckIn";
        public const string CheckOut = "Reservations.CheckOut";
    }

    public static class GuestServices
    {
        public const string View = "GuestServices.View";
        public const string Create = "GuestServices.Create";
        public const string Edit = "GuestServices.Edit";
        public const string Cancel = "GuestServices.Cancel";
    }

    public static class Housekeeping
    {
        public const string View = "Housekeeping.View";
        public const string Create = "Housekeeping.Create";
        public const string Edit = "Housekeeping.Edit";
        public const string Approve = "Housekeeping.Approve";
    }

    public static class Maintenance
    {
        public const string View = "Maintenance.View";
        public const string Create = "Maintenance.Create";
        public const string Edit = "Maintenance.Edit";
        public const string Approve = "Maintenance.Approve";
    }

    public static class Employees
    {
        public const string View = "Employees.View";
        public const string Create = "Employees.Create";
        public const string Edit = "Employees.Edit";
        public const string Delete = "Employees.Delete";
    }

    public static class Billing
    {
        public const string View = "Billing.View";
        public const string Create = "Billing.Create";
        public const string Cancel = "Billing.Cancel";
        public const string ManagePayments = "Billing.ManagePayments";
        public const string Approve = "Billing.Approve";
    }

    public static class Finance
    {
        public const string View = "Finance.View";
        public const string Create = "Finance.Create";
        public const string Edit = "Finance.Edit";
        public const string ViewReports = "Finance.ViewReports";
    }

    public static class Inventory
    {
        public const string View = "Inventory.View";
        public const string Create = "Inventory.Create";
        public const string Edit = "Inventory.Edit";
        public const string Approve = "Inventory.Approve";
    }

    public static class Reports
    {
        public const string ViewReports = "Reports.ViewReports";
    }

    public static class AuditLogs
    {
        public const string View = "AuditLogs.View";
    }

    public static IReadOnlyList<PermissionDefinition> GetAll() =>
    [
        new(Dashboard.View, "Dashboard", "View", "View main operational dashboard"),

        new(Users.View, "Users", "View", "View system users"),
        new(Users.Create, "Users", "Create", "Create system users"),
        new(Users.Edit, "Users", "Edit", "Edit system users"),
        new(Users.Delete, "Users", "Delete", "Deactivate or delete users"),
        new(Users.ManageUsers, "Users", "ManageUsers", "Manage user roles and access"),

        new(Roles.View, "Roles", "View", "View roles and permissions"),
        new(Roles.Create, "Roles", "Create", "Create custom roles"),
        new(Roles.Edit, "Roles", "Edit", "Edit role permissions"),
        new(Roles.Delete, "Roles", "Delete", "Delete custom roles"),

        new(Settings.View, "Settings", "View", "View hotel settings"),
        new(Settings.ManageSettings, "Settings", "ManageSettings", "Update hotel settings"),

        new(Rooms.View, "Rooms", "View", "View rooms and room types"),
        new(Rooms.Create, "Rooms", "Create", "Create rooms and room types"),
        new(Rooms.Edit, "Rooms", "Edit", "Edit rooms and change non-stay status"),
        new(Rooms.Delete, "Rooms", "Delete", "Delete rooms or room types"),

        new(Rates.View, "Rates", "View", "View rate plans and seasonal rates"),
        new(Rates.Create, "Rates", "Create", "Create rate plans and rates"),
        new(Rates.Edit, "Rates", "Edit", "Edit rate plans and rates"),
        new(Rates.Delete, "Rates", "Delete", "Delete rate plans"),

        new(Guests.View, "Guests", "View", "View guest profiles"),
        new(Guests.Create, "Guests", "Create", "Register new guests"),
        new(Guests.Edit, "Guests", "Edit", "Edit guest profiles"),
        new(Guests.Delete, "Guests", "Delete", "Archive guest profiles"),

        new(Reservations.View, "Reservations", "View", "View reservations and calendar"),
        new(Reservations.Create, "Reservations", "Create", "Create reservations"),
        new(Reservations.Edit, "Reservations", "Edit", "Modify reservations"),
        new(Reservations.Approve, "Reservations", "Approve", "Confirm reservations"),
        new(Reservations.Cancel, "Reservations", "Cancel", "Cancel reservations"),
        new(Reservations.CheckIn, "Reservations", "CheckIn", "Perform guest check-in"),
        new(Reservations.CheckOut, "Reservations", "CheckOut", "Perform guest check-out"),

        new(GuestServices.View, "GuestServices", "View", "View guest services and orders"),
        new(GuestServices.Create, "GuestServices", "Create", "Create service catalog or orders"),
        new(GuestServices.Edit, "GuestServices", "Edit", "Update service order status"),
        new(GuestServices.Cancel, "GuestServices", "Cancel", "Cancel service orders"),

        new(Housekeeping.View, "Housekeeping", "View", "View housekeeping tasks"),
        new(Housekeeping.Create, "Housekeeping", "Create", "Create housekeeping tasks"),
        new(Housekeeping.Edit, "Housekeeping", "Edit", "Update housekeeping task progress"),
        new(Housekeeping.Approve, "Housekeeping", "Approve", "Inspect and approve room readiness"),

        new(Maintenance.View, "Maintenance", "View", "View maintenance requests"),
        new(Maintenance.Create, "Maintenance", "Create", "Open maintenance requests"),
        new(Maintenance.Edit, "Maintenance", "Edit", "Update maintenance repair details"),
        new(Maintenance.Approve, "Maintenance", "Approve", "Close maintenance and restore room"),

        new(Employees.View, "Employees", "View", "View employees and attendance"),
        new(Employees.Create, "Employees", "Create", "Add employees"),
        new(Employees.Edit, "Employees", "Edit", "Edit employee records"),
        new(Employees.Delete, "Employees", "Delete", "Terminate or archive employees"),

        new(Billing.View, "Billing", "View", "View invoices and payments"),
        new(Billing.Create, "Billing", "Create", "Issue invoices"),
        new(Billing.Cancel, "Billing", "Cancel", "Cancel or reverse invoices/payments"),
        new(Billing.ManagePayments, "Billing", "ManagePayments", "Record payments and receipts"),
        new(Billing.Approve, "Billing", "Approve", "Approve exceptional checkout with balance"),

        new(Finance.View, "Finance", "View", "View expenses and revenues"),
        new(Finance.Create, "Finance", "Create", "Record expenses and revenues"),
        new(Finance.Edit, "Finance", "Edit", "Edit expense records"),
        new(Finance.ViewReports, "Finance", "ViewReports", "View financial reports"),

        new(Inventory.View, "Inventory", "View", "View inventory and purchase orders"),
        new(Inventory.Create, "Inventory", "Create", "Create items and purchase orders"),
        new(Inventory.Edit, "Inventory", "Edit", "Issue or adjust stock"),
        new(Inventory.Approve, "Inventory", "Approve", "Approve and receive purchase orders"),

        new(Reports.ViewReports, "Reports", "ViewReports", "Access operational and analytical reports"),
        new(AuditLogs.View, "AuditLogs", "View", "View system audit trail")
    ];
}

