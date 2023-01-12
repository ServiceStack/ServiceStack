using Microsoft.AspNetCore.Mvc.Rendering;
using MyApp.ServiceModel;
using MyApp.ServiceModel.Types;

namespace MyApp;

public class AppData
{
    public Dictionary<string, string> Colors = new() {
        {"#ffa4a2", "Red"},
        {"#b2fab4", "Green"},
        {"#9be7ff", "Blue"}
    };
    public List<string> FilmGenres => EnumUtils.GetValues<FilmGenres>().Map(x => x.ToDescription());

    public List<KeyValuePair<string, string>> Titles => EnumUtils.GetValues<Title>()
        .Where(x => x != Title.Unspecified)
        .ToKeyValuePairs();
}

public static class AppDataExtensions
{
    internal static readonly AppData Instance = new();

    public static Dictionary<string, string> ContactColors(this IHtmlHelper html) => Instance.Colors;
    public static List<KeyValuePair<string, string>> ContactTitles(this IHtmlHelper html) => Instance.Titles;
    public static List<string> ContactGenres(this IHtmlHelper html) => Instance.FilmGenres;
}
