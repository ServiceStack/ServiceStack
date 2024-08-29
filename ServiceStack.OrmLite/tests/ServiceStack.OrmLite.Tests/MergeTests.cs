using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Tests.UseCase;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests;

public class TimeSheet
{
    [AutoIncrement]
    public int Id { get; set; }

    [Required, References(typeof(Employee))]
    public int EmployeeId { get; set; }
    [Reference]
    public Employee Employee { get; set; }

    public bool? IsApproved { get; set; }

    [References(typeof(Employee))]
    public int? ApprovedById { get; set; }
    [Reference]
    public Employee ApprovedBy { get; set; }
}

public class MergeTests
{
    [Test]
    public void Can_merge_optional_self_references()
    {
        var timesheet = new TimeSheet
        {
            Id = 1,
            EmployeeId = 2,
            ApprovedById = 3
        };

        var employees = 4.Times(i => new Employee { Id = i, Name = "Employee " + i });

        timesheet.Merge(employees);

        timesheet.PrintDump();

        Assert.That(timesheet.Employee.Id, Is.EqualTo(2));
        Assert.That(timesheet.ApprovedBy.Id, Is.EqualTo(3));
    }
}