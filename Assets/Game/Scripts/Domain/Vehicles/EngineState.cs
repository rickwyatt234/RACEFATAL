using System;
using RaceFatal.Shared;

namespace RaceFatal.Vehicles
{
    public class EngineState
    {
        public string EngineId { get; } //physical engine instance id (81ca87...etc)
        public string EngineDefinitionId { get;  } //engine definition id (e.g. "engine_250cc_v1") - used to look up engine definition data
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
