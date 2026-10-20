using BepInEx.Configuration;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace KeyTips
{
    /// <summary>
    /// 提示文本的语言包管理（上游 LocaleManager 的 5.0BE 版）。
    /// 语言包目录固定在插件 DLL 同级的 locales\，通过 F12 配置菜单选择语言。
    /// </summary>
    public static class LocaleManager
    {
        public static ConfigEntry<string> CurrentLanguage;

        private static readonly Dictionary<string, Dictionary<string, string>> _loadedTranslations =
            new Dictionary<string, Dictionary<string, string>>();

        // 插件自己的 JSON 走托管 System.Text.Json：游戏内那份 Newtonsoft 是 IL2CPP 类型，
        // 反序列化不了插件程序集里的托管 POCO。
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        private const string FallbackLangName = "English";

        private static ManualLogSource _log;

        public static void Initialize(ConfigFile config, ManualLogSource log)
        {
            _log = log;
            string dirPath = Path.Combine(Path.GetDirectoryName(typeof(KeyTipsPlugin).Assembly.Location), "locales");

            _loadedTranslations.Clear();
            List<string> availableLanguages = new List<string>();

            if (Directory.Exists(dirPath))
            {
                foreach (string file in Directory.GetFiles(dirPath, "*.json"))
                {
                    try
                    {
                        LocaleData data = JsonSerializer.Deserialize<LocaleData>(File.ReadAllText(file), JsonOptions);
                        if (data != null && !string.IsNullOrEmpty(data.Language) && data.Translate != null)
                        {
                            _loadedTranslations[data.Language] = data.Translate;
                            availableLanguages.Add(data.Language);
                        }
                    }
                    catch (Exception e)
                    {
                        _log?.LogWarning($"语言文件 {file} 加载时发生错误! {e.Message}");
                    }
                }
            }
            else
            {
                _log?.LogWarning($"语言目录不存在（{dirPath}），提示文本将显示英文键名。");
            }

            if (availableLanguages.Count == 0)
            {
                availableLanguages.Add(FallbackLangName);
                _loadedTranslations[FallbackLangName] = new Dictionary<string, string>();
            }

            CurrentLanguage = config.Bind(
                "Language / 语言",
                "Language Opinion / 语言设置",
                availableLanguages.Contains(FallbackLangName) ? FallbackLangName : availableLanguages[0],
                new ConfigDescription(
                    "Change language (Configuration menu's requires game restart). / 更改语言（F12菜单需要重启游戏生效）。",
                    new AcceptableValueList<string>(availableLanguages.ToArray())));
        }

        public static string Get(string key)
        {
            if (CurrentLanguage != null
                && _loadedTranslations.TryGetValue(CurrentLanguage.Value, out Dictionary<string, string> current)
                && current.TryGetValue(key, out string text))
            {
                return text;
            }

            if (_loadedTranslations.TryGetValue(FallbackLangName, out Dictionary<string, string> fallback)
                && fallback.TryGetValue(key, out string fallbackText))
            {
                return fallbackText;
            }

            return key;
        }

        /// <summary>拓展语法糖</summary>
        public static string i18n(this string key) => Get(key);
    }
}
