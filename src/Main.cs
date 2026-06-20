using BepInEx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;
using Naninovel;

// ============================================================================
// 重要：游戏代码访问
// ============================================================================
// 在游戏完全加载之前访问任何游戏类都会导致崩溃！
//
// 禁止在 Awake() 或更早的时候：
//   - 访问游戏管理器单例（GameManager.i, AudioManager.instance 等）
//   - 在 Harmony 特性中使用 typeof(游戏类)
//
// 只有在 OnSceneLoaded() 之后或 CheckGameReady() 返回 true 时才允许。
//
// 如果遇到崩溃或静默失败：
//   参见 docs/technical-reference.md 中的 "重要：游戏代码访问" 部分
// ============================================================================

namespace HuXiangLianPian.Accessibility
{
    /// <summary>
    /// Mod 主入口点。协调所有处理器并处理全局快捷键。
    ///
    /// 最佳实践：保持这个类精简！
    /// - 只包含生命周期方法（Awake, Update, OnDestroy）
    /// - 只包含全局快捷键分发（F1-F12, Tab, Enter）
    /// - 只包含处理器实例化和更新调用
    ///
    /// 把所有功能逻辑放在单独的 Handler 类中。
    /// 这样代码更容易维护和测试。
    /// </summary>
    [BepInPlugin("com.boz700908.HuXiangLianPianAccessibility", "HuXiangLianPianAccessibility", "1.0.0")]
    public class Main : BaseUnityPlugin
    {
        #region Fields
        private bool _gameReady = false;
        private float _lastReadyCheckLogTime = 0f;
        private const float READY_CHECK_LOG_INTERVAL = 3f; // 每3秒打一次日志
        private float _lastUpdateLogTime = 0f;
        private const float UPDATE_LOG_INTERVAL = 5f; // 每5秒打一次Update心跳日志
        private int _updateFrameCount = 0;
        private bool _firstUpdateLog = false; // 是否已输出首次Update日志

        /// <summary>
        /// 调试模式 - 开启时记录所有屏幕阅读器输出和详细游戏状态。
        /// 按 F12 切换。
        /// 开发阶段默认开启。
        /// </summary>
        public static bool DebugMode = true;

        /// <summary>
        /// BepInEx 日志实例，其他类可以通过 Main.Log 访问。
        /// </summary>
        public static BepInEx.Logging.ManualLogSource Log { get; private set; }

        // 处理器 - 每个功能/界面一个
        private MenuHandler _menuHandler;
        // private DialogHandler _dialogHandler;
        // private SettingsHandler _settingsHandler;

        private bool _menuHandlerErrorLogged = false;
        #endregion

        #region Lifecycle
        void Awake()
        {
            Log = Logger;
            Log.LogInfo("=== 无障碍Mod生命周期: Awake ===");
            Log.LogInfo($"调试模式: {(DebugMode ? "开启" : "关闭")}");
            Log.LogInfo($"GameObject名称: {gameObject.name}");
            Log.LogInfo($"已启用: {enabled}");

            ModConfig.Initialize(Config);
            Log.LogInfo("配置已初始化");

            ScreenReader.Initialize();
            Log.LogInfo("屏幕阅读器已初始化");

            Loc.Initialize();
            Log.LogInfo("本地化已初始化");

            InitializeHandlers();
            Log.LogInfo("处理器已初始化");

            SceneManager.sceneLoaded += OnSceneLoaded;
            StartCoroutine(AnnounceStartupDelayed());

            // 暂时注释掉InvokeRepeating，先确认Mod基本功能正常
            // InvokeRepeating(nameof(InvokeHeartbeat), 1f, 5f);

            Log.LogInfo("=== 无障碍Mod生命周期: Awake完成 ===");
        }

        void OnEnable()
        {
            Log.LogInfo("=== 无障碍Mod生命周期: OnEnable ===");
        }

        void Start()
        {
            Log.LogInfo("=== 无障碍Mod生命周期: Start ===");
        }

