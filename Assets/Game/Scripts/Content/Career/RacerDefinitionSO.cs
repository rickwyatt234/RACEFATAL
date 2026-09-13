using RaceFatal.Career;
using UnityEngine;

namespace RaceFatal.Content.Career
{
    [CreateAssetMenu(
        fileName = "RacerDefinition",
        menuName = "RaceFatal/Career/Racer")]
    public class RacerDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;

        [Header("AI Personality")]
        [Range(0f, 1f)][SerializeField] private float pace = 0.75f;
        [Range(0f, 1f)][SerializeField] private float aggression = 0.5f;
        [Range(0f, 1f)][SerializeField] private float overtakingSkill = 0.5f;
        [Range(0f, 1f)][SerializeField] private float defensiveSkill = 0.5f;
        [Range(0f, 1f)][SerializeField] private float weaponAggression = 0.5f;

        public string Id => id;

        public float Pace => pace;
        public float Aggression => aggression;
        public float OvertakingSkill => overtakingSkill;
        public float DefensiveSkill => defensiveSkill;
        public float WeaponAggression => weaponAggression;

        public RacerDefinition CreateDefinition()
        {
            return new RacerDefinition(
                id,
                displayName,
                pace,
                aggression,
                overtakingSkill,
                defensiveSkill,
                weaponAggression);
        }

        [ContextMenu("Randomize AI Personality")]
        private void RandomizeAIPersonality()
        {
            RecordUndo(
                "Randomize Racer AI Personality");

            pace = RandomValue(0f, 1f);
            aggression = RandomValue(0f, 1f);
            overtakingSkill = RandomValue(0f, 1f);
            defensiveSkill = RandomValue(0f, 1f);
            weaponAggression = RandomValue(0f, 1f);

            MarkDirty();
        }

        [ContextMenu("Randomize Competitive AI Personality")]
        private void RandomizeCompetitiveAIPersonality()
        {
            RecordUndo(
                "Randomize Competitive Racer AI Personality");

            /*
             * Every racer remains reasonably competent at
             * actually racing, while aggression and combat
             * behavior are allowed to vary much more strongly.
             */
            pace = RandomValue(0.65f, 1f);
            aggression = RandomValue(0.15f, 1f);
            overtakingSkill = RandomValue(0.4f, 1f);
            defensiveSkill = RandomValue(0.3f, 1f);
            weaponAggression = RandomValue(0.1f, 1f);

            MarkDirty();
        }

        [ContextMenu("Reset AI Personality")]
        private void ResetAIPersonality()
        {
            RecordUndo(
                "Reset Racer AI Personality");

            pace = 0.75f;
            aggression = 0.5f;
            overtakingSkill = 0.5f;
            defensiveSkill = 0.5f;
            weaponAggression = 0.5f;

            MarkDirty();
        }

        private float RandomValue(
            float minimum,
            float maximum)
        {
            float value =
                Random.Range(
                    minimum,
                    maximum);

            /*
             * Two decimal places keeps authored racer values
             * easy to inspect and compare.
             */
            return Mathf.Round(
                value * 100f) / 100f;
        }

        private void RecordUndo(
            string operationName)
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(
                this,
                operationName);
#endif
        }

        private void MarkDirty()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(
                this);
#endif
        }
    }
}