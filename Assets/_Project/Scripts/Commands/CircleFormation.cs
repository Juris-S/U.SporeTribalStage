using UnityEngine;

namespace SporeTribal.Commands
{
    public static class CircleFormation
    {
        public static Vector3[] GetSlots(Vector3 center, int count, float spacing)
        {
            var slots = new Vector3[count];

            float radius = count < 2 ? 0f : spacing / (2f * Mathf.Sin(Mathf.PI / count));

            for (int i = 0; i < count; i++)
            {
                float angle = 2f * Mathf.PI * i / count;
                slots[i] = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            }

            return slots;
        }
    }
}
