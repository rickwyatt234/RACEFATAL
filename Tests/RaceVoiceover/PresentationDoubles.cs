using System;
using RaceFatal.Racing;
namespace UnityEngine
{
    public class SerializeField : Attribute { }
    public class HeaderAttribute : Attribute { public HeaderAttribute(string text) { } }
    public class TooltipAttribute : Attribute { public TooltipAttribute(string text) { } }
    public class RangeAttribute : Attribute { public RangeAttribute(float a, float b) { } }
    public class MinAttribute : Attribute { public MinAttribute(float value) { } }
    public class DisallowMultipleComponent : Attribute { }
    public class Transform { public void SetParent(Transform parent, bool worldPositionStays) { } }
    public class MonoBehaviour { public bool isActiveAndEnabled = true; public Transform transform = new Transform(); }
    public class GameObject
    {
        public GameObject(string name) { }
        public Transform transform = new Transform();
        public T AddComponent<T>() where T : new() => new T();
    }
    public class AudioClip { public float length = 1f; }
    public class AudioSource
    {
        public bool playOnAwake, loop, ignoreListenerPause, ignoreListenerVolume;
        public float spatialBlend, dopplerLevel, pitch, volume;
        public int priority;
        public bool isPlaying;
        public AudioClip clip;
        public void Play() { isPlaying = true; }
        public void Stop() { isPlaying = false; }
    }
    public static class AudioListener { public static float volume = 1f; }
    public static class Time { public static float unscaledDeltaTime = .1f; }
    public static class Mathf
    {
        public static float Clamp01(float v) => Math.Clamp(v, 0f, 1f);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static bool Approximately(float a, float b) => Math.Abs(a - b) < .00001f;
        public static float MoveTowards(float from, float to, float step) => from < to ? Math.Min(from + step, to) : Math.Max(from - step, to);
    }
}
namespace RaceFatal.Presentation.Combat
{
    public class TargetLockThreatReceiver { public bool HasHardLock; }
}
namespace RaceFatal.Presentation.Tactics
{
    public static class TacticalRaceController { public static bool IsActive; }
}
namespace RaceFatal.Presentation.Bootstrap
{
    public static class BootstrapController { public static Context Context; }
    public class Context { public RaceFatal.Career.GameSessionManager Sessions; }
}
namespace RaceFatal.Presentation.Racing
{
    public static class RacePauseController { public static bool IsGameplayBlocked; }
    public class RaceRuntimeController
    {
        public RaceDirector Director;
        public string PlayerRacerId;
        public bool IsRaceActive;
        public bool TryGetRacerView(string id, out RacerView view) { view = new RacerView(); return true; }
    }
    public class RacerView { public T GetComponent<T>() where T : new() => new T(); }
}
