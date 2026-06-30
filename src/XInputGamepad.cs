using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HuXiangLianPian.Accessibility
{
    internal sealed class XInputGamepad
    {
        [Flags]
        public enum Button : ushort
        {
            DPadUp = 0x0001,
            DPadDown = 0x0002,
            DPadLeft = 0x0004,
            DPadRight = 0x0008,
            Start = 0x0010,
            Back = 0x0020,
            LeftThumb = 0x0040,
            RightThumb = 0x0080,
            LeftShoulder = 0x0100,
            RightShoulder = 0x0200,
            A = 0x1000,
            B = 0x2000,
            X = 0x4000,
            Y = 0x8000
        }

        private const int ErrorSuccess = 0;
        private const float StickDeadzone = 0.28f;

        private bool _available;
        private bool _connected;
        private ushort _previousButtons;
        private ushort _currentButtons;
        private Vector2 _leftStick;
        private bool _loggedState;

        public bool Connected => _connected;
        public Vector2 LeftStick => _leftStick;

        public void Update()
        {
            _previousButtons = _currentButtons;
            _connected = false;
            _leftStick = Vector2.zero;

            for (uint i = 0; i < 4; i++)
            {
                if (TryGetState(i, out XInputState state))
                {
                    _available = true;
                    _connected = true;
                    _currentButtons = state.Gamepad.Buttons;
                    _leftStick = NormalizeStick(state.Gamepad.ThumbLX, state.Gamepad.ThumbLY);

                    if (Main.DebugMode && !_loggedState)
                    {
                        _loggedState = true;
                        Main.Log.LogInfo($"[INPUT] XInput controller detected: index={i}");
                    }

                    return;
                }
            }

            _currentButtons = 0;
            if (Main.DebugMode && !_loggedState && _available)
            {
                _loggedState = true;
                Main.Log.LogInfo("[INPUT] XInput available but no controller is connected");
            }
        }

        public bool GetButtonDown(Button button)
        {
            ushort mask = (ushort)button;
            return (_currentButtons & mask) != 0 && (_previousButtons & mask) == 0;
        }

        public bool GetButton(Button button)
        {
            return (_currentButtons & (ushort)button) != 0;
        }

        public Vector2 GetDigitalDirection()
        {
            float x = 0f;
            float y = 0f;

            if (GetButton(Button.DPadRight)) x = 1f;
            else if (GetButton(Button.DPadLeft)) x = -1f;

            if (GetButton(Button.DPadUp)) y = 1f;
            else if (GetButton(Button.DPadDown)) y = -1f;

            if (x == 0f && Mathf.Abs(_leftStick.x) >= StickDeadzone)
            {
                x = Math.Sign(_leftStick.x);
            }

            if (y == 0f && Mathf.Abs(_leftStick.y) >= StickDeadzone)
            {
                y = Math.Sign(_leftStick.y);
            }

            return new Vector2(x, y);
        }

        private static Vector2 NormalizeStick(short x, short y)
        {
            var value = new Vector2(
                Mathf.Clamp(x / 32767f, -1f, 1f),
                Mathf.Clamp(y / 32767f, -1f, 1f));

            if (value.magnitude < StickDeadzone)
            {
                return Vector2.zero;
            }

            return value;
        }

        private static bool TryGetState(uint index, out XInputState state)
        {
            try
            {
                return XInputGetState14(index, out state) == ErrorSuccess;
            }
            catch (DllNotFoundException)
            {
                try
                {
                    return XInputGetState910(index, out state) == ErrorSuccess;
                }
                catch
                {
                    state = default;
                    return false;
                }
            }
            catch
            {
                state = default;
                return false;
            }
        }

        [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
        private static extern int XInputGetState14(uint dwUserIndex, out XInputState pState);

        [DllImport("xinput9_1_0.dll", EntryPoint = "XInputGetState")]
        private static extern int XInputGetState910(uint dwUserIndex, out XInputState pState);

        [StructLayout(LayoutKind.Sequential)]
        private struct XInputState
        {
            public uint PacketNumber;
            public XInputGamepadState Gamepad;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XInputGamepadState
        {
            public ushort Buttons;
            public byte LeftTrigger;
            public byte RightTrigger;
            public short ThumbLX;
            public short ThumbLY;
            public short ThumbRX;
            public short ThumbRY;
        }
    }
}
