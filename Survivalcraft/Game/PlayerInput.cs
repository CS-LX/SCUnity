using Engine;
using TemplatesDatabase;

namespace Game {
    public struct PlayerInput {
        public PlayerInput() { }
        public Vector2 Look = default;

        public Vector3 Move = default;

        public Vector3 CrouchMove = default;

        public Vector3? VrMove = default;

        public Vector2? VrLook = default;

        public Vector2 CameraLook = default;

        public Vector3 CameraMove = default;

        public Vector3 CameraCrouchMove = default;

        public bool ToggleCreativeFly = default;

        public bool ToggleCrouch = default;

        public bool ToggleMount = default;

        public bool EditItem = default;

        public bool Jump = default;

        public int ScrollInventory = default;

        public bool ToggleInventory = default;

        public bool ToggleClothing = default;

        public bool TakeScreenshot = default;

        public bool SwitchCameraMode = default;

        public bool TimeOfDay = default;

        public bool Lighting = default;

        public bool Precipitation = default;

        public bool Fog = default;

        public bool KeyboardHelp = default;

        public bool GamepadHelp = default;

        public Ray3? Dig = default;

        public Ray3? Hit = default;

        public Ray3? Aim = default;

        public Ray3? Interact = default;

        public Ray3? PickBlockType = default;

        public bool Drop = default;

        public int? SelectInventorySlot = default;

        /// <summary>
        ///     模组如果需要添加或使用额外信息，可以在这个ValuesDictionary读写元素
        /// </summary>
        public ValuesDictionary ValuesDictionaryForMods = new();
    }
}