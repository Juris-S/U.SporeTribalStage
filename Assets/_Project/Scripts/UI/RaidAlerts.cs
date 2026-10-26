using System.Collections.Generic;
using SporeTribal.AI;
using SporeTribal.Economy;
using UnityEngine;

namespace SporeTribal.UI
{
    public class RaidAlerts : MonoBehaviour
    {
        [Tooltip("The player's tribe: alerts are about raids on it.")]
        [SerializeField] private Village village;
        [SerializeField] private float showSeconds = 5f;
        [Tooltip("Minimum seconds between two 'under attack' banners, so a long fight doesn't spam them.")]
        [SerializeField] private float underAttackCooldown = 15f;
        [SerializeField] private int fontSize = 20;

        private static readonly string[] Compass = { "east", "north-east", "north", "north-west", "west", "south-west", "south", "south-east" };
        private static readonly Vector2 BannerSize = new Vector2(460f, 40f);

        private readonly List<(string text, float until)> banners = new List<(string, float)>();
        private TribeRaidDirector[] directors;
        private Village[] villages;
        private float nextUnderAttack;
        private GUIStyle style;

        private void OnEnable()
        {
            directors = FindObjectsByType<TribeRaidDirector>();
            foreach (TribeRaidDirector director in directors) director.Alerted += OnRaidAlert;
            villages = FindObjectsByType<Village>();
            foreach (Village v in villages) v.Surrendered += OnSurrendered;
            village.Attacked += OnAttacked;
        }

        private void OnDisable()
        {
            foreach (TribeRaidDirector director in directors) if (director != null) director.Alerted -= OnRaidAlert;
            foreach (Village v in villages) if (v != null) v.Surrendered -= OnSurrendered;
            village.Attacked -= OnAttacked;
        }

        private void OnRaidAlert(Village target, TribeRaidDirector.Alert alert, Vector3 from)
        {
            if (target != village) return;
            switch (alert)
            {
                case TribeRaidDirector.Alert.FoodRaidersComing:
                    Show($"Incoming food raider from the {Direction(from)}!");
                    break;
                case TribeRaidDirector.Alert.RaidersComing:
                    Show($"Raiders are coming from the {Direction(from)}!");
                    break;
                case TribeRaidDirector.Alert.ThiefIntercepted:
                    Show("Food thief stopped, the food is back in store.");
                    break;
            }
        }

        private void OnAttacked(Vector3 where)
        {
            if (Time.time < nextUnderAttack) return;
            nextUnderAttack = Time.time + underAttackCooldown;
            Show("Your tribe is under attack!");
        }

        private void OnSurrendered(Village who) => Show(who == village ? "Your main hut has fallen. Your tribe surrenders." : "The rival tribe has surrendered!");

        private void Show(string text) => banners.Add((text, Time.time + showSeconds));

        private string Direction(Vector3 from)
        {
            Vector3 to = from - village.DropOffCentre;
            float angle = Mathf.Atan2(to.z, to.x) * Mathf.Rad2Deg;
            int index = Mathf.RoundToInt(Mathf.Repeat(angle, 360f) / 45f) % 8;
            return Compass[index];
        }

        private void OnGUI()
        {
            banners.RemoveAll(banner => Time.time > banner.until);
            if (banners.Count == 0) return;
            if (style == null) style = new GUIStyle(GUI.skin.box) { fontSize = fontSize, alignment = TextAnchor.MiddleCenter };

            float y = 12f;
            foreach ((string text, float _) in banners)
            {
                GUI.Box(new Rect((Screen.width - BannerSize.x) * 0.5f, y, BannerSize.x, BannerSize.y), text, style);
                y += BannerSize.y + 6f;
            }
        }
    }
}
