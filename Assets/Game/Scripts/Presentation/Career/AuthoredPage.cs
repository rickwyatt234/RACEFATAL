using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace RaceFatal.Presentation.Career
{
    // Fixed authored slots display an unbounded collection of data, never cloned UI.
    public sealed class AuthoredPage : MonoBehaviour
    {
        [SerializeField] private Component[] rows;
        [SerializeField] private Button previous, next;
        [SerializeField] private TMP_Text pageLabel;
        private readonly List<Action<Component>> entries = new List<Action<Component>>();
        private int page;
        private bool bound;
        public void Begin() { entries.Clear(); }
        public void Add(Action<Component> bind) { entries.Add(bind); }
        public void Render()
        {
            if (rows == null || rows.Length == 0) throw new InvalidOperationException("Missing authored list rows.");
            if (!bound)
            {
                UiBindings.Bind(previous, () => { page--; Render(); });
                UiBindings.Bind(next, () => { page++; Render(); });
                bound = true;
            }
            int count = Math.Max(1, (entries.Count + rows.Length - 1) / rows.Length);
            page = Math.Max(0, Math.Min(page, count - 1));
            for(int i=0;i<rows.Length;i++)
            {
                int index=page*rows.Length+i;
                rows[i].gameObject.SetActive(index<entries.Count);
                if(index<entries.Count) entries[index](rows[i]);
            }
            previous.interactable=page>0; next.interactable=page+1<count;
            pageLabel.text=$"{page+1} / {count}  ·  {entries.Count} ITEMS";
        }
        public static AuthoredPage Require(Component container)
        {
            var result = container != null ? container.GetComponent<AuthoredPage>() : null;
            if (result == null) throw new InvalidOperationException("Missing authored list. Run the UI migration in the Editor.");
            return result;
        }
        public static void BeginAll(Component root)
        {
            foreach(var list in root.GetComponentsInChildren<AuthoredPage>(true)) list.Begin();
        }
        public static void RenderAll(Component root)
        {
            foreach(var list in root.GetComponentsInChildren<AuthoredPage>(true)) list.Render();
        }
#if UNITY_EDITOR
        public void EditorConfigure(Component[] slots, Button back, Button forward, TMP_Text label)
        {
            if (Application.isPlaying) throw new InvalidOperationException("Editor only");
            rows=slots; previous=back; next=forward; pageLabel=label;
            foreach(var row in rows) row.gameObject.SetActive(false);
        }
#endif
    }
}
