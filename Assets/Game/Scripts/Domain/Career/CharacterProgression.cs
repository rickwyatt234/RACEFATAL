using System.Collections.Generic;

namespace RaceFatal.Career
{
    public class CharacterProgression
    {
        private readonly HashSet<string> puchasedPerkIds = new HashSet<string>();
        public int Fame { get; private set; }
        public IReadOnlyCollection<string> PurchasedPerkIds =>
            puchasedPerkIds;
        
        public void AddFame(int amount)
        {
            if (amount > 0)
                Fame += amount;
        }

        public bool TryPurchasePerk(string perkId, int fameCost)
        {
            if (Fame < fameCost)
            {
                return false;
            }
            if (puchasedPerkIds.Contains(perkId))
            {
                return false;
            }

            Fame -= fameCost;
            puchasedPerkIds.Add(perkId);
            return true;
        }

        public bool HasPurchasedPerk(string perkId)
        {
            return puchasedPerkIds.Contains(perkId);
        }
    }
}
