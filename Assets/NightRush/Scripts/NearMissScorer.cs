using UnityEngine;

namespace NightRush
{
    /// <summary>
    /// Player-only. Awards near-miss combos for skimming past traffic without touching it,
    /// and detects slipstream behind trucks (deck: "Risk and reward").
    /// </summary>
    public class NearMissScorer : MonoBehaviour
    {
        CarController car;

        void Awake() { car = GetComponent<CarController>(); }

        void FixedUpdate()
        {
            var gm = GameManager.I;
            if (gm == null || !gm.CanDrive || gm.Traffic == null) { car.InSlipstream = false; return; }

            bool slip = false;
            foreach (var v in gm.Traffic.Active)
            {
                if (v == null) continue;
                Vector3 rel = transform.InverseTransformPoint(v.transform.position);
                if (rel.z > 25f) v.nearMissAwarded = false; // not passed yet

                float gap = Mathf.Abs(rel.x) - (car.halfWidth + v.halfWidth);
                float reach = v.length * 0.5f + 2.1f;
                if (!v.nearMissAwarded && car.Speed > 22f && Mathf.Abs(rel.z) < reach)
                {
                    if (gap <= 0.05f) v.nearMissAwarded = true;           // contact: no reward
                    else if (gap < 1.1f) { v.nearMissAwarded = true; gm.RegisterNearMiss(); }
                }

                if (v.providesSlipstream && rel.z > v.length * 0.5f + 1f && rel.z < v.length * 0.5f + 26f && Mathf.Abs(rel.x) < 2f)
                    slip = true;
            }
            car.InSlipstream = slip;
        }
    }
}
