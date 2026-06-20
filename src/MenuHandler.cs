using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using Naninovel;
using Naninovel.UI;
using NananaGames.UI;

namespace HuXiangLianPian.Accessibility
{
    /// <summary>
    /// 菜单导航处理器。
    /// 监听UI选中变化，根据不同类型的菜单和元素进行特殊适配。
    /// </summary>
    public class MenuHandler : IDisposable
    {
        #region Fields
        private GameObject _lastSelectedObject;
        private string _lastAnnouncedText;
        private float _lastAnnounceTime;
        private const float MIN_ANNOUNCE_INTERVAL = 0.1f; // 最小朗读间隔，防止刷屏
        private float _lastLogTime = 0f;
        private const float LOG_INTERVAL = 5f; // 每5秒打一次状态日志

        // 当前打开的菜单类型
        private MenuType _currentMenuType = MenuType.None;
        private MenuType _lastMenuType = MenuType.None;
        #endregion

        #region Enums
        private enum MenuType
        {
            None,
            Title,      // 标题菜单
            Settings,   // 设置菜单
            SaveLoad,   // 存档/读档菜单
            Backlog,    // 历史记录
            Dialogue,   // 对话界面
            Other       // 其他菜单
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 每帧更新，检查选中变化。
        /// </summary>
        public void Update()
        {
            // 开发阶段：禁用Engine.Initialized检查，让Mod直接可用
            // if (!Engine.Initialized) return;

            // 每隔几秒打一次状态日志
            if (Time.unscaledTime - _lastLogTime > LOG_INTERVAL)
            {
                _lastLogTime = Time.unscaledTime;
                Main.Log.LogInfo($"MenuHandler状态 - EventSystem: {(EventSystem.current != null ? "存在" : "不存在")}, 当前菜单: {_currentMenuType}");
            }

            if (EventSystem.current == null) return;

            // 检测当前菜单类型
            DetectCurrentMenu();

            var currentSelected = EventSystem.current.currentSelectedGameObject;

            // 选中对象变化时
            if (currentSelected != _lastSelectedObject)
            {
                _lastSelectedObject = currentSelected;
                HandleSelectionChanged(currentSelected);
            }
        }

