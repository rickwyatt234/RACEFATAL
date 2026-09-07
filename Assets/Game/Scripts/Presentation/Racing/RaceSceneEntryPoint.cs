using RaceFatal.Infrastructure;
using RaceFatal.Presentation.Bootstrap;
using RaceFatal.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Racing
{
    public class RaceSceneEntryPoint :
        MonoBehaviour
    {
        [Header("Scene Assembly")]

        [SerializeField]
        private RaceSceneAssembler assembler;

        [Header("Prototype")]

        [Tooltip(
            "Temporary prototype behavior. " +
            "Later this will be replaced by the " +
            "race countdown.")]
        [SerializeField]
        private bool startImmediately = true;

        private void Awake()
        {
            RaceStartupTrace.Mark(
                "10_Race scene loaded. " +
                "RaceSceneEntryPoint.Awake()",
                this);
        }

        private void Start()
        {
            RaceStartupTrace.Mark(
                "RaceSceneEntryPoint.Start()",
                this);

            // -------------------------------------------------
            // CONTEXT
            // -------------------------------------------------

            GameContext context =
                BootstrapController.Context;

            if (context == null)
            {
                RaceStartupTrace.Fail(
                    "Race scene has no GameContext.",
                    this);

                return;
            }

            RaceStartupTrace.Mark(
                "GameContext survived scene transition.");

            // -------------------------------------------------
            // PENDING RACE
            // -------------------------------------------------

            if (!context.RaceLaunch
                .HasPendingRace)
            {
                RaceStartupTrace.Fail(
                    "RaceLaunchContext contains " +
                    "no pending race.",
                    this);

                return;
            }

            RaceDirector director =
                context.RaceLaunch
                    .ConsumePendingRace();

            if (director == null)
            {
                RaceStartupTrace.Fail(
                    "ConsumePendingRace() returned null.",
                    this);

                return;
            }

            RaceStartupTrace.Mark(
                $"Pending RaceDirector consumed. " +
                $"Participants=" +
                $"{director.State.Participants.Count}.");

            // -------------------------------------------------
            // ASSEMBLER
            // -------------------------------------------------

            if (assembler == null)
            {
                RaceStartupTrace.Fail(
                    "RaceSceneAssembler is not assigned.",
                    this);

                return;
            }

            RaceStartupTrace.Mark(
                "Beginning " +
                "RaceSceneAssembler.Build()...");

            if (!assembler.Build(
                    director))
            {
                RaceStartupTrace.Fail(
                    "RaceSceneAssembler.Build() " +
                    "returned false.",
                    this);

                return;
            }

            RaceStartupTrace.Mark(
                "RaceSceneAssembler.Build() succeeded.");

            // -------------------------------------------------
            // START
            // -------------------------------------------------

            if (!startImmediately)
            {
                RaceStartupTrace.Mark(
                    "Race assembled but waiting " +
                    "for a start command.");

                return;
            }

            RaceStartupTrace.Mark(
                "Calling StartRace()...");

            assembler.StartRace();

            RaceStartupTrace.Mark(
                "Race started.");

            RaceStartupTrace.Complete();
        }
    }
}