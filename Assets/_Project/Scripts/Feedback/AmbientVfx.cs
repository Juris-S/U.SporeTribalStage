using SporeTribal.CameraControl;
using UnityEngine;

namespace SporeTribal.Feedback
{
    public class AmbientVfx : MonoBehaviour
    {
        [SerializeField] private VfxKey effect = VfxKey.Wind;
        [SerializeField] private RTSCameraController cameraController;
        [Tooltip("Height above the ground point under the camera focus.")]
        [SerializeField] private float height = 1.5f;

        private Transform spawned;

        private void Start()
        {
            GameObject instance = VfxPlayer.Play(effect, Anchor(), 0f);
            if (instance != null) spawned = instance.transform;
        }

        private void LateUpdate()
        {
            if (spawned != null) spawned.position = Anchor();
        }

        private Vector3 Anchor() => cameraController.Focus + Vector3.up * height;
    }
}
