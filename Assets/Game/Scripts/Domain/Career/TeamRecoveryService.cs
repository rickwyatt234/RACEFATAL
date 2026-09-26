using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RaceFatal.Data;
using RaceFatal.Equipment;
using RaceFatal.Shared;
using RaceFatal.Vehicles;

namespace RaceFatal.Career
{
    public sealed class TeamRecoveryQuote
    {
        public string PlayerBikeId { get; internal set; }
        public string PartnerBikeId { get; internal set; }
        public string PartnerRacerId { get; internal set; }
        public string PartnerDefinitionId { get; internal set; }
        public string StarterBuildId { get; internal set; }
        public int NewBikes { get; internal set; }
        public int TotalCost { get; internal set; }
        public int Charge { get; internal set; }
        public int Assistance => TotalCost - Charge;
        public bool RecruitPartner => PartnerRacerId == null;
        public string Signature { get; internal set; }
        public string SetupSummary { get; internal set; }
        public string Description => $"{SetupSummary}\n\nREADY BIKES TO SUPPLY  {NewBikes}\nPARTNER  {(RecruitPartner ? "NEW REPLACEMENT RACER" : "EXISTING TEAM RACER")}\n\nREPLACEMENT COST  {TotalCost:N0} CR\nTEAM PAYS  {Charge:N0} CR\nRECOVERY ASSISTANCE  {Assistance:N0} CR\n\nReady owned bikes and reserve racers are used first. New bikes use your campaign's starter build. Wrecks and deceased racers remain permanent.\n\nAssistance is available once per completed race. Recovery does not award research, fame or race rewards.";
    }

    public sealed class TeamRecoveryService
    {
        private readonly GameDatabase database;
        public TeamRecoveryService(GameDatabase database) { this.database = database ?? throw new ArgumentNullException(nameof(database)); }
        private static string Checkpoint(TeamState team) => team.SettledRaceResults.Count.ToString(CultureInfo.InvariantCulture);

        public Result<TeamRecoveryQuote> Quote(GameSessionState session)
        {
            if (session?.CareerRun?.IsActive != true) return Fail("CREATE A NEW PLAYER RACER FROM CAREER HOME FIRST.");
            var team = session.PlayerTeam;
            if (!string.IsNullOrEmpty(team.Calendar.Active?.pendingInstanceId))
                return Fail("FINISH OR WITHDRAW FROM THE PAID EVENT BEFORE RECOVERY.");
            var selectedPlayer = team.Garage.FindBike(session.SelectedPlayerBikeId);
            var selectedPartner = team.Garage.FindBike(session.SelectedPartnerBikeId);
            var racer = team.Roster.FindRacer(session.SelectedPartnerRacerId);
            if (selectedPlayer?.IsRaceReady == true && selectedPartner?.IsRaceReady == true &&
                selectedPlayer.BikeId != selectedPartner.BikeId && selectedPlayer.EngineClass == selectedPartner.EngineClass &&
                racer?.CanRace == true && !racer.IsPlayerCharacter)
                return Fail("YOUR TEAM ALREADY HAS AN ACTIVE PARTNER AND TWO COMPATIBLE READY BIKES.");

            var quote = new TeamRecoveryQuote { StarterBuildId = session.SuccessorStarterBuildId };
            var ready = team.Garage.Bikes.Where(b => b.IsRaceReady)
                .OrderByDescending(b => b.BikeId == session.SelectedPlayerBikeId)
                .ThenByDescending(b => b.BikeId == session.SelectedPartnerBikeId).ThenBy(b => b.BikeId, StringComparer.Ordinal).ToList();
            var existingPair = ready.GroupBy(b => b.EngineClass).FirstOrDefault(g => g.Count() >= 2);
            var build = database.GetBikeBuildDefinition(quote.StarterBuildId);
            List<BikeState> chosen;
            if (existingPair != null) chosen = existingPair.Take(2).ToList();
            else
            {
                if (build == null || build.CreditCost < 0 || build.Equipment.Any(m => m == null)) return Fail("CONFIGURE A VALID STARTER BUILD AND RECOVERY PRICE.");
                var engine = database.GetEngineDefinition(build.EngineDefinitionId);
                if (engine == null) return Fail("STARTER ENGINE IS MISSING.");
                chosen = ready.Where(b => b.EngineClass == engine.EngineClass).Take(2).ToList();
                quote.NewBikes = 2 - chosen.Count;
                var test = new BikeBuildFactory(database, new VehicleFactory(), new EquipmentFactory())
                    .CreateInGarage(build, new GarageState(), team.PrimaryColor, team.SecondaryColor);
                if (!test.IsSuccess) return Fail(test.ErrorMessage);
            }
            quote.PlayerBikeId = chosen.Count > 0 ? chosen[0].BikeId : null;
            quote.PartnerBikeId = chosen.Count > 1 ? chosen[1].BikeId : null;
            var reserve = team.Roster.Racers.Where(r => r.CanRace && !r.IsPlayerCharacter)
                .OrderByDescending(r => r.RacerId == session.SelectedPartnerRacerId).ThenBy(r => r.RacerId, StringComparer.Ordinal).FirstOrDefault();
            quote.PartnerRacerId = reserve?.RacerId;
            if (reserve == null)
            {
                string templateId = team.Roster.FindRacer(session.DefaultPartnerRacerId)?.DefinitionId;
                var template = string.IsNullOrEmpty(templateId) ? null : database.GetRacerDefinition(templateId);
                if (template == null) template = database.RacerDefinitions.Values.OrderBy(r => r.Id, StringComparer.Ordinal).FirstOrDefault();
                if (template == null) return Fail("NO PARTNER RACER CONTENT IS CONFIGURED.");
                quote.PartnerDefinitionId = template.Id;
            }
            long cost = (long)quote.NewBikes * (build?.CreditCost ?? 0) + (reserve == null ? RosterService.RecruitmentCost : 0);
            if (cost > int.MaxValue) return Fail("RECOVERY PRICE IS TOO LARGE.");
            quote.TotalCost = (int)cost;
            quote.Charge = Math.Min(team.Credits, quote.TotalCost);
            if (quote.Assistance > 0 && team.RecoverySupportCheckpoint == Checkpoint(team))
                return Fail("RECOVERY ASSISTANCE ALREADY USED. COMPLETE A RACE BEFORE CLAIMING AGAIN, OR USE SHOP AND GARAGE.");
            string playerBikeName = chosen.Count > 0
                ? database.GetBikeDefinition(chosen[0].BikeDefinitionId)?.DisplayName : build?.DisplayName;
            string partnerBikeName = chosen.Count > 1
                ? database.GetBikeDefinition(chosen[1].BikeDefinitionId)?.DisplayName : build?.DisplayName;
            var setupClass = chosen.Count > 0 ? chosen[0].EngineClass : database.GetEngineDefinition(build.EngineDefinitionId)?.EngineClass;
            string partnerName = reserve?.Name ?? database.GetRacerDefinition(quote.PartnerDefinitionId)?.DisplayName;
            quote.SetupSummary = $"PLAYER BIKE  {playerBikeName}\nPARTNER BIKE  {partnerBikeName}\nENGINE CLASS  {setupClass}\nPARTNER  {partnerName}{(reserve == null ? " (NEW IDENTITY)" : "")}";
            quote.Signature = string.Join("|", team.TeamId, session.CareerRun.RunId, team.Credits, Checkpoint(team),
                quote.PlayerBikeId, quote.PartnerBikeId, quote.PartnerRacerId, quote.PartnerDefinitionId,
                quote.StarterBuildId, quote.NewBikes, quote.TotalCost, quote.Charge);
            return Result<TeamRecoveryQuote>.Success(quote);
        }

