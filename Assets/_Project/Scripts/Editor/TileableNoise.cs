using UnityEngine;

namespace SporeTribal.EditorTools
{
    public static class TileableNoise
    {
        public static float Value(float x, float y, int period, int octave, int salt = 0)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);

            float a = Hash(x0, y0, period, octave, salt);
            float b = Hash(x0 + 1, y0, period, octave, salt);
            float c = Hash(x0, y0 + 1, period, octave, salt);
            float d = Hash(x0 + 1, y0 + 1, period, octave, salt);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        public static float Hash(int x, int y, int period, int octave, int salt = 0)
        {
            x = ((x % period) + period) % period;
            y = ((y % period) + period) % period;
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + octave * 2147483647 + salt * 144665);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }
    }
}
