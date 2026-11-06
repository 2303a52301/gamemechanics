using UnityEngine;

namespace NightRush
{
    /// <summary>
    /// Moving traffic obstacle. Each type has its own behaviour from the deck's Traffic System slide:
    /// autos cut lanes suddenly, buses are slow and wide, bikes weave, trucks block but give slipstream.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class TrafficVehicle : MonoBehaviour
    {
        public VehicleType type;
        public float speed;
        public float halfWidth, length;
        public bool providesSlipstream;
        [HideInInspector] public bool nearMissAwarded;

        Rigidbody rb;
        int targetLane;
        float baseX, cruise, laneChangeSpeed = 3f, changeChance, weaveAmp, weavePhase, decideTimer;

        public int Lane { get { return targetLane; } }

        public void Init(VehicleType t, int lane, float spd, float hw, float len)
        {
            type = t; cruise = spd; speed = spd; halfWidth = hw; length = len;
            targetLane = lane;
            baseX = Track.LaneX(lane);

            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            if (t == VehicleType.Rock || t == VehicleType.Log) { cruise = 0f; speed = 0f; }

            switch (t)
            {
                case VehicleType.Auto:  changeChance = 0.6f; laneChangeSpeed = 5f; break;       // sudden lane cuts
                case VehicleType.Bike:  changeChance = 0.4f; laneChangeSpeed = 4f; weaveAmp = 1.0f; break; // weaves
                case VehicleType.Sedan: changeChance = 0.25f; laneChangeSpeed = 2.5f; break;
                case VehicleType.Truck: providesSlipstream = true; break;
                default: break; // Bus: slow and wide, never changes lane
            }
            weavePhase = Random.value * 6.28f;
            decideTimer = Random.Range(1f, 4f);
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (type == VehicleType.Rock || type == VehicleType.Log) return; // static obstacles

            decideTimer -= dt;
            if (decideTimer <= 0f) { decideTimer = Random.Range(2f, 5f); TryChangeLane(); }

            FollowLeader(dt);

            baseX = Mathf.MoveTowards(baseX, Track.LaneX(targetLane), laneChangeSpeed * dt);
            float off = weaveAmp > 0f ? Mathf.Sin(Time.time * 1.8f + weavePhase) * weaveAmp : 0f;

            Vector3 p = rb.position;
            float newX = baseX + off;
            float vx = (newX - p.x) / dt;
            p.x = newX;
            p.z += speed * dt;
            rb.MovePosition(p);

            float yaw = Mathf.Clamp(Mathf.Atan2(vx, Mathf.Max(speed, 1f)) * Mathf.Rad2Deg, -25f, 25f);
            rb.MoveRotation(Quaternion.Euler(0f, yaw, 0f));
        }

        void TryChangeLane()
        {
            if (Random.value > changeChance) return;
            int nl = targetLane + (Random.value < 0.5f ? -1 : 1);
            if (nl < 0 || nl >= Track.Lanes) return;
            var gm = GameManager.I;
            if (gm == null || gm.Traffic == null) return;
            if (gm.Traffic.IsLaneFree(nl, transform.position.z, length + 6f, this)) targetLane = nl;
        }

        // Keep a gap to whatever is ahead in the same lane so vehicles never drive through each other.
        void FollowLeader(float dt)
        {
            var gm = GameManager.I;
            float limit = cruise;
            if (gm != null && gm.Traffic != null)
            {
                Vector3 p = transform.position;
                foreach (var o in gm.Traffic.Active)
                {
                    if (o == null || o == this || o.type == VehicleType.Rock || o.type == VehicleType.Log) continue;
                    Vector3 op = o.transform.position;
                    float dz = op.z - p.z;
                    if (dz <= 0f || Mathf.Abs(op.x - p.x) > 2f) continue;
                    if (dz < (length + o.length) * 0.5f + 7f) limit = Mathf.Min(limit, o.speed);
                }
            }
            speed = Mathf.MoveTowards(speed, limit, (limit < speed ? 8f : 3f) * dt);
        }
    }
}
