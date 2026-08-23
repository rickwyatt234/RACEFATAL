/*
    REPRESENTS 1 PHYSICAL BIKE OWNED BY TEAM
    INDIVIDUAL ASSET. IF DESTROYED, IT CANNOT BE USED.

    TO DO: EVENTUALLY OWN BIKE LOADOUT: NODE ASSIGNMENTS
*/

using System;
using RaceFatal.Shared;

namespace RaceFatal.Vehicles
{
    public class BikeState
    {
        public string BikeId { get; }
        public string BikeDefinitionId { get; }
        public string PrimaryColor { get; private set; }
        public string SecondaryColor { get; private set; }
        public BikeLoadout Loadout { get; }
        public bool IsDestroyed { get; private set; }

        public bool IsRaceReady => 
            !IsDestroyed && 
            Loadout.Engine != null &&
            Loadout.Chassis != null;

        public EngineClass? EngineClass 
        { 
            get
            {
                if (Loadout.Engine != null)
                {
                    return Loadout.Engine.EngineClass;
                }
                return null;
            }
        }

        public BikeState(
            string bikeId,
            string bikeDefinitionId,
            int smallNodes,
            int mediumNodes,
            int largeNodes,
            string primaryColor,
            string secondaryColor)
        {
            BikeId = bikeId;
            BikeDefinitionId = bikeDefinitionId;
            if (smallNodes < 0 || mediumNodes < 0 || largeNodes < 0)
            {
                throw new ArgumentException("Node counts cannot be negative.");
            }
            PrimaryColor = primaryColor;
            SecondaryColor = secondaryColor;

            Loadout = new BikeLoadout(smallNodes, mediumNodes, largeNodes);
        }

        public void Paint(string primaryColor, string secondaryColor)
        {
            PrimaryColor = primaryColor;
            SecondaryColor = secondaryColor;
        }

        public void Destroy()
        {
            IsDestroyed = true;
            Loadout.DestroyInstalledEquipment();
        }

    }
}

