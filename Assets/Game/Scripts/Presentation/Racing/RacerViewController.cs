using System;
using System.Collections.Generic;
using RaceFatal.Racing;
using RaceFatal.Vehicles;
using UnityEngine;

namespace RaceFatal.Presentation.Racing
{
    public class RacerViewController : MonoBehaviour
    {
        private readonly Dictionary<string, BikeEquipmentMountView> equipmentMounts 
            = new Dictionary<string, BikeEquipmentMountView>();

        private RaceParticipant participant;
        private BikeEquipmentMountView[] physicalMounts;
        public RaceParticipant Participant => participant;
        public string RacerId => participant?.RacerId;
        public bool IsInitialized => participant != null;

        private void Awake()
        {
            physicalMounts = GetComponentsInChildren<BikeEquipmentMountView>(true);
        }

        private void Initialize(RaceParticipant raceParticipant)
        {
            participant = raceParticipant ?? throw new ArgumentNullException(nameof(raceParticipant));
            BindEquipmentMounts();
        }

        private void BindEquipmentMounts()
        {
            equipmentMounts.Clear();
            foreach (BikeNode node in participant.Bike.Loadout.Nodes)
            {
                if (!node.IsOccupied)
                    continue;

                BikeEquipmentMountView mountView = FindMount(node.NodeSize, node.Index);

                if (mountView == null)
                {
                    Debug.LogWarning($"No mount view found for node size {node.NodeSize} and index {node.Index}");
                    continue;
                }

                equipmentMounts[node.InstalledEquipment.EquipmentId] = mountView;
            }
        }

        public bool TryGetEquipmentMount(string equipmentId, out BikeEquipmentMountView mountView)
        {
            return equipmentMounts.TryGetValue(equipmentId, out mountView);
        }
        private BikeEquipmentMountView FindMount(RaceFatal.Shared.NodeSize size,
            int index)
        {
            foreach (BikeEquipmentMountView mount
                     in physicalMounts)
            {
                if (mount.NodeSize == size &&
                    mount.NodeIndex == index)
                {
                    return mount;
                }
            }

            return null;
        }
    }
}