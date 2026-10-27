using PrimeTween;
using UnityEngine;

namespace SporeTribal.Feedback
{
    public static class TweenFx
    {
        public static Tween Selected(Transform target, Vector3 rest) =>
            Punch(target, rest, Vector3.one * 0.15f, 0.25f, 8f);

        public static Tween Happy(Transform target, Vector3 rest) =>
            Punch(target, rest, new Vector3(0f, 0.12f, 0f), 0.4f, 3f);

        public static Tween Excited(Transform target, Vector3 rest) =>
            Punch(target, rest, new Vector3(0f, 0.15f, 0f), 0.5f, 4f);

        public static Tween Fighting(Transform target, Vector3 rest) =>
            Punch(target, rest, new Vector3(0.2f, -0.15f, 0.2f), 0.15f, 6f);

        public static Tween Idle(Transform target, Vector3 rest, float breath = 0.03f)
        {
            Rest(target, rest);
            return Tween.Scale(target, rest * (1f + breath), 1.2f, Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Yoyo);
        }

        public static void Rest(Transform target, Vector3 rest)
        {
            Tween.StopAll(target);
            target.localScale = rest;
        }

        private static Tween Punch(Transform target, Vector3 rest, Vector3 strength, float duration, float frequency)
        {
            Rest(target, rest);
            return Tween.PunchScale(target, strength, duration, frequency);
        }
    }
}
