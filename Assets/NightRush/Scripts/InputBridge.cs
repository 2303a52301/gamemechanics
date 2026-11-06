using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace NightRush
{
    /// <summary>Reads input from either the new Input System or the legacy Input Manager, whichever is enabled.</summary>
    public static class InputBridge
    {
#if ENABLE_INPUT_SYSTEM
        public static float Steer()
        {
            float v = 0f;
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.dKey.isPressed || k.rightArrowKey.isPressed) v += 1f;
                if (k.aKey.isPressed || k.leftArrowKey.isPressed) v -= 1f;
            }
            var g = Gamepad.current;
            if (g != null) v += g.leftStick.x.ReadValue();
            return Mathf.Clamp(v, -1f, 1f);
        }

        public static float Throttle()
        {
            float v = 0f;
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.wKey.isPressed || k.upArrowKey.isPressed) v += 1f;
                if (k.sKey.isPressed || k.downArrowKey.isPressed) v -= 1f;
            }
            var g = Gamepad.current;
            if (g != null) v += g.rightTrigger.ReadValue() - g.leftTrigger.ReadValue();
            return Mathf.Clamp(v, -1f, 1f);
        }

        public static bool Handbrake()
        {
            var k = Keyboard.current; var g = Gamepad.current;
            return (k != null && k.spaceKey.isPressed) || (g != null && g.buttonEast.isPressed);
        }

        public static bool Nitro()
        {
            var k = Keyboard.current; var g = Gamepad.current;
            return (k != null && (k.leftShiftKey.isPressed || k.rightShiftKey.isPressed)) || (g != null && g.buttonSouth.isPressed);
        }

        public static bool RestartPressed() { var k = Keyboard.current; return k != null && k.rKey.wasPressedThisFrame; }
        public static bool MenuPressed() { var k = Keyboard.current; return k != null && k.escapeKey.wasPressedThisFrame; }
        public static bool SubmitPressed() { var k = Keyboard.current; return k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame); }
        public static bool SpeedUpPressed() { var k = Keyboard.current; return k != null && (k.rightBracketKey.wasPressedThisFrame || k.equalsKey.wasPressedThisFrame); }
        public static bool SpeedDownPressed() { var k = Keyboard.current; return k != null && (k.leftBracketKey.wasPressedThisFrame || k.minusKey.wasPressedThisFrame); }
#else
        public static float Steer() { return Mathf.Clamp(Input.GetAxisRaw("Horizontal"), -1f, 1f); }

        public static float Throttle()
        {
            float v = 0f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v -= 1f;
            return v;
        }

        public static bool Handbrake() { return Input.GetKey(KeyCode.Space); }
        public static bool Nitro() { return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift); }
        public static bool RestartPressed() { return Input.GetKeyDown(KeyCode.R); }
        public static bool MenuPressed() { return Input.GetKeyDown(KeyCode.Escape); }
        public static bool SubmitPressed() { return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter); }
        public static bool SpeedUpPressed() { return Input.GetKeyDown(KeyCode.RightBracket) || Input.GetKeyDown(KeyCode.Equals); }
        public static bool SpeedDownPressed() { return Input.GetKeyDown(KeyCode.LeftBracket) || Input.GetKeyDown(KeyCode.Minus); }
#endif
    }
}
