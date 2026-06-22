using BepInEx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;
using Naninovel;
using Naninovel.UI;

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
        private const float UPDATE_LOG_INTERVAL = 30f; // 每30秒打一次Update心跳日志
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
        private DialogueHandler _dialogueHandler;
        // private DialogHandler _dialogHandler;
        // private SettingsHandler _settingsHandler;

        private bool _menuHandlerErrorLogged = false;
        private bool _dialogueHandlerInitialized = false;
        #endregion

        #region Lifecycle
        void Awake()
        {
            Log = Logger;
            Log.LogInfo($"无障碍Mod已加载 (调试模式: {(DebugMode ? "开启" : "关闭")})");

            ModConfig.Initialize(Config);
            ScreenReader.Initialize();
            Loc.Initialize();
            InitializeHandlers();

            SceneManager.sceneLoaded += OnSceneLoaded;
            StartCoroutine(AnnounceStartupDelayed());
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
            _dialogueHandler = new DialogueHandler();
        }

        private IEnumerator AnnounceStartupDelayed()
        {
            // Log.LogInfo("启动语音协程开始");
            // 短暂延迟，确保屏幕阅读器准备就绪
            yield return new WaitForSeconds(1f);
            ScreenReader.Say(Loc.Get("mod_loaded"));
            // Log.LogInfo("启动语音已播放");
        }

        void Update()
        {
            _updateFrameCount++;

            // 第一帧就打日志，确认Update在执行
            if (!_firstUpdateLog)
            {
                _firstUpdateLog = true;
                // Log.LogInfo($"=== 首次Update - 第{_updateFrameCount}帧 ===");
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
            if (_dialogueHandler != null && !_dialogueHandlerInitialized)
            {
                _dialogueHandler.Initialize();
                _dialogueHandlerInitialized = true;
                Log.LogInfo("对话处理器已初始化");
            }
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

            // F1 = 快速存档
            if (Input.GetKeyDown(KeyCode.F1))
            {
                DebugLogger.LogInput("F1", "快速存档");
                QuickSave();
                return true;
            }

            // F2 = 快速读档
            if (Input.GetKeyDown(KeyCode.F2))
            {
                DebugLogger.LogInput("F2", "快速读档");
                QuickLoad();
                return true;
            }

            // F3 = 打开存档菜单
            if (Input.GetKeyDown(KeyCode.F3))
            {
                DebugLogger.LogInput("F3", "打开存档菜单");
                OpenSaveMenu();
                return true;
            }

            // F4 = 打开读档菜单
            if (Input.GetKeyDown(KeyCode.F4))
            {
                DebugLogger.LogInput("F4", "打开读档菜单");
                OpenLoadMenu();
                return true;
            }

            return false;
        }
        #endregion

        #region Handler Updates
        private void UpdateHandlers()
        {
            // 开发阶段：尝试初始化对话处理器（因为禁用了游戏就绪检测）
            if (_dialogueHandler != null && !_dialogueHandlerInitialized)
            {
                try
                {
                    // 检查引擎是否已初始化
                    if (Engine.Initialized)
                    {
                        _dialogueHandler.Initialize();
                        _dialogueHandlerInitialized = true;
                        Log.LogInfo("对话处理器已初始化（开发阶段自动初始化）");
                    }
                }
                catch (System.Exception)
                {
                    // 引擎还没准备好，忽略异常
                }
            }

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

        #region SaveLoad
        /// <summary>
        /// 快速存档。
        /// </summary>
        private void QuickSave()
        {
            try
            {
                var stateManager = Engine.GetService<IStateManager>();
                if (stateManager != null)
                {
                    // 快速存档是异步方法，这里直接调用，不等待结果
                    var task = stateManager.QuickSave();
                    ScreenReader.Say("正在快速存档");
                    Log.LogInfo("快速存档已触发");
                }
                else
                {
                    ScreenReader.Say("存档功能不可用");
                    Log.LogWarning("无法获取IStateManager服务");
                }
            }
            catch (System.Exception e)
            {
                ScreenReader.Say("快速存档失败");
                Log.LogWarning($"快速存档时出错: {e.Message}");
            }
        }

        /// <summary>
        /// 快速读档。
        /// </summary>
        private void QuickLoad()
        {
            try
            {
                var stateManager = Engine.GetService<IStateManager>();
                if (stateManager != null)
                {
                    if (stateManager.QuickLoadAvailable)
                    {
                        // 快速读档是异步方法，这里直接调用，不等待结果
                        var task = stateManager.QuickLoad();
                        ScreenReader.Say("正在快速读档");
                        Log.LogInfo("快速读档已触发");
                    }
                    else
                    {
                        ScreenReader.Say("没有可用的快速存档");
                        Log.LogInfo("快速读档不可用，没有存档");
                    }
                }
                else
                {
                    ScreenReader.Say("读档功能不可用");
                    Log.LogWarning("无法获取IStateManager服务");
                }
            }
            catch (System.Exception e)
            {
                ScreenReader.Say("快速读档失败");
                Log.LogWarning($"快速读档时出错: {e.Message}");
            }
        }

        /// <summary>
        /// 打开存档菜单。
        /// </summary>
        private void OpenSaveMenu()
        {
            try
            {
                var uiManager = Engine.GetService<IUIManager>();
                if (uiManager != null)
                {
                    var saveLoadUI = uiManager.GetUI<ISaveLoadUI>();
                    if (saveLoadUI != null)
                    {
                        saveLoadUI.Visible = true;
                        
                        // 切换到存档面板
                        SwitchToSavePanel(saveLoadUI);
                        
                        ScreenReader.Say("打开存档菜单");
                        Log.LogInfo("已打开存档菜单");
                    }
                    else
                    {
                        ScreenReader.Say("存档菜单不可用");
                        Log.LogWarning("无法获取ISaveLoadUI");
                    }
                }
                else
                {
                    ScreenReader.Say("UI功能不可用");
                    Log.LogWarning("无法获取IUIManager服务");
                }
            }
            catch (System.Exception e)
            {
                ScreenReader.Say("打开存档菜单失败");
                Log.LogWarning($"打开存档菜单时出错: {e.Message}");
            }
        }

        /// <summary>
        /// 打开读档菜单。
        /// </summary>
        private void OpenLoadMenu()
        {
            try
            {
                var uiManager = Engine.GetService<IUIManager>();
                if (uiManager != null)
                {
                    var saveLoadUI = uiManager.GetUI<ISaveLoadUI>();
                    if (saveLoadUI != null)
                    {
                        saveLoadUI.Visible = true;
                        
                        // 切换到读档面板
                        SwitchToLoadPanel(saveLoadUI);
                        
                        ScreenReader.Say("打开读档菜单");
                        Log.LogInfo("已打开读档菜单");
                    }
                    else
                    {
                        ScreenReader.Say("读档菜单不可用");
                        Log.LogWarning("无法获取ISaveLoadUI");
                    }
                }
                else
                {
                    ScreenReader.Say("UI功能不可用");
                    Log.LogWarning("无法获取IUIManager服务");
                }
            }
            catch (System.Exception e)
            {
                ScreenReader.Say("打开读档菜单失败");
                Log.LogWarning($"打开读档菜单时出错: {e.Message}");
            }
        }

        /// <summary>
        /// 切换到存档面板。
        /// </summary>
        private void SwitchToSavePanel(ISaveLoadUI saveLoadUI)
        {
            try
            {
                // 方法1：尝试设置PresentationMode属性（SaveLoadMenu的标准方式）
                var presentationModeProperty = saveLoadUI.GetType().GetProperty("PresentationMode", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.FlattenHierarchy);
                if (presentationModeProperty != null)
                {
                    var enumType = presentationModeProperty.PropertyType;
                    if (enumType.IsEnum)
                    {
                        // 尝试获取Save枚举值
                        var saveValue = System.Enum.Parse(enumType, "Save");
                        presentationModeProperty.SetValue(saveLoadUI, saveValue);
                        Log.LogInfo("已通过PresentationMode属性设置为存档面板");
                        return;
                    }
                }

                // 方法2：尝试调用SetPresentationMode方法
                var setPresentationModeMethod = saveLoadUI.GetType().GetMethod("SetPresentationMode", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.FlattenHierarchy);
                if (setPresentationModeMethod != null)
                {
                    var parameters = setPresentationModeMethod.GetParameters();
                    if (parameters.Length == 1)
                    {
                        var enumType = parameters[0].ParameterType;
                        if (enumType.IsEnum)
                        {
                            var saveValue = System.Enum.Parse(enumType, "Save");
                            setPresentationModeMethod.Invoke(saveLoadUI, new object[] { saveValue });
                            Log.LogInfo("已通过SetPresentationMode方法设置为存档面板");
                            return;
                        }
                    }
                }

                // 方法3：尝试找到SaveLoadSwitchPanelButton并点击
                var uiGameObject = saveLoadUI as MonoBehaviour;
                if (uiGameObject != null)
                {
                    var switchButton = uiGameObject.transform.Find("SaveLoadSwitchPanelButton")?.GetComponent<UnityEngine.UI.Button>();
                    if (switchButton == null)
                    {
                        // 尝试在子对象中查找
                        var buttons = uiGameObject.GetComponentsInChildren<UnityEngine.UI.Button>(true);
                        foreach (var btn in buttons)
                        {
                            if (btn.name == "SaveLoadSwitchPanelButton")
                            {
                                switchButton = btn;
                                break;
                            }
                        }
                    }
                    
                    if (switchButton != null && switchButton.interactable)
                    {
                        // 检查当前是否已经是存档面板
                        var savePanel = uiGameObject.transform.Find("SavePanel");
                        if (savePanel != null && !savePanel.gameObject.activeSelf)
                        {
                            switchButton.onClick.Invoke();
                            Log.LogInfo("已点击切换面板按钮切换到存档面板");
                            return;
                        }
                    }
                }
                
                // 方法4：尝试找到SaveButton并点击（备用）
                if (uiGameObject != null)
                {
                    var saveButton = uiGameObject.transform.Find("SaveButton")?.GetComponent<UnityEngine.UI.Button>();
                    if (saveButton == null)
                    {
                        // 尝试在子对象中查找
                        var buttons = uiGameObject.GetComponentsInChildren<UnityEngine.UI.Button>(true);
                        foreach (var btn in buttons)
                        {
                            if (btn.name == "SaveButton")
                            {
                                saveButton = btn;
                                break;
                            }
                        }
                    }
                    
                    if (saveButton != null && saveButton.interactable)
                    {
                        saveButton.onClick.Invoke();
                        Log.LogInfo("已点击存档面板按钮");
                        return;
                    }
                }
                
                // 方法5：尝试通过PanelType属性设置（备用）
                var panelTypeProperty = saveLoadUI.GetType().GetProperty("PanelType", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (panelTypeProperty != null)
                {
                    // 假设Save是0
                    panelTypeProperty.SetValue(saveLoadUI, 0);
                    Log.LogInfo("已通过PanelType属性设置为存档面板");
                    return;
                }
                
                Log.LogWarning("无法切换到存档面板，未找到可用的方法");
            }
            catch (System.Exception e)
            {
                Log.LogWarning($"切换到存档面板时出错: {e.Message}");
            }
        }

        /// <summary>
        /// 切换到读档面板。
        /// </summary>
        private void SwitchToLoadPanel(ISaveLoadUI saveLoadUI)
        {
            try
            {
                // 方法1：尝试设置PresentationMode属性（SaveLoadMenu的标准方式）
                var presentationModeProperty = saveLoadUI.GetType().GetProperty("PresentationMode", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.FlattenHierarchy);
                if (presentationModeProperty != null)
                {
                    var enumType = presentationModeProperty.PropertyType;
                    if (enumType.IsEnum)
                    {
                        // 尝试获取Load枚举值
                        var loadValue = System.Enum.Parse(enumType, "Load");
                        presentationModeProperty.SetValue(saveLoadUI, loadValue);
                        Log.LogInfo("已通过PresentationMode属性设置为读档面板");
                        return;
                    }
                }

                // 方法2：尝试调用SetPresentationMode方法
                var setPresentationModeMethod = saveLoadUI.GetType().GetMethod("SetPresentationMode", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.FlattenHierarchy);
                if (setPresentationModeMethod != null)
                {
                    var parameters = setPresentationModeMethod.GetParameters();
                    if (parameters.Length == 1)
                    {
                        var enumType = parameters[0].ParameterType;
                        if (enumType.IsEnum)
                        {
                            var loadValue = System.Enum.Parse(enumType, "Load");
                            setPresentationModeMethod.Invoke(saveLoadUI, new object[] { loadValue });
                            Log.LogInfo("已通过SetPresentationMode方法设置为读档面板");
                            return;
                        }
                    }
                }

                // 方法3：尝试找到SaveLoadSwitchPanelButton并点击
                var uiGameObject = saveLoadUI as MonoBehaviour;
                if (uiGameObject != null)
                {
                    var switchButton = uiGameObject.transform.Find("SaveLoadSwitchPanelButton")?.GetComponent<UnityEngine.UI.Button>();
                    if (switchButton == null)
                    {
                        // 尝试在子对象中查找
                        var buttons = uiGameObject.GetComponentsInChildren<UnityEngine.UI.Button>(true);
                        foreach (var btn in buttons)
                        {
                            if (btn.name == "SaveLoadSwitchPanelButton")
                            {
                                switchButton = btn;
                                break;
                            }
                        }
                    }
                    
                    if (switchButton != null && switchButton.interactable)
                    {
                        // 检查当前是否已经是读档面板
                        var loadPanel = uiGameObject.transform.Find("LoadPanel");
                        if (loadPanel != null && !loadPanel.gameObject.activeSelf)
                        {
                            switchButton.onClick.Invoke();
                            Log.LogInfo("已点击切换面板按钮切换到读档面板");
                            return;
                        }
                    }
                }
                
                // 方法4：尝试找到LoadButton并点击（备用）
                if (uiGameObject != null)
                {
                    var loadButton = uiGameObject.transform.Find("LoadButton")?.GetComponent<UnityEngine.UI.Button>();
                    if (loadButton == null)
                    {
                        // 尝试在子对象中查找
                        var buttons = uiGameObject.GetComponentsInChildren<UnityEngine.UI.Button>(true);
                        foreach (var btn in buttons)
                        {
                            if (btn.name == "LoadButton")
                            {
                                loadButton = btn;
                                break;
                            }
                        }
                    }
                    
                    if (loadButton != null && loadButton.interactable)
                    {
                        loadButton.onClick.Invoke();
                        Log.LogInfo("已点击读档面板按钮");
                        return;
                    }
                }
                
                // 方法5：尝试通过PanelType属性设置（备用）
                var panelTypeProperty = saveLoadUI.GetType().GetProperty("PanelType", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (panelTypeProperty != null)
                {
                    // 假设Load是1
                    panelTypeProperty.SetValue(saveLoadUI, 1);
                    Log.LogInfo("已通过PanelType属性设置为读档面板");
                    return;
                }
                
                Log.LogWarning("无法切换到读档面板，未找到可用的方法");
            }
            catch (System.Exception e)
            {
                Log.LogWarning($"切换到读档面板时出错: {e.Message}");
            }
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
