using RaceFatal.Career;
using RaceFatal.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RaceFatal.Presentation.Career
{
    // A four-week display of the existing career clock. Browsing never advances time.
    public sealed class CareerHomeCalendarView : MonoBehaviour
    {
        private TMP_Text heading, details;
        private readonly Button[] days = new Button[28];
        private readonly Button[] events = new Button[4];
        private readonly TMP_Text[] eventLabels = new TMP_Text[4];
        private TeamState team;
        private GameDatabase database;
        private int block;
        public static CareerHomeCalendarView Create(Transform parent)
        {
            var root = CareerRuntimeUi.Rect(parent,"HomeCalendar",new Vector2(410,-385),new Vector2(1380,450));
            var view = root.gameObject.AddComponent<CareerHomeCalendarView>();
            view.Build(); return view;
        }
        private void Build()
        {
            heading = CareerRuntimeUi.Text(transform,"Heading","",Vector2.zero,new Vector2(720,45),28);
            CareerRuntimeUi.Button(transform,"<",new Vector2(740,0),new Vector2(55,38),()=> { if(block>0) block--; Render(); });
            CareerRuntimeUi.Button(transform,">",new Vector2(805,0),new Vector2(55,38),()=> { if(block<int.MaxValue/4-1) block++; Render(); });
            CareerRuntimeUi.Button(transform,"NOW",new Vector2(870,0),new Vector2(90,38),()=> { block=(team.Calendar.Week-1)/4; Render(); });
            string[] labels={"MON","TUE","WED","THU","FRI","SAT","SUN"};
            for(int i=0;i<7;i++) CareerRuntimeUi.Text(transform,labels[i],labels[i],new Vector2(i*138,-50),new Vector2(130,30),19);
            for(int i=0;i<28;i++)
            {
                int index=i;
                days[i]=CareerRuntimeUi.Button(transform,(i+1).ToString(),new Vector2(i%7*138,-85-i/7*65),new Vector2(130,57),()=>SelectDay(index));
            }
            CareerRuntimeUi.Text(transform,"EventTitle","THIS WEEK'S EVENTS",new Vector2(995,0),new Vector2(385,38),24);
            for(int i=0;i<4;i++)
            {
                int index=i;
                events[i]=CareerRuntimeUi.Button(transform,"",new Vector2(995,-48-i*63),new Vector2(385,56),()=>SelectEvent(index));
                eventLabels[i]=events[i].GetComponentInChildren<TMP_Text>();
                eventLabels[i].enableAutoSizing=true; eventLabels[i].fontSizeMin=15;
            }
            details=CareerRuntimeUi.Scroll(transform,"CalendarDetails",new Vector2(0,-355),new Vector2(1380,90));
        }
        public void Bind(TeamState value, GameDatabase db)
        {
            team=value; database=db; block=(team.Calendar.Week-1)/4; Render();
        }
        private void Render()
        {
            if(team==null) return;
            heading.text=$"CALENDAR // WEEKS {block*4+1}–{block*4+4}  ·  NOW {team.Calendar.Week}";
            for(int i=0;i<28;i++)
            {
                bool current=block*4+i/7+1==team.Calendar.Week;
                days[i].image.color=current?new Color(.12f,.42f,.45f):new Color(.08f,.15f,.19f);
                days[i].GetComponentInChildren<TMP_Text>().text=$"{(long)block*28+i+1}";
            }
            var active=team.Calendar.Active;
            for(int i=0;i<4;i++)
            {
                var definition=i<team.Calendar.DrawIds.Count?database.GetCareerEventDefinition(team.Calendar.DrawIds[i]):null;
                events[i].gameObject.SetActive(active!=null?i==0:definition!=null);
                eventLabels[i].text=active!=null?$"[>] {active.displayName}":definition==null?"":$"[{Symbol(definition.Kind)}] {definition.DisplayName}";
            }
            details.text="Select a day or event for details. The highlighted row is the current career week. Events are weekly; individual event dates are not assigned yet.";
        }
        private static string Symbol(CareerEventKind kind) => kind==CareerEventKind.Championship?"C":kind==CareerEventKind.Deathmatch?"!":"R";
        private void SelectDay(int index)
        {
            int week=block*4+index/7+1;
            details.text=$"DAY {(long)block*28+index+1} // WEEK {week}\n"+(week==team.Calendar.Week?"See this week's selectable events. Specific days have not been assigned.":"No day-specific events scheduled.");
        }
        private void SelectEvent(int index)
        {
            var active=team.Calendar.Active;
            if(active!=null) { details.text=$"{active.displayName} // ROUND {active.roundIndex+1}/{active.raceIds.Count}\n{active.description}"; return; }
            if(index>=team.Calendar.DrawIds.Count)return;
            var e=database.GetCareerEventDefinition(team.Calendar.DrawIds[index]);
            if(e!=null) details.text=$"{e.DisplayName} // {e.Kind} // ENTRY {e.EntryFee:N0} CREDITS\n{e.Description}\nOpen Races to review entry requirements and enter.";
        }
    }
}
