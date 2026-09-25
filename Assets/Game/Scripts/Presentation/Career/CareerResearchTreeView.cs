using System;
using System.Collections.Generic;
using System.Linq;
using RaceFatal.Career;
using RaceFatal.Content.Career;
using RaceFatal.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace RaceFatal.Presentation.Career
{
    public class CareerResearchTreeView : MonoBehaviour
    {
        [SerializeField] private ResearchTreeViewport viewport;
        [SerializeField] private RectTransform content;
        private readonly Dictionary<string, Vector2> positions = new Dictionary<string, Vector2>();
        private readonly List<GameObject> generated = new List<GameObject>();
        private string field;
        private string selectedId;
        public void Render(GameDatabase database, TeamState team, ResearchTreeLayoutSO layout, string category, string selected, Action<string> select)
        {
            bool restoreFocus = EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null
                && EventSystem.current.currentSelectedGameObject.transform.IsChildOf(content);
            Button selectedButton = null;
            foreach (var obj in generated) { obj.SetActive(false); Destroy(obj); }
            generated.Clear(); positions.Clear(); selectedId = selected;
            var all = database.TechnologyDefinitions.Values.OrderBy(t => t.DisplayOrder).ThenBy(t => t.Id).ToList();
            var visible = all.Where(t => category == "ALL" || t.Field.ToString() == category).ToList();
            // Include cross-field prerequisites so every visible dependency is navigable.
            var ids = new HashSet<string>(visible.Select(t => t.Id));
            for (int i = 0; i < visible.Count; i++)
                foreach (string prerequisite in visible[i].PrerequisiteTechnologyIds)
                {
                    var parent = database.GetTechnologyDefinition(prerequisite);
                    if (parent != null && ids.Add(parent.Id)) visible.Add(parent);
                }
            if (visible.Count == 0) return;
            for (int i = 0; i < all.Count; i++)
            {
                var technology = all[i];
                if (ids.Contains(technology.Id)) positions[technology.Id] = layout != null
                    ? layout.PositionFor(technology.Id, new Vector2(i * 320, (technology.Tier - 1) * 180))
                    : new Vector2(i * 320, (technology.Tier - 1) * 180);
            }
            Vector2 min = new Vector2(positions.Values.Min(v => v.x), positions.Values.Min(v => v.y));
            Vector2 max = new Vector2(positions.Values.Max(v => v.x), positions.Values.Max(v => v.y));
            Vector2 center = (min + max) / 2;
            foreach (string id in positions.Keys.ToArray()) positions[id] -= center;
            content.sizeDelta = new Vector2(Mathf.Max(1500, max.x - min.x + 700), Mathf.Max(1200, max.y - min.y + 500));
            var linesObject = new GameObject("Connections", typeof(RectTransform), typeof(CanvasRenderer), typeof(ResearchTreeConnections));
            linesObject.transform.SetParent(content, false); generated.Add(linesObject);
            var lines = linesObject.GetComponent<ResearchTreeConnections>(); lines.raycastTarget = false;
            var rect = (RectTransform)lines.transform; rect.sizeDelta = content.sizeDelta;
            foreach (var technology in visible)
                foreach (string parent in technology.PrerequisiteTechnologyIds)
                    if (positions.ContainsKey(parent)) lines.Edges.Add(new ResearchTreeConnections.Edge {
                        from = positions[parent] + Vector2.up * 46, to = positions[technology.Id] - Vector2.up * 46,
                        tint = team.HasTechnology(parent) ? new Color(.15f, .9f, .8f) : new Color(.3f, .36f, .42f) });
            lines.SetVerticesDirty();
            foreach (var technology in visible)
            {
                bool done = team.HasTechnology(technology.Id);
                bool prerequisites = technology.PrerequisiteTechnologyIds.All(team.HasTechnology);
                bool available = new ResearchService(database).CanResearch(team, technology.Id).IsSuccess;
                string state = done ? "RESEARCHED" : available ? "AVAILABLE" : prerequisites && team.ResearchPoints < technology.ResearchCost ? "LOW RP" : "LOCKED";
                var obj = new GameObject(technology.Id, typeof(RectTransform), typeof(Image), typeof(Button));
                obj.transform.SetParent(content, false); generated.Add(obj);
                var node = (RectTransform)obj.transform; node.sizeDelta = new Vector2(260, 92); node.anchoredPosition = positions[technology.Id];
                var background = obj.GetComponent<Image>();
                background.color = done ? new Color(.08f, .4f, .3f) : available ? new Color(.08f, .28f, .4f) : new Color(.17f, .18f, .22f);
                if (technology.Id == selected) { var outline = obj.AddComponent<Outline>(); outline.effectColor = Color.cyan; outline.effectDistance = new Vector2(3, -3); }
                var button = obj.GetComponent<Button>(); button.targetGraphic = background;
                var focus = obj.AddComponent<ResearchTreeNodeFocus>(); focus.Viewport = viewport; focus.Position = positions[technology.Id];
                if (technology.Id == selected) selectedButton = button;
                string id = technology.Id; button.onClick.AddListener(() => select(id));
                var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)); labelObject.transform.SetParent(obj.transform, false);
                var labelRect = (RectTransform)labelObject.transform; labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one; labelRect.offsetMin = new Vector2(8, 5); labelRect.offsetMax = new Vector2(-8, -5);
                var label = labelObject.GetComponent<TextMeshProUGUI>(); label.raycastTarget = false; label.fontSize = 19; label.enableAutoSizing = true; label.fontSizeMin = 13; label.fontSizeMax = 19; label.alignment = TextAlignmentOptions.Center;
                label.text = $"{technology.DisplayName}\nTIER {technology.Tier} • {technology.ResearchCost:N0} RP\n{state}";
            }
            if (restoreFocus && selectedButton != null) selectedButton.Select();
            if (field != category) { field = category; Recenter(); }
        }
        public void Focus(string id) { if (id != null && positions.TryGetValue(id, out var position)) viewport.Focus(position); }
        public void Recenter() { viewport.SetZoom(.7f); Focus(selectedId); }
        public void ZoomIn() => viewport.SetZoom(viewport.content.localScale.x * 1.2f);
        public void ZoomOut() => viewport.SetZoom(viewport.content.localScale.x / 1.2f);
    }
}
