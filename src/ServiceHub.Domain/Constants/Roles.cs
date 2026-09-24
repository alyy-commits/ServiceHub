namespace ServiceHub.Domain.Constants;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Employee = "Employee";
    public const string Customer = "Customer";

    public static readonly string[] All = { Admin, Employee, Customer };
}
