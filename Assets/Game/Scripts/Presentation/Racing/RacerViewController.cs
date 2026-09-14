using System;
using System.Collections.Generic;
using RaceFatal.Racing;
using RaceFatal.Vehicles;
using UnityEngine;

namespace RaceFatal.Presentation.Racing
{
    public class RacerViewController : MonoBehaviour
    {
        [Header("Bike Presentation")]
        [SerializeField] private BikeEquipmentLayoutView equipmentLayout;

        private readonly Dictionary<string, BikeEquipmentMountBinding>
            equipmentMounts =
                new Dictionary<string, BikeEquipmentMountBinding>();

        private RaceParticipant participant;

        public RaceParticipant Participant =>
            participant;

        public string RacerId =>
            participant?.RacerId;

        public bool IsInitialized =>
            participant != null;

        private void Awake()
        {
            if (equipmentLayout == null)
            {
                equipmentLayout =
                    GetComponentInChildren<
                        BikeEquipmentLayoutView>(true);
            }
        }

        public void Initialize(
            RaceParticipant raceParticipant)
        {
            participant =
                raceParticipant ??
                throw new ArgumentNullException(
                    nameof(raceParticipant));

            if (equipmentLayout == null)
            {
                equipmentLayout =
                    GetComponentInChildren<
                        BikeEquipmentLayoutView>(true);
            }

            BindEquipmentMounts();
        }

        private void BindEquipmentMounts()
        {
            equipmentMounts.Clear();

            if (equipmentLayout == null)
            {
                Debug.LogError(
                    $"{nameof(RacerViewController)} on '{name}' " +
                    $"requires a {nameof(BikeEquipmentLayoutView)}.",
                    this);

                return;
            }

            foreach (BikeNode node
                     in participant.Bike.Loadout.Nodes)
            {
                if (!node.IsOccupied)
                    continue;

                if (!equipmentLayout.TryGetMount(
                        node.NodeSize,
                        node.Index,
                        out BikeEquipmentMountBinding mount))
                {
                    Debug.LogWarning(
                        $"Bike '{participant.Bike.BikeDefinitionId}' " +
                        $"has equipment installed in " +
                        $"{node.NodeSize} {node.Index}, but its prefab " +
                        $"does not define that physical mount.",
                        this);

                    continue;
                }

                if (mount.EquipmentOrigin == null)
                {
                    Debug.LogWarning(
                        $"Physical mount {node.NodeSize} {node.Index} " +
                        $"does not have a usable Transform.",
                        this);

                    continue;
                }

                equipmentMounts[
                    node.InstalledEquipment.EquipmentId] =
                        mount;
            }
        }

        public bool TryGetEquipmentMount(
            string equipmentId,
            out BikeEquipmentMountBinding mount)
        {
            if (string.IsNullOrWhiteSpace(
                    equipmentId))
            {
                mount = null;
                return false;
            }

            return equipmentMounts.TryGetValue(
                equipmentId,
                out mount);
        }
    }
}