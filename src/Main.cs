using MelonLoader;
using UnityEngine;
using System.Collections;

// ============================================================================
// 重要：游戏代码访问
// ============================================================================
// 在游戏完全加载之前访问任何游戏类都会导致崩溃！
//
// 禁止在 OnInitializeMelon() 或更早的时候：
//   - 访问游戏管理器单例（GameManager.i, AudioManager.instance 等）
//   - 在 Harmony 特性中使用 typeof(游戏类)
//
// 只有在 OnSceneWasLoaded() 之后或 CheckGameReady() 返回 true 时才允许。
//
// 如果遇到崩溃或静默失败：
//   参见 docs/technical-reference.md 中的 "重要：游戏代码访问" 部分
// ============================================================================

[assembly: MelonInfo(typeof(HuXiangLianPian.Accessibility.Main), "HuXiangLianPianAccessibility", "1.0.0", "boz700908")]
[assembly: MelonGame("StationWorks", "HuXiangLianPian")]

namespace HuXiangLianPian.Accessibility
{
    /// <summary>
    /// Mod 主入口点。协调所有处理器并处理全局快捷键。
    ///
    /// 最佳实践：保持这个类精简！
    /// - 只包含生命周期方法（OnInitializeMelon, OnUpdate, OnApplicationQuit）
    /// - 只包含全局快捷键分发（F1-F12, Tab, Enter）
    /// - 只包含处理器实例化和更新调用
    ///
    /// 把所有功能逻辑放在单独的 Handler 类中。
    /// 这样代码更容易维护和测试。
    /// </summary>
    public class Main : MelonMod
    {
        #region Fields
        private bool _gameReady = false;

        /// <summary>
        /// 调试模式 - 开启时记录所有屏幕阅读器输出和详细游戏状态。
        /// 按 F12 切换。
        /// </summary>
        public static bool DebugMode = false;

        // 处理器 - 每个功能/界面一个
        // private DialogHandler _dialogHandler;
        // private MenuHandler _menuHandler;
        // private SettingsHandler _settingsHandler;
        #endregion

        #region Lifecycle
        public override void OnInitializeMelon()
        {
            ScreenReader.Initialize();
            Loc.Initialize();
            InitializeHandlers();
            MelonCoroutines.Start(AnnounceStartupDelayed());
        }

        private void InitializeHandlers()
        {
            // 在这里创建处理器实例
            // _dialogHandler = new DialogHandler();
            // _menuHandler = new MenuHandler();
        }

        private IEnumerator AnnounceStartupDelayed()
        {
            // 短暂延迟，确保屏幕阅读器准备就绪
            yield return new WaitForSeconds(1f);
            ScreenReader.Say(Loc.Get("mod_loaded"));
        }

        public override void OnUpdate()
        {
            // 等待游戏准备就绪
            if (!CheckGameReady()) return;

            // 先处理全局快捷键
            if (ProcessHotkeys()) return;

            // 更新所有处理器
            UpdateHandlers();
        }

        private bool CheckGameReady()
        {
            if (_gameReady) return true;

            // 检查游戏单例 - 根据你的游戏调整！
            // 对于 Naninovel 引擎，可以检查 Naninovel.Engine 或相关管理器
            // if (/* 游戏就绪条件 */)
            // {
            //     _gameReady = true;
            //     MelonLogger.Msg("游戏就绪");
            // }

            return _gameReady;
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            MelonLogger.Msg($"场景已加载: {sceneName}");
            DebugLogger.LogState($"场景切换为: {sceneName}");
            _gameReady = false; // 场景切换时重置
        }

        public override void OnApplicationQuit()
        {
            ScreenReader.Shutdown();
        }
        #endregion

        #region Hotkeys
        /// <summary>
        /// 处理全局快捷键。如果处理了按键返回 true。
        /// 只在这里分发到处理器 - 不要把逻辑放在 Main 里！
        /// </summary>
        private bool ProcessHotkeys()
        {
            // F12 = 切换调试模式
            if (Input.GetKeyDown(KeyCode.F12))
            {
                DebugMode = !DebugMode;
                var status = DebugMode ? Loc.Get("debug_mode_enabled") : Loc.Get("debug_mode_disabled");
                MelonLogger.Msg(status);
                ScreenReader.Say(status);
                return true;
            }

            // F1 = 帮助（总是在 Main 中处理）
            if (Input.GetKeyDown(KeyCode.F1))
            {
                DebugLogger.LogInput("F1", "帮助");
                AnnounceHelp();
                return true;
            }

            // 其他 F 键分发到处理器：
            // if (Input.GetKeyDown(KeyCode.F2))
            // {
            //     DebugLogger.LogInput("F2", "对话状态");
            //     _dialogHandler.AnnounceStatus();
            //     return true;
            // }

            // Tab = 导航
            // if (Input.GetKeyDown(KeyCode.Tab))
            // {
            //     int direction = Input.GetKey(KeyCode.LeftShift) ? -1 : 1;
            //     DebugLogger.LogInput(direction > 0 ? "Tab" : "Shift+Tab", "导航");
            //     _buttonNavigator.Navigate(direction);
            //     return true;
            // }

            return false;
        }
        #endregion

        #region Handler Updates
        private void UpdateHandlers()
        {
            // 对需要每帧检查的处理器调用 Update()
            // _dialogHandler.Update();
            // _menuHandler.Update();
        }
        #endregion

        #region Help
        private void AnnounceHelp()
        {
            string help = Loc.Get("help_title") + " " +
                "F1 帮助。 " +
                "F12 切换调试模式。 ";
                // 实现更多功能后添加：
                // "F2 对话状态。 " +
                // "Tab 下一个元素。 " +
                // "回车 确认。";
            ScreenReader.Say(help);
        }
        #endregion
    }
}
