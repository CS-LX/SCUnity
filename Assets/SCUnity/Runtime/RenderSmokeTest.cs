using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using E = Engine.Graphics;

namespace SCUnity.Runtime
{
    internal sealed class RenderSmokeTest : IDisposable
    {
        readonly E.PrimitivesRenderer2D primitives = new E.PrimitivesRenderer2D();
        E.RenderTarget2D target;
        public bool Complete { get; private set; }
        public Exception Failure { get; private set; }
        public readonly List<string> Checks = new List<string>();

        public void Draw(SurvivalcraftGame game, string output)
        {
            IndexUploads();
            PortRenderer.Active.Begin();
            target = new E.RenderTarget2D(64, 64, 1, E.ColorFormat.Rgba8888, E.DepthFormat.Depth24Stencil8);
            E.Display.RenderTarget = target;
            E.Display.Clear(Engine.Color.Black, 1, 0);
            Quad(0, 0, 32, 64, .2f, Engine.Color.Red);
            Quad(0, 0, 64, 64, .8f, Engine.Color.Blue);
            Quad(0, 0, 64, 16, .1f, Engine.Color.Green);
            E.Display.RenderTarget = null;
            E.Display.Clear(Engine.Color.Black, 1, 0);
            primitives.TexturedBatch(target, false, 0, E.DepthStencilState.None, E.RasterizerState.CullNone,
                    E.BlendState.Opaque, E.SamplerState.PointClamp)
                .QueueQuad(new Engine.Vector2(0, 0), new Engine.Vector2(128, 128), 0,
                    Engine.Vector2.Zero, Engine.Vector2.One, Engine.Color.White);
            primitives.Flush();
            Quad(160, 0, 224, 128, .2f, Engine.Color.Red);
            Quad(160, 0, 288, 128, .8f, Engine.Color.Blue);
            primitives.FlatBatch(0, E.DepthStencilState.None, E.RasterizerState.CullNone, E.BlendState.NonPremultiplied)
                .QueueQuad(new Engine.Vector2(240, 64), new Engine.Vector2(288, 128), 0, new Engine.Color(255, 0, 0, 128));
            primitives.Flush();
            game.StartCoroutine(Capture(output));
        }
        void IndexUploads()
        {
            using (var small = new E.IndexBuffer(E.IndexFormat.SixteenBits, 6))
            using (var large = new E.IndexBuffer(E.IndexFormat.ThirtyTwoBits, 6))
            {
                small.SetData(new[] { 0, 1, 2, 2, 1, 3 }, 0, 6);
                small.SetData(new uint[] { 0, 1, 2, 2, 1, 3 }, 0, 6);
                large.SetData(new ushort[] { 0, 1, 2, 2, 1, 3 }, 0, 6);
                Checks.Add("32 to 16 bit narrowing and 16 to 32 bit widening upload full buffers");
                long uploads = Engine.UnityRuntime.Host.UploadCount;
                bool rejected = false;
                try { small.SetData(new[] { 0, 65536 }, 0, 2); }
                catch (OverflowException) { rejected = true; }
                if (!rejected || Engine.UnityRuntime.Host.UploadCount != uploads)
                    throw new Exception("Invalid narrowing changed the buffer.");
                Checks.Add("Index overflow is rejected before any buffer upload");
                rejected = false;
                try { small.SetData(new int[7], 0, 7); }
                catch (ArgumentException) { rejected = true; }
                if (!rejected || Engine.UnityRuntime.Host.UploadCount != uploads)
                    throw new Exception("Out-of-range index upload changed the buffer.");
                Checks.Add("Out-of-range index count is rejected before any buffer upload");
            }
        }
        void Quad(float x1, float y1, float x2, float y2, float depth, Engine.Color color)
        {
            primitives.FlatBatch(0, E.DepthStencilState.Default, E.RasterizerState.CullNone, E.BlendState.Opaque)
                .QueueQuad(new Engine.Vector2(x1, y1), new Engine.Vector2(x2, y2), depth, color);
            primitives.Flush();
        }
        IEnumerator Capture(string output)
        {
            yield return new WaitForEndOfFrame();
            Texture2D image = null;
            try
            {
                image = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(output, "render-probe.png"), image.EncodeToPNG());
                Check(image, 32, 80, Color.red, "Near geometry occludes later far geometry in render texture");
                Check(image, 96, 80, Color.blue, "Far geometry appears outside the near surface");
                Check(image, 32, 16, Color.green, "Render texture preserves vertical orientation");
                Check(image, 192, 80, Color.red, "Backbuffer uses the bound depth attachment");
                Check(image, 256, 32, Color.blue, "Backbuffer depth clear and range are correct");
                Check(image, 256, 96, new Color(.5f, 0, .5f), "Nonpremultiplied alpha blends correctly");
            }
            catch (Exception error) { Failure = error; }
            finally { if (image != null) UnityEngine.Object.Destroy(image); Complete = true; }
        }
        void Check(Texture2D image, int x, int y, Color expected, string check)
        {
            Color actual = image.GetPixel(x, image.height - 1 - y);
            if (Mathf.Abs(actual.r - expected.r) > .03f || Mathf.Abs(actual.g - expected.g) > .03f || Mathf.Abs(actual.b - expected.b) > .03f)
                throw new InvalidOperationException(check + ": expected " + expected + ", got " + actual);
            Checks.Add(check);
        }
        public void Dispose() => target?.Dispose();
    }
}
