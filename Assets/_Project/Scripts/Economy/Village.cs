using UnityEngine;

namespace SporeTribal.Economy
{
    public class Village : MonoBehaviour
    {
        public int Food { get; private set; }

        public void Deposit(int amount) => Food += amount;

        private void OnGUI() => GUI.Label(new Rect(12f, 12f, 220f, 28f), $"Food: {Food}");
    }
}
