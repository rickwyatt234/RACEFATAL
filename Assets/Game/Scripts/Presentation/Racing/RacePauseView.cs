using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RaceFatal.Presentation.Racing
{
    public sealed class RacePauseView : MonoBehaviour
    {
        private CanvasGroup group;
        private GameObject menu;
        private GameObject confirmation;
        private Button resumeButton;
        private Button cancelButton;
        private GameObject ownedEventSystem;
        public bool IsVisible => gameObject.activeSelf && group != null && group.alpha > 0f;
        public bool IsConfirmingQuit => IsVisible && confirmation.activeSelf;

        public void Initialize(RacePauseController controller, KeyCode pauseKey, KeyCode hideKey)
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            gameObject.AddComponent<GraphicRaycaster>();
            group = gameObject.AddComponent<CanvasGroup>();

            // An invisible full-screen raycast blocker remains in screenshot mode.
            var backdrop = Rect(transform, "Backdrop", Vector2.zero, Vector2.zero);
            backdrop.anchorMin = Vector2.zero;
            backdrop.anchorMax = Vector2.one;
            backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
            backdrop.gameObject.AddComponent<Image>().color = new Color(.005f, .012f, .02f, .78f);

            menu = Panel(backdrop, "PauseMenu");
            Text(menu.transform, "RACE//FATAL", 26, new Vector2(0, 170), new Vector2(540, 42));
            Text(menu.transform, "PAUSED", 52, new Vector2(0, 105), new Vector2(540, 72));
            resumeButton = Button(menu.transform, "RESUME", 15, controller.Resume);
            Button(menu.transform, "HIDE MENU // " + hideKey, -60, controller.ToggleMenu);
            Button(menu.transform, "QUIT GAME", -135, ShowQuitConfirmation);
            Text(menu.transform, pauseKey + " // RESUME     " + hideKey + " // SHOW / HIDE MENU\nThe race stays frozen while the menu is hidden.", 18,
                new Vector2(0, -230), new Vector2(590, 70));

            confirmation = Panel(backdrop, "QuitConfirmation");
            Text(confirmation.transform, "QUIT GAME?", 42, new Vector2(0, 125), new Vector2(540, 65));
            Text(confirmation.transform, "This race will not be saved.\nYour last campaign save will be kept.", 24,
                new Vector2(0, 30), new Vector2(530, 100));
            cancelButton = Button(confirmation.transform, "BACK", -75, ShowMenu);
            Button(confirmation.transform, "QUIT GAME", -150, controller.QuitGame);

            if (EventSystem.current == null)
            {
                ownedEventSystem = new GameObject("PauseEventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                ownedEventSystem.transform.SetParent(controller.transform, false);
            }
            Close();
        }

        public void ShowMenu()
        {
            gameObject.SetActive(true);
            group.alpha = 1f;
            group.interactable = true;
            menu.SetActive(true);
            confirmation.SetActive(false);
            Select(resumeButton.gameObject);
        }

        public void HideMenu()
        {
            group.alpha = 0f;
            group.interactable = false;
            Select(null);
        }

        public void Close()
        {
            Select(null);
            gameObject.SetActive(false);
        }

        private void ShowQuitConfirmation()
        {
            menu.SetActive(false);
            confirmation.SetActive(true);
            Select(cancelButton.gameObject);
        }

        private void OnDestroy()
        {
            if (ownedEventSystem != null)
                Destroy(ownedEventSystem);
        }

        private static void Select(GameObject target)
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(target);
        }

        private static GameObject Panel(Transform parent, string name)
        {
            var panel = Rect(parent, name, Vector2.zero, new Vector2(650, 590));
            panel.gameObject.AddComponent<Image>().color = new Color(.025f, .055f, .075f, .98f);
            return panel.gameObject;
        }

        private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static void Text(Transform parent, string value, float size, Vector2 position, Vector2 dimensions)
        {
            var label = Rect(parent, "Label", position, dimensions).gameObject.AddComponent<TextMeshProUGUI>();
            label.text = value;
            label.fontSize = size;
            label.color = new Color(.78f, .95f, .98f);
            label.alignment = TextAlignmentOptions.Center;
            label.richText = false;
            label.raycastTarget = false;
        }

        private static Button Button(Transform parent, string label, float y, UnityAction action)
        {
            var rect = Rect(parent, label, new Vector2(0, y), new Vector2(530, 60));
            var image = rect.gameObject.AddComponent<Image>();
            image.color = Color.white;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = new Color(.09f, .2f, .25f);
            colors.highlightedColor = colors.selectedColor = new Color(.16f, .38f, .43f);
            colors.pressedColor = new Color(.07f, .14f, .18f);
            button.colors = colors;
            button.onClick.AddListener(action);
            Text(rect, label, 24, Vector2.zero, rect.sizeDelta);
            return button;
        }
    }
}
