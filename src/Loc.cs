using System.Collections.Generic;

namespace HuXiangLianPian.Accessibility
{
    /// <summary>
    /// 无障碍Mod的本地化系统。
    /// 目前仅支持中文，保留多语言架构以便未来扩展。
    ///
    /// 使用方法：
    ///   Loc.Get("key")              - 获取字符串
    ///   Loc.Get("key", arg1, arg2)  - 获取带占位符的字符串 {0}, {1}
    /// </summary>
    public static class Loc
    {
        #region Fields
        private static bool _initialized = false;
        private static string _currentLang = "zh";

        // 各语言字典
        private static readonly Dictionary<string, string> _chinese = new();
        // 未来可添加更多语言：
        // private static readonly Dictionary<string, string> _english = new();
        // private static readonly Dictionary<string, string> _japanese = new();
        #endregion

        #region Public Methods
        /// <summary>
        /// 初始化本地化系统。Mod启动时调用一次。
        /// </summary>
        public static void Initialize()
        {
            InitializeStrings();
            RefreshLanguage();
            _initialized = true;
        }

        /// <summary>
        /// 刷新语言设置。当玩家改变语言时调用。
        /// </summary>
        public static void RefreshLanguage()
        {
            string gameLang = GetGameLanguage();
            switch (gameLang)
            {
                case "zh":
                default:
                    _currentLang = "zh";
                    break;
                    // 未来添加更多语言：
                    // case "en":
                    //     _currentLang = "en";
                    //     break;
            }
        }

        /// <summary>
        /// 获取本地化字符串。
        /// </summary>
        public static string Get(string key)
        {
            if (!_initialized) Initialize();

            var dict = GetCurrentDictionary();

            // 尝试当前语言
            if (dict.TryGetValue(key, out string value))
                return value;

            // 最后兜底：返回key本身（方便调试）
            return key;
        }

        /// <summary>
        /// 获取带占位符的本地化字符串。
        /// 使用 {0}, {1}, {2} 等作为占位符。
        /// </summary>
        public static string Get(string key, params object[] args)
        {
            string template = Get(key);
            try
            {
                return string.Format(template, args);
            }
            catch
            {
                return template;
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 获取游戏当前语言。
        /// 目前游戏仅支持中文，直接返回"zh"。
        /// 未来可从游戏设置中读取。
        /// </summary>
        private static string GetGameLanguage()
        {
            // TODO: 未来如果游戏支持多语言，从游戏设置中读取
            // 示例：
            // return PlayerPrefs.GetString("language", "zh");
            return "zh";
        }

        private static Dictionary<string, string> GetCurrentDictionary()
        {
            switch (_currentLang)
            {
                case "zh":
                default:
                    return _chinese;
                    // 未来添加更多语言：
                    // case "en":
                    //     return _english;
            }
        }

        /// <summary>
        /// 添加字符串到所有语言字典。
        /// 未来添加更多语言时扩展参数。
        /// </summary>
        private static void Add(string key, string chinese)
        {
            _chinese[key] = chinese;
            // 未来添加更多语言：
            // _english[key] = english;
        }

        /// <summary>
        /// 在这里定义所有翻译字符串。
        /// </summary>
        private static void InitializeStrings()
        {
            // ===== 通用 =====
            Add("mod_loaded",
                "痴情妹妹纱雪无障碍Mod已加载。按F1查看帮助。");

            Add("help_title",
                "快捷键说明：");

            Add("debug_mode_enabled",
                "调试模式已开启");

            Add("debug_mode_disabled",
                "调试模式已关闭");

            // ===== 带占位符的字符串 =====
            // 语法：{0}, {1}, {2} 等
            Add("item_count",
                "{0} 个物品");

            // ===== 各功能模块专用 =====
            // 为每个Handler添加字符串
            // 命名规范：[模块名]_[动作]
            // 示例：
            // Add("menu_opened", "菜单已打开");
            // Add("menu_closed", "菜单已关闭");
            // Add("dialog_skip", "跳过对话");
        }
        #endregion
    }
}
