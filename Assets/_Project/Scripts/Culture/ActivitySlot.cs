using UnityEngine;

namespace SporeTribal.Culture
{
    public readonly struct ActivitySlot
    {
        public readonly Vector3 Position;
        public readonly Vector3 Look;

        public ActivitySlot(Vector3 position, Vector3 look)
        {
            Position = position;
            Look = look;
        }
    }
}
