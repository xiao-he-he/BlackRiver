using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HeyHeyThere.WeatherFXFree
{
    /// <summary>
    /// Weather as states: one call changes it, the effects that go fading out as the new ones fade in.
    /// <code>
    /// weather.Change("storm", 6f);                       // over 6 seconds
    /// weather.Weather = "snow";                          // over Fade
    /// weather.forecast.Add(new WeatherSystem.Chance { state = "rain", weight = 1f });  // changes on its own
    /// weather.GetEffect("lightning").Strike();
    /// </code>
    /// A state is WeatherFX effects with their settings (States), added under this object in play. An
    /// effect the next state has too, with the same settings, stays as it is.
    /// </summary>
    public class WeatherSystem : MonoBehaviour
    {
        /// <summary>An effect of a state and its settings, by their snake_case names.</summary>
        public readonly struct StateEffect
        {
            public readonly string Effect;
            public readonly (string name, object value)[] Settings;

            public StateEffect(string effect, params (string, object)[] settings)
            {
                Effect = effect;
                Settings = settings;
            }

            internal string Key => Effect + string.Concat(Settings.Select(s => $";{s.name}={s.value}"));
        }

        /// <summary>A state the forecast picks, by weight.</summary>
        [Serializable]
        public struct Chance
        {
            public string state;
            public float weight;
        }

        /// <summary>The states, in order.</summary>
        public static readonly string[] AllStates = { "clear", "cloudy", "drizzle", "rain", "storm", "hail", "snow",
            "blizzard", "fog", "windy", "sandstorm", "heatwave", "ashfall" };

        /// <summary>
        /// Each state's effects. Its first is its lead: the state is offered (Offered) when that one is
        /// installed, and brings whichever of the others are.
        /// </summary>
        public static readonly Dictionary<string, StateEffect[]> States = new Dictionary<string, StateEffect[]>
        {
            ["clear"] = new StateEffect[0],
            ["cloudy"] = new[] { E("clouds", ("coverage", 0.55f)), E("cloud_shadows", ("coverage", 0.35f)) },
            ["drizzle"] = new[]
            {
                E("rain", ("density", 0.3f), ("drop_length", 14f), ("fall_speed", 0.8f), ("splashes", 0.4f)),
                E("clouds", ("coverage", 0.8f), ("color", new Color(0.85f, 0.87f, 0.9f)), ("shade", new Color(0.56f, 0.6f, 0.68f))),
                E("mist", ("density", 0.35f)),
            },
            ["rain"] = new[] { E("rain", ("density", 0.55f)), E("storm_clouds", ("cover", 0.3f), ("flashes", 0f)) },
            ["storm"] = new[]
            {
                E("storm_clouds", ("cover", 0.4f), ("flashes", 0.5f)),
                E("rain", ("density", 0.8f), ("wind", 0.45f), ("drop_length", 34f), ("fall_speed", 1.3f)),
                E("lightning", ("frequency", 0.3f)),
            },
            ["hail"] = new[] { E("hail", ("density", 0.5f)), E("storm_clouds", ("cover", 0.35f), ("flashes", 0.15f)) },
            ["snow"] = new[]
            {
                E("snow", ("density", 0.45f)),
                E("clouds", ("coverage", 0.75f), ("color", new Color(0.92f, 0.93f, 0.96f)), ("shade", new Color(0.7f, 0.74f, 0.84f))),
            },
            ["blizzard"] = new[] { E("blizzard"), E("fog", ("density", 0.3f)) },
            ["fog"] = new[] { E("fog", ("density", 0.55f)), E("mist", ("density", 0.5f)) },
            ["windy"] = new[]
            {
                E("wind", ("density", 0.5f)), E("leaves", ("density", 0.25f), ("wind", 0.9f)),
                E("clouds", ("coverage", 0.35f), ("wind", 1.2f)),
            },
            ["sandstorm"] = new[] { E("sandstorm"), E("wind", ("density", 0.35f), ("color", new Color(1f, 0.9f, 0.7f, 0.5f))) },
            ["heatwave"] = new[] { E("heat_haze"), E("sun") },
            ["ashfall"] = new[] { E("ash"), E("embers", ("density", 0.3f)) },
        };

        /// <summary>The effects that take Ground.</summary>
        public static readonly string[] Grounded = { "rain", "hail", "lightning", "tornado" };

        /// <summary>Raised when the weather changes, with the new state.</summary>
        public event Action<string> WeatherChanged;

        [Tooltip("The state: clear, cloudy, drizzle, rain, storm, hail, snow, blizzard, fog, windy, sandstorm, heatwave or ashfall.")]
        [SerializeField] string weather = "clear";
        [Tooltip("Seconds a change takes when not given.")]
        public float fade = 4f;
        [Tooltip("Where rain splashes, hail bounces, lightning strikes and a tornado touches down: 0 to 1 down the view, 1 its bottom.")]
        [SerializeField, Range(0f, 1f)] float ground = 1f;
        [Tooltip("Random weather: each state's weight. Empty holds the weather.")]
        public List<Chance> forecast = new List<Chance>();
        [Tooltip("Seconds between the forecast's changes, at random from x to y.")]
        public Vector2 changeEvery = new Vector2(60f, 180f);

        readonly Dictionary<string, (WeatherFX fx, string key)> shown = new Dictionary<string, (WeatherFX, string)>();
        float next = -1f;

        static StateEffect E(string effect, params (string, object)[] settings) => new StateEffect(effect, settings);

        /// <summary>The states whose lead effect is installed, and clear.</summary>
        public static IReadOnlyList<string> Offered =>
            AllStates.Where(s => States[s].Length == 0 || WeatherFX.ShaderOf(States[s][0].Effect)).ToArray();

        /// <summary>The state; setting it changes the weather over Fade.</summary>
        public string Weather
        {
            get => weather;
            set => Change(value);
        }

        public float Ground
        {
            get => ground;
            set
            {
                ground = value;
                foreach (var e in shown)
                    if (Grounded.Contains(e.Key))
                        e.Value.fx.Set("ground", value);
            }
        }

        /// <summary>Changes the weather over <paramref name="seconds"/> (-1: Fade).</summary>
        public void Change(string state, float seconds = -1f)
        {
            if (!States.ContainsKey(state))
                throw new ArgumentException($"WeatherSystem: no state {state}, pick one of {string.Join(", ", Offered)}");
            weather = state;
            if (isActiveAndEnabled && Application.isPlaying)
                Apply(seconds < 0f ? fade : seconds);
        }

        /// <summary>The state's WeatherFX for <paramref name="effect"/>, or null.</summary>
        public WeatherFX GetEffect(string effect) => shown.TryGetValue(effect, out var s) ? s.fx : null;

        void Start()
        {
            Apply(0f);
        }

        void Update()
        {
            if (forecast.Count == 0)
                return;
            if (next < 0f)
                next = UnityEngine.Random.Range(changeEvery.x, changeEvery.y);
            next -= Time.deltaTime;
            if (next > 0f)
                return;
            next = UnityEngine.Random.Range(changeEvery.x, changeEvery.y);
            var offered = Offered;
            var chances = forecast.Where(c => offered.Contains(c.state) && c.weight > 0f).ToList();
            float pick = UnityEngine.Random.value * chances.Sum(c => c.weight);
            foreach (var c in chances)
            {
                pick -= c.weight;
                if (pick < 0f)
                {
                    if (c.state != weather)
                        Change(c.state);
                    return;
                }
            }
        }

        void Apply(float seconds)
        {
            var want = States[weather].ToDictionary(e => e.Effect);
            foreach (var e in shown.Keys.ToList())
                if (!want.TryGetValue(e, out var w) || w.Key != shown[e].key)
                {
                    if (shown[e].fx)
                        shown[e].fx.Stop(seconds);
                    shown.Remove(e);
                }
            foreach (var e in States[weather])
            {
                if (shown.ContainsKey(e.Effect) || !WeatherFX.ShaderOf(e.Effect))
                    continue;
                var fx = WeatherFX.Add(e.Effect, transform);
                foreach (var (name, value) in e.Settings)
                    if (value is Color c)
                        fx.Set(name, c);
                    else
                        fx.Set(name, (float)value);
                if (Grounded.Contains(e.Effect))
                    fx.Set("ground", ground);
                shown[e.Effect] = (fx.FadeIn(seconds), e.Key);
            }
            WeatherChanged?.Invoke(weather);
        }
    }
}
