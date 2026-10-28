using SporeTribal.AI;
using SporeTribal.Definitions;
using SporeTribal.Inputs;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SporeTribal.Commands
{
    public class PerformanceInput : MonoBehaviour
    {
        [SerializeField] private SocialPerformanceDirector director;

        private static TribalControls.DiplomacyActions Actions => GameInput.Controls.Diplomacy;

        private void OnEnable()
        {
            Actions.Horn.performed += OnHorn;
            Actions.Maraca.performed += OnMaraca;
            Actions.Didgeridoo.performed += OnDidgeridoo;
            Actions.Gift.performed += OnGift;
            Actions.SoothingSong.performed += OnSoothingSong;
        }

        private void OnDisable()
        {
            Actions.Horn.performed -= OnHorn;
            Actions.Maraca.performed -= OnMaraca;
            Actions.Didgeridoo.performed -= OnDidgeridoo;
            Actions.Gift.performed -= OnGift;
            Actions.SoothingSong.performed -= OnSoothingSong;
        }

        private void OnHorn(InputAction.CallbackContext _) => Answer(InstrumentKind.Horn);
        private void OnMaraca(InputAction.CallbackContext _) => Answer(InstrumentKind.Maraca);
        private void OnDidgeridoo(InputAction.CallbackContext _) => Answer(InstrumentKind.Didgeridoo);

        private void Answer(InstrumentKind kind)
        {
            if (director.Current != SocialPerformanceDirector.State.Calling) return;
            if (director.MatchingCount(kind) > 0) director.Submit(kind);
        }

        private void OnGift(InputAction.CallbackContext _)
        {
            bool large = Actions.LargeGiftModifier.IsPressed();
            if (!director.Gift(large)) Debug.Log("[Diplomacy] Gift not sent: not enough food or no rival set.");
        }

        private void OnSoothingSong(InputAction.CallbackContext _)
        {
            if (!director.SingSoothingSong()) Debug.Log("[Diplomacy] The soothing song needs a living chieftain.");
        }
    }
}
