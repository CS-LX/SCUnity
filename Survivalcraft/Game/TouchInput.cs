using Engine;
using TemplatesDatabase;

namespace Game {
    public struct TouchInput {
        public TouchInput() { }
        public TouchInputType InputType = default;

        public Vector2 Position = default;

        public Vector2 Move = default;

        public Vector2 TotalMove = default;

        public Vector2 TotalMoveLimited = default;

        public float Duration = default;

        public int DurationFrames = default;

        /// <summary>
        ///     模组如果需要添加或使用额外信息，可以在这个ValuesDictionary读写元素
        /// </summary>
        public ValuesDictionary ValuesDictionaryForMods = new();
    }
}
