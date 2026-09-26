using RaceFatal.Shared;

namespace RaceFatal.Career
{
    public sealed class TeamManagementService
    {
        public const int MaximumNameLength = 40;

        public Result UpdateIdentity(TeamState team, string name, string primary, string secondary, bool repaintBikes)
        {
            if (team == null) return Result.Failure("LOAD A CAMPAIGN FIRST.");
            name = name?.Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length > MaximumNameLength)
                return Result.Failure($"TEAM NAME MUST CONTAIN 1–{MaximumNameLength} CHARACTERS.");
            foreach (char character in name)
                if (char.IsControl(character) || character == '<' || character == '>')
                    return Result.Failure("TEAM NAME CANNOT CONTAIN CONTROL CHARACTERS OR ANGLE BRACKETS.");
            if (!TryNormalizeColor(primary, out primary) || !TryNormalizeColor(secondary, out secondary))
                return Result.Failure("ENTER HEX COLORS, FOR EXAMPLE #33CCFF AND #FF3399.");

            // Validate the entire edit before changing either the team or its bikes.
            team.Rename(name);
            team.SetColors(primary, secondary);
            if (repaintBikes)
                foreach (var bike in team.Garage.Bikes) bike.Paint(primary, secondary);
            return Result.Success();
        }

        public static bool TryNormalizeColor(string value, out string normalized)
        {
            normalized = null;
            if (string.IsNullOrWhiteSpace(value)) return false;
            value = value.Trim();
            if (value.StartsWith("#")) value = value.Substring(1);
            if (value.Length != 3 && value.Length != 4 && value.Length != 6 && value.Length != 8) return false;
            foreach (char character in value)
                if (!((character >= '0' && character <= '9') ||
                      (character >= 'a' && character <= 'f') || (character >= 'A' && character <= 'F'))) return false;
            if (value.Length <= 4)
            {
                var expanded = new System.Text.StringBuilder();
                foreach (char character in value) expanded.Append(character).Append(character);
                value = expanded.ToString();
            }
            normalized = "#" + value.ToUpperInvariant();
            return true;
        }
    }
}
