using System.Collections.Generic;

namespace SporeTribal.Utilities
{
    public sealed class Mulberry32
    {
        private uint state;

        public uint Seed { get; }

        public Mulberry32(uint seed)
        {
            Seed = seed;
            state = seed;
        }

        public uint NextUInt()
        {
            state += 0x6D2B79F5u;
            uint t = state;
            t = (t ^ (t >> 15)) * (t | 1u);
            t ^= t + (t ^ (t >> 7)) * (t | 61u);
            return t ^ (t >> 14);
        }

        public float Value() => (NextUInt() >> 8) * (1f / 16777216f);

        public float Range(float min, float max) => min + (max - min) * Value();

        public int Range(int minInclusive, int maxExclusive) =>
            minInclusive + (int)(NextUInt() % (uint)(maxExclusive - minInclusive));

        public T Pick<T>(IReadOnlyList<T> items) => items[Range(0, items.Count)];

        public Mulberry32 Fork(string salt)
        {
            uint hash = 2166136261u ^ Seed;
            foreach (char c in salt) hash = (hash ^ c) * 16777619u;
            return new Mulberry32(hash);
        }
    }
}