        /// <summary>
        /// 朗读当前选中的元素。
        /// </summary>
        public void AnnounceCurrentSelection()
        {
            if (_lastSelectedObject != null)
            {
                HandleSelectionChanged(_lastSelectedObject, force: true);
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 检测当前打开的菜单类型。
        /// </summary>
        private void DetectCurrentMenu()
        {
            try
            {
                var uiManager = Engine.GetService<IUIManager>();
                if (uiManager == null) return;

                MenuType newMenuType = MenuType.None;

                // 检查设置菜单
                var settingsUI = uiManager.GetUI<ISettingsUI>();
                if (settingsUI != null && settingsUI.Visible)
                {
                    newMenuType = MenuType.Settings;
                }

                // 检查存档/读档菜单
                var saveLoadUI = uiManager.GetUI<ISaveLoadUI>();
                if (saveLoadUI != null && saveLoadUI.Visible)
                {
                    newMenuType = MenuType.SaveLoad;
                }

                // 检查标题菜单
                var titleUI = uiManager.GetUI<ITitleUI>();
                if (titleUI != null && titleUI.Visible)
                {
                    newMenuType = MenuType.Title;
                }

                // 检查历史记录
                var backlogUI = uiManager.GetUI<IBacklogUI>();
                if (backlogUI != null && backlogUI.Visible)
                {
                    newMenuType = MenuType.Backlog;
                }

                // 如果菜单类型变化了
                if (newMenuType != _currentMenuType)
                {
                    _lastMenuType = _currentMenuType;
                    _currentMenuType = newMenuType;

                    // 朗读菜单标题
                    if (newMenuType != MenuType.None)
                    {
                        string menuName = GetMenuName(newMenuType);
                        if (!string.IsNullOrEmpty(menuName))
                        {
                            ScreenReader.Say(menuName);
                            DebugLogger.Log(LogCategory.State, $"菜单打开: {menuName}");
                        }

                        // 调试模式：输出菜单详细信息
                        if (Main.DebugMode)
                        {
                            LogMenuDetails(newMenuType, uiManager);
                        }
                    }
                    else
                    {
                        DebugLogger.Log(LogCategory.State, "菜单关闭");
                    }
                }
            }
            catch (System.Exception e)
            {
                // 开发阶段：输出异常信息，方便调试
                if (Main.DebugMode && Time.unscaledTime - _lastLogTime > LOG_INTERVAL)
                {
                    _lastLogTime = Time.unscaledTime;
                    Main.Log.LogWarning($"检测菜单时出错: {e.GetType().Name} - {e.Message}");
                }
            }
        }

        /// <summary>
        /// 获取菜单的显示名称。
        /// </summary>
        private string GetMenuName(MenuType menuType)
        {
            switch (menuType)
            {
                case MenuType.Title:
                    return "标题菜单";
                case MenuType.Settings:
                    return "设置菜单";
                case MenuType.SaveLoad:
                    return "存档读档菜单";
                case MenuType.Backlog:
                    return "历史记录";
                case MenuType.Dialogue:
                    return "对话界面";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// 输出菜单详细信息（调试用）。
        /// 包括可点击对象数量、对象名称等。
        /// </summary>
        private void LogMenuDetails(MenuType menuType, IUIManager uiManager)
        {
            try
            {
                string menuName = GetMenuName(menuType);
                Main.Log.LogInfo($"========== 菜单详细信息: {menuName} ==========");

                // 获取对应的UI对象
                IManagedUI ui = null;
                switch (menuType)
                {
                    case MenuType.Title:
                        ui = uiManager.GetUI<ITitleUI>();
                        break;
                    case MenuType.Settings:
                        ui = uiManager.GetUI<ISettingsUI>();
                        break;
                    case MenuType.SaveLoad:
                        ui = uiManager.GetUI<ISaveLoadUI>();
                        break;
                    case MenuType.Backlog:
                        ui = uiManager.GetUI<IBacklogUI>();
                        break;
                }

                if (ui == null)
                {
                    Main.Log.LogInfo("  无法获取UI对象");
                    return;
                }

                // 获取UI的GameObject
                var uiGameObject = ui as MonoBehaviour;
                if (uiGameObject == null)
                {
                    Main.Log.LogInfo("  UI对象不是MonoBehaviour");
                    return;
                }

                // 统计所有可交互元素
                var buttons = uiGameObject.GetComponentsInChildren<Button>(true);
                var toggles = uiGameObject.GetComponentsInChildren<Toggle>(true);
                var selectables = uiGameObject.GetComponentsInChildren<Selectable>(true);

                Main.Log.LogInfo($"  可交互元素总数: {selectables.Length}");
                Main.Log.LogInfo($"  按钮数量: {buttons.Length}");
                Main.Log.LogInfo($"  开关数量: {toggles.Length}");
                Main.Log.LogInfo("");

                // 列出所有按钮
                if (buttons.Length > 0)
                {
                    Main.Log.LogInfo("  --- 按钮列表 ---");
                    for (int i = 0; i < buttons.Length; i++)
                    {
                        var btn = buttons[i];
                        string btnText = GetButtonText(btn);
                        Main.Log.LogInfo($"    [{i}] {btn.gameObject.name} - 文本: {btnText} - 可交互: {btn.interactable}");
                    }
                    Main.Log.LogInfo("");
                }

                // 列出所有开关
                if (toggles.Length > 0)
                {
                    Main.Log.LogInfo("  --- 开关列表 ---");
                    for (int i = 0; i < toggles.Length; i++)
                    {
                        var toggle = toggles[i];
                        string toggleText = GetToggleText(toggle);
                        Main.Log.LogInfo($"    [{i}] {toggle.gameObject.name} - 文本: {toggleText} - 状态: {(toggle.isOn ? "开" : "关")} - 可交互: {toggle.interactable}");
                    }
                    Main.Log.LogInfo("");
                }

                // 列出所有Selectable（包括上面没有覆盖的类型）
                if (selectables.Length > 0)
                {
                    Main.Log.LogInfo("  --- 所有可交互元素 ---");
                    for (int i = 0; i < selectables.Length; i++)
                    {
                        var sel = selectables[i];
                        Main.Log.LogInfo($"    [{i}] {sel.gameObject.name} - 类型: {sel.GetType().Name} - 可交互: {sel.interactable}");
                    }
                    Main.Log.LogInfo("");
                }

                Main.Log.LogInfo($"========== 菜单详细信息结束 ==========");
            }
            catch (System.Exception e)
            {
                Main.Log.LogWarning($"输出菜单详细信息时出错: {e.Message}");
            }
        }

        /// <summary>
        /// 处理选中变化。
        /// </summary>
        private void HandleSelectionChanged(GameObject selected, bool force = false)
        {
            if (selected == null) return;

            // 防止频繁朗读
            if (!force && Time.unscaledTime - _lastAnnounceTime < MIN_ANNOUNCE_INTERVAL)
            {
                return;
            }

            string text = GetTextForSelectedObject(selected);
            if (!string.IsNullOrEmpty(text) && (force || text != _lastAnnouncedText))
            {
                _lastAnnouncedText = text;
                _lastAnnounceTime = Time.unscaledTime;
                ScreenReader.Say(text);

                if (Main.DebugMode)
                {
                    DebugLogger.Log(LogCategory.Handler, "MenuHandler", $"选中: {selected.name} -> {text}");
                }
            }
        }

        /// <summary>
        /// 根据选中对象的类型获取对应的文本。
        /// </summary>
        private string GetTextForSelectedObject(GameObject selected)
        {
            if (selected == null) return string.Empty;

            // 检查是否是存档槽位
            var saveLoadSlot = selected.GetComponent<SaveLoadSlot>();
            if (saveLoadSlot != null)
            {
                return GetSaveLoadSlotText(saveLoadSlot);
            }

            // 检查是否是GameStateSlot（基类）
            var gameStateSlot = selected.GetComponent<GameStateSlot>();
            if (gameStateSlot != null)
            {
                return GetGameStateSlotText(gameStateSlot);
            }

            // 检查是否是设置项开关
            var scriptableToggle = selected.GetComponent<ScriptableToggle>();
            if (scriptableToggle != null)
            {
                return GetScriptableToggleText(scriptableToggle);
            }

            // 检查是否是Toggle
            var toggle = selected.GetComponent<Toggle>();
            if (toggle != null)
            {
                return GetToggleText(toggle);
            }

            // 检查是否是Button
            var button = selected.GetComponent<Button>();
            if (button != null)
            {
                return GetButtonText(button);
            }

            // 检查是否有TMP_Text子组件
            var tmpText = selected.GetComponentInChildren<TMP_Text>();
            if (tmpText != null && !string.IsNullOrEmpty(tmpText.text))
            {
                return tmpText.text;
            }

            // 检查是否有Text子组件
            var text = selected.GetComponentInChildren<Text>();
            if (text != null && !string.IsNullOrEmpty(text.text))
            {
                return text.text;
            }

            // 最后返回对象名称（调试用）
            if (Main.DebugMode)
            {
                return $"未知元素: {selected.name}";
            }

            return string.Empty;
        }

        /// <summary>
        /// 获取存档槽位的文本。
        /// </summary>
        private string GetSaveLoadSlotText(SaveLoadSlot slot)
        {
            if (slot == null) return string.Empty;

            // 获取槽位编号
            int slotNumber = slot.SlotNumber;

            // 尝试从GameStateMap获取详细信息
            // 注意：这里可能需要通过反射或其他方式获取
            // 先返回基础信息

            // 尝试获取槽位上的文本组件
            var texts = slot.GetComponentsInChildren<TMP_Text>();
            if (texts != null && texts.Length > 0)
            {
                // 组合所有文本
                string result = string.Empty;
                foreach (var t in texts)
                {
                    if (!string.IsNullOrEmpty(t.text))
                    {
                        result += t.text + " ";
                    }
                }
                if (!string.IsNullOrEmpty(result))
                {
                    return $"存档槽位 {slotNumber}，{result.Trim()}";
                }
            }

            return $"存档槽位 {slotNumber}";
        }

        /// <summary>
        /// 获取游戏状态槽位的文本。
        /// </summary>
        private string GetGameStateSlotText(GameStateSlot slot)
        {
            if (slot == null) return string.Empty;
            return $"存档槽位 {slot.SlotNumber}";
        }

        /// <summary>
        /// 获取可脚本化开关的文本。
        /// </summary>
        private string GetScriptableToggleText(ScriptableToggle toggle)
        {
            if (toggle == null) return string.Empty;

            // 获取开关的文本
            var text = toggle.GetComponentInChildren<TMP_Text>();
            string label = text != null ? text.text : toggle.name;

            // 获取开关状态
            bool isOn = toggle.UIComponent != null && toggle.UIComponent.isOn;
            string status = isOn ? "开启" : "关闭";

            return $"{label}，{status}";
        }

        /// <summary>
        /// 获取Toggle的文本。
        /// </summary>
        private string GetToggleText(Toggle toggle)
        {
            if (toggle == null) return string.Empty;

            // 获取开关的文本
            var text = toggle.GetComponentInChildren<TMP_Text>();
            string label = text != null ? text.text : toggle.name;

            // 获取开关状态
            string status = toggle.isOn ? "开启" : "关闭";

            return $"{label}，{status}";
        }

        /// <summary>
        /// 获取按钮的文本。
        /// </summary>
        private string GetButtonText(Button button)
        {
            if (button == null) return string.Empty;

            // 获取按钮上的文本
            var tmpText = button.GetComponentInChildren<TMP_Text>();
            if (tmpText != null && !string.IsNullOrEmpty(tmpText.text))
            {
                return tmpText.text;
            }

            var text = button.GetComponentInChildren<Text>();
            if (text != null && !string.IsNullOrEmpty(text.text))
            {
                return text.text;
            }

            // 返回按钮名称
            return button.name;
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            // 清理资源
        }
        #endregion
    }
}
