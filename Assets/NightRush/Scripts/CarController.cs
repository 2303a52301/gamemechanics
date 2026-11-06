using UnityEngine;

namespace NightRush
{
    /// <summary>
    /// Arcade car physics used by the player and the rivals. Inputs (Throttle, Steer, Handbrake, NitroHeld)
    /// are set from outside by PlayerInput or RivalAI, so the same car code drives everything.
    /// Steering sets a target heading angle; grip decides how fast velocity follows the heading (low grip = drift).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CarController : MonoBehaviour
    {
        [Header("Speed (m/s)")]
        public float maxSpeed = 55f;
        public float nitroMultiplier = 1.3f;
        public float acceleration = 15f;
        public float brakeForce = 35f;
        public float coastDrag = 3f;

        [Header("Steering (degrees of heading)")]
        public float yawLimitLowSpeed = 26f;
        public float yawLimitHighSpeed = 8f;
        public float yawRate = 140f;
        public float driftYawMultiplier = 2.2f;

        [Header("Grip (higher = tighter)")]
        public float grip = 8f;
        public float driftGrip = 1.6f;

        [Header("Nitro")]
        public float nitroDrain = 0.28f;
        public float driftCharge = 0.2f;
        public float passiveNitro = 0f;

        [HideInInspector] public float halfWidth = 0.9f;
        [HideInInspector] public bool isPlayer;

        // Inputs
        [HideInInspector] public float Throttle, Steer;
        [HideInInspector] public bool Handbrake, NitroHeld;
        [HideInInspector] public bool InSlipstream;

        // State for HUD / camera / AI
        public float Speed { get { return speed; } }
        public float Speed01 { get { return Mathf.Clamp01(speed / (maxSpeed * nitroMultiplier)); } }
        public float SpeedKmh { get { return planarSpeed * 3.6f; } }
        public float Nitro01 { get { return nitro; } }
        public float YawLimit { get { return Mathf.Lerp(yawLimitLowSpeed, yawLimitHighSpeed, Mathf.Clamp01(speed / maxSpeed)); } }
        public bool NitroActive { get; private set; }
        public bool Drifting { get; private set; }

        public System.Action<CarController, float> Crashed;

        Rigidbody rb;
        float speed, yaw, planarSpeed, crashCooldownEnd;
        float nitro = 0.4f;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.mass = 1200f;
            rb.useGravity = false; // cars glide on the Y = 0 plane; no wheel colliders needed
            rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            yaw = transform.eulerAngles.y;
        }

        public void AddNitro(float amount) { nitro = Mathf.Clamp01(nitro + amount); }

        static Vector3 GetVel(Rigidbody r)
        {
#if UNITY_6000_0_OR_NEWER
            return r.linearVelocity;
#else
            return r.velocity;
#endif
        }

        static void SetVel(Rigidbody r, Vector3 v)
        {
#if UNITY_6000_0_OR_NEWER
            r.linearVelocity = v;
#else
            r.velocity = v;
#endif
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            var gm = GameManager.I;
            bool drive = gm == null || gm.CanDrive;
            float thr = drive ? Throttle : 0f;
            float steer = drive ? Steer : 0f;

            Vector3 v = GetVel(rb);
            v.y = 0f;
            planarSpeed = v.magnitude;

            NitroActive = drive && NitroHeld && nitro > 0.02f && speed > 5f;
            Drifting = drive && Handbrake && speed > 18f && Mathf.Abs(steer) > 0.2f;
            if (NitroActive) nitro -= nitroDrain * dt;
            if (Drifting) nitro += driftCharge * dt;     // drift to charge boost
            if (InSlipstream) nitro += 0.08f * dt;       // slipstream fills nitro too
            nitro = Mathf.Clamp01(nitro + passiveNitro * dt);

            // Longitudinal speed
            float cap = maxSpeed * (NitroActive ? nitroMultiplier : 1f) + (InSlipstream ? 6f : 0f);
            if (thr > 0.01f)
            {
                if (speed < cap)
                {
                    float a = acceleration * Mathf.Lerp(1.3f, 0.45f, Mathf.Clamp01(speed / maxSpeed)) * (NitroActive ? 2f : 1f);
                    speed = Mathf.MoveTowards(speed, cap, a * thr * dt);
                }
                else speed = Mathf.MoveTowards(speed, cap, 12f * dt);
            }
            else if (thr < -0.01f) speed = Mathf.MoveTowards(speed, 0f, brakeForce * -thr * dt);
            else speed = Mathf.MoveTowards(speed, 0f, coastDrag * dt);
            if (Drifting) speed -= speed * 0.06f * dt;

            // Heading
            float limit = YawLimit * Mathf.Clamp01(speed / 6f) * (Drifting ? driftYawMultiplier : 1f);
            yaw = Mathf.MoveTowardsAngle(yaw, steer * limit, yawRate * dt);

            // Velocity follows heading, with grip deciding how quickly
            Vector3 desired = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * speed;
            float g = Drifting ? driftGrip : grip;
            Vector3 nv = Vector3.Lerp(v, desired, 1f - Mathf.Exp(-g * dt));

            // Road edge walls
            Vector3 pos = rb.position;
            float lim = Track.HalfWidth + 1f;
            if (Mathf.Abs(pos.x) > lim)
            {
                float s = Mathf.Sign(pos.x);
                pos.x = lim * s;
                if (nv.x * s > 0f) nv.x = 0f;
                speed = Mathf.MoveTowards(speed, 0f, 20f * dt);
                rb.position = pos;
            }

            SetVel(rb, nv);
            rb.angularVelocity = Vector3.zero;
            rb.MoveRotation(Quaternion.Euler(0f, yaw, 0f));
        }

        void OnCollisionEnter(Collision c)
        {
            if (Time.time < crashCooldownEnd) return;
            crashCooldownEnd = Time.time + 0.6f;
            speed *= 0.5f;

            var tv = c.collider.GetComponentInParent<TrafficVehicle>();
            if (tv != null) tv.nearMissAwarded = true;

            if (Crashed != null) Crashed(this, c.relativeVelocity.magnitude);
        }

        void OnCollisionStay(Collision c)
        {
            speed = Mathf.MoveTowards(speed, 8f, 45f * Time.fixedDeltaTime);
        }
    }
}
