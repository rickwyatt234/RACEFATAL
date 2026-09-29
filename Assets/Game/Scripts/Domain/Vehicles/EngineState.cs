using System;
using RaceFatal.Shared;

namespace RaceFatal.Vehicles
{
    public class EngineState
    {
        public string EngineId { get; }
        public string EngineDefinitionId { get; }
        public EngineClass EngineClass { get; }
        public bool IsDestroyed { get; private set; }

        public EngineState(string engineId, string engineDefinitionId, EngineClass engineClass)
        {
            EngineId = engineId;
            EngineDefinitionId = engineDefinitionId;
            EngineClass = engineClass;
        }

        public void Destroy()
        {
            IsDestroyed = true;
        }
    }
}