        // 暂时注释掉，先确认Mod基本功能正常
        // private void InvokeHeartbeat()
        // {
        //     Log.LogInfo($"Invoke心跳 - 已执行{_updateFrameCount}帧，已启用: {enabled}，EventSystem: {(EventSystem.current != null ? "存在" : "不存在")}");
        // }

        private void InitializeHandlers()
        {
            // 在这里创建处理器实例
            _menuHandler = new MenuHandler();
            // _dialogHandler = new DialogHandler();
        }

        private IEnumerator AnnounceStartupDelayed()
        {
            Log.LogInfo("启动语音协程开始");
            // 短暂延迟，确保屏幕阅读器准备就绪
            yield return new WaitForSeconds(1f);
            ScreenReader.Say(Loc.Get("mod_loaded"));
            Log.LogInfo("启动语音已播放");
        }

        void Update()
        {
            _updateFrameCount++;

            // 第一帧就打日志，确认Update在执行
            if (!_firstUpdateLog)
            {
                _firstUpdateLog = true;
                Log.LogInfo($"=== 首次Update - 第{_updateFrameCount}帧 ===");
            }

            // 每隔几秒打一次心跳日志，确认Update在正常执行
            if (Time.unscaledTime - _lastUpdateLogTime > UPDATE_LOG_INTERVAL)
            {
                _lastUpdateLogTime = Time.unscaledTime;
                Log.LogInfo($"Update心跳 - 已执行{_updateFrameCount}帧，EventSystem: {(EventSystem.current != null ? "存在" : "不存在")}");
            }

            // 开发阶段：禁用游戏就绪检测，让Mod直接可用
            // 后续稳定后再恢复检测
            // if (!CheckGameReady()) return;

            // 先处理全局快捷键
            if (ProcessHotkeys()) return;

            // 更新所有处理器
            UpdateHandlers();
        }

        private bool CheckGameReady()
        {
            if (_gameReady) return true;

            // 检查Naninovel引擎是否已初始化
            try
            {
                bool engineInitialized = Engine.Initialized;

                // 每隔几秒打一次日志，避免刷屏
                if (Time.unscaledTime - _lastReadyCheckLogTime > READY_CHECK_LOG_INTERVAL)
                {
                    _lastReadyCheckLogTime = Time.unscaledTime;
                    Log.LogInfo($"检测Naninovel引擎状态: Initialized={engineInitialized}");
                }

                if (engineInitialized)
                {
                    _gameReady = true;
                    Log.LogInfo("Naninovel引擎已就绪，开始初始化无障碍功能");
                    OnGameReady();
                }
            }
            catch (System.Exception e)
            {
                // 每隔几秒打一次日志
                if (Time.unscaledTime - _lastReadyCheckLogTime > READY_CHECK_LOG_INTERVAL)
                {
                    _lastReadyCheckLogTime = Time.unscaledTime;
                    Log.LogWarning($"检测Naninovel引擎时出错: {e.Message}");
                }
                // 引擎还没准备好，忽略异常
            }

            return _gameReady;
        }

        /// <summary>
        /// 游戏就绪时调用，初始化需要游戏API的功能。
        /// </summary>
        private void OnGameReady()
        {
            // 在这里初始化需要游戏API的处理器
            // 比如订阅Naninovel事件
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Log.LogInfo($"场景已加载: {scene.name}");
            DebugLogger.LogState($"场景切换为: {scene.name}");
            _gameReady = false; // 场景切换时重置
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
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
                Log.LogInfo(status);
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
            try
            {
                _menuHandler.Update();
            }
            catch (System.Exception e)
            {
                // 只输出一次异常，避免刷屏
                if (!_menuHandlerErrorLogged)
                {
                    _menuHandlerErrorLogged = true;
                    Log.LogError($"MenuHandler.Update() 出错: {e.GetType().Name} - {e.Message}");
                    Log.LogError($"堆栈跟踪: {e.StackTrace}");
                }
            }
            // _dialogHandler.Update();
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
