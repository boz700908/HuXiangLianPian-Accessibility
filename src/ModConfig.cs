using BepInEx.Configuration;

namespace HuXiangLianPian.Accessibility
{
    /// <summary>
    /// Mod配置类。
    /// 目前仅保留启用/禁用开关，后续可根据需要添加更多配置。
    /// </summary>
    public static class ModConfig
    {
        #region Config Entries
        private static ConfigFile _config;

        // 是否启用Mod
        private static ConfigEntry<bool> _enabled;
        #endregion

        #region Public Accessors
        /// <summary>是否启用Mod。</summary>
        public static bool Enabled => _enabled.Value;
        #endregion

        #region Initialization
        /// <summary>
        /// 初始化配置。在 Awake() 中调用一次。
        /// 传入插件的 Config 属性：ModConfig.Initialize(Config);
        /// </summary>
        public static void Initialize(ConfigFile config)
        {
            _config = config;

            _enabled = config.Bind("General",
                "Enabled", true,
                "是否启用无障碍Mod");
        }
        #endregion
    }
}
