namespace Game.IContentReader {
    public class ContentStreamReader : IContentReader {
        public override string Type => "System.IO.Stream";
        public override string[] DefaultSuffix => new string[] {  };
        public override object Get(ContentInfo[] contents) => contents[0].Duplicate();
    }
}