namespace MyApp.Client.Pages.AutoQueryGrids
{
    public partial class ContactsMeta
    {
        public string SourceCode = @"[Icon(Svg = Icons.Contact)]
public class Contact : AuditBase
{
    [AutoIncrement]
    public int Id { get; set; }

    [Computed]
    public string DisplayName => FirstName + "" "" + LastName;
    [Format(FormatMethods.IconRounded)]
    public string ProfileUrl { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }

    [Format(FormatMethods.Currency)]
    public int? SalaryExpectation { get; set; }

    public string JobType { get; set; }
    public int AvailabilityWeeks { get; set; }
    public EmploymentType PreferredWorkType { get; set; }
    public string PreferredLocation { get; set; }

    [Format(FormatMethods.LinkEmail, Options = 
        @""{target:'_self',subject:'New Job Opportunity',
           body:'We have an exciting new opportunity...', cls:'text-green-600'}"")]
    public string Email { get; set; }
    [Format(FormatMethods.LinkPhone)]
    public string Phone { get; set; }

    public string About { get; set; }

    [Reference]
    public List<JobApplication> Applications { get; set; }
}";

    }
}
