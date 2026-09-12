using Engine;
using TemplatesDatabase;

namespace Game {
    public struct BodyRaycastResult {
        public BodyRaycastResult() { }
        public Ray3 Ray = default;

        public ComponentBody ComponentBody = default;

        public float Distance = default;

        /// <summary>
        ///     模组如果需要添加或使用额外信息，可以在这个ValuesDictionary读写元素
        /// </summary>
        public ValuesDictionary ValuesDictionaryForMods = new();

        public Vector3 HitPoint() => Ray.Sample(Distance);
    }
}