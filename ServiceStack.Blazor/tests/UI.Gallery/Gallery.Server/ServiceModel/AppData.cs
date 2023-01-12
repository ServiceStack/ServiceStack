using ServiceStack;
namespace MyApp.ServiceModel;

public class AppData
{
    public static AppData Instance { get; } = new();

    public string[] Currencies => NumberCurrency.All;
}
