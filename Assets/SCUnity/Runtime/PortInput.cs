using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using H = Engine.UnityRuntime.Host;
using UKey = UnityEngine.InputSystem.Key;
using SKey = Silk.NET.Input.Key;

namespace SCUnity.Runtime {
    public sealed class PortInput : IDisposable {
        Keyboard keyboard;
        readonly Dictionary<UKey, SKey> map = new Dictionary<UKey, SKey>();
        readonly HashSet<UKey> down = new HashSet<UKey>();
        readonly bool[] mouseDown = new bool[5];
        bool ime;
        bool locked;

        public PortInput() {
            foreach (UKey key in Enum.GetValues(typeof(UKey))) {
                string name = key.ToString();
                if (name.StartsWith("Digit")) name = "Number" + name.Substring(5);
                if (name.StartsWith("Numpad")) name = "Keypad" + name.Substring(6);
                switch (name) {
                    case "Backquote": name = "GraveAccent"; break;
                    case "Equals": name = "Equal"; break;
                    case "LeftCtrl": name = "ControlLeft"; break;
                    case "RightCtrl": name = "ControlRight"; break;
                    case "LeftShift": name = "ShiftLeft"; break;
                    case "RightShift": name = "ShiftRight"; break;
                    case "LeftAlt": name = "AltLeft"; break;
                    case "RightAlt": name = "AltRight"; break;
                    case "LeftMeta": name = "SuperLeft"; break;
                    case "RightMeta": name = "SuperRight"; break;
                    case "UpArrow": name = "Up"; break;
                    case "DownArrow": name = "Down"; break;
                    case "LeftArrow": name = "Left"; break;
                    case "RightArrow": name = "Right"; break;
                    case "Quote": name = "Apostrophe"; break;
                    case "KeypadPlus": name = "KeypadAdd"; break;
                    case "KeypadMinus": name = "KeypadSubtract"; break;
                    case "KeypadPeriod": name = "KeypadDecimal"; break;
                }
                if (Enum.TryParse(name, out SKey target)
                    && key != UKey.None)
                    map[key] = target;
            }
            H.GetClipboard = () => GUIUtility.systemCopyBuffer;
            H.SetClipboard = value => GUIUtility.systemCopyBuffer = value;
            H.WarpMouse = p => Mouse.current?.WarpCursorPosition(new Vector2(p.X, Screen.height - p.Y));
            H.WindowHandle = Process.GetCurrentProcess().MainWindowHandle;
        }

        void Text(char c) => H.TextInput(c);

        void Composition(IMECompositionString composition) {
            string text = composition.ToString();
            H.CompositionInput(text, text.Length);
        }

        public void Sample(bool forceFocus = false) {
            bool active = forceFocus || Application.isFocused;
            H.SetFocus(active);
            if (Engine.Window.Size.X != Screen.width
                || Engine.Window.Size.Y != Screen.height)
                H.Resize(Screen.width, Screen.height);
            if (keyboard != Keyboard.current) {
                if (keyboard != null) {
                    keyboard.onTextInput -= Text;
                    keyboard.onIMECompositionChange -= Composition;
                }
                foreach (var key in down) H.KeyInput((int)map[key], false);
                keyboard = Keyboard.current;
                if (keyboard != null) {
                    keyboard.onTextInput += Text;
                    keyboard.onIMECompositionChange += Composition;
                }
                down.Clear();
            }
            if (keyboard != null) {
                foreach (var pair in map) {
                    bool pressed = active && keyboard[pair.Key].isPressed;
                    if (pressed && down.Add(pair.Key)) {
                        H.KeyInput((int)pair.Value, true);
                    }
                    else if (!pressed
                        && down.Remove(pair.Key))
                        H.KeyInput((int)pair.Value, false);
                }
                if (ime != H.ImeEnabled) {
                    ime = H.ImeEnabled;
                    keyboard.SetIMEEnabled(ime);
                }
                if (ime) keyboard.SetIMECursorPosition(new Vector2(H.ImePosition.X, H.ImePosition.Y));
            }
            var mouse = Mouse.current;
            if (mouse != null) {
                var p = mouse.position.ReadValue();
                var d = mouse.delta.ReadValue();
                var wheel = mouse.scroll.ReadValue();
                H.MouseMoveInput(
                    Mathf.RoundToInt(p.x),
                    Mathf.RoundToInt(Screen.height - p.y),
                    Mathf.RoundToInt(d.x),
                    -Mathf.RoundToInt(d.y),
                    wheel.x / 120f,
                    wheel.y / 120f
                );
                ButtonControl[] buttons = {
                    mouse.leftButton,
                    mouse.rightButton,
                    mouse.middleButton,
                    mouse.backButton,
                    mouse.forwardButton
                };
                for (int i = 0; i < buttons.Length; i++) {
                    bool pressed = active && buttons[i].isPressed;
                    if (pressed != mouseDown[i]) {
                        mouseDown[i] = pressed;
                        H.MouseInput(i, pressed);
                    }
                }
            }
            bool wantLock = !Engine.Input.Mouse.IsMouseVisible && Engine.Window.IsActive;
            if (locked != wantLock) {
                locked = wantLock;
                Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            }
            Cursor.visible = !active || Engine.Input.Mouse.IsMouseVisible;
            for (int i = 0; i < 4; i++) {
                var pad = i < Gamepad.all.Count ? Gamepad.all[i] : null;
                var left = pad == null ? Vector2.zero : pad.leftStick.ReadValue();
                var right = pad == null ? Vector2.zero : pad.rightStick.ReadValue();
                bool[] buttons = pad == null ? new bool[14] : new[] {
                    pad.buttonSouth.isPressed,
                    pad.buttonEast.isPressed,
                    pad.buttonWest.isPressed,
                    pad.buttonNorth.isPressed,
                    pad.selectButton.isPressed,
                    pad.startButton.isPressed,
                    pad.leftStickButton.isPressed,
                    pad.rightStickButton.isPressed,
                    pad.leftShoulder.isPressed,
                    pad.rightShoulder.isPressed,
                    pad.dpad.left.isPressed,
                    pad.dpad.up.isPressed,
                    pad.dpad.right.isPressed,
                    pad.dpad.down.isPressed
                };
                H.GamePadState(
                    i,
                    pad != null,
                    new Engine.Vector2(left.x, left.y),
                    new Engine.Vector2(right.x, right.y),
                    pad?.leftTrigger.ReadValue() ?? 0,
                    pad?.rightTrigger.ReadValue() ?? 0,
                    buttons
                );
            }
        }

        public void Dispose() {
            if (keyboard != null) {
                keyboard.onTextInput -= Text;
                keyboard.onIMECompositionChange -= Composition;
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            H.GetClipboard = null;
            H.SetClipboard = null;
            H.WarpMouse = null;
        }
    }
}