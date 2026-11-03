using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
namespace RaceFatal.Presentation.Career
{
    internal static class UiBindings
    {
        internal static void Bind(Button button, UnityAction action)
        {
            if (button == null) throw new InvalidOperationException("Missing authored UI button. Run the UI migration in the Editor.");
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }
        internal static void Bind(GameObject root, string name, UnityAction action)
        {
            foreach (var button in root.GetComponentsInChildren<Button>(true))
                if (button.name == name) { Bind(button, action); return; }
            throw new InvalidOperationException("Missing authored UI button: " + name);
        }
    }
}
