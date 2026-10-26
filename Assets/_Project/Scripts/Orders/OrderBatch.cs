using System.Collections.Generic;
using SporeTribal.Villagers;

namespace SporeTribal.Orders
{
    public class OrderBatch
    {
        public readonly struct Entry
        {
            public readonly VillagerUnit Unit;
            public readonly OrderResult Result;

            public Entry(VillagerUnit unit, OrderResult result)
            {
                Unit = unit;
                Result = result;
            }
        }

        public readonly List<Entry> Entries = new List<Entry>();

        public RefusalReason? AllRefusedReason()
        {
            if (Entries.Count == 0) return null;

            var counts = new Dictionary<RefusalReason, int>();
            foreach (Entry entry in Entries)
            {
                if (entry.Result.Accepted) return null;
                counts.TryGetValue(entry.Result.Refusal, out int n);
                counts[entry.Result.Refusal] = n + 1;
            }

            RefusalReason best = RefusalReason.None;
            int bestCount = 0;
            foreach (KeyValuePair<RefusalReason, int> pair in counts)
                if (pair.Value > bestCount) { best = pair.Key; bestCount = pair.Value; }
            return best;
        }
    }
}
