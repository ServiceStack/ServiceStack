using System;
using System.Linq;

namespace ServiceStack;
#nullable enable

using System.Text.RegularExpressions;

public static class UserAgentHelper
{
    public static readonly Regex RegexEdge = new(@"Edg/(\d+[\.\d]*)", RegexOptions.Compiled);
    public static readonly Regex RegexChrome = new(@"Chrome/(\d+[\.\d]*)", RegexOptions.Compiled);
    public static readonly Regex RegexSafari = new(@"Version/(\d+[\.\d]*)", RegexOptions.Compiled);
    public static readonly Regex RegexFirefox = new(@"Firefox/(\d+[\.\d]*)", RegexOptions.Compiled);
    public static readonly Regex RegexChromium = new(@"Chromium/(\d+[\.\d]*)", RegexOptions.Compiled);
    public static readonly Regex RegexUCBrowser = new(@"UCBrowser/(\d+[\.\d]*)", RegexOptions.Compiled);
    public static readonly Regex RegexSamsung = new(@"SamsungBrowser/(\d+[\.\d]*)", RegexOptions.Compiled);
    
    public static readonly Regex RegexAppleWebKit = new(@"applewebkit/605\.1\.", RegexOptions.Compiled);
    
    /// <summary>
    /// Determines the browser name and version from a user agent string
    /// Also detects popular bots and crawlers
    /// </summary>
    /// <param name="userAgent">The user agent string to parse</param>
    /// <returns>A string containing browser name and version or bot identification</returns>
    public static (string, string?) GetBrowserInfo(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return ("None", null);
            
        // Check for bots first
        if (IsBotUserAgent(userAgent, out string botName))
            return (botName, null);

        // Edge (Chromium-based)
        if (userAgent.Contains("Edg/"))
        {
            var match = RegexEdge.Match(userAgent);
            return match.Success 
                ? ("Microsoft Edge", match.Groups[1].Value) 
                : ("Microsoft Edge", null);
        }

        // Chrome
        if (userAgent.Contains("Chrome/") && !userAgent.Contains("Chromium/"))
        {
            var match = RegexChrome.Match(userAgent);
            return match.Success 
                ? ("Google Chrome", match.Groups[1].Value) 
                : ("Google Chrome", null);
        }

        // Firefox
        if (userAgent.Contains("Firefox/"))
        {
            var match = RegexFirefox.Match(userAgent);
            return match.Success 
                ? ("Firefox", match.Groups[1].Value) 
                : ("Firefox", null);
        }

        // Safari
        if (userAgent.Contains("Safari/") && !userAgent.Contains("Chrome/") && !userAgent.Contains("Chromium/"))
        {
            var match = RegexSafari.Match(userAgent);
            return match.Success 
                ? ("Safari", match.Groups[1].Value) 
                : ("Safari", null);
        }

        // Opera
        if (userAgent.Contains("OPR/") || userAgent.Contains("Opera/"))
        {
            var match = Regex.Match(userAgent, @"OPR/(\d+[\.\d]*)");
            if (!match.Success)
                match = Regex.Match(userAgent, @"Opera/(\d+[\.\d]*)");
            return match.Success 
                ? ("Opera", match.Groups[1].Value) 
                : ("Opera", null);
        }

        // Internet Explorer
        if (userAgent.Contains("MSIE ") || userAgent.Contains("Trident/"))
        {
            var match = Regex.Match(userAgent, @"MSIE (\d+[\.\d]*)");
            if (!match.Success)
                match = Regex.Match(userAgent, @"rv:(\d+[\.\d]*)");
            return match.Success 
                ? ("Internet Explorer", match.Groups[1].Value) 
                : ("Internet Explorer", null);
        }

        // UC Browser
        if (userAgent.Contains("UCBrowser/"))
        {
            var match = RegexUCBrowser.Match(userAgent);
            return match.Success 
                ? ("UC Browser", match.Groups[1].Value) 
                : ("UC Browser", null);
        }

        // Samsung Browser
        if (userAgent.Contains("SamsungBrowser/"))
        {
            var match = RegexSamsung.Match(userAgent);
            return match.Success 
                ? ("Samsung Browser", match.Groups[1].Value) 
                : ("Samsung Browser", null);
        }

        // Chromium
        if (userAgent.Contains("Chromium/"))
        {
            var match = RegexChromium.Match(userAgent);
            return match.Success 
                ? ("Chromium", match.Groups[1].Value) 
                : ("Chromium", null);
        }

        return ("Unknown", null);
    }
    
