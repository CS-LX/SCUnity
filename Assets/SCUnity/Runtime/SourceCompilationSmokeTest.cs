using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Engine;
using Engine.Graphics;
using Engine.Media;
using Game;
using H = Engine.UnityRuntime.Host;

namespace SCUnity.Runtime {
    // Runs only in the opt-in acceptance Player after the real menu has loaded.
    internal static class SourceCompilationSmokeTest {
        public static string[] Run() {
            var checks = new List<string>();
            void Check(bool value, string name) {
                if (!value) throw new InvalidOperationException("Source compilation regression: " + name);
                checks.Add(name);
            }

            Check(typeof(Matrix).Assembly.GetName().Name == "Engine"
                && typeof(GameEntitySystem.Entity).Assembly.GetName().Name == "EntitySystem"
                && typeof(PlayerInput).Assembly.GetName().Name == "Survivalcraft", "assembly-identities");
            foreach (string name in new[] { "Lit.psh", "Lit.vsh", "Unlit.psh", "Unlit.vsh", "Debugfont.lst", "Debugfont.png", "icon.png" }) {
                using var stream = typeof(Matrix).Assembly.GetManifestResourceStream("Engine.Resources." + name);
                if (stream == null || stream.Length == 0) throw new Exception("Missing embedded resource: " + name);
            }
            Check(true, "embedded-engine-resources");

            var matrix = new Matrix(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Check(matrix.M11 == 1 && matrix.M12 == 2 && matrix.M21 == 5 && matrix.M41 == 13 && matrix.M44 == 16, "matrix-constructor-layout");
            var glyph = new BitmapFont.Glyph('X', Vector2.Zero, Vector2.One, new Vector2(2, 3), 7);
            Check(glyph.Code == 'X' && !glyph.IsBlank && glyph.Offset.Y == 3 && glyph.Width == 7, "glyph-constructor");
            var input = new PlayerInput();
            var copy = input;
            Check(input.ValuesDictionaryForMods != null && ReferenceEquals(input.ValuesDictionaryForMods, copy.ValuesDictionaryForMods)
                && default(PlayerInput).ValuesDictionaryForMods == null, "struct-new-default-copy-semantics");
            Check(new PlayerStats.DeathRecord(2, Vector3.One, "fixture").ValuesDictionaryForMods != null, "struct-parameterized-initializer");
            Check(Terrain.ExtractSunlightHeight(unchecked((int)0xff000000)) == 255, "unsigned-shift");

            var font = BitmapFont.DebugFont;
            var fontBatch = new FontBatch2D();
            var flat = new FlatBatch2D();
            var position = Vector2.Zero;
            new TextBoxWidget.NormalDrawItem("AB", 0, 2, fontBatch, 1, Vector2.Zero, Color.White).Draw(ref position);
            Check(position.X > 0 && fontBatch.TriangleVertices.Count == 8, "normal-text-captured-parameters");
            new TextBoxWidget.CompositionTextDrawItem("C", fontBatch, flat, 1, Vector2.Zero, Color.White).Draw(ref position);
            Check(flat.LineVertices.Count == 2 && fontBatch.TriangleVertices.Count == 12, "composition-text-captured-parameters");
            new TextBoxWidget.CaretDrawItem(flat, 2, 16, "AB", 1, font, Vector2.Zero, Color.White, Vector2.One).Draw(ref position);
            Check(flat.TriangleVertices.Count == 4, "caret-captured-parameters");
            new TextBoxWidget.SelectionDrawItem(flat, "ABC", 0, 1, font, Vector2.Zero, Color.White, 16, Vector2.One).Draw(ref position);
            Check(flat.TriangleVertices.Count == 8, "selection-captured-parameters");
            new TextBoxWidget.EndOfLineDrawItem(font, Vector2.One, 1).Draw(ref position);
            Check(position.X == 0 && position.Y > 0, "end-of-line-captured-parameters");

            using (var texture = new Texture2D(2, 1, 1, ColorFormat.Rgba8888)) {
                texture.SetData(0, new[] { new Color(11, 22, 33, 44), new Color(55, 66, 77, 88) });
                byte[] data = H.GetTextureData(texture.m_texture, out int width, out int height, out _, out _);
                Check(width == 2 && height == 1 && data.SequenceEqual(new byte[] { 11, 22, 33, 44, 55, 66, 77, 88 }), "pinned-texture-pixel-pointer");
            }
            var releases = JsonSerializer.Deserialize("[{\"id\":7,\"tag_name\":\"source-test\",\"assets\":[{\"name\":\"fixture\"}]}]",
                GiteeReleaseInfoJsonContext.Default.ListReleaseInfo);
            Check(releases.Count == 1 && releases[0].id == 7 && releases[0].tag_name == "source-test"
                && releases[0].assets[0].name == "fixture", "unity-json-source-generator");
            return checks.ToArray();
        }
    }
}
