namespace MyApp.Data;

public static class Roles
{
    public const string Admin = nameof(Admin);
    public const string Employee = nameof(Employee);
    public const string Manager = nameof(Manager);

    public static string[] All { get; set; } = [Admin, Employee, Manager];
}