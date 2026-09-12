using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using E = Engine.Graphics;
using Screen = UnityEngine.Screen;

namespace SCUnity.Runtime {
    // Only enabled by the explicit smoke-test flag. The real screen, input,
    // tree widgets and renderer run unchanged; only server replies are fixtures.
    internal sealed class CommunitySearchSmokeTest : IDisposable {
        static CommunitySearchSmokeTest active;
        readonly Harmony observer = new Harmony("scunity.community.search.smoke");
        readonly SurvivalcraftGame game;
        readonly string output;
        readonly Mouse mouse;
        readonly IEnumerator scenario;
        readonly List<E.Texture2D> icons = new List<E.Texture2D>();
        CommunityContentScreen screen;
        Action pending;
        int delay, requests;
        public readonly List<string> Checks = new List<string>();
        public bool Complete { get; private set; }

        public CommunitySearchSmokeTest(SurvivalcraftGame game, string output, Mouse mouse) {
            this.game = game;
            this.output = output;
            this.mouse = mouse;
            active = this;
            observer.Patch(AccessTools.Method(typeof(CommunityContentManager), "List"),
                prefix: new HarmonyMethod(typeof(CommunitySearchSmokeTest), nameof(List)));
            observer.Patch(AccessTools.Method(typeof(CommunityContentManager), "IsAdmin"),
                prefix: new HarmonyMethod(typeof(CommunitySearchSmokeTest), nameof(IsAdmin)));
            screen = ScreensManager.FindScreen<CommunityContentScreen>("CommunityContent");
            scenario = Run();
        }

        static bool IsAdmin(Action<bool> success) { success(false); return false; }

        static bool List(string keySearch, Action<List<CommunityContentEntry>, string> success) {
            var test = active;
            test.requests++;
            test.delay = 8;
            test.pending = () => {
                var icon = CreateIcon(Color.red);
                icon.DebugName = "Community search fixture " + keySearch;
                test.icons.Add(icon);
                success(new List<CommunityContentEntry> {
                    new CommunityContentEntry {
                        Type = ExternalContentType.World, Name = "Search result " + keySearch,
                        CollectionID = 1, CollectionName = "Search regression", CollectionDetails = "Fixture response",
                        ExtraText = "", Icon = icon
                    }
                }, "");
                // A downloaded thumbnail is shared by its collection and entry.
                foreach (var root in test.screen.m_treePanel.Nodes) {
                    root.Icon = root.Nodes[0].Icon;
                    root.Expanded = true;
                }
            };
            return false;
        }

        public void Tick() {
            if (pending != null && --delay == 0) {
                var reply = pending;
                pending = null;
                reply();
            }
            if (!Complete) Complete = !scenario.MoveNext();
        }

