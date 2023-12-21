using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using ServiceStack;

namespace MyApp.Components.Pages.Account.Manage;

public static class ManageNavPages
{
    const string NavItemClass = "text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-900 hover:text-gray-900 dark:hover:text-gray-50 group flex items-center px-4 py-2 text-base font-medium rounded-md mr-2 whitespace-nowrap";

    const string ActiveClass = "bg-gray-100 dark:bg-gray-800 text-gray-900 dark:text-gray-50";
    public static string Index => "Account/Manage";
    public static string Email => "Account/Manage/Email";

    public static string ChangePassword => "Account/Manage/ChangePassword";

    public static string ExternalLogins => "Account/Manage/ExternalLogins";

    public static string TwoFactorAuthentication => "Account/Manage/TwoFactorAuthentication";
    public static string PersonalData => "Account/Manage/PersonalData";

    public static string? IndexNavClass(NavigationManager navigationManager) => PageNavClass(navigationManager, Index);
    public static string? EmailNavClass(NavigationManager navigationManager) => PageNavClass(navigationManager, Email);

    public static string? ChangePasswordNavClass(NavigationManager navigationManager) => PageNavClass(navigationManager, ChangePassword);

    public static string? ExternalLoginsNavClass(NavigationManager navigationManager) => PageNavClass(navigationManager, ExternalLogins);

    public static string? TwoFactorAuthenticationNavClass(NavigationManager navigationManager) => PageNavClass(navigationManager, TwoFactorAuthentication);

    public static string? PersonalDataNavClass(NavigationManager navigationManager) => PageNavClass(navigationManager, PersonalData);
    public static string? PageNavClass(NavigationManager navigationManager, string page)
    {
        return navigationManager.Uri.EndsWith(page, StringComparison.OrdinalIgnoreCase) 
            ? CssUtils.ClassNames(NavItemClass, ActiveClass) 
            : NavItemClass;
    }

    public static string ActivePageKey => "ActivePage";
    public static void AddActivePage(this ViewDataDictionary viewData, string activePage) => viewData[ActivePageKey] = activePage;
}
