namespace Engine.Serialization {
    public class DictionarySerializer<K, V> : ISerializer<Dictionary<K, V>> {
        public void Serialize(InputArchive archive, ref Dictionary<K, V> value) {
            value = new global::System.Collections.Generic.Dictionary<K, V>() {  };
            archive.SerializeDictionary(null, value);
        }

        public void Serialize(OutputArchive archive, Dictionary<K, V> value) {
            archive.SerializeDictionary(null, value);
        }
    }
}