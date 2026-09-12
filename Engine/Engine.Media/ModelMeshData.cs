namespace Engine.Media {
    public class ModelMeshData {
        public string Name;

        public int ParentBoneIndex;

        public bool IsVisible = true;

        public List<ModelMeshPartData> MeshParts = new global::System.Collections.Generic.List<global::Engine.Media.ModelMeshPartData>() {  };

        public BoundingBox BoundingBox;
    }
}