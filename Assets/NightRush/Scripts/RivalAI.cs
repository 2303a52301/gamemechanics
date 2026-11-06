using UnityEngine;

namespace NightRush
{
    /// <summary>Simple lane-based rival: picks the clearest lane, brakes for blockers, uses nitro on open road.</summary>
    public class RivalAI : MonoBehaviour
    {
        CarController car;
        int targetLane;
        float evalTimer, frontClear = 90f;

        void Awake() { car = GetComponent<CarController>(); }

        void Start()
        {
            targetLane = Track.NearestLane(transform.position.x);
            evalTimer = Random.value * 0.3f;
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || !gm.CanDrive) { car.Throttle = 0f; car.Steer = 0f; car.NitroHeld = false; return; }

            evalTimer -= Time.deltaTime;
            if (evalTimer <= 0f) { evalTimer = 0.25f; ChooseLane(gm); }

            Vector3 p = transform.position;
            Vector3 aim = new Vector3(Track.LaneX(targetLane), 0f, p.z + 12f + car.Speed * 0.25f);
            Vector3 dir = aim - p; dir.y = 0f;
            float ang = Vector3.SignedAngle(Vector3.forward, dir, Vector3.up);
            car.Steer = Mathf.Clamp(ang / Mathf.Max(1f, car.YawLimit), -1f, 1f);

            car.Throttle = frontClear < 5f ? -0.6f : (frontClear < 10f ? 0f : 1f);
            car.NitroHeld = frontClear > 70f && car.Nitro01 > 0.4f;
        }

        void ChooseLane(GameManager gm)
        {
            if (gm.Traffic == null) return;
            Vector3 p = transform.position;
            float bestScore = float.NegativeInfinity;
            int best = targetLane;
            float curClear = 90f;

            for (int l = 0; l < Track.Lanes; l++)
            {
                float lx = Track.LaneX(l);
                float clear = 90f;
                foreach (var v in gm.Traffic.Active)
                {
                    if (v == null) continue;
                    Vector3 vp = v.transform.position;
                    float gap = (vp.z - v.length * 0.5f) - (p.z + 2.1f); // nose to their tail
                    if (gap < -(v.length + 4.2f)) continue;              // already behind us
                    if (Mathf.Abs(vp.x - lx) < v.halfWidth + 1.2f) clear = Mathf.Min(clear, Mathf.Max(gap, 0f));
                }
                if (l == targetLane) curClear = clear;
                float score = clear - 6f * Mathf.Abs(l - targetLane) + (l == targetLane ? 6f : 0f) + Random.Range(-3f, 3f);
                if (score > bestScore) { bestScore = score; best = l; }
            }
            targetLane = best;
            frontClear = curClear;
        }
    }
}
