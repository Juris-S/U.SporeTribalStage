using System;

namespace SporeTribal.Utilities
{
    public static class GameSeed
    {
        public static uint Current { get; private set; }

        public static uint Roll() => Current = (uint)Guid.NewGuid().GetHashCode();

        public static void Set(uint seed) => Current = seed;

        public static Mulberry32 Stream(string salt) => new Mulberry32(Current).Fork(salt);
    }
}
