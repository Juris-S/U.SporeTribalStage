using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

namespace SporeTribal.Feedback
{
    public class ClickPing : MonoBehaviour
    {
        [SerializeField] private Material material;
        [SerializeField] private PingStyle style = new PingStyle();

        private readonly List<MeshRenderer> pool = new List<MeshRenderer>();

        public void Play(Vector3 point)
        {
            MeshRenderer ping = Rent();
            ping.transform.position = point + Vector3.up * style.Lift;

            Tween.Scale(ping.transform, style.StartSize, style.EndSize, style.Duration, Ease.OutCubic);
            Tween.Custom(ping, style.Color, new TweenSettings<float>(0f, 1f, style.Duration, Ease.Linear), static (ring, color, k) =>
            {
                color.a *= 1f - k * k;
                GroundRing.SetRim(ring, color);
            }).OnComplete(ping, static ring => ring.gameObject.SetActive(false));
        }

        private MeshRenderer Rent()
        {
            foreach (MeshRenderer ping in pool)
            {
                if (ping == null || ping.gameObject.activeSelf) continue;
                ping.gameObject.SetActive(true);
                return ping;
            }

            MeshRenderer created = GroundRing.Create("ClickPing", material, style.Color);
            pool.Add(created);
            return created;
        }
    }
}
