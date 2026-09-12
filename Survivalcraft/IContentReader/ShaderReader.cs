using Engine.Graphics;

namespace Game.IContentReader {
    public class ShaderReader : IContentReader {
        public override string Type => "Engine.Graphics.Shader";
        public override string[] DefaultSuffix => new string[] { "vsh", "psh" };

        public override object Get(ContentInfo[] contents) {
            ShaderMacro[] shaderMacros = contents[0].Filename.StartsWith("AlphaTested") ? new global::Engine.Graphics.ShaderMacro[] { new ShaderMacro("ALPHATESTED") } : new global::Engine.Graphics.ShaderMacro[] {  };
            return new Shader(
                new StreamReader(contents[0].Duplicate()).ReadToEnd(),
                new StreamReader(contents[1].Duplicate()).ReadToEnd(),
                shaderMacros
            );
        }
    }
}