    /// <summary>
    /// Determines if the user agent belongs to a known bot or crawler
    /// </summary>
    /// <param name="userAgent">The user agent string to check</param>
    /// <param name="botName">Output parameter that will contain the bot name if detected</param>
    /// <returns>True if the user agent is from a known bot, false otherwise</returns>
    public static bool IsBotUserAgent(string userAgent, out string botName)
    {
        userAgent = userAgent.ToLower();
        
        // Google bots
        if (userAgent.Contains("googlebot"))
        {
            var match = Regex.Match(userAgent, @"googlebot/(\d+[\.\d]*)");
            botName = match.Success ? $"Googlebot {match.Groups[1].Value}" : "Googlebot";
            return true;
        }
        
        if (userAgent.Contains("google-read-aloud") || userAgent.Contains("googleweblight"))
        {
            botName = "Google Web Light Bot";
            return true;
        }
        
        if (userAgent.Contains("adsbot-google"))
        {
            botName = "Google AdsBot";
            return true;
        }
        
        // Bing bots
        if (userAgent.Contains("bingbot"))
        {
            var match = Regex.Match(userAgent, @"bingbot/(\d+[\.\d]*)");
            botName = match.Success ? $"Bingbot {match.Groups[1].Value}" : "Bingbot";
            return true;
        }
        
        // Baidu bot
        if (userAgent.Contains("baiduspider"))
        {
            botName = "Baidu Spider";
            return true;
        }
        
        // Yahoo bot
        if (userAgent.Contains("slurp"))
        {
            botName = "Yahoo! Slurp";
            return true;
        }
        
        // DuckDuckGo bot
        if (userAgent.Contains("duckduckbot"))
        {
            botName = "DuckDuckBot";
            return true;
        }
        
        // Yandex bot
        if (userAgent.Contains("yandexbot") || userAgent.Contains("yandex.com/bots"))
        {
            botName = "YandexBot";
            return true;
        }
        
        // Facebook crawler
        if (userAgent.Contains("facebookexternalhit"))
        {
            botName = "Facebook Bot";
            return true;
        }
        
        // LinkedIn bot
        if (userAgent.Contains("linkedinbot"))
        {
            botName = "LinkedIn Bot";
            return true;
        }
        
        // Twitter bot
        if (userAgent.Contains("twitterbot"))
        {
            botName = "Twitter Bot";
            return true;
        }
        
        // WhatsApp Preview bot
        if (userAgent.Contains("whatsapp"))
        {
            botName = "WhatsApp Bot";
            return true;
        }
        
        // Crawler detection
        if (userAgent.Contains("crawler") || userAgent.Contains("spider") || 
            userAgent.Contains("bot") || userAgent.Contains("crawl"))
        {
            // Generic bot detection - try to extract the bot name from the user agent
            var botMatch = Regex.Match(userAgent, @"([a-zA-Z0-9\._-]+bot|[a-zA-Z0-9\._-]+spider|[a-zA-Z0-9\._-]+crawler)");
            if (botMatch.Success)
            {
                botName = botMatch.Groups[1].Value;
                return true;
            }
            
            botName = "Unknown Bot";
            return true;
        }
        
        botName = string.Empty;
        return false;
    }
    
  
    /// <summary>
    /// Determines the specific device type based on the user agent
    /// </summary>
    /// <param name="userAgent">The user agent string to check</param>
    /// <returns>The specific device type: iPhone, iPad, iPod, Android Mobile, Android Tablet, Unknown Mobile, or Desktop</returns>
    public static string GetDeviceType(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return "Desktop";
            
        userAgent = userAgent.ToLower();
        
        // Apple devices
        if (userAgent.Contains("iphone"))
            return "iPhone";
            
        if (userAgent.Contains("ipad") || (userAgent.Contains("macintosh") && 
            userAgent.Contains("safari") && !userAgent.Contains("chrome") && 
            RegexAppleWebKit.IsMatch(userAgent)))
            return "iPad";
            
        if (userAgent.Contains("ipod"))
            return "iPod";
            
        // Android devices
        if (userAgent.Contains("android"))
        {
            if (userAgent.Contains("mobile"))
                return "Android Mobile";
                
            return "Android Tablet";
        }
        
        // Other mobile devices
        string[] mobileKeywords =
        [
            "windows phone", "blackberry", "webos", "opera mini", "opera mobi", 
            "iemobile", "mobile safari", "samsung", "nokia", "motorola", "htc", 
            "mobile", "kindle", "silk", "fennec", "tablet", "lg-", "sony-", 
            "huawei", "oneplus", "crios/", "fxios/", "yabrowser/", "ucbrowser/", 
            "instagram", "facebook", "snapchat", "twitter", "pinterest", "tiktok", "whatsapp"
        ];
        
        foreach (var keyword in mobileKeywords)
        {
            if (userAgent.Contains(keyword))
                return "Mobile";
        }
        
        // Check for screen dimensions indicating mobile
        var match = Regex.Match(userAgent, @"(\d+)x(\d+)");
        if (match.Success)
        {
            if (int.TryParse(match.Groups[1].Value, out int width) && 
                int.TryParse(match.Groups[2].Value, out int height))
            {
                int smaller = Math.Min(width, height);
                int larger = Math.Max(width, height);
                
                // If the smaller dimension is less than 480, it's likely mobile
                if (smaller <= 480 && larger <= 1280)
                    return "Mobile";
            }
        }
        
        return "Desktop";
    }
}