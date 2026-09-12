namespace Game {
    public class TerrainGeometrySubset : IDisposable {
        public TerrainGeometryDynamicArray<TerrainVertex> Vertices = new global::Game.TerrainGeometryDynamicArray<global::Game.TerrainVertex>() {  };

        public TerrainGeometryDynamicArray<int> Indices = new global::Game.TerrainGeometryDynamicArray<int>() {  };

        public object m_tag;

        public object Tag {
            get => m_tag;
            set => m_tag = value;
        }

        public TerrainGeometrySubset() { }

        public TerrainGeometrySubset(TerrainGeometryDynamicArray<TerrainVertex> vertices, TerrainGeometryDynamicArray<int> indices) {
            Vertices = vertices;
            Indices = indices;
        }

        public void Dispose() {
            Vertices.Clear();
            Indices.Clear();
        }
    }
}