        public Result Recover(GameSessionState session, string confirmedSignature)
        {
            var quoted = Quote(session);
            if (!quoted.IsSuccess) return Result.Failure(quoted.ErrorMessage);
            var quote = quoted.Value;
            if (confirmedSignature != quote.Signature) return Result.Failure("RECOVERY PLAN CHANGED. REVIEW IT AGAIN.");
            var team = session.PlayerTeam;
            var kit = new GarageState();
            var factory = new BikeBuildFactory(database, new VehicleFactory(), new EquipmentFactory());
            for (int i = 0; i < quote.NewBikes; i++)
            {
                var created = factory.CreateInGarage(database.GetBikeBuildDefinition(quote.StarterBuildId), kit, team.PrimaryColor, team.SecondaryColor);
                if (!created.IsSuccess) return Result.Failure(created.ErrorMessage);
                if (quote.PlayerBikeId == null) quote.PlayerBikeId = created.Value.BikeId;
                else quote.PartnerBikeId = created.Value.BikeId;
            }
            if (!team.TrySpendCredits(quote.Charge)) return Result.Failure("CREDITS CHANGED. REVIEW RECOVERY AGAIN.");
            var imported = team.Garage.ImportKit(kit);
            if (!imported.IsSuccess) { team.AddCredits(quote.Charge); return imported; }
            if (quote.RecruitPartner)
            {
                var template = database.GetRacerDefinition(quote.PartnerDefinitionId);
                var rookie = new RacerState(Guid.NewGuid().ToString("N"), template.DisplayName + " / Reserve " + (team.Roster.Racers.Count + 1),
                    team.TeamId, false, template.Id);
                team.Roster.AddRacer(rookie);
                quote.PartnerRacerId = rookie.RacerId;
            }
            var assigned = session.AssignBikePair(quote.PlayerBikeId, quote.PartnerBikeId);
            if (!assigned.IsSuccess) return assigned;
            assigned = session.SelectPartnerRacer(quote.PartnerRacerId);
            if (!assigned.IsSuccess) return assigned;
            if (quote.Assistance > 0) team.RestoreRecoverySupport(Checkpoint(team));
            return Result.Success();
        }
        private static Result<TeamRecoveryQuote> Fail(string message) => Result<TeamRecoveryQuote>.Failure(message);
    }
}
