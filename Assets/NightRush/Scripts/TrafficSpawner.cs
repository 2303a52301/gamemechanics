using System.Collections.Generic;
using UnityEngine;

namespace NightRush
{
    /// <summary>Keeps traffic (and static obstacles) ahead of the player, using the selected theme's vehicle mix.</summary>
    public class TrafficSpawner : MonoBehaviour
    {
        public readonly List<TrafficVehicle> Active = new List<TrafficVehicle>();
        public Transform player;
        public float density = 1f;      // 0..1, set by GameManager (night curve in Traffic City)
        public float multiplier = 1f;   // per theme

        const float SpawnMin = 140f, SpawnMax = 260f, DespawnBehind = 50f, DespawnAhead = 340f;
        float spawnTimer;
        ZoneProfile zone;

        static readonly VehicleType[] MixOrder =
        {
            VehicleType.Sedan, VehicleType.Auto, VehicleType.Bike, VehicleType.Bus, VehicleType.Truck, VehicleType.Rock, VehicleType.Log
        };

        static readonly Color[] Paints =
        {
            new Color(0.85f, 0.85f, 0.88f), new Color(0.6f, 0.62f, 0.66f), new Color(0.05f, 0.05f, 0.07f),
            new Color(0.1f, 0.25f, 0.7f), new Color(0.7f, 0.1f, 0.1f), new Color(0.05f, 0.5f, 0.5f), new Color(0.9f, 0.45f, 0.05f)
        };

        public void Begin(Transform p, ZoneProfile z)
        {
            player = p;
            zone = z;
            multiplier = z.trafficMultiplier;
            int tries = 0;
            while (Active.Count < TargetCount() && tries < 500) { tries++; TrySpawn(60f, SpawnMax); }
        }

        int TargetCount() { return Mathf.RoundToInt(Mathf.Lerp(5f, 40f, Mathf.Clamp01(density * multiplier))); }

        void Update()
        {
            if (player == null) return;

            for (int i = Active.Count - 1; i >= 0; i--)
            {
                var v = Active[i];
                if (v == null) { Active.RemoveAt(i); continue; }
                float dz = v.transform.position.z - player.position.z;
                if (dz < -DespawnBehind || dz > DespawnAhead) { Destroy(v.gameObject); Active.RemoveAt(i); }
            }

            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f && Active.Count < TargetCount())
            {
                spawnTimer = 0.15f;
                TrySpawn(SpawnMin, SpawnMax);
            }
        }

        bool TrySpawn(float zMin, float zMax)
        {
            VehicleType t = PickType();
            int lane = Random.Range(0, Track.Lanes);
            float z = player.position.z + Random.Range(zMin, zMax);
            if (!IsLaneFree(lane, z, ApproxLength(t) + 8f, null)) return false;

            float hw, len;
            var go = VehicleFactory.Create(t, PickColor(t), out hw, out len);
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(Track.LaneX(lane), 0f, z);
            var tv = go.AddComponent<TrafficVehicle>(); // adds its Rigidbody automatically
            tv.Init(t, lane, RandomSpeed(t), hw, len);
            Active.Add(tv);
            return true;
        }

        /// <summary>True if no traffic vehicle or obstacle occupies that lane around z.</summary>
        public bool IsLaneFree(int lane, float z, float clearLength, TrafficVehicle self)
        {
            float lx = Track.LaneX(lane);
            foreach (var v in Active)
            {
                if (v == null || v == self) continue;
                Vector3 p = v.transform.position;
                if (Mathf.Abs(p.x - lx) < 2.2f && Mathf.Abs(p.z - z) < (clearLength + v.length) * 0.5f + 4f) return false;
            }
            return true;
        }

        VehicleType PickType()
        {
            float total = 0f;
            for (int i = 0; i < zone.mix.Length; i++) total += zone.mix[i];
            float r = Random.value * total;
            for (int i = 0; i < zone.mix.Length; i++)
            {
                r -= zone.mix[i];
                if (r <= 0f) return MixOrder[i];
            }
            return VehicleType.Sedan;
        }

        static float ApproxLength(VehicleType t)
        {
            switch (t)
            {
                case VehicleType.Auto: return 2.6f;
                case VehicleType.Bike: return 1.9f;
                case VehicleType.Bus: return 11f;
                case VehicleType.Truck: return 8.8f;
                case VehicleType.Rock: return 2.2f;
                case VehicleType.Log: return 1.2f;
                default: return 4.2f;
            }
        }

        float RandomSpeed(VehicleType t)
        {
            float s;
            switch (t)
            {
                case VehicleType.Auto: s = Random.Range(11f, 15f); break;
                case VehicleType.Bike: s = Random.Range(17f, 25f); break;
                case VehicleType.Bus: s = Random.Range(13f, 17f); break;
                case VehicleType.Truck: s = Random.Range(13f, 17f); break;
                case VehicleType.Rock: return 0f;
                case VehicleType.Log: return 0f;
                default: s = Random.Range(18f, 28f); break;
            }
            return s * zone.trafficSpeed;
        }

        static Color PickColor(VehicleType t)
        {
            switch (t)
            {
                case VehicleType.Auto: return Random.value < 0.6f ? new Color(1f, 0.78f, 0.05f) : new Color(0.1f, 0.55f, 0.2f);
                case VehicleType.Bus: return new Color(0.75f, 0.08f, 0.08f);
                case VehicleType.Rock: return new Color(0.38f, 0.36f, 0.34f);
                case VehicleType.Log: return new Color(0.32f, 0.2f, 0.1f);
                default: return Paints[Random.Range(0, Paints.Length)];
            }
        }
    }
}
