using PrimeTween;
using UnityEngine;
using UnityEngine.AI;

namespace SporeTribal.Feedback
{
    public sealed class VillagerFeedback
    {
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
        private Tween travel;
        private float danceRamp;
        private float shoved;
        private Vector3 shoveDirection;
        private float shoveDistance;
        private float shoveTilt;
        private Vector3 circleCentre;
        private Vector3 circleOffset;
        private float circlePulse;
        private float circleBeats;

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

        public void Shove(Vector3 direction, float distance, float seconds, float tilt)
        {
            Stop();
            direction.y = 0f;
            shoveDirection = direction.normalized;
            shoveDistance = distance;
            shoveTilt = tilt;
            shoved = 0f;
            loop = Tween.Custom(this, 0f, 1f, seconds, static (fx, n) =>
            {
                float arc = Mathf.Sin(n * Mathf.PI);
                float reach = arc * fx.shoveDistance;
                if (fx.agent.enabled) fx.agent.Move(fx.shoveDirection * (reach - fx.shoved));
                fx.shoved = reach;
                fx.setTilt(arc * fx.shoveTilt);
                fx.agent.baseOffset = fx.baseOffset + 0.5f * fx.restScale.x * Mathf.Abs(Mathf.Sin(arc * fx.shoveTilt * Mathf.Deg2Rad));
            }, Ease.Linear).OnComplete(this, static fx => fx.Rest());
        }

        public void Circle(Vector3 centre, float seconds, float pulse, float beats)
        {
            if (travel.isAlive) travel.Stop();
            circleCentre = centre;
            circleOffset = transform.position - centre;
            circleOffset.y = 0f;
            circlePulse = pulse;
            circleBeats = beats;
            travel = Tween.Custom(this, 0f, 1f, seconds, static (fx, n) =>
            {
                float env = Mathf.Min(1f, Mathf.Min(n, 1f - n) * 4f);
                float radius = 1f + fx.circlePulse * env * Mathf.Sin(n * fx.circleBeats * Mathf.PI * 2f);
                Vector3 at = fx.circleCentre + fx.circleOffset * radius;
                Vector3 step = at - fx.transform.position;
                step.y = 0f;
                if (fx.agent.enabled) fx.agent.Move(step);
            }, Ease.Linear);
        }

        public void Stop()
        {
            if (travel.isAlive) travel.Stop();
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
