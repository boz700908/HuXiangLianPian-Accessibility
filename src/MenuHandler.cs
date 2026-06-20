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
            if (!Engine.Initialized) return;
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
                    }
                }
            }
            catch (System.Exception)
            {
                // 忽略错误
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
