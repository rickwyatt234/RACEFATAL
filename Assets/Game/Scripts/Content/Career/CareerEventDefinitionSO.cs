using System.Collections.Generic;
using System.Linq;
using RaceFatal.Career;
using RaceFatal.Content.Racing;
using UnityEngine;

namespace RaceFatal.Content.Career
{
    [CreateAssetMenu(menuName = "RaceFatal/Career/Calendar Event")]
    public sealed class CareerEventDefinitionSO : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [TextArea(2, 5)] [SerializeField] private string description;
        [SerializeField] private CareerEventKind kind;
        [Min(0)] [SerializeField] private int requiredFame;
        [Min(0)] [SerializeField] private int entryFee;
        [Tooltip("One race for a single event or deathmatch; ordered circuit races for a championship. Deathmatch requires enabled Deathmatch rules on its race asset.")] [SerializeField] private List<RaceDefinitionSO> rounds =
            new List<RaceDefinitionSO>();
        [Tooltip("Credits for the player's finishing place each round. Entries beyond this table pay zero. DNF pays 40%.")] [SerializeField] private List<int> racePayouts =
            new List<int> {  10000,  8000,  6500,  5000,  5000,  5000,  3500,  3500,  3500,  2500,  2500,  2500 };
        [Tooltip("Extra credits by final TEAM rank after completing the championship. Tied points share rank and prize.")] [SerializeField] private List<int> championshipPrizes =
            new List<int> {  20000,  10000,  5000 };
        [Tooltip("Points by finishing place, summed for both racers on each team. DNF earns zero.")] [SerializeField] private List<int> positionPoints =
            new List<int> {  25,  18,  15,  12,  10,  8,  6,  5,  4,  3,  2,  1 };
        [Header("Weekly Event Pool")]
        [Tooltip("1, 2, 3 designate the fixed opening races. Zero means the random pool only.")]
        [Range(0, 3)] [SerializeField] private int starterOrder;
        [Tooltip("Earliest week this event can join the random pool.")]
        [Min(1)] [SerializeField] private int firstWeek = 1;
        [Tooltip("Preferred start day. Conflicts shift to the nearest free block within the week, preferring later on ties.")]
        [Range(1, 7)] [SerializeField] private int firstDayOfWeek = 1;
        [HideInInspector] [SerializeField] private int repeatEveryWeeks = 5;
        // Legacy serialized timing is retained for compatibility; weekly rounds are consecutive.
        [HideInInspector] [SerializeField] private int roundSpacingDays = 1;
        public string Id => id;

        public CareerEventDefinition CreateDefinition() => new CareerEventDefinition(id, displayName, description, kind, requiredFame, entryFee, rounds.Select(r => r != null ? r.Id : null), racePayouts, championshipPrizes, positionPoints, firstWeek, firstDayOfWeek, repeatEveryWeeks, roundSpacingDays, starterOrder);
    }
}
