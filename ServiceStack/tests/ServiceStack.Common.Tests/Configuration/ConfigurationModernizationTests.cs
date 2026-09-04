using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ServiceStack.Configuration;

namespace ServiceStack.Common.Tests.Configuration;

[TestFixture]
public class ConfigurationModernizationTests
{
    [Test]
    public void AppSettingsBase_NullKey_And_NullSettings_Safe()
    {
        var appSettings = new AppSettingsBase(null);
        Assert.That(appSettings.GetNullableString(null), Is.Null);
        Assert.That(appSettings.GetString(null), Is.Null);
        Assert.That(appSettings.Get("unknown", "default"), Is.EqualTo("default"));
        Assert.That(appSettings.GetAllKeys(), Is.Not.Null);
        Assert.That(appSettings.GetAllKeys().Count, Is.EqualTo(0));
        Assert.That(appSettings.GetAll(), Is.Not.Null);
        Assert.That(appSettings.GetAll().Count, Is.EqualTo(0));
    }

    [Test]
    public void AppSettingsUtils_SaveAppSetting_ExactMatch_DoesNotCorruptPrefixSharingKeys()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), "test_app_settings_" + Guid.NewGuid().ToString("N") + ".txt");
        try
        {
            var initialContent = string.Join(Environment.NewLine, new[]
            {
                "HostName example.org",
                "Host localhost",
                "HostPort 8080",
                "Api https://api.example.com",
                "ApiKey secret123",
            }) + Environment.NewLine;

            File.WriteAllText(tempFile, initialContent);

            // Update "Host" -> should only modify "Host", NOT "HostName" or "HostPort"
            AppSettingsUtils.SaveAppSetting(tempFile, "Host", "127.0.0.1");

            // Update "Api" -> should only modify "Api", NOT "ApiKey"
            AppSettingsUtils.SaveAppSetting(tempFile, "Api", "https://newapi.example.com");

            var settings = new TextFileSettings(tempFile);
            Assert.That(settings.GetString("HostName"), Is.EqualTo("example.org"));
            Assert.That(settings.GetString("Host"), Is.EqualTo("127.0.0.1"));
            Assert.That(settings.GetString("HostPort"), Is.EqualTo("8080"));
            Assert.That(settings.GetString("Api"), Is.EqualTo("https://newapi.example.com"));
            Assert.That(settings.GetString("ApiKey"), Is.EqualTo("secret123"));
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Test]
    public void RuntimeAppSettings_SafeConversion_And_NullGuards()
    {
        var runtime = new RuntimeAppSettings();
        runtime.Settings["IntVal"] = _ => 42;
        runtime.Settings["StrInt"] = _ => "123";
        runtime.Settings["NullVal"] = _ => null;
        runtime.Settings["Invalid"] = _ => "not-a-number";

        Assert.That(runtime.Get<int>(null, "IntVal", 0), Is.EqualTo(42));
        Assert.That(runtime.Get<int>(null, "StrInt", 0), Is.EqualTo(123));
        Assert.That(runtime.Get<int>(null, "NullVal", 99), Is.EqualTo(99));
        Assert.That(runtime.Get<int>(null, "Invalid", 99), Is.EqualTo(99));
        Assert.That(runtime.Get<int>(null, null, 99), Is.EqualTo(99));
        Assert.That(runtime.Get<int>(null, "NonExistent", 99), Is.EqualTo(99));
    }

    [Test]
    public void DictionarySettings_DefensiveCopy_And_NullGuards()
    {
        Assert.DoesNotThrow(() => new DictionarySettings((IEnumerable<KeyValuePair<string, string>>)null));
        Assert.DoesNotThrow(() => new DictionarySettings((Dictionary<string, string>)null));

        var dictSettings = new DictionarySettings();
        dictSettings.Set("k1", "v1");

        // Verify defensive snapshot in GetAll()
        var all = dictSettings.GetAll();
        Assert.That(all["k1"], Is.EqualTo("v1"));
        all["k1"] = "mutated";
        Assert.That(dictSettings.GetString("k1"), Is.EqualTo("v1"));

        // Null guards
        Assert.Throws<ArgumentNullException>(() => dictSettings.Set<string>(null, "v"));
        Assert.That(dictSettings.GetString(null), Is.Null);
        Assert.That(dictSettings.Exists(null), Is.False);
    }

    [Test]
    public void EnvironmentVariableSettings_NullGuards()
    {
        var env = new EnvironmentVariableSettings();
        Assert.That(env.GetString(null), Is.Null);
        Assert.That(env.Exists(null), Is.False);
        Assert.That(env.Get("nonexistent_env_var_" + Guid.NewGuid().ToString("N")), Is.Null);
    }

    [Test]
    public void MultiAppSettings_NullResilience()
    {
        Assert.Throws<ArgumentNullException>(() => new MultiAppSettings(null));
        Assert.Throws<ArgumentNullException>(() => new MultiAppSettings(new IAppSettings[0]));

        var d1 = new DictionarySettings();
        d1.Set("k1", "v1");

        // Contains null element
        var multi = new MultiAppSettings(d1, null);
        Assert.That(multi.GetString("k1"), Is.EqualTo("v1"));
        Assert.That(multi.GetString(null), Is.Null);
        Assert.That(multi.GetString("unknown"), Is.Null);
        Assert.That(multi.GetAllKeys(), Does.Contain("k1"));

        Assert.DoesNotThrow(() => multi.Set("k2", "v2"));
        Assert.That(multi.GetString("k2"), Is.EqualTo("v2"));
    }

    [Test]
    public void ConfigUtils_ColonInValue_And_NullHandling()
    {
        Assert.That(ConfigUtils.GetListFromAppSettingValue(null), Is.Not.Null);
        Assert.That(ConfigUtils.GetListFromAppSettingValue(null).Count, Is.EqualTo(0));
        Assert.That(ConfigUtils.GetListFromAppSettingValue(""), Is.Not.Null);
        Assert.That(ConfigUtils.GetListFromAppSettingValue("").Count, Is.EqualTo(0));

        // Colon in value preserved
        var parsed = ConfigUtils.GetDictionaryFromAppSettingValue("ApiUrl:https://example.com:8080/path,Timeout:00:05:00");
        Assert.That(parsed["ApiUrl"], Is.EqualTo("https://example.com:8080/path"));
        Assert.That(parsed["Timeout"], Is.EqualTo("00:05:00"));

        var kvps = ConfigUtils.GetKeyValuePairsFromAppSettingValue("ApiUrl:https://example.com:8080/path");
        Assert.That(kvps.Count, Is.EqualTo(1));
        Assert.That(kvps[0].Key, Is.EqualTo("ApiUrl"));
        Assert.That(kvps[0].Value, Is.EqualTo("https://example.com:8080/path"));

        Assert.Throws<ArgumentNullException>(() => ConfigUtils.GetDictionaryFromAppSettingValue(null));
        Assert.Throws<ArgumentNullException>(() => ConfigUtils.GetKeyValuePairsFromAppSettingValue(null));
        Assert.Throws<FormatException>(() => ConfigUtils.GetDictionaryFromAppSettingValue("InvalidNoColon"));
        Assert.Throws<FormatException>(() => ConfigUtils.GetKeyValuePairsFromAppSettingValue("InvalidNoColon"));
    }
}