        IEnumerator Run() {
            CheckIconOwnership();
            ScreensManager.SwitchScreen(screen);
            while (ScreensManager.IsAnimating || pending != null) yield return null;
            for (int i = 0; i < 8; i++) yield return null;
            Require(screen.m_treePanel.Children.Count > 0, "Community thumbnails are visible before search");
            for (int round = 0; round < 3; round++) {
                screen.m_inputKey.Text = round == 1 ? "第二次搜索" : "第一次搜索";
                int before = requests;
                var point = screen.m_searchKey.WidgetToScreen(screen.m_searchKey.ActualSize / 2);
                InputSystem.QueueStateEvent(mouse, new MouseState {
                    position = new Vector2(point.X, Screen.height - point.Y), buttons = 1
                });
                yield return null;
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState {
                    position = new Vector2(point.X, Screen.height - point.Y), buttons = 0
                });
                for (int i = 0; i < 20; i++) yield return null;
                Require(requests > before && pending == null, "Search button completes request " + (round + 1));
                Require(screen.m_treePanel.Nodes.Count == 1 && screen.m_treePanel.Nodes[0].Nodes.Count == 1,
                    "Search results remain intact after replacement " + (round + 1));
                Require(screen.m_itemsCache.Values.SelectMany(x => x).OfType<TreeViewNode>()
                    .All(x => x.Nodes.Count == 1 && x.Icon != null && !x.Icon.m_isDisposed && x.Icon.m_texture != 0),
                    "Search cache contains no disposed thumbnails " + (round + 1));
            }
            int cachedRequests = requests;
            screen.PopulateList(null);
            for (int i = 0; i < 8; i++) yield return null;
            Require(requests == cachedRequests && screen.m_treePanel.Nodes[0].Nodes.Count == 1,
                "Repeating the current query reuses a live cached result");
            screen.PopulateList(null, true);
            for (int i = 0; i < 20; i++) yield return null;
            Require(requests == cachedRequests + 1 && screen.m_treePanel.Nodes[0].Icon.m_texture != 0,
                "Forced refresh replaces thumbnails safely");
            Engine.UnityRuntime.Host.SetFocus(false);
            Engine.UnityRuntime.Host.SetFocus(true);
            ScreensManager.SwitchScreen("MainMenu");
            while (ScreensManager.IsAnimating) yield return null;
            ScreensManager.SwitchScreen(screen);
            while (ScreensManager.IsAnimating || pending != null) yield return null;
            for (int i = 0; i < 8; i++) yield return null;
            Require(game.Errors.Count == 0 && game.ExecutedDraws > 0,
                "Community screen survives focus changes and re-entry");
            game.StartCoroutine(Capture());
            while (!captured) yield return null;
            if (captureFailure != null) throw captureFailure;
            ScreensManager.SwitchScreen("MainMenu");
            while (ScreensManager.IsAnimating) yield return null;
        }

        bool captured;
        Exception captureFailure;
        IEnumerator Capture() {
            yield return new WaitForEndOfFrame();
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            try {
                File.WriteAllBytes(Path.Combine(output, "community-search.png"), image.EncodeToPNG());
                Require(image.GetPixels32().Count(p => p.r > 230 && p.g < 25 && p.b < 25) > 50,
                    "Reloaded community thumbnails are red on the Unity GPU");
            }
            catch (Exception error) { captureFailure = error; }
            finally {
                UnityEngine.Object.Destroy(image);
                captured = true;
            }
        }

        void CheckIconOwnership() {
            var shared = ContentManager.Get<Subtexture>("Textures/Atlas/WorldIcon").Texture;
            using (var node = new TreeViewNode("shared", "", shared)) { }
            Require(!shared.m_isDisposed && shared.m_texture != 0, "Tree disposal preserves content-owned textures");
            using (var first = CreateIcon(Color.red))
            using (var second = CreateIcon(Color.blue)) {
                var child = new TreeViewNode("entry", "", first);
                var previous = child.Subtexture;
                child.Icon = second;
                Require(child.Subtexture.Texture == second && child.Subtexture != previous,
                    "Replacing an icon invalidates its old subtexture");
                var root = new TreeViewNode("collection", "", second);
                root.AddChild(child);
                root.Dispose();
                root.Dispose();
                Require(second.m_isDisposed && root.Icon == null && child.Icon == null,
                    "Shared collection thumbnails are released and disposal is repeatable");
            }
        }

        static E.Texture2D CreateIcon(Color color) {
            var bitmap = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            try {
                bitmap.SetPixels(Enumerable.Repeat(color, 256).ToArray());
                bitmap.Apply();
                // Exercise the same image decoder/upload path as downloaded PNGs.
                using (var png = new MemoryStream(bitmap.EncodeToPNG()))
                    return (E.Texture2D)AccessTools.Method(typeof(E.Texture2D), "Load",
                        new[] { typeof(Stream), typeof(bool), typeof(int) }).Invoke(null, new object[] { png, false, 1 });
            }
            finally { UnityEngine.Object.Destroy(bitmap); }
        }

        void Require(bool condition, string check) {
            if (!condition) throw new InvalidOperationException(check);
            Checks.Add(check);
        }

        public void Dispose() {
            observer.UnpatchSelf();
            pending = null;
            foreach (var icon in icons) if (!icon.m_isDisposed) icon.Dispose();
            active = null;
        }
    }
}
