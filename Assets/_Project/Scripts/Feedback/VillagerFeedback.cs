using PrimeTween;
using UnityEngine;
using UnityEngine.AI;

namespace SporeTribal.Feedback
{
    public sealed class VillagerFeedback
    {
        private struct BounceArgs
        {
            public float Height;
            public bool PlayTool;
            public int Hops;
            public float Waves;
        }

        private struct DanceArgs
        {
            public int Style;
            public float Phase;
            public float Cycles;
            public float Amp;
        }

        private readonly NavMeshAgent agent;
        private readonly Transform transform;
        private readonly System.Action<float> poseTool;
        private readonly System.Action<float> setTilt;
        private readonly Vector3 restScale;
        private readonly float baseOffset;
        private Tween loop;
        private float danceRamp;

        public VillagerFeedback(NavMeshAgent agent, System.Action<float> poseTool, System.Action<float> setTilt)
        {
            this.agent = agent;
            this.poseTool = poseTool;
            this.setTilt = setTilt;
            transform = agent.transform;
            restScale = transform.localScale;
            baseOffset = agent.baseOffset;
        }

        public void Selected() => TweenFx.Selected(transform, restScale);
        public void Happy() => TweenFx.Happy(transform, restScale);
        public void Excited() => TweenFx.Excited(transform, restScale);
        public void Fighting() => TweenFx.Fighting(transform, restScale);
        public bool IsBusy => loop.isAlive;

        public void Bounce(float seconds, float height, bool playTool)
        {
            Stop();
            setTilt(0f);
            int hops = Mathf.Max(3, Mathf.RoundToInt(seconds / 0.5f));
            loop = Tween.Custom(this, new BounceArgs { Height = height, PlayTool = playTool, Hops = hops, Waves = hops * 8f / 3f },
                new TweenSettings<float>(0f, 1f, seconds, Ease.Linear), static (fx, args, n) =>
                {
                    fx.agent.baseOffset = fx.baseOffset + Mathf.Abs(Mathf.Sin(n * Mathf.PI * args.Hops)) * args.Height;
                    if (args.PlayTool) fx.poseTool(Mathf.Sin(n * Mathf.PI * args.Waves) * 18f);
                }).OnComplete(this, static fx => fx.Rest());
        }

        public void Dance(float seconds, bool liked)
        {
            Stop();
            Tween.StopAll(transform);
            int style = liked ? Random.Range(0, 4) : Random.Range(4, 8);
            var args = new DanceArgs
            {
                Style = style,
                Phase = Random.value * Mathf.PI * 2f,
                Cycles = DanceHz(style) * seconds * Mathf.PI * 2f * Random.Range(0.85f, 1.15f),
                Amp = Random.Range(0.8f, 1.2f)
            };
            danceRamp = seconds / 0.3f;
            loop = Tween.Custom(this, args, new TweenSettings<float>(0f, 1f, seconds, Ease.Linear),
                static (fx, a, n) => fx.DanceStep(a, n)).OnComplete(this, static fx =>
                {
                    fx.transform.localScale = fx.restScale;
                    fx.Rest();
                });
        }

        public void Stop()
        {
            if (!loop.isAlive) return;
            loop.Stop();
            transform.localScale = restScale;
            Rest();
        }

        private void Rest()
        {
            if (agent != null) agent.baseOffset = baseOffset;
            poseTool(0f);
            setTilt(0f);
        }

        private static float DanceHz(int style) => style switch
        {
            0 => 1f, 1 => 0.6f, 2 => 1.1f, 3 => 1.8f, 4 => 0f, 5 => 0.35f, 6 => 0.5f, _ => 0.25f
        };

        private void DanceStep(DanceArgs a, float n)
        {
            float t = n * a.Cycles + a.Phase;
            float s = Mathf.Sin(t);
            float env = Mathf.Min(1f, Mathf.Min(n, 1f - n) * danceRamp);
            float ky = 1f;
            float lift = 0f;
            float tilt = 0f;
            switch (a.Style)
            {
                case 0:
                    lift = Mathf.Abs(s) * 0.15f * a.Amp;
                    ky = 1f + 0.06f * Mathf.Abs(s);
                    break;
                case 1:
                    tilt = s * 8f * a.Amp;
                    lift = Mathf.Abs(s) * 0.04f;
                    break;
                case 2:
                    float p = Mathf.Abs(s);
                    ky = 1f - 0.12f * (1f - p) * a.Amp;
                    lift = p * 0.12f;
                    break;
                case 3:
                    tilt = s * 6f * a.Amp;
                    lift = Mathf.Abs(s) * 0.05f;
                    break;
                case 4:
                    ky = 0.86f;
                    break;
                case 5:
                    ky = 0.9f;
                    tilt = s * 4f;
                    break;
                case 6:
                    ky = 0.9f;
                    tilt = Mathf.Pow(Mathf.Max(0f, s), 8f) * 6f * a.Amp;
                    break;
                default:
                    ky = 0.88f + 0.05f * s;
                    break;
            }
            transform.localScale = Vector3.Scale(restScale, new Vector3(1f, Mathf.Lerp(1f, ky, env), 1f));
            setTilt(tilt * env);
            agent.baseOffset = baseOffset + lift * env;
        }
    }
}
