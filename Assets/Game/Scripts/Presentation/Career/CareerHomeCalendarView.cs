using System.Collections.Generic;
using System.Linq;
using RaceFatal.Career;
using RaceFatal.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RaceFatal.Presentation.Career
{
    public sealed class CareerHomeCalendarView : MonoBehaviour
    {
        [SerializeField] private TMP_Text heading, details, eventTitle, pageLabel;
        [SerializeField] private Button[] days = new Button[28];
        [SerializeField] private Button[] events = new Button[4];
        [SerializeField] private TMP_Text[] eventLabels = new TMP_Text[4];
        [SerializeField] private Button previousBlock, nextBlock, today, previousEvents, nextEvents;
        private TeamState team;
        private GameDatabase database;
        private CareerCalendarService calendar;
        private CareerController owner;
        private int block, selectedDay, page;
        private IReadOnlyList<CareerEventOccurrence> visible = new List<CareerEventOccurrence>();
        private List<CareerEventOccurrence> selected = new List<CareerEventOccurrence>();
        public static CareerHomeCalendarView Create(Transform parent, CareerController owner)
        {
            var view = parent.GetComponentInChildren<CareerHomeCalendarView>(true);
            if (view == null) throw new System.InvalidOperationException("Author the Home calendar in the Editor before playing.");
            view.owner = owner;
            view.BindControls();
            return view;
        }
        private void BindControls()
        {
            UiBindings.Bind(previousBlock, () => { if (block > 0) block--; Render(); });
            UiBindings.Bind(nextBlock, () => { if ((long)(block+2)*28<=CareerCalendarState.MaxDay) block++; Render(); });
            UiBindings.Bind(today, () => { block=(team.Calendar.AbsoluteDay-1)/28; Render(); SelectDay(team.Calendar.AbsoluteDay); });
            UiBindings.Bind(previousEvents, () => { if(page>0)page--; RenderEvents(); });
            UiBindings.Bind(nextEvents, () => { if((page+1)*4<selected.Count)page++; RenderEvents(); });
            for (int i=0;i<days.Length;i++) { int index=i; UiBindings.Bind(days[i],()=>SelectDay(block*28+index+1)); }
            for (int i=0;i<events.Length;i++) { int index=i; UiBindings.Bind(events[i],()=>OpenEvent(index)); }
        }
#if UNITY_EDITOR
        public void EditorAuthor()
        {
            if (Application.isPlaying) throw new System.InvalidOperationException("Editor only");
            if (heading != null) return;
            heading = EditorUiFactory.Text(transform,"Heading","",Vector2.zero,new Vector2(730,45),25);
            previousBlock = EditorUiFactory.Button(transform,"<",new Vector2(740,0),new Vector2(55,38),()=> { if(block>0) block--; Render(); });
            nextBlock = EditorUiFactory.Button(transform,">",new Vector2(805,0),new Vector2(55,38),()=> { if((long)(block+2)*28<=CareerCalendarState.MaxDay) block++; Render(); });
            today = EditorUiFactory.Button(transform,"NOW",new Vector2(870,0),new Vector2(90,38),()=> { block=(team.Calendar.AbsoluteDay-1)/28; Render(); SelectDay(team.Calendar.AbsoluteDay); });
            for(int i=0;i<7;i++) EditorUiFactory.Text(transform,"Day"+i,"DAY "+(i+1),new Vector2(i*138,-50),new Vector2(130,30),19);
            for(int i=0;i<28;i++)
            {
                int index=i;
                days[i]=EditorUiFactory.Button(transform,"",new Vector2(i%7*138,-85-i/7*65),new Vector2(130,57),()=>SelectDay(block*28+index+1));
                days[i].GetComponentInChildren<TMP_Text>().fontSize=18;
            }
            eventTitle = EditorUiFactory.Text(transform,"EventTitle","SELECT A DAY",new Vector2(995,0),new Vector2(385,38),22);
            for(int i=0;i<4;i++)
            {
                int index=i;
                events[i]=EditorUiFactory.Button(transform,"",new Vector2(995,-48-i*63),new Vector2(385,56),()=>OpenEvent(index));
                eventLabels[i]=events[i].GetComponentInChildren<TMP_Text>();
                eventLabels[i].enableAutoSizing=true; eventLabels[i].fontSizeMin=15;
            }
            previousEvents = EditorUiFactory.Button(transform,"<",new Vector2(995,-306),new Vector2(55,35),()=> { if(page>0)page--; RenderEvents(); });
            pageLabel=EditorUiFactory.Text(transform,"Page","",new Vector2(1060,-306),new Vector2(250,35),19);
            nextEvents = EditorUiFactory.Button(transform,">",new Vector2(1325,-306),new Vector2(55,35),()=> { if((page+1)*4<selected.Count)page++; RenderEvents(); });
            details=EditorUiFactory.Scroll(transform,"CalendarDetails",new Vector2(0,-355),new Vector2(1380,90));
        }
#endif
        public void Bind(TeamState value, GameDatabase db)
        {
            team=value; database=db; calendar=new CareerCalendarService(db);
            block=(team.Calendar.AbsoluteDay-1)/28;
            Render(); SelectDay(team.Calendar.AbsoluteDay);
        }
        private void Render()
        {
            if(team==null) return;
            heading.text=$"WEEKS {block*4+1}–{block*4+4} // TODAY {team.Calendar.DateLabel}";
            visible=calendar.GetOccurrences(team,block*28+1,(int)System.Math.Min((long)block*28+28,CareerCalendarState.MaxDay));
            for(int i=0;i<28;i++)
            {
                long candidate=(long)block*28+i+1;
                days[i].interactable=candidate<=CareerCalendarState.MaxDay;
                if (!days[i].interactable) { days[i].GetComponentInChildren<TMP_Text>().text=""; continue; }
                int day=(int)candidate;
                days[i].image.color=day==team.Calendar.AbsoluteDay?new Color(.12f,.42f,.45f):day<team.Calendar.AbsoluteDay?new Color(.065f,.09f,.11f):new Color(.08f,.15f,.19f);
                var items=visible.Where(e=>e.Day==day).ToList();
                string symbols=string.Join(" ",items.Take(3).Select(e=>"["+Symbol(database.GetCareerEventDefinition(e.EventId)?.Kind ?? CareerEventKind.Other)+"]"));
                if(items.Count>3)symbols+=" +"+(items.Count-3);
                days[i].GetComponentInChildren<TMP_Text>().text=$"W{(day-1)/7+1} D{(day-1)%7+1}\n{symbols}";
            }
            SelectDay(block*28+1);
        }
        private static string Symbol(CareerEventKind kind) => kind==CareerEventKind.Championship?"C":kind==CareerEventKind.Deathmatch?"!":kind==CareerEventKind.Other?"O":"R";
        private void SelectDay(int day)
        {
            selectedDay=day; page=0;
            selected=visible.Where(e=>e.Day==day).ToList();
            eventTitle.text=CareerCalendarState.FormatDay(day);
            RenderEvents();
            details.text=selected.Count==0?"No events on this date. Browse ahead to choose a scheduled event.":"Select an event to inspect its venue, rewards and entry rules. Entering moves today to this date; browsing does not advance time.\n[R] Race   [C] Championship   [!] Deathmatch   [O] Other";
        }
        private void RenderEvents()
        {
            for(int i=0;i<4;i++)
            {
                int index=page*4+i;
                events[i].gameObject.SetActive(index<selected.Count);
                if(index>=selected.Count)continue;
                var item=selected[index]; var definition=database.GetCareerEventDefinition(item.EventId);
                bool consumed=!item.IsActiveRound && team.Calendar.IsConsumed(item.EventId,item.OccurrenceDay);
                string status=item.Day<team.Calendar.AbsoluteDay?"PAST":consumed?"ENTERED":!team.Calendar.UnlockedIds.Contains(item.EventId)?"LOCKED":"VIEW";
                eventLabels[i].text=$"[{Symbol(definition?.Kind ?? CareerEventKind.Other)}] {definition?.DisplayName ?? item.EventId}\n{status}"+(definition?.Kind==CareerEventKind.Championship?$" · ROUND {item.RoundIndex+1}":"");
            }
            pageLabel.text=selected.Count==0?"NO EVENTS":$"{page+1}/{(selected.Count+3)/4} · {selected.Count} EVENTS";
        }
        private void OpenEvent(int index)
        {
            int itemIndex=page*4+index;
            if(itemIndex>=selected.Count)return;
            var item=selected[itemIndex];
            owner?.ShowCalendarEvent(item.EventId,item.Day);
        }
    }
}
