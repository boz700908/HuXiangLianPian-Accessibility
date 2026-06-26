using System;
using NananaGames.UI;
using Naninovel;
using Naninovel.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
        private const float LOG_INTERVAL = 30f; // 每30秒打一次状态日志

        // 当前打开的菜单类型
        private MenuType _currentMenuType = MenuType.None;
        private MenuType _lastMenuType = MenuType.None;
        private bool? _lastSettingsSoundTabOn;
        private string _lastSaveLoadMode;

        // 硬编码的按钮文本映射（图片按钮无法动态获取文本）
        private static readonly System.Collections.Generic.Dictionary<string, string> _buttonTextMap = new System.Collections.Generic.Dictionary<string, string>
        {
            // 标题菜单
            { "NewGameButton", "开始游戏" },
            { "ContinueButton", "读取进度" },
            { "SettingsButton", "环境设定" },
            { "CGGalleryButton", "CG画廊" },
            { "ExitButton", "离开游戏" },
            
            // 设置菜单 - 按钮
            { "ReturnButton", "关闭" },
            { "ReturnTitleButton", "回到标题画面" },
            
            // 设置菜单 - 标签
            { "SettingToggle", "设置标签" },
            { "SoundToggle", "声音标签" },
            
            // 设置菜单 - 画面设置
            { "FullToggle", "全屏模式" },
            { "WindowToggle", "窗口模式" },
            
            // 设置菜单 - 分辨率
            { "Res1440Toggle", "2560*1440" },
            { "Res1080Toggle", "1920*1080" },
            
            // 设置菜单 - 跳过设置
            { "SkipAllToggle", "允许跳过未读部分，是" },
            { "ReadOnlyToggle", "允许跳过未读部分，否" },
            
            // 存档读档菜单
            { "DeleteButton", "删除" },
            { "QuickButton", "快速存档" },
            { "AutoButton", "自动存档" },
            { "FirstButton", "第一页" },
            { "PrevButton", "上一页" },
            { "NextButton", "下一页" },
            { "LastButton", "最后一页" },
            { "LoadButton", "切换到读档界面" },
            { "SaveButton", "切换到存档界面" },
            { "PreviousPageButton", "上一页" },
            { "NextPageButton", "下一页" },
            { "SaveLoadSwitchPanelButton", "切换存档读档界面" },
            
            // 确认对话框
            { "ConfirmButton", "确认" },
            { "CancelButton", "取消" },
            { "YesButton", "是" },
            { "NoButton", "否" },
            { "OkButton", "确定" },
        };

        // 硬编码的滑块文本映射
        private static readonly System.Collections.Generic.Dictionary<string, string> _sliderTextMap = new System.Collections.Generic.Dictionary<string, string>
        {
            // 设置菜单 - 文字设置
            { "MessageSpeed", "文字显示速度" },
            { "AutoDelay", "自动模式文字速度" },
            
            // 设置菜单 - 声音设置
            { "Master", "全体音量" },
            { "Music", "音乐音量" },
            { "Voice", "角色语音" },
            { "SE", "SE音量" },
        };

        // 滑块值变化实时朗读
        private Slider _currentSlider;
        private float _lastSliderValue;
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
            Confirmation, // 确认对话框
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
                LogStatus();
            }

            if (EventSystem.current == null) return;

            // 检测当前菜单类型
            DetectCurrentMenu();

            if (_currentMenuType == MenuType.None)
            {
                ClearSelectionState();
                return;
            }

            RefreshSettingsNavigationOnTabChange();
            RefreshSaveLoadNavigationOnPanelChange();

            var currentSelected = EventSystem.current.currentSelectedGameObject;
            if (currentSelected != null && !IsSelectableVisibleAndInteractable(currentSelected.GetComponent<Selectable>()))
            {
                if (Main.DebugMode)
                {
                    Main.Log.LogInfo($"清理无效选中对象: {currentSelected.name}");
                }
                EventSystem.current.SetSelectedGameObject(null);
                _lastSelectedObject = null;
                _currentSlider = null;
                currentSelected = null;
                RefreshCurrentMenuNavigation();
                currentSelected = EventSystem.current.currentSelectedGameObject;
            }

            // 选中对象变化时
            if (currentSelected != _lastSelectedObject)
            {
                _lastSelectedObject = currentSelected;
                HandleSelectionChanged(currentSelected);
            }

            // 滑块值变化实时朗读
            CheckSliderValueChanged();
        }

        /// <summary>
        /// 输出状态日志（调试用）。
        /// </summary>
        private void LogStatus()
        {
            Main.Log.LogInfo($"MenuHandler状态 - EventSystem: {(EventSystem.current != null ? "存在" : "不存在")}, 当前菜单: {_currentMenuType}");

            try
            {
                var uiManager = Engine.GetService<IUIManager>();
                if (uiManager != null)
                {
                    // 收集所有UI信息
                    var allUIs = new System.Collections.Generic.List<IManagedUI>();
                    uiManager.GetManagedUIs(allUIs);

                    Main.Log.LogInfo($"  已注册UI总数: {allUIs.Count}");

                    // 列出可见的UI
                    int visibleCount = 0;
                    foreach (var ui in allUIs)
                    {
                        if (ui.Visible)
                        {
                            visibleCount++;
                            string uiName = ui is UnityEngine.Object obj ? obj.name : ui.GetType().Name;
                            Main.Log.LogInfo($"    可见UI: {uiName} ({ui.GetType().Name})");
                        }
                    }

                    if (visibleCount == 0)
                    {
                        Main.Log.LogInfo("  没有可见的UI");
                    }
                }
                else
                {
                    Main.Log.LogInfo("  IUIManager: null");
                }
            }
            catch (System.Exception e)
            {
                Main.Log.LogWarning($"  获取UI列表时出错: {e.GetType().Name} - {e.Message}");
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

                // 检查确认对话框（优先级最高，因为它是模态的）
                var confirmationUI = uiManager.GetUI<IConfirmationUI>();
                if (confirmationUI != null && confirmationUI.Visible)
                {
                    newMenuType = MenuType.Confirmation;
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

                        // 如果是确认对话框，朗读对话框文本内容
                        if (newMenuType == MenuType.Confirmation)
                        {
                            AnnounceConfirmationMessage(uiManager);
                        }

                        // 自动修复导航
                        FixNavigation(uiManager, newMenuType);

                        // 调试模式：输出菜单详细信息
                        if (Main.DebugMode)
                        {
                            LogMenuDetails(newMenuType, uiManager);
                        }
                    }
                    else
                    {
                        MenuType closedMenuType = _lastMenuType;
                        DebugLogger.Log(LogCategory.State, "菜单关闭");
                        ClearSelectionState();
                        RestoreAfterSettingsClosed(uiManager);
                        if (closedMenuType == MenuType.SaveLoad)
                        {
                            Main.AnnounceCurrentDialogue("存档读档菜单返回后");
                        }
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
        /// 清理菜单关闭后遗留的键盘焦点，避免 EventSystem 停在隐藏 UI 元素上。
        /// </summary>
        private void ClearSelectionState()
        {
            _lastSelectedObject = null;
            _currentSlider = null;
            _lastSettingsSoundTabOn = null;
            _lastSaveLoadMode = null;

            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            {
                if (Main.DebugMode)
                {
                    Main.Log.LogInfo($"菜单关闭，清空选中对象: {EventSystem.current.currentSelectedGameObject.name}");
                }
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        /// <summary>
        /// 游戏的 SettingsUI Esc 逻辑只隐藏设置菜单；从标题菜单进入时需要恢复标题菜单。
        /// </summary>
        private void RestoreAfterSettingsClosed(IUIManager uiManager)
        {
            if (_lastMenuType != MenuType.Settings || uiManager == null) return;

            try
            {
                var settingsUI = uiManager.GetUI<SettingsUI>();
                if (settingsUI == null || settingsUI.Visible || settingsUI.BackUIType != SettingsUI.UIType.Title)
                {
                    return;
                }

                var titleUI = uiManager.GetUI<ITitleUI>();
                if (titleUI != null && !titleUI.Visible)
                {
                    titleUI.Show();
                    if (Main.DebugMode)
                    {
                        Main.Log.LogInfo("设置菜单关闭后恢复标题菜单");
                    }
                }
            }
            catch (System.Exception e)
            {
                Main.Log.LogWarning($"设置菜单关闭后恢复标题菜单时出错: {e.GetType().Name} - {e.Message}");
            }
        }

        /// <summary>
        /// 朗读确认对话框的消息文本。
        /// </summary>
        private void AnnounceConfirmationMessage(IUIManager uiManager)
        {
            try
            {
                var confirmationUI = uiManager.GetUI<IConfirmationUI>();
                if (confirmationUI == null) return;

                var uiGameObject = confirmationUI as MonoBehaviour;
                if (uiGameObject == null) return;

                // 尝试从子对象中找到消息文本
                // 确认对话框通常会有一个包含消息文本的TMP_Text或Text组件
                var tmpTexts = uiGameObject.GetComponentsInChildren<TMP_Text>(true);
                if (tmpTexts != null && tmpTexts.Length > 0)
                {
                    // 找到最长的文本，通常是消息内容
                    TMP_Text messageText = null;
                    int maxLength = 0;
                    foreach (var tmpText in tmpTexts)
                    {
                        if (!string.IsNullOrEmpty(tmpText.text) && tmpText.text.Length > maxLength)
                        {
                            // 排除按钮文本（通常比较短）
                            if (tmpText.text.Length > 5)
                            {
                                messageText = tmpText;
                                maxLength = tmpText.text.Length;
                            }
                        }
                    }

                    if (messageText != null)
                    {
                        string message = messageText.text;
                        if (!string.IsNullOrEmpty(message))
                        {
                            // 延迟一点朗读，让菜单标题先读完
                            // 这里直接朗读，ScreenReader内部会处理队列
                            ScreenReader.Say(message);
                            DebugLogger.Log(LogCategory.Handler, "MenuHandler", $"确认对话框消息: {message}");
                            return;
                        }
                    }
                }

                // 如果没找到TMP_Text，尝试找普通Text
                var texts = uiGameObject.GetComponentsInChildren<Text>(true);
                if (texts != null && texts.Length > 0)
                {
                    Text messageText = null;
                    int maxLength = 0;
                    foreach (var text in texts)
                    {
                        if (!string.IsNullOrEmpty(text.text) && text.text.Length > maxLength)
                        {
                            if (text.text.Length > 5)
                            {
                                messageText = text;
                                maxLength = text.text.Length;
                            }
                        }
                    }

                    if (messageText != null)
                    {
                        string message = messageText.text;
                        if (!string.IsNullOrEmpty(message))
                        {
                            ScreenReader.Say(message);
                            DebugLogger.Log(LogCategory.Handler, "MenuHandler", $"确认对话框消息: {message}");
                            return;
                        }
                    }
                }

                // 调试：列出所有文本组件，方便后续完善
                if (Main.DebugMode)
                {
                    Main.Log.LogInfo("  --- 确认对话框所有文本组件 ---");
                    foreach (var tmpText in tmpTexts)
                    {
                        Main.Log.LogInfo($"    TMP_Text: '{tmpText.text}' (长度: {tmpText.text.Length})");
                    }
                    foreach (var text in texts)
                    {
                        Main.Log.LogInfo($"    Text: '{text.text}' (长度: {text.text.Length})");
                    }
                }
            }
            catch (System.Exception e)
            {
                if (Main.DebugMode)
                {
                    Main.Log.LogWarning($"朗读确认对话框消息时出错: {e.Message}");
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
                case MenuType.Confirmation:
                    return "确认对话框";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// 自动修复菜单导航。
        /// Naninovel的按钮默认导航模式是None，导致方向键无法导航。
        /// </summary>
        private void FixNavigation(IUIManager uiManager, MenuType menuType)
        {
            try
            {
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
                    case MenuType.Confirmation:
                        ui = uiManager.GetUI<IConfirmationUI>();
                        break;
                }

                if (ui == null) return;

                var uiGameObject = ui as MonoBehaviour;
                if (uiGameObject == null) return;

                // 获取所有可交互元素
                var selectables = uiGameObject.GetComponentsInChildren<Selectable>(true);

                if (Main.DebugMode)
                {
                    Main.Log.LogInfo($"修复导航 - 找到 {selectables.Length} 个可交互元素");
                }

                // 设置导航模式为 Automatic
                // 注意：修复所有Selectable（包括隐藏的），因为标签切换时隐藏的元素会变成可见
                int fixedCount = 0;
                foreach (var sel in selectables)
                {
                    var nav = sel.navigation;
                    if (nav.mode == Navigation.Mode.None)
                    {
                        nav.mode = Navigation.Mode.Automatic;
                        sel.navigation = nav;
                        fixedCount++;
                    }
                }

                if (Main.DebugMode)
                {
                    Main.Log.LogInfo($"修复导航 - 已修复 {fixedCount} 个元素的导航模式");
                }

                // 手动设置导航目标（Automatic模式在复杂布局下可能不工作）
                // 手动设置导航目标（Automatic模式在复杂布局下可能不工作）
                SetupManualNavigation(selectables);

                // 选中第一个可交互元素
                foreach (var sel in selectables)
                {
                    if (IsSelectableVisibleAndInteractable(sel))
                    {
                        EventSystem.current.SetSelectedGameObject(sel.gameObject);
                        if (Main.DebugMode)
                        {
                            Main.Log.LogInfo($"修复导航 - 默认选中: {sel.gameObject.name}");
                        }
                        break;
                    }
                }
            }
            catch (System.Exception e)
            {
                if (Main.DebugMode)
                {
                    Main.Log.LogWarning($"修复导航时出错: {e.Message}");
                }
            }
        }

        /// <summary>
        /// 手动设置导航目标。
        /// Automatic模式在复杂布局下可能不工作，需要手动设置。
        /// </summary>
        private void SetupManualNavigation(Selectable[] selectables)
        {
            try
            {
                // 过滤出可见的、可交互的Selectable
                var visibleSelectables = new System.Collections.Generic.List<Selectable>();
                foreach (var sel in selectables)
                {
                    if (IsSelectableVisibleAndInteractable(sel))
                    {
                        visibleSelectables.Add(sel);
                    }
                }

                if (visibleSelectables.Count <= 1) return;

                SortSelectablesForLinearNavigation(visibleSelectables);

                for (int i = 0; i < visibleSelectables.Count; i++)
                {
                    var current = visibleSelectables[i];
                    var nav = current.navigation;
                    nav.mode = Navigation.Mode.Explicit;

                    nav.selectOnUp = i > 0 ? visibleSelectables[i - 1] : null;
                    nav.selectOnDown = i < visibleSelectables.Count - 1 ? visibleSelectables[i + 1] : null;
                    nav.selectOnLeft = null;
                    nav.selectOnRight = null;

                    current.navigation = nav;
                }

                if (Main.DebugMode)
                {
                    Main.Log.LogInfo($"线性导航 - 已为 {visibleSelectables.Count} 个可见元素设置上下导航");
                }
            }
            catch (System.Exception e)
            {
                if (Main.DebugMode)
                {
                    Main.Log.LogWarning($"手动设置导航时出错: {e.Message}");
                }
            }
        }

        private void SortSelectablesForLinearNavigation(System.Collections.Generic.List<Selectable> selectables)
        {
            if (TrySortSaveLoadSelectables(selectables))
            {
                return;
            }

            selectables.Sort((a, b) =>
            {
                int priorityCompare = GetNavigationPriority(a).CompareTo(GetNavigationPriority(b));
                if (priorityCompare != 0) return priorityCompare;

                Vector3 aPos = a.transform.position;
                Vector3 bPos = b.transform.position;

                float yDiff = bPos.y - aPos.y;
                if (Mathf.Abs(yDiff) > 8f)
                {
                    return yDiff > 0f ? 1 : -1;
                }

                float xDiff = aPos.x - bPos.x;
                if (Mathf.Abs(xDiff) > 8f)
                {
                    return xDiff > 0f ? 1 : -1;
                }

                return string.CompareOrdinal(GetTransformPath(a.transform), GetTransformPath(b.transform));
            });
        }

        private int GetNavigationPriority(Selectable selectable)
        {
            if (selectable == null) return 100;
            string name = selectable.name;

            if (name == "SettingToggle") return 0;
            if (name == "SoundToggle") return 1;
            if (selectable is Slider) return 10;
            if (name == "ReturnButton") return 80;
            if (name == "ReturnTitleButton") return 90;
            if (name == "ExitButton") return 91;
            return 50;
        }

        private bool TrySortSaveLoadSelectables(System.Collections.Generic.List<Selectable> selectables)
        {
            bool hasSaveLoadSlot = false;
            foreach (var selectable in selectables)
            {
                if (selectable != null && selectable.GetComponent<SaveLoadSlot>() != null)
                {
                    hasSaveLoadSlot = true;
                    break;
                }
            }

            if (!hasSaveLoadSlot)
            {
                return false;
            }

            var result = new System.Collections.Generic.List<Selectable>();
            var slots = new System.Collections.Generic.List<SaveLoadSlot>();
            foreach (var selectable in selectables)
            {
                var slot = selectable != null ? selectable.GetComponent<SaveLoadSlot>() : null;
                if (slot != null)
                {
                    slots.Add(slot);
                }
            }

            slots.Sort((a, b) => a.SlotNumber.CompareTo(b.SlotNumber));
            foreach (var slot in slots)
            {
                var slotSelectable = slot.GetComponent<Selectable>();
                if (slotSelectable != null && selectables.Contains(slotSelectable) && !result.Contains(slotSelectable))
                {
                    result.Add(slotSelectable);
                }

                var deleteButton = FindDeleteButtonForSlot(slot);
                if (deleteButton != null && selectables.Contains(deleteButton) && !result.Contains(deleteButton))
                {
                    result.Add(deleteButton);
                }
            }

            AddByNames(result, selectables, true, "PageBtn_");
            AddByNames(result, selectables, "QuickButton", "AutoButton", "FirstButton", "PrevButton", "NextButton", "LastButton");
            AddByNames(result, selectables, "LoadButton", "SaveButton", "SaveLoadSwitchPanelButton");
            AddByNames(result, selectables, "ReturnTitleButton", "ExitButton", "ReturnButton");

            foreach (var selectable in selectables)
            {
                if (!result.Contains(selectable))
                {
                    result.Add(selectable);
                }
            }

            selectables.Clear();
            selectables.AddRange(result);
            return true;
        }

        private Selectable FindDeleteButtonForSlot(SaveLoadSlot slot)
        {
            if (slot == null) return null;

            var buttons = slot.GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                if (button != null && button.name == "DeleteButton" && IsSelectableVisibleAndInteractable(button))
                {
                    return button;
                }
            }

            return null;
        }

        private void AddByNames(System.Collections.Generic.List<Selectable> target, System.Collections.Generic.List<Selectable> source, params string[] names)
        {
            AddByNames(target, source, prefixMatch: false, names: names);
        }

        private void AddByNames(System.Collections.Generic.List<Selectable> target, System.Collections.Generic.List<Selectable> source, bool prefixMatch, params string[] names)
        {
            foreach (var name in names)
            {
                foreach (var selectable in source)
                {
                    if (selectable == null || target.Contains(selectable)) continue;

                    bool matches = prefixMatch
                        ? selectable.name.StartsWith(name, StringComparison.Ordinal)
                        : selectable.name == name;
                    if (matches)
                    {
                        target.Add(selectable);
                    }
                }
            }
        }

        private string GetTransformPath(Transform transform)
        {
            if (transform == null) return string.Empty;

            string path = transform.name;
            Transform parent = transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        /// <summary>
        /// 设置菜单的标签页切换不会重新打开 UI，需要在标签状态变化后重建导航。
        /// </summary>
        private void RefreshSettingsNavigationOnTabChange()
        {
            if (_currentMenuType != MenuType.Settings || EventSystem.current == null)
            {
                return;
            }

            try
            {
                var uiManager = Engine.GetService<IUIManager>();
                var settingsUI = uiManager?.GetUI<ISettingsUI>() as MonoBehaviour;
                if (settingsUI == null)
                {
                    return;
                }

                var soundToggleTransform = settingsUI.transform.Find("SoundToggle");
                Toggle soundToggle = soundToggleTransform != null ? soundToggleTransform.GetComponent<Toggle>() : null;
                if (soundToggle == null)
                {
                    var toggles = settingsUI.GetComponentsInChildren<Toggle>(true);
                    foreach (var toggle in toggles)
                    {
                        if (toggle.name == "SoundToggle")
                        {
                            soundToggle = toggle;
                            break;
                        }
                    }
                }

                if (soundToggle == null)
                {
                    return;
                }

                bool soundTabOn = soundToggle.isOn;
                if (_lastSettingsSoundTabOn.HasValue && _lastSettingsSoundTabOn.Value == soundTabOn)
                {
                    return;
                }

                _lastSettingsSoundTabOn = soundTabOn;
                var selectables = settingsUI.GetComponentsInChildren<Selectable>(true);
                SetupManualNavigation(selectables);

                var currentSelected = EventSystem.current.currentSelectedGameObject;
                var currentSelectable = currentSelected != null ? currentSelected.GetComponent<Selectable>() : null;
                if (!IsSelectableVisibleAndInteractable(currentSelectable))
                {
                    SelectFirstVisibleSelectable(selectables, soundTabOn ? "SoundToggle" : "SettingToggle");
                }

                if (Main.DebugMode)
                {
                    Main.Log.LogInfo($"设置菜单标签切换后刷新导航: {(soundTabOn ? "声音" : "设置")}");
                }
            }
            catch (System.Exception e)
            {
                if (Main.DebugMode)
                {
                    Main.Log.LogWarning($"刷新设置菜单标签导航时出错: {e.GetType().Name} - {e.Message}");
                }
            }
        }

        private void RefreshSaveLoadNavigationOnPanelChange()
        {
            if (_currentMenuType != MenuType.SaveLoad || EventSystem.current == null)
            {
                return;
            }

            try
            {
                var uiManager = Engine.GetService<IUIManager>();
                var saveLoadUI = uiManager?.GetUI<ISaveLoadUI>() as MonoBehaviour;
                if (saveLoadUI == null)
                {
                    return;
                }

                string currentMode = GetSaveLoadMode(saveLoadUI);
                EnsureSaveLoadSwitchButtonVisible(saveLoadUI, currentMode);
                if (!string.IsNullOrEmpty(_lastSaveLoadMode) && _lastSaveLoadMode == currentMode)
                {
                    return;
                }

                _lastSaveLoadMode = currentMode;
                var selectables = saveLoadUI.GetComponentsInChildren<Selectable>(true);
                SetupManualNavigation(selectables);

                var currentSelected = EventSystem.current.currentSelectedGameObject;
                var currentSelectable = currentSelected != null ? currentSelected.GetComponent<Selectable>() : null;
                if (!IsSelectableVisibleAndInteractable(currentSelectable))
                {
                    SelectFirstVisibleSelectable(selectables, currentMode == "Save" ? "UI_SaveSlot (Clone)" : "UI_LoadSlot (Clone)");
                }

                if (Main.DebugMode)
                {
                    Main.Log.LogInfo($"存档读档面板切换后刷新导航: {currentMode}");
                }
            }
            catch (System.Exception e)
            {
                if (Main.DebugMode)
                {
                    Main.Log.LogWarning($"刷新存档读档面板导航时出错: {e.GetType().Name} - {e.Message}");
                }
            }
        }

        private void EnsureSaveLoadSwitchButtonVisible(MonoBehaviour saveLoadUI, string currentMode)
        {
            if (saveLoadUI == null) return;

            string buttonName = currentMode == "Save" ? "LoadButton" : "SaveButton";
            var buttonTransform = FindChildRecursive(saveLoadUI.transform, buttonName);
            var buttonBehaviour = buttonTransform != null ? buttonTransform.GetComponent<ScriptableUIBehaviour>() : null;
            if (buttonBehaviour != null && !buttonBehaviour.Visible)
            {
                buttonBehaviour.Show();
            }

            var switchTransform = FindChildRecursive(saveLoadUI.transform, "SaveLoadSwitchPanelButton");
            var switchBehaviour = switchTransform != null ? switchTransform.GetComponent<ScriptableUIBehaviour>() : null;
            if (switchBehaviour != null && !switchBehaviour.Visible)
            {
                switchBehaviour.Show();
            }
        }

        private string GetSaveLoadMode(MonoBehaviour saveLoadUI)
        {
            try
            {
                var property = saveLoadUI.GetType().GetProperty("PresentationMode",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.FlattenHierarchy);
                object value = property?.GetValue(saveLoadUI, null);
                if (value != null)
                {
                    return value.ToString();
                }
            }
            catch
            {
                // Fallback below.
            }

            Transform savePanel = FindChildRecursive(saveLoadUI.transform, "SavePanel");
            Transform loadPanel = FindChildRecursive(saveLoadUI.transform, "LoadPanel");
            if (savePanel != null && savePanel.gameObject.activeInHierarchy) return "Save";
            if (loadPanel != null && loadPanel.gameObject.activeInHierarchy) return "Load";
            return "Unknown";
        }

        private void RefreshCurrentMenuNavigation()
        {
            try
            {
                var uiManager = Engine.GetService<IUIManager>();
                if (uiManager == null) return;

                IManagedUI ui = null;
                switch (_currentMenuType)
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
                    case MenuType.Confirmation:
                        ui = uiManager.GetUI<IConfirmationUI>();
                        break;
                }

                var uiGameObject = ui as MonoBehaviour;
                if (uiGameObject == null) return;

                var selectables = uiGameObject.GetComponentsInChildren<Selectable>(true);
                SetupManualNavigation(selectables);
                SelectFirstVisibleSelectable(selectables);
            }
            catch (System.Exception e)
            {
                if (Main.DebugMode)
                {
                    Main.Log.LogWarning($"刷新当前菜单导航时出错: {e.GetType().Name} - {e.Message}");
                }
            }
        }

        private void SelectFirstVisibleSelectable(Selectable[] selectables, string preferredName = null)
        {
            Selectable fallback = null;
            foreach (var sel in selectables)
            {
                if (!IsSelectableVisibleAndInteractable(sel))
                {
                    continue;
                }

                if (fallback == null)
                {
                    fallback = sel;
                }

                if (!string.IsNullOrEmpty(preferredName) && sel.name == preferredName)
                {
                    EventSystem.current.SetSelectedGameObject(sel.gameObject);
                    return;
                }
            }

            if (fallback != null)
            {
                EventSystem.current.SetSelectedGameObject(fallback.gameObject);
            }
        }

        private bool IsSelectableVisibleAndInteractable(Selectable selectable)
        {
            if (selectable == null || !selectable.interactable || !selectable.gameObject.activeInHierarchy)
            {
                return false;
            }

            var canvasGroups = selectable.GetComponentsInParent<CanvasGroup>(true);
            foreach (var canvasGroup in canvasGroups)
            {
                if (!canvasGroup.interactable || canvasGroup.alpha <= 0.01f)
                {
                    return false;
                }
            }

            return true;
        }

        private Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null) return null;
            if (root.name == childName) return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChildRecursive(root.GetChild(i), childName);
                if (found != null) return found;
            }

            return null;
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
                    case MenuType.Confirmation:
                        ui = uiManager.GetUI<IConfirmationUI>();
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
                        var nav = btn.navigation;
                        var canvasGroup = btn.GetComponentInParent<CanvasGroup>();
                        Main.Log.LogInfo($"    [{i}] {btn.gameObject.name} - 文本: {btnText} - 可交互: {btn.interactable}");
                        Main.Log.LogInfo($"        导航模式: {nav.mode}");
                        Main.Log.LogInfo($"        上: {(nav.selectOnUp != null ? nav.selectOnUp.name : "null")}");
                        Main.Log.LogInfo($"        下: {(nav.selectOnDown != null ? nav.selectOnDown.name : "null")}");
                        Main.Log.LogInfo($"        左: {(nav.selectOnLeft != null ? nav.selectOnLeft.name : "null")}");
                        Main.Log.LogInfo($"        右: {(nav.selectOnRight != null ? nav.selectOnRight.name : "null")}");
                        Main.Log.LogInfo($"        父CanvasGroup: {(canvasGroup != null ? $"interactable={canvasGroup.interactable}, alpha={canvasGroup.alpha}" : "null")}");
                        Main.Log.LogInfo($"        激活状态: {btn.gameObject.activeInHierarchy}");
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

            // 更新当前滑块
            var slider = selected.GetComponent<Slider>();
            if (slider != null)
            {
                _currentSlider = slider;
                _lastSliderValue = slider.value;
            }
            else
            {
                _currentSlider = null;
            }

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

            // 检查是否是Slider
            var slider = selected.GetComponent<Slider>();
            if (slider != null)
            {
                return GetSliderText(slider);
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

            // 先检查硬编码的映射（图片按钮无法动态获取文本）
            string label = toggle.name;
            if (TryGetSaveLoadPageLabel(toggle.name, out string pageLabel))
            {
                label = pageLabel;
            }
            else if (_buttonTextMap.TryGetValue(toggle.name, out string hardcodedText))
            {
                label = hardcodedText;
            }
            else
            {
                // 获取开关的文本
                var text = toggle.GetComponentInChildren<TMP_Text>();
                if (text != null && !string.IsNullOrEmpty(text.text))
                {
                    label = text.text;
                }
            }

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

            // 先检查硬编码的映射（图片按钮无法动态获取文本）
            string label = toggle.name;
            if (TryGetSaveLoadPageLabel(toggle.name, out string pageLabel))
            {
                label = pageLabel;
            }
            else if (_buttonTextMap.TryGetValue(toggle.name, out string hardcodedText))
            {
                label = hardcodedText;
            }
            else
            {
                // 获取开关的文本
                var text = toggle.GetComponentInChildren<TMP_Text>();
                if (text != null && !string.IsNullOrEmpty(text.text))
                {
                    label = text.text;
                }
            }

            // 获取开关状态
            string status = toggle.isOn ? "开启" : "关闭";

            return $"{label}，{status}";
        }

        /// <summary>
        /// 存档/读档页码 Toggle 使用 PageBtn_N 对象名，没有可读取文本。
        /// </summary>
        private bool TryGetSaveLoadPageLabel(string objectName, out string label)
        {
            label = string.Empty;

            const string prefix = "PageBtn_";
            if (string.IsNullOrEmpty(objectName) || !objectName.StartsWith(prefix, StringComparison.Ordinal))
            {
                return false;
            }

            string pageNumberText = objectName.Substring(prefix.Length);
            if (!int.TryParse(pageNumberText, out int pageNumber) || pageNumber <= 0)
            {
                return false;
            }

            label = $"第 {pageNumber} 页";
            return true;
        }

        /// <summary>
        /// 获取Slider的文本。
        /// </summary>
        private string GetSliderText(Slider slider)
        {
            if (slider == null) return string.Empty;

            // 先检查硬编码的映射
            string label = slider.name;
            if (_sliderTextMap.TryGetValue(slider.name, out string hardcodedText))
            {
                label = hardcodedText;
            }
            else if (TryGetNearbyLabelText(slider.transform, out string nearbyLabel))
            {
                label = nearbyLabel;
            }
            else
            {
                // 获取滑块的文本
                var text = slider.GetComponentInChildren<TMP_Text>();
                if (text != null && !string.IsNullOrEmpty(text.text))
                {
                    label = text.text;
                }
            }

            // 获取滑块当前值（百分比）
            float valuePercent = 0f;
            if (slider.maxValue > slider.minValue)
            {
                valuePercent = (slider.value - slider.minValue) / (slider.maxValue - slider.minValue) * 100f;
            }

            return $"{label}，{Mathf.RoundToInt(valuePercent)}%";
        }

        private bool TryGetNearbyLabelText(Transform transform, out string label)
        {
            label = string.Empty;
            if (transform == null) return false;

            Transform current = transform;
            for (int depth = 0; depth < 4 && current != null; depth++, current = current.parent)
            {
                var texts = current.GetComponentsInChildren<TMP_Text>(true);
                foreach (var text in texts)
                {
                    if (text == null || !text.gameObject.activeInHierarchy || string.IsNullOrWhiteSpace(text.text))
                    {
                        continue;
                    }

                    string normalized = NormalizeLabelText(text.text);
                    if (!string.IsNullOrEmpty(normalized) && !LooksLikeValueText(normalized))
                    {
                        label = normalized;
                        return true;
                    }
                }
            }

            return false;
        }

        private string NormalizeLabelText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            string normalized = text.Trim();
            normalized = normalized.Replace("\r", string.Empty).Replace("\n", string.Empty).Replace(" ", string.Empty);
            return normalized;
        }

        private bool LooksLikeValueText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return true;

            bool hasLetterOrDigit = false;
            foreach (char c in text)
            {
                if (char.IsLetterOrDigit(c))
                {
                    hasLetterOrDigit = true;
                    break;
                }
            }

            if (!hasLetterOrDigit) return true;
            if (text.EndsWith("%", StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>
        /// 检查滑块值是否变化，如果变化则实时朗读新值。
        /// </summary>
        private void CheckSliderValueChanged()
        {
            if (_currentSlider == null) return;

            try
            {
                float currentValue = _currentSlider.value;
                if (Mathf.Abs(currentValue - _lastSliderValue) > 0.001f)
                {
                    _lastSliderValue = currentValue;

                    // 朗读新值（只朗读百分比，不重复朗读名称）
                    float valuePercent = 0f;
                    if (_currentSlider.maxValue > _currentSlider.minValue)
                    {
                        valuePercent = (currentValue - _currentSlider.minValue) / (_currentSlider.maxValue - _currentSlider.minValue) * 100f;
                    }

                    string valueText = $"{Mathf.RoundToInt(valuePercent)}%";
                    ScreenReader.Say(valueText);

                    if (Main.DebugMode)
                    {
                        DebugLogger.Log(LogCategory.Handler, "MenuHandler", $"滑块值变化: {_currentSlider.name} -> {valueText}");
                    }
                }
            }
            catch (System.Exception e)
            {
                if (Main.DebugMode)
                {
                    Main.Log.LogWarning($"检查滑块值变化时出错: {e.Message}");
                }
            }
        }

        /// <summary>
        /// 获取按钮的文本。
        /// </summary>
        private string GetButtonText(Button button)
        {
            if (button == null) return string.Empty;

            // 先检查硬编码的映射（图片按钮无法动态获取文本）
            if (_buttonTextMap.TryGetValue(button.name, out string hardcodedText))
            {
                return hardcodedText;
            }

            // 先检查是否是LabeledButton（Naninovel自定义按钮）
            var labeledButton = button as LabeledButton;
            if (labeledButton != null)
            {
                return GetLabeledButtonText(labeledButton);
            }

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

        /// <summary>
        /// 获取LabeledButton的文本。
        /// 使用反射尝试获取Label或Text属性。
        /// </summary>
        private string GetLabeledButtonText(LabeledButton labeledButton)
        {
            if (labeledButton == null) return string.Empty;

            // 调试：确认方法被调用
            if (Main.DebugMode)
            {
                Main.Log.LogInfo($"GetLabeledButtonText 被调用: {labeledButton.name}, 类型: {labeledButton.GetType().FullName}");
            }

            try
            {
                // 尝试获取Label属性（Naninovel中常见的命名）
                var labelProperty = labeledButton.GetType().GetProperty("Label",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.IgnoreCase |
                    System.Reflection.BindingFlags.FlattenHierarchy);

                if (labelProperty != null)
                {
                    var labelValue = labelProperty.GetValue(labeledButton);
                    if (labelValue != null)
                    {
                        if (Main.DebugMode) Main.Log.LogInfo($"  找到Label属性: {labelValue}");
                        // 如果是LocalizableText类型，转换为string
                        if (labelValue is LocalizableText localizableText)
                        {
                            return localizableText.ToString();
                        }
                        return labelValue.ToString();
                    }
                }

                // 尝试获取Text属性
                var textProperty = labeledButton.GetType().GetProperty("Text",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.IgnoreCase |
                    System.Reflection.BindingFlags.FlattenHierarchy);

                if (textProperty != null)
                {
                    var textValue = textProperty.GetValue(labeledButton);
                    if (textValue != null)
                    {
                        if (Main.DebugMode) Main.Log.LogInfo($"  找到Text属性: {textValue}");
                        return textValue.ToString();
                    }
                }

                // 尝试获取LabelText字段或属性
                var labelTextProperty = labeledButton.GetType().GetProperty("LabelText",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.IgnoreCase |
                    System.Reflection.BindingFlags.FlattenHierarchy);

                if (labelTextProperty != null)
                {
                    var labelTextValue = labelTextProperty.GetValue(labeledButton);
                    if (labelTextValue != null)
                    {
                        if (Main.DebugMode) Main.Log.LogInfo($"  找到LabelText属性: {labelTextValue}");
                        return labelTextValue.ToString();
                    }
                }

                // 尝试获取字段
                var labelTextField = labeledButton.GetType().GetField("Label",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.IgnoreCase |
                    System.Reflection.BindingFlags.FlattenHierarchy);

                if (labelTextField != null)
                {
                    var labelFieldValue = labelTextField.GetValue(labeledButton);
                    if (labelFieldValue != null)
                    {
                        if (Main.DebugMode) Main.Log.LogInfo($"  找到Label字段: {labelFieldValue}");
                        return labelFieldValue.ToString();
                    }
                }

                // 调试模式：列出所有公开属性和字段，方便后续完善
                if (Main.DebugMode)
                {
                    Main.Log.LogInfo($"  --- LabeledButton完整结构 ({labeledButton.name}) ---");

                    var allProperties = labeledButton.GetType().GetProperties(
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.FlattenHierarchy);

                    Main.Log.LogInfo($"  公开属性数量: {allProperties.Length}");
                    foreach (var prop in allProperties)
                    {
                        try
                        {
                            var value = prop.GetValue(labeledButton);
                            string valueStr = value != null ? value.ToString() : "null";
                            if (valueStr.Length > 80) valueStr = valueStr.Substring(0, 80) + "...";
                            Main.Log.LogInfo($"    [Prop] {prop.Name} ({prop.PropertyType.Name}): {valueStr}");
                        }
                        catch (System.Exception e)
                        {
                            Main.Log.LogInfo($"    [Prop] {prop.Name} ({prop.PropertyType.Name}): <读取失败: {e.Message}>");
                        }
                    }

                    var allFields = labeledButton.GetType().GetFields(
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.FlattenHierarchy);

                    Main.Log.LogInfo($"  字段数量: {allFields.Length}");
                    foreach (var field in allFields)
                    {
                        try
                        {
                            var value = field.GetValue(labeledButton);
                            string valueStr = value != null ? value.ToString() : "null";
                            if (valueStr.Length > 80) valueStr = valueStr.Substring(0, 80) + "...";
                            Main.Log.LogInfo($"    [Field] {field.Name} ({field.FieldType.Name}): {valueStr}");
                        }
                        catch (System.Exception e)
                        {
                            Main.Log.LogInfo($"    [Field] {field.Name} ({field.FieldType.Name}): <读取失败: {e.Message}>");
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                if (Main.DebugMode)
                {
                    Main.Log.LogWarning($"获取LabeledButton文本时出错: {e.GetType().Name} - {e.Message}");
                    Main.Log.LogWarning($"堆栈跟踪: {e.StackTrace}");
                }
            }

            // 最后尝试获取子组件中的文本（包括inactive的子对象）
            var tmpText = labeledButton.GetComponentInChildren<TMP_Text>(true);
            if (tmpText != null && !string.IsNullOrEmpty(tmpText.text))
            {
                if (Main.DebugMode) Main.Log.LogInfo($"  找到子TMP_Text: {tmpText.text}");
                return tmpText.text;
            }

            var text = labeledButton.GetComponentInChildren<Text>(true);
            if (text != null && !string.IsNullOrEmpty(text.text))
            {
                if (Main.DebugMode) Main.Log.LogInfo($"  找到子Text: {text.text}");
                return text.text;
            }

            // 调试：列出所有子对象，看看文本在哪里
            if (Main.DebugMode)
            {
                Main.Log.LogInfo($"  --- LabeledButton子对象列表 ({labeledButton.name}) ---");
                var allChildren = labeledButton.GetComponentsInChildren<Transform>(true);
                foreach (var child in allChildren)
                {
                    var childTmpText = child.GetComponent<TMP_Text>();
                    var childText = child.GetComponent<Text>();
                    string textInfo = string.Empty;
                    if (childTmpText != null) textInfo += $" TMP_Text: '{childTmpText.text}'";
                    if (childText != null) textInfo += $" Text: '{childText.text}'";
                    Main.Log.LogInfo($"    {child.gameObject.name}{textInfo}");
                }
            }

            // 返回按钮名称
            return labeledButton.name;
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
