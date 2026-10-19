using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HeyHeyThere.WeatherFXFree
{
    /// <summary>
    /// The demo: turn effects on and off over a pixel-art landscape; they stack. Step through the
    /// weather states for a WeatherSystem to cross-fade to. Click to strike lightning. The landscapes are in DemoArt/.
    /// </summary>
    public class WeatherDemo : MonoBehaviour
    {
        public const float W = 1280f, H = 720f;  // world pixels on screen: the camera's view, as the demo's window
        const int Zoom = 4;
        const float Ground = 0.66f, Lake = 0.69f;
        static readonly string[] Start = { "water", "fog", "god_rays", "snow" };  // the first three installed are on
        static readonly string[] Reshape = { "waterfall", "sun", "moon" };  // the landscape changes under them

        /// <summary>A landscape per mood, "&lt;mood&gt;-cliff" with the waterfall's cliff, and either with "-bare" on
        /// for a mood without its own sun or moon: W/Zoom x H/Zoom pixels in the top-left corner of the texture.</summary>
        public Texture2D[] lands;
        public Shader landShader;
        public string mood = "dusk";
        public Canvas canvas;

        readonly Dictionary<string, WeatherFX> active = new Dictionary<string, WeatherFX>();
        readonly Dictionary<string, Toggle> toggles = new Dictionary<string, Toggle>();
        string[] moods;
        Material land;
        Text moodLabel, weatherLabel;
        WeatherSystem system;
        Font font;

        bool HasWater => WeatherFX.Effects.Contains("water");

        void Awake()
        {
            if (!canvas)
                Build();
        }

        public void Build()
        {
            DemoInput.Ensure();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            moods = lands.Select(Mood).Where(n => !n.Contains("-")).ToArray();
            // if (moods == null) return;
            system = new GameObject("Weather").AddComponent<WeatherSystem>();
            system.transform.SetParent(transform, false);
            system.Ground = Ground;
             system.WeatherChanged += _ => ShowLand();
             MakeLand();
            canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.transform.SetParent(transform, false);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;  // above the weather, which a camera's canvas would be under
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(W, H);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var catcher = Add<Image>("Clicks", 0f, 0f, W, H, canvas.transform);
            catcher.color = Color.clear;
            var r = catcher.rectTransform;
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
            catcher.gameObject.AddComponent<Clicks>().demo = this;

            var effects = WeatherFX.Effects;
            const float row = 26f, column = 176f;
            int columns = effects.Count > 10 ? 2 : 1, rows = (effects.Count + columns - 1) / columns;
            float width = 22f + column * columns;
            var panel = Add<Image>("Panel", 16f, 16f, width, 16f + row * (rows + 3), canvas.transform);
            panel.color = new Color(0.1f, 0.1f, 0.13f, 0.82f);
            var p = panel.transform;
            MakeButton("<", 8f, 8f, 28f, p).onClick.AddListener(() => Step(-1));
            moodLabel = Label("Mood", mood, 16, 40f, 8f, width - 80f, 24f, Color.white, p);
            moodLabel.alignment = TextAnchor.MiddleCenter;
            MakeButton(">", width - 36f, 8f, 28f, p).onClick.AddListener(() => Step(1));
            MakeButton("<", 8f, 8f + row, 28f, p).onClick.AddListener(() => StepWeather(-1));
            weatherLabel = Label("Weather", "weather: " + system.Weather, 16, 40f, 8f + row, width - 80f, 24f, Color.white, p);
            weatherLabel.alignment = TextAnchor.MiddleCenter;
            MakeButton(">", width - 36f, 8f + row, 28f, p).onClick.AddListener(() => StepWeather(1));
            for (int i = 0; i < effects.Count; i++)
            {
                string name = effects[i];
                var t = MakeToggle(name, 12f + column * (i / rows), 8f + row * (i % rows + 2), p);
                t.onValueChanged.AddListener(on => Switch(name, on));
                toggles[name] = t;
            }
            MakeToggle("pixel art", 12f, 8f + row * (rows + 2), p).onValueChanged.AddListener(on => WeatherFX.DefaultPixelSize = on ? Zoom : 0);
            if (effects.Contains("lightning"))
            {
                var hint = Label("Hint", "Click to strike lightning.", 18, 48f + width, 16f, 400f, 28f, Color.white, canvas.transform);
                hint.gameObject.AddComponent<Outline>().effectColor = new Color(0.06f, 0.05f, 0.12f);
            }
            foreach (var name in Start.Where(toggles.ContainsKey).Take(3))
                toggles[name].isOn = true;
        }

        /// <summary>The landscape as a quad filling the camera's view: 1280x720 world pixels at the origin.</summary>
        void MakeLand()
        {
            var go = new GameObject("Landscape", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(0f, 0f, 1f);
            var mesh = new Mesh { name = "Landscape" };
            float w = W / WeatherFX.PixelsPerUnit / 2f, h = H / WeatherFX.PixelsPerUnit / 2f;
            mesh.SetVertices(new[] { new Vector3(-w, -h), new Vector3(w, -h), new Vector3(-w, h), new Vector3(w, h) });
            mesh.SetUVs(0, new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) });
            mesh.SetTriangles(new[] { 0, 2, 1, 1, 2, 3 }, 0);
            mesh.RecalculateBounds();
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            land = new Material(landShader) { name = "Landscape" };
            go.GetComponent<MeshRenderer>().sharedMaterial = land;
            ShowLand();
        }

        /// <summary>A landscape's name (day, day-cliff, day-cliff-bare).</summary>
        static string Mood(Texture t) => t.name.Substring("".Length).Replace('_', '-');

        void ShowLand()
        {
            string cliff = mood + (active.ContainsKey("waterfall") ? "-cliff" : "");
            bool body = active.ContainsKey("sun") || active.ContainsKey("moon") || (system && system.GetEffect("sun"));
            string bare = cliff + (body ? "-bare" : "");  // one sun or moon in the sky
            var tex = lands.FirstOrDefault(t => Mood(t) == bare) ?? lands.FirstOrDefault(t => Mood(t) == cliff) ?? lands.First(t => Mood(t) == mood);
            land.mainTexture = tex;
            land.mainTextureScale = new Vector2(W / Zoom / tex.width, H / Zoom / tex.height);
            land.mainTextureOffset = new Vector2(0f, 1f - H / Zoom / tex.height);
            if (moodLabel)
                moodLabel.text = mood;
        }

        void Step(int by)
        {
            mood = moods[(Array.IndexOf(moods, mood) + by + moods.Length) % moods.Length];
            ShowLand();
        }

        void StepWeather(int by)
        {
            var states = WeatherSystem.Offered;
            int i = 0;
            for (int k = 0; k < states.Count; k++)
                if (states[k] == system.Weather)
                    i = k;
            system.Change(states[(i + by + states.Count) % states.Count], 2f);
            weatherLabel.text = "weather: " + system.Weather;
        }

        /// <summary>A pixel of the demo's window (y down from its top-left) in the world.</summary>
        static Vector2 World(float x, float y) => new Vector2((x - W / 2f) / WeatherFX.PixelsPerUnit, (H / 2f - y) / WeatherFX.PixelsPerUnit);

        static Vector2 Units(float w, float h) => new Vector2(w, h) / WeatherFX.PixelsPerUnit;

        /// <summary>Where the lake starts, in window pixels.</summary>
        float LakeY => Mathf.Floor(H / Zoom * (HasWater ? Lake : 0f)) * Zoom;

        public void Switch(string name, bool on)
        {
            if (!on)
            {
                if (active.TryGetValue(name, out var w))
                {
                    w.Stop(0.4f);
                    active.Remove(name);
                }
                if (Reshape.Contains(name))
                    ShowLand();
                return;
            }
            if (active.ContainsKey(name))
                return;
            var fx = WeatherFX.Add(name, transform);
            switch (name)
            {
                case "water":
                case "ripples":
                    fx.Place(World(0f, LakeY), Units(W, H - LakeY));
                    break;
                case "waterfall":
                    float top = Mathf.Floor(H / Zoom * 0.2f) * Zoom - 4f;
                    fx.Place(World(W / 2f - 48f, top), Units(96f, LakeY - top + 24f));
                    break;
                case "lightning":
                case "tornado":
                    fx.Set("ground", Ground);
                    break;
                case "rain":
                case "hail":
                    fx.Set("ground", HasWater ? 1f : Ground + 0.01f);
                    break;
                case "heat_haze":  // above the rest, to see them all
                    fx.Renderer.sortingOrder = WeatherFX.BaseSortingOrder + WeatherFX.AllEffects.Length;
                    break;
                case "rain_window":  // and the glass above the haze
                    fx.Renderer.sortingOrder = WeatherFX.BaseSortingOrder + WeatherFX.AllEffects.Length + 1;
                    break;
            }
            active[name] = fx.FadeIn(0.4f);
            if (Reshape.Contains(name))
                ShowLand();
        }

        public void Strike()
        {
            if (system.GetEffect("lightning"))
            {
                system.GetEffect("lightning").Strike();
                return;
            }
            if (!toggles.TryGetValue("lightning", out var t))
                return;
            t.isOn = true;
            active["lightning"].Strike();
        }

        class Clicks : MonoBehaviour, IPointerClickHandler
        {
            public WeatherDemo demo;

            public void OnPointerClick(PointerEventData e)
            {
                if (e.button == PointerEventData.InputButton.Left)
                    demo.Strike();
            }
        }

        Toggle MakeToggle(string text, float x, float y, Transform parent)
        {
            var t = Add<Toggle>(text, x, y, 170f, 24f, parent);
            var box = Add<Image>("Box", 0f, 4f, 16f, 16f, t.transform);
            box.color = new Color(0.24f, 0.24f, 0.3f);
            var check = Add<Image>("Check", 3f, 3f, 10f, 10f, box.transform);
            check.color = new Color(0.55f, 0.85f, 1f);
            t.targetGraphic = box;
            t.graphic = check;
            check.canvasRenderer.SetAlpha(0f);  // off: the toggle only shows it on a change
            Label("Label", text, 16, 24f, 0f, 146f, 24f, new Color(0.9f, 0.9f, 0.94f), t.transform).alignment = TextAnchor.MiddleLeft;
            return t;
        }

        Button MakeButton(string text, float x, float y, float w, Transform parent)
        {
            var image = Add<Image>(text, x, y, w, 24f, parent);
            image.color = new Color(0.24f, 0.24f, 0.3f);
            var b = image.gameObject.AddComponent<Button>();
            b.targetGraphic = image;
            Label("Label", text, 16, 0f, 0f, w, 24f, Color.white, image.transform).alignment = TextAnchor.MiddleCenter;
            return b;
        }

        static T Add<T>(string name, float x, float y, float w, float h, Transform parent) where T : Component
        {
            var go = new GameObject(name, typeof(RectTransform));
            var r = (RectTransform)go.transform;
            r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 1f);  // top left, y down
            r.anchoredPosition = new Vector2(x, -y);
            r.sizeDelta = new Vector2(w, h);
            return go.AddComponent<T>();
        }

        Text Label(string name, string text, int size, float x, float y, float w, float h, Color color, Transform parent)
        {
            var t = Add<Text>(name, x, y, w, h, parent);
            t.font = font;
            t.fontSize = size;
            t.color = color;
            t.text = text;
            t.raycastTarget = false;
            return t;
        }
    }
}
