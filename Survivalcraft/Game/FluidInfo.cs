using Engine;
using TemplatesDatabase;

namespace Game {
    public struct FluidInfo {
        public FluidInfo() { }
        public FluidBlock Block = default;

        public float SurfaceHeight = default;

        public Vector2 FlowSpeed = default;

        /// <summary>
        ///     模组如果需要添加或使用额外信息，可以在这个ValuesDictionary读写元素
        /// </summary>
        public ValuesDictionary ValuesDictionaryForMods = new();
    }
}