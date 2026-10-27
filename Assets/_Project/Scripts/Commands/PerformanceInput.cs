using SporeTribal.AI;
using SporeTribal.Definitions;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SporeTribal.Commands
{
    public class PerformanceInput : MonoBehaviour
    {
        [SerializeField] private SocialPerformanceDirector director;

        [Header("Keys")]
        [SerializeField] private Key hornKey = Key.Digit1;
        [SerializeField] private Key maracaKey = Key.Digit2;
        [SerializeField] private Key didgeridooKey = Key.Digit3;
        [Tooltip("Gift food to the rival (hold Shift for the large gift).")]
        [SerializeField] private Key giftKey = Key.G;
        [SerializeField] private Key soothingSongKey = Key.H;

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (director.Current == SocialPerformanceDirector.State.Calling)
            {
                if (keyboard[hornKey].wasPressedThisFrame) SubmitHeld(InstrumentKind.Horn);
                if (keyboard[maracaKey].wasPressedThisFrame) SubmitHeld(InstrumentKind.Maraca);
                if (keyboard[didgeridooKey].wasPressedThisFrame) SubmitHeld(InstrumentKind.Didgeridoo);
            }
            if (keyboard[giftKey].wasPressedThisFrame && !director.Gift(keyboard.shiftKey.isPressed))
                Debug.Log("[Diplomacy] Gift not sent: not enough food or no rival set.");
            if (keyboard[soothingSongKey].wasPressedThisFrame && !director.SingSoothingSong())
                Debug.Log("[Diplomacy] The soothing song needs a living chieftain.");
        }

        private void SubmitHeld(InstrumentKind kind)
        {
            if (director.MatchingCount(kind) > 0) director.Submit(kind);
        }
    }
}
