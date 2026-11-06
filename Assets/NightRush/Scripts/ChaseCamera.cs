using UnityEngine;

namespace NightRush
{
    /// <summary>Third-person chase camera with speed-based FOV and crash shake.</summary>
    [RequireComponent(typeof(Camera))]
    public class ChaseCamera : MonoBehaviour
    {
        public Vector3 offset = new Vector3(0f, 3.1f, -8f);

        Transform target;
        CarController car;
        Camera cam;
        float smoothYaw, shake;
        Vector3 smoothPos;
        bool snap = true;

        void Awake() { cam = GetComponent<Camera>(); }

        public void SetTarget(Transform t, CarController c) { target = t; car = c; snap = true; }
        public void Shake(float magnitude) { shake = Mathf.Max(shake, magnitude); }

        void LateUpdate()
        {
            if (target == null) return;
            float dt = Time.deltaTime;

            float ty = target.eulerAngles.y;
            if (ty > 180f) ty -= 360f;
            smoothYaw = snap ? ty * 0.6f : Mathf.LerpAngle(smoothYaw, ty * 0.6f, 1f - Mathf.Exp(-4f * dt));

            float s01 = car != null ? car.Speed01 : 0f;
            Quaternion rot = Quaternion.Euler(0f, smoothYaw, 0f);
            Vector3 desired = target.position + rot * (offset + new Vector3(0f, 0f, -s01 * 1.5f));
            smoothPos = snap ? desired : Vector3.Lerp(smoothPos, desired, 1f - Mathf.Exp(-10f * dt));

            Vector3 look = target.position + Vector3.up * 1.3f + rot * Vector3.forward * 8f;
            Vector3 pos = smoothPos;
            if (shake > 0.001f)
            {
                pos += Random.insideUnitSphere * shake * 0.35f;
                shake = Mathf.MoveTowards(shake, 0f, dt * 2.5f);
            }
            transform.position = pos;
            transform.rotation = Quaternion.LookRotation(look - pos);

            float fov = 60f + s01 * 16f + (car != null && car.NitroActive ? 10f : 0f);
            cam.fieldOfView = snap ? fov : Mathf.Lerp(cam.fieldOfView, fov, 1f - Mathf.Exp(-4f * dt));
            snap = false;
        }
    }
}
