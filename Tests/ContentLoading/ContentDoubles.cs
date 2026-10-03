using System;
using System.Collections.Generic;
using RaceFatal.Career;
using RaceFatal.Content.Racing;
using RaceFatal.Content.Tracks;
using RaceFatal.Equipment;
using RaceFatal.Vehicles;

namespace UnityEngine
{
    public class Object { public string name; }
    public class ScriptableObject : Object { }
    public class GameObject : Object { }
    public class Sprite : Object { }
    public class SerializeField : Attribute { }
    public class Header : Attribute { public Header(string text) { } }
    public class Tooltip : Attribute { public Tooltip(string text) { } }
    public class Min : Attribute { public Min(float minimum) { } }
    public class CreateAssetMenu : Attribute { public string fileName, menuName; }
    public static class Debug
    {
        public static readonly List<string> Errors = new List<string>();
        public static readonly List<string> Warnings = new List<string>();
        public static void LogError(object message, Object context) => Errors.Add(message.ToString());
        public static void LogWarning(object message, Object context) => Warnings.Add(message.ToString());
    }
}

namespace RaceFatal.Content
{
    // Unused catalog categories stay empty; racing/track content above is production code.
    public class DefinitionContent<T> where T : class
    {
        public T CreateDefinition() => null;
        public T CreateDefinition(object progression) => null;
    }
    public class VehicleContent
    {
        public BikeDefinition CreateBikeDefinition() => null;
        public EngineDefinition CreateEngineDefinition() => null;
        public ChassisDefinition CreateChassisDefinition() => null;
        public EquipmentDefinition CreateEquipmentDefinition() => null;
    }
    public class GameContentCatalogSO : UnityEngine.ScriptableObject
    {
        public List<VehicleContent> BikeDefinitions = new List<VehicleContent>();
        public List<VehicleContent> EngineDefinitions = new List<VehicleContent>();
        public List<VehicleContent> ChassisDefinitions = new List<VehicleContent>();
        public List<VehicleContent> EquipmentDefinitions = new List<VehicleContent>();
        public List<TrackDefinitionSO> TrackDefinitions = new List<TrackDefinitionSO>();
        public List<RaceDefinitionSO> RaceDefinitions = new List<RaceDefinitionSO>();
        public List<DefinitionContent<BikeBuildDefinition>> BikeBuildDefinitions = new List<DefinitionContent<BikeBuildDefinition>>();
        public List<DefinitionContent<RacerDefinition>> RacerDefinitions = new List<DefinitionContent<RacerDefinition>>();
        public List<DefinitionContent<RacerPerkDefinition>> RacerPerkDefinitions = new List<DefinitionContent<RacerPerkDefinition>>();
        public List<DefinitionContent<OpponentTeamDefinition>> OpponentTeamDefinitions = new List<DefinitionContent<OpponentTeamDefinition>>();
        public List<DefinitionContent<TechnologyDefinition>> TechnologyDefinitions = new List<DefinitionContent<TechnologyDefinition>>();
        public List<DefinitionContent<ResearcherDefinition>> ResearcherDefinitions = new List<DefinitionContent<ResearcherDefinition>>();
        public List<DefinitionContent<CareerEventDefinition>> CareerEventDefinitions = new List<DefinitionContent<CareerEventDefinition>>();
        public object ResearchProgression;
    }
}
