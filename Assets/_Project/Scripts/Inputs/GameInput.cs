using SporeTribal.Definitions;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SporeTribal.Inputs
{
    public static class GameInput
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static readonly bool DevToolsAvailable = true;
#else
        public static readonly bool DevToolsAvailable = false;
#endif

        private static TribalControls controls;

        public static TribalControls Controls
        {
            get
            {
                if (controls == null) Create();
                return controls;
            }
        }

        public static bool IsLocked { get; private set; }

        public static InputAction InstrumentAction(InstrumentKind kind) => kind switch
        {
            InstrumentKind.Horn => Controls.Diplomacy.Horn,
            InstrumentKind.Maraca => Controls.Diplomacy.Maraca,
            InstrumentKind.Didgeridoo => Controls.Diplomacy.Didgeridoo,
            _ => null
        };

        public static void Lock()
        {
            IsLocked = true;
            Controls.Disable();
        }

        public static void Unlock()
        {
            IsLocked = false;
            EnableMaps();
        }

        private static void Create()
        {
            controls = new TribalControls();
            EnableMaps();
            Application.quitting += Shutdown;
        }

        private static void EnableMaps()
        {
            Controls.Camera.Enable();
            Controls.Diplomacy.Enable();
            Controls.Interface.Enable();
            if (DevToolsAvailable) Controls.DevTools.Enable();
        }

        private static void Shutdown()
        {
            Application.quitting -= Shutdown;
            if (controls == null) return;
            controls.Disable();
            controls.Dispose();
            ResetStatics();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            controls = null;
            IsLocked = false;
        }
    }
}
