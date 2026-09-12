namespace Engine.Serialization {
    public class DynamicArraySerializer<T> : ISerializer<DynamicArray<T>> {
        public void Serialize(InputArchive archive, ref DynamicArray<T> value) {
            value = new global::Engine.DynamicArray<T>() {  };
            archive.SerializeCollection(null, value);
        }

        public void Serialize(OutputArchive archive, DynamicArray<T> value) {
            archive.SerializeCollection(null, null, value);
        }
    }
}