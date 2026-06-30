using System;
using Naninovel;
using Naninovel.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HuXiangLianPian.Accessibility
{
    internal sealed class GamepadHandler
    {
        private const float AxisThreshold = 0.55f;
        private const float NavigateRepeatDelay = 0.22f;
        private const float SliderStep = 0.05f;

        private readonly Action _quickSave;
        private readonly Action _quickLoad;
        private readonly Action _openSaveMenu;
        private readonly Action _openLoadMenu;
        private readonly XInputGamepad _xinput = new XInputGamepad();

        private bool _shortcutMode;
        private float _lastNavigateTime;
        private float _lastSubmitCancelTime;
        private bool _loggedAnyButton;
        private bool _loggedJoystickNames;
        private bool _loggedLeftAxis;
        private bool _loggedRightAxis;

        private readonly string[] _rightStickXAxes = { "4th axis", "5th axis", "RightStickX", "Joystick Right X" };
        private readonly string[] _rightStickYAxes = { "5th axis", "4th axis", "RightStickY", "Joystick Right Y" };

        public GamepadHandler(Action quickSave, Action quickLoad, Action openSaveMenu, Action openLoadMenu)
        {
            _quickSave = quickSave;
            _quickLoad = quickLoad;
            _openSaveMenu = openSaveMenu;
            _openLoadMenu = openLoadMenu;
        }

        public void Update()
        {
            _xinput.Update();
            LogDetectedInput();

            if (IsShortcutButtonDown())
            {
                ToggleShortcutMode();
                return;
            }

            if (_shortcutMode)
            {
                ProcessShortcutMode();
                return;
            }

            if (ProcessBacklogShortcut()) return;
            ProcessSubmitCancelFallback();
            ProcessFallbackNavigation();
        }

        private void ToggleShortcutMode()
        {
            _shortcutMode = !_shortcutMode;
            if (_shortcutMode)
            {
                const string message = "手柄快捷：上快存 下快读 左存档 右读档，B取消";
                VisualHintOverlay.Show(message, 8f);
                ScreenReader.Say("手柄快捷模式");
                DebugLogger.LogInput("R3", "打开手柄快捷模式");
            }
            else
            {
                VisualHintOverlay.Hide();
                ScreenReader.Say("已关闭手柄快捷模式");
                DebugLogger.LogInput("R3", "关闭手柄快捷模式");
            }
        }

        private void ProcessShortcutMode()
        {
            if (IsCancelButtonDown() || Input.GetKeyDown(KeyCode.Escape))
            {
                _shortcutMode = false;
                VisualHintOverlay.Hide();
                ScreenReader.Say("已取消");
                return;
            }

            Vector2 direction = ReadGamepadDirection();
            if (direction == Vector2.zero || !CanRepeatNavigate()) return;

            _shortcutMode = false;
            VisualHintOverlay.Hide();

            if (Mathf.Abs(direction.y) >= Mathf.Abs(direction.x))
            {
                if (direction.y > 0f)
                {
                    DebugLogger.LogInput("Gamepad Up", "快速存档");
                    _quickSave?.Invoke();
                }
                else
                {
                    DebugLogger.LogInput("Gamepad Down", "快速读档");
                    _quickLoad?.Invoke();
                }
            }
            else if (direction.x < 0f)
            {
                DebugLogger.LogInput("Gamepad Left", "打开存档菜单");
                _openSaveMenu?.Invoke();
            }
            else
            {
                DebugLogger.LogInput("Gamepad Right", "打开读档菜单");
                _openLoadMenu?.Invoke();
            }
        }

        private void ProcessFallbackNavigation()
        {
            if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null) return;

            Vector2 direction = ReadGamepadDirection();
            if (direction == Vector2.zero || !CanRepeatNavigate()) return;

            var selected = EventSystem.current.currentSelectedGameObject;
            var selectable = selected.GetComponent<Selectable>();
            if (selectable == null) return;

            if (Mathf.Abs(direction.y) >= Mathf.Abs(direction.x))
            {
                Selectable next = direction.y > 0f
                    ? selectable.navigation.selectOnUp
                    : selectable.navigation.selectOnDown;
                if (next != null && next.IsInteractable() && next.gameObject.activeInHierarchy)
                {
                    EventSystem.current.SetSelectedGameObject(next.gameObject);
                }
                return;
            }

            var slider = selected.GetComponent<Slider>();
            if (slider == null) return;

            float range = slider.maxValue - slider.minValue;
            if (range <= 0f) return;

            float delta = range * SliderStep * (direction.x > 0f ? 1f : -1f);
            slider.value = Mathf.Clamp(slider.value + delta, slider.minValue, slider.maxValue);
        }

        private Vector2 ReadGamepadDirection()
        {
            Vector2 xinputDirection = _xinput.GetDigitalDirection();
            if (xinputDirection != Vector2.zero)
            {
                return xinputDirection;
            }

            float x = SafeGetAxis("Horizontal");
            float y = SafeGetAxis("Vertical");

            if (Mathf.Abs(x) < AxisThreshold) x = 0f;
            if (Mathf.Abs(y) < AxisThreshold) y = 0f;

            return new Vector2(Math.Sign(x), Math.Sign(y));
        }

        private bool CanRepeatNavigate()
        {
            if (Time.unscaledTime - _lastNavigateTime < NavigateRepeatDelay) return false;
            _lastNavigateTime = Time.unscaledTime;
            return true;
        }

        private bool IsShortcutButtonDown()
        {
            return _xinput.GetButtonDown(XInputGamepad.Button.Back)
                || Input.GetKeyDown(KeyCode.JoystickButton6);
        }

        private bool IsBacklogButtonDown()
        {
            return _xinput.GetButtonDown(XInputGamepad.Button.Y)
                || Input.GetKeyDown(KeyCode.JoystickButton5);
        }

        private bool IsSubmitButtonDown()
        {
            return _xinput.GetButtonDown(XInputGamepad.Button.A)
                || Input.GetKeyDown(KeyCode.JoystickButton0);
        }

        private bool IsCancelButtonDown()
        {
            return _xinput.GetButtonDown(XInputGamepad.Button.B)
                || Input.GetKeyDown(KeyCode.JoystickButton1);
        }

        private bool ProcessBacklogShortcut()
        {
            if (!IsBacklogButtonDown()) return false;

            try
            {
                var uiManager = Engine.GetService<IUIManager>();
                var backlogUI = uiManager?.GetUI<IBacklogUI>();
                if (backlogUI == null)
                {
                    ScreenReader.Say("历史记录不可用");
                    DebugLogger.LogInput("Gamepad Y", "历史记录不可用");
                    return true;
                }

                if (backlogUI.Visible)
                {
                    backlogUI.Hide();
                    DebugLogger.LogInput("Gamepad Y", "关闭历史记录");
                }
                else
                {
                    backlogUI.Show();
                    ScreenReader.Say("历史记录");
                    DebugLogger.LogInput("Gamepad Y", "打开历史记录");
                }

                return true;
            }
            catch (Exception e)
            {
                Main.Log.LogWarning($"处理手柄历史记录快捷键时出错: {e.GetType().Name} - {e.Message}");
                ScreenReader.Say("打开历史记录失败");
                return true;
            }
        }

        private void ProcessSubmitCancelFallback()
        {
            if (Time.unscaledTime - _lastSubmitCancelTime < 0.12f) return;

            if (IsSubmitButtonDown())
            {
                _lastSubmitCancelTime = Time.unscaledTime;
                SubmitCurrentSelection();
                return;
            }

            if (IsCancelButtonDown())
            {
                _lastSubmitCancelTime = Time.unscaledTime;
                ActivateNamedButton("CancelButton", "NoButton", "ReturnButton", "CloseButton");
            }
        }

        private void SubmitCurrentSelection()
        {
            var eventSystem = EventSystem.current;
            var selected = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
            if (selected == null) return;

            ExecuteEvents.Execute(selected, new BaseEventData(eventSystem), ExecuteEvents.submitHandler);
            var button = selected.GetComponent<Button>();
            if (button != null && button.IsInteractable())
            {
                button.onClick.Invoke();
                DebugLogger.LogInput("Gamepad A", $"点击 {selected.name}");
                return;
            }

            var toggle = selected.GetComponent<Toggle>();
            if (toggle != null && toggle.IsInteractable())
            {
                toggle.isOn = !toggle.isOn;
                DebugLogger.LogInput("Gamepad A", $"切换 {selected.name}");
            }
        }

        private bool ActivateNamedButton(params string[] names)
        {
            if (EventSystem.current == null) return false;

            foreach (var name in names)
            {
                var button = FindVisibleButton(name);
                if (button == null) continue;

                button.onClick.Invoke();
                DebugLogger.LogInput("Gamepad B", $"点击 {button.name}");
                return true;
            }

            return false;
        }

        private Button FindVisibleButton(string name)
        {
            var buttons = Resources.FindObjectsOfTypeAll<Button>();
            foreach (var button in buttons)
            {
                if (button == null || button.name != name || !button.IsInteractable() || !button.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var canvasGroups = button.GetComponentsInParent<CanvasGroup>(true);
                bool blocked = false;
                foreach (var canvasGroup in canvasGroups)
                {
                    if (!canvasGroup.interactable || canvasGroup.alpha <= 0.01f)
                    {
                        blocked = true;
                        break;
                    }
                }

                if (!blocked) return button;
            }

            return null;
        }

        private void LogDetectedInput()
        {
            if (!Main.DebugMode) return;

            if (!_loggedJoystickNames)
            {
                _loggedJoystickNames = true;
                string[] names = Input.GetJoystickNames();
                Main.Log.LogInfo($"[INPUT] Unity joysticks: {(names == null || names.Length == 0 ? "none" : string.Join(", ", names))}; XInput connected: {_xinput.Connected}");
            }

            if (!_loggedAnyButton)
            {
                for (int i = 0; i <= 19; i++)
                {
                    var key = KeyCode.JoystickButton0 + i;
                    if (Input.GetKeyDown(key) || Input.GetKey(key))
                    {
                        _loggedAnyButton = true;
                        Main.Log.LogInfo($"[INPUT] Gamepad button detected: {key}");
                        break;
                    }
                }
            }

            if (!_loggedLeftAxis)
            {
                float horizontal = SafeGetAxis("Horizontal");
                float vertical = SafeGetAxis("Vertical");
                if (Mathf.Abs(horizontal) >= AxisThreshold || Mathf.Abs(vertical) >= AxisThreshold)
                {
                    _loggedLeftAxis = true;
                    Main.Log.LogInfo($"[INPUT] Gamepad left/navigation axis detected: Horizontal={horizontal:0.00}, Vertical={vertical:0.00}");
                }
            }

            if (!_loggedRightAxis)
            {
                foreach (var axis in _rightStickXAxes)
                {
                    float value = SafeGetAxis(axis);
                    if (Mathf.Abs(value) >= AxisThreshold)
                    {
                        _loggedRightAxis = true;
                        Main.Log.LogInfo($"[INPUT] Possible right stick axis detected: {axis}={value:0.00}");
                        break;
                    }
                }

                if (!_loggedRightAxis)
                {
                    foreach (var axis in _rightStickYAxes)
                    {
                        float value = SafeGetAxis(axis);
                        if (Mathf.Abs(value) >= AxisThreshold)
                        {
                            _loggedRightAxis = true;
                            Main.Log.LogInfo($"[INPUT] Possible right stick axis detected: {axis}={value:0.00}");
                            break;
                        }
                    }
                }
            }
        }

        private float SafeGetAxis(string axisName)
        {
            try
            {
                return Input.GetAxis(axisName);
            }
            catch
            {
                return 0f;
            }
        }
    }
}
