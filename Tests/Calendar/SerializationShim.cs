using System;
using System.Text.Json;
namespace UnityEngine
{
    // Fields-only serialization for headless domain regressions, not a substitute
    // for checking Unity's serializer in editor validation menus.
    public static class JsonUtility
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { IncludeFields = true };
        public static string ToJson(object value) => JsonSerializer.Serialize(value, Options);
        public static T FromJson<T>(string value) => JsonSerializer.Deserialize<T>(value, Options);
    }
    public static class Debug { public static void Log(object message) => Console.WriteLine(message); }
}
namespace UnityEditor
{
    public sealed class MenuItem : Attribute { public MenuItem(string path) {} }
}
