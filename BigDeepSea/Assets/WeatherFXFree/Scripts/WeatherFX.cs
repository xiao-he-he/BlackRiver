using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HeyHeyThere.WeatherFXFree
{
    /// <summary>
    /// 2D weather drawn by a shader on a quad: no textures, no particles, one component.
    /// <code>
    /// WeatherFX.Add("rain");                                   // covers the view
    /// WeatherFX.Add("fog").Set("density", 0.8f).FadeIn(3f);
    /// WeatherFX.Add("water").Place(new Vector2(-6.4f, -1.4f), new Vector2(12.8f, 2.2f));
    /// storm.Strike();                                          // lightning, now
    /// rain.Stop(2f);                                           // fades out, then destroys its object
    /// </code>
    /// With Size at zero, rain, snow, fog and the other screen-wide effects cover the main camera's view
    /// and follow it, while their patterns stay put in the world. Give a Size (in units) to place an
    /// effect as a rect with its top-left corner at the transform, extending right and down. The shader's
    /// settings are its material's: Set and Get them by name, or give the component a material of the
    /// effect's shader to start from.
    /// </summary>
    [ExecuteAlways, DefaultExecutionOrder(1000), RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class WeatherFX : MonoBehaviour
    {
        /// <summary>The shaders' folder under Resources.</summary>
        public const string Folder = "Weather FX Free";

        /// <summary>World pixels to a unit: the patterns are sized in pixels.</summary>
        public const float PixelsPerUnit = 100f;

        /// <summary>Every effect, in their default drawing order (sorting order 100 and up).</summary>
        public static readonly string[] AllEffects = { "stars", "meteors", "moon", "sun", "aurora", "clouds",
            "storm_clouds", "rainbow", "tornado", "rain", "snow", "blizzard", "hail", "fog", "mist", "cloud_shadows",
            "god_rays", "heat_haze", "water", "ripples", "waterfall", "lightning", "leaves", "ash", "embers", "fireflies",
            "pollen", "sandstorm", "wind", "bubbles", "caustics", "frost", "day_night", "rain_window" };

        public const int BaseSortingOrder = 100;

        /// <summary>
        /// Chunky pixels for every WeatherFX whose PixelSize is -1: set once, for a pixel-art game, to the
        /// size of a pixel of its art in world pixels.
        /// </summary>
        public static int DefaultPixelSize;

        /// <summary>Called once Stop's fade is over, just before the object goes.</summary>
        public event Action Finished;

        [SerializeField] string effect = "rain";
        [Tooltip("Zero uses the effect's size: the whole view for most, a rect for water and waterfall. In units.")]
        [SerializeField] Vector2 size;
        [Tooltip("Size of a pixel in world pixels, 0 smooth; -1 follows WeatherFX.DefaultPixelSize.")]
        [SerializeField, Range(-1, 16)] int pixelSize = -1;
        [Tooltip("Optional: a material of the effect's shader, its settings the ones to start from.")]
        [SerializeField] Material material;

        static readonly Dictionary<string, Shader> shaders = new Dictionary<string, Shader>();
        static readonly List<WeatherFX> all = new List<WeatherFX>();
        static readonly Dictionary<long, RenderTexture> screens = new Dictionary<long, RenderTexture>();
        static readonly int TimeId = Shader.PropertyToID("_WfxTime"), ScreenId = Shader.PropertyToID("_WfxScreen"),
            RectSizeId = Shader.PropertyToID("_RectSize"), OriginId = Shader.PropertyToID("_Origin"),
            PixelSizeId = Shader.PropertyToID("_PixelSize"), IntensityId = Shader.PropertyToID("_Intensity"),
            SeedId = Shader.PropertyToID("_Seed"), StrikeAgeId = Shader.PropertyToID("_StrikeAge"),
            StrikeSeedId = Shader.PropertyToID("_StrikeSeed");
        static int timeFrame = -1, screenFrame = -1;

        Material instance;  // the renderer's, made here from `material` or the shader's defaults
        Mesh mesh;
        MeshRenderer meshRenderer;
        Rect drawn;
        Coroutine fading;
        float strikeAge = 100f;
        int strikes;
        bool dirty = true;

        /// <summary>The effects whose shader is installed.</summary>
        public static IReadOnlyList<string> Effects => AllEffects.Where(e => ShaderOf(e)).ToArray();

        /// <summary>An effect's shader, or null if this package lacks it.</summary>
        public static Shader ShaderOf(string effect)
        {
            if (effect == null)
                return null;
            if (!shaders.TryGetValue(effect, out var s))
                shaders[effect] = s = Resources.Load<Shader>($"{Folder}/{Pascal(effect)}");
            return s;
        }

        /// <summary>
        /// Adds an effect on a new object under <paramref name="parent"/> (or the scene's root) and returns
        /// it, its seed random and its sorting order its place in AllEffects.
        /// </summary>
        public static WeatherFX Add(string effect, Transform parent = null)
        {
            if (!ShaderOf(effect))
                throw new ArgumentException($"WeatherFX: no effect {effect}, pick one of {string.Join(", ", Effects)}");
            var go = new GameObject(effect);
            go.transform.SetParent(parent, false);
            var w = go.AddComponent<WeatherFX>();
            w.effect = effect;
            w.Renderer.sortingOrder = BaseSortingOrder + Array.IndexOf(AllEffects, effect);
            w.Apply();
            w.Set(SeedId, UnityEngine.Random.value * 10f);
            return w;
        }

        public string Effect
        {
            get => effect;
            set
            {
                effect = value;
                Apply();
            }
        }

        /// <summary>In units; zero uses the effect's size (see the class).</summary>
        public Vector2 Size
        {
            get => size;
            set
            {
                size = value;
                Sync();
            }
        }

        /// <summary>Size of a pixel in world pixels, 0 smooth; -1 follows DefaultPixelSize.</summary>
        public int PixelSize
        {
            get => pixelSize;
            set
            {
                pixelSize = value;
                Sync();
            }
        }

        /// <summary>The size drawn: Size, or the effect's own if that is zero.</summary>
        public Vector2 EffectiveSize => size != Vector2.zero ? size : effect == "water" ? new Vector2(6.4f, 1.6f)
            : effect == "waterfall" ? new Vector2(0.96f, 2.56f) : Vector2.zero;

        /// <summary>True when the effect covers the view.</summary>
        public bool CoversView => EffectiveSize == Vector2.zero;

        /// <summary>The material drawn with: set its properties here, or through Set.</summary>
        public Material Material
        {
            get
            {
                if (dirty || !instance)
                    Apply();
                return instance;
            }
        }

        public MeshRenderer Renderer => meshRenderer ? meshRenderer : meshRenderer = GetComponent<MeshRenderer>();

        /// <summary>
        /// Sets a shader property: "ground_fog" in snake_case, "GroundFog", or "_GroundFog".
        /// Returns itself for chaining.
        /// </summary>
        public WeatherFX Set(string property, float value) => Set(Id(property), value);

        public WeatherFX Set(string property, Color value)
        {
            Material.SetColor(Id(property), value);
            return this;
        }

        public WeatherFX Set(string property, Vector4 value)
        {
            Material.SetVector(Id(property), value);
            return this;
        }

        WeatherFX Set(int id, float value)
        {
            Material.SetFloat(id, value);
            return this;
        }

        public float Get(string property) => Material.GetFloat(Id(property));

        public Color GetColor(string property) => Material.GetColor(Id(property));

        /// <summary>A rect with its top-left corner at <paramref name="topLeft"/> (world units) and this size in units.</summary>
        public WeatherFX Place(Vector2 topLeft, Vector2 rectSize)
        {
            transform.position = new Vector3(topLeft.x, topLeft.y, transform.position.z);
            Size = rectSize;
            return this;
        }

        /// <summary>Fades in from nothing to its current intensity over <paramref name="seconds"/>. Returns itself.</summary>
        public WeatherFX FadeIn(float seconds = 1f)
        {
            float to = Material.GetFloat(IntensityId);
            Fade(0f, to, seconds, null);
            return this;
        }

        /// <summary>Fades out over <paramref name="fade"/> seconds, then raises Finished and destroys its object.</summary>
        public void Stop(float fade = 1f)
        {
            Fade(Material.GetFloat(IntensityId), 0f, fade, () =>
            {
                Finished?.Invoke();
                Kill(gameObject);
            });
        }

        /// <summary>Lightning: strikes now, on top of the strikes that come on their own (its Frequency).</summary>
        public void Strike()
        {
            strikeAge = 0f;
            strikes++;
            Set(StrikeSeedId, strikes + UnityEngine.Random.value * 100f);
            Set(StrikeAgeId, 0f);
        }

        void Fade(float from, float to, float seconds, Action done)
        {
            if (fading != null)
                StopCoroutine(fading);
            fading = null;
            if (!Application.isPlaying || !isActiveAndEnabled || seconds <= 0f)
            {
                Set(IntensityId, to);
                done?.Invoke();
                return;
            }
            fading = StartCoroutine(Fading(from, to, seconds, done));
        }

        IEnumerator Fading(float from, float to, float seconds, Action done)
        {
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                Set(IntensityId, Mathf.Lerp(from, to, t / seconds));
                yield return null;
            }
            Set(IntensityId, to);
            fading = null;
            done?.Invoke();
        }

        /// <summary>
        /// Water and heat haze read the screen: the main camera drawn into a texture before them, without
        /// the WeatherFX at or above their sorting (layer and order), once a frame per sorting they sit at.
        /// This does it for a camera now, for one rendered by hand, fitting the effects that cover the
        /// view to it first.
        /// </summary>
        public static void RenderScreens(Camera cam)
        {
            if (!cam)
                return;
            foreach (var w in all)
                if (w.isActiveAndEnabled)
                    w.Sync(cam);
            var readers = all.Where(w => w.isActiveAndEnabled && w.ReadsScreen).ToList();
            var target = cam.targetTexture;
            int width = cam.pixelWidth, height = cam.pixelHeight;
            foreach (var key in readers.Select(w => w.SortKey).Distinct().OrderBy(k => k))
            {
                if (!screens.TryGetValue(key, out var rt) || !rt || rt.width != width || rt.height != height)
                {
                    if (rt)
                        rt.Release();
                    screens[key] = rt = new RenderTexture(width, height, 24) { name = "WeatherFX screen", hideFlags = HideFlags.DontSave };
                }
                var hidden = all.Where(w => w.isActiveAndEnabled && w.Renderer.enabled && w.SortKey >= key).ToList();
                foreach (var w in hidden)
                    w.Renderer.enabled = false;
                cam.targetTexture = rt;
                try
                {
                    cam.Render();
                }
                finally
                {
                    cam.targetTexture = target;
                    foreach (var w in hidden)
                        w.Renderer.enabled = true;
                }
                foreach (var w in readers.Where(w => w.SortKey == key))
                    w.Material.SetTexture(ScreenId, rt);
            }
        }

        bool ReadsScreen => effect == "water" || effect == "heat_haze" || effect == "rain_window";

        long SortKey => (long)SortingLayer.GetLayerValueFromID(Renderer.sortingLayerID) * 65536L + Renderer.sortingOrder;

        static int Id(string property)
        {
            if (!property.StartsWith("_"))
                property = "_" + Pascal(property);
            return Shader.PropertyToID(property);
        }

        /// <summary>"cloud_shadows" to "CloudShadows"; "Density" stays.</summary>
        static string Pascal(string name) =>
            string.Concat(name.Split('_').Where(p => p.Length > 0).Select(p => char.ToUpperInvariant(p[0]) + p.Substring(1)));

        void Reset()
        {
            Renderer.sortingOrder = BaseSortingOrder + Math.Max(0, Array.IndexOf(AllEffects, effect));
        }

        void OnEnable()
        {
            all.Add(this);
            dirty = true;
        }

        void OnDisable()
        {
            all.Remove(this);
        }

        void OnValidate()
        {
            dirty = true;  // applied on the next update: OnValidate mustn't make assets
        }

        void Update()
        {
            if (effect == "lightning" && strikeAge < 100f && Application.isPlaying)
            {
                strikeAge += Time.deltaTime;
                Set(StrikeAgeId, strikeAge);
            }
        }

        void LateUpdate()
        {
            if (dirty || !instance)
                Apply();
            if (timeFrame != Time.frameCount)
            {
                timeFrame = Time.frameCount;
                Shader.SetGlobalFloat(TimeId, (float)((Application.isPlaying ? Time.timeAsDouble : Time.realtimeSinceStartupAsDouble) % 3600.0));
            }
            Sync();
            if (ReadsScreen && screenFrame != Time.frameCount)
            {
                screenFrame = Time.frameCount;
                RenderScreens(Camera.main);
            }
        }

        /// <summary>Makes the material for the effect (keeping the current one's settings) and the quad.</summary>
        void Apply()
        {
            dirty = false;
            var shader = ShaderOf(effect);
            if (!shader)
            {
                Debug.LogError($"WeatherFX: no effect {effect}, pick one of {string.Join(", ", Effects)}", this);
                return;
            }
            if (!instance || instance.shader != shader)
            {
                var old = instance;
                instance = material ? new Material(material) { shader = shader } : new Material(shader);
                instance.name = effect;
                instance.hideFlags = HideFlags.DontSave;
                if (old)
                {
                    instance.CopyMatchingPropertiesFromMaterial(old);
                    Kill(old);
                }
            }
            var r = Renderer;
            r.sharedMaterial = instance;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            if (!mesh)
            {
                mesh = new Mesh { name = "WeatherFX", hideFlags = HideFlags.DontSave };
                mesh.MarkDynamic();
                GetComponent<MeshFilter>().sharedMesh = mesh;
                drawn = default;
            }
            Sync();
        }

        void Sync() => Sync(Camera.main);

        /// <summary>
        /// Fits the quad to the rect (for an effect that covers the view, the camera's view at the object's
        /// depth) and tells the shader where it is in world pixels, y down.
        /// </summary>
        void Sync(Camera cam)
        {
            if (!instance || !mesh)
                return;
            Vector3 topLeft, bottomRight;
            var local = new Rect(0f, -EffectiveSize.y, EffectiveSize.x, EffectiveSize.y);
            if (CoversView)
            {
                if (!cam)
                    return;
                float depth = cam.orthographic ? Vector3.Dot(transform.position - cam.transform.position, cam.transform.forward)
                    : Mathf.Max(Vector3.Dot(transform.position - cam.transform.position, cam.transform.forward), cam.nearClipPlane);
                if (cam.orthographic && depth < cam.nearClipPlane)
                    depth = cam.nearClipPlane + 0.01f;
                topLeft = cam.ViewportToWorldPoint(new Vector3(0f, 1f, depth));
                bottomRight = cam.ViewportToWorldPoint(new Vector3(1f, 0f, depth));
                Vector3 a = transform.InverseTransformPoint(topLeft), b = transform.InverseTransformPoint(bottomRight);
                local = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
            }
            else
            {
                topLeft = transform.TransformPoint(new Vector3(local.xMin, local.yMax));
                bottomRight = transform.TransformPoint(new Vector3(local.xMax, local.yMin));
            }
            var worldSize = new Vector2(Mathf.Abs(bottomRight.x - topLeft.x), Mathf.Abs(topLeft.y - bottomRight.y)) * PixelsPerUnit;
            instance.SetVector(RectSizeId, worldSize.x > 0f && worldSize.y > 0f ? (Vector4)worldSize : Vector4.one);
            instance.SetVector(OriginId, new Vector4(topLeft.x * PixelsPerUnit, -topLeft.y * PixelsPerUnit));
            instance.SetFloat(PixelSizeId, pixelSize >= 0 ? pixelSize : DefaultPixelSize);
            if (local != drawn)
            {
                drawn = local;
                mesh.SetVertices(new[] { new Vector3(local.xMin, local.yMax), new Vector3(local.xMax, local.yMax),
                    new Vector3(local.xMin, local.yMin), new Vector3(local.xMax, local.yMin) });
                mesh.SetUVs(0, new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) });
                mesh.SetTriangles(new[] { 0, 1, 2, 2, 1, 3 }, 0);
                mesh.RecalculateBounds();
            }
        }

        void OnDestroy()
        {
            Kill(instance);
            Kill(mesh);
        }

        static void Kill(UnityEngine.Object o)
        {
            if (!o)
                return;
            if (Application.isPlaying)
                Destroy(o);
            else
                DestroyImmediate(o);
        }
    }
}
