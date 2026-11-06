using System.Collections.Generic;
using UnityEngine;

namespace NightRush
{
    /// <summary>
    /// Endless road made of recycled 80 m segments. The selected theme decides the surface, terrain,
    /// roadside scenery (city towers, desert dunes and cacti, highway hills and gantries, forest trees),
    /// guardrails, lane lines and street lamps.
    /// </summary>
    public class RoadGenerator : MonoBehaviour
    {
        const float SegLen = 80f;
        const int SegCount = 10;

        Transform player;
        ZoneProfile zone;
        readonly List<Transform> segs = new List<Transform>();
        Material road, terrain, curb, dash, pole, lampHead, building, warmWin, neonA, neonB, decorA, decorB, rail, rockMat;

        public void Build(ZoneProfile z, Transform p)
        {
            zone = z; player = p;
            road = VehicleFactory.Mat(z.surface, z.roadSmooth);
            terrain = VehicleFactory.Mat(z.ground, 0.05f);
            curb = VehicleFactory.Mat(new Color(0.13f, 0.13f, 0.14f), 0.2f);
            dash = VehicleFactory.Mat(new Color(0.9f, 0.9f, 0.8f), 0.3f, z.decor == DecorKind.City ? 0.4f : 0f);
            pole = VehicleFactory.Mat(new Color(0.09f, 0.09f, 0.11f), 0.3f);
            lampHead = VehicleFactory.Mat(z.lamp, 0.5f, 4f);
            building = VehicleFactory.Mat(z.building, 0.25f);
            warmWin = VehicleFactory.Mat(new Color(1f, 0.78f, 0.4f), 0.3f, 2.2f);
            neonA = VehicleFactory.Mat(z.neonA, 0.3f, 2.8f);
            neonB = VehicleFactory.Mat(z.neonB, 0.3f, 2.8f);
            decorA = VehicleFactory.Mat(z.decorA, 0.1f);
            decorB = VehicleFactory.Mat(z.decorB, 0.1f);
            rail = VehicleFactory.Mat(new Color(0.62f, 0.64f, 0.67f), 0.5f);
            rockMat = VehicleFactory.Mat(new Color(0.38f, 0.36f, 0.34f), 0.1f);

            for (int i = 0; i < SegCount; i++) segs.Add(CreateSegment(-2f * SegLen + i * SegLen));
        }

        Transform CreateSegment(float z0)
        {
            var root = new GameObject("Segment").transform;
            root.SetParent(transform, false);
            root.position = new Vector3(0f, 0f, z0);
            float hw = Track.HalfWidth;
            float mid = SegLen * 0.5f;

            VehicleFactory.Box(root, "Asphalt", new Vector3(0f, -0.05f, mid), new Vector3(hw * 2f + 2f, 0.1f, SegLen), road);
            VehicleFactory.Box(root, "TerrainL", new Vector3(-(hw + 61f), -0.12f, mid), new Vector3(120f, 0.2f, SegLen), terrain);
            VehicleFactory.Box(root, "TerrainR", new Vector3(hw + 61f, -0.12f, mid), new Vector3(120f, 0.2f, SegLen), terrain);

            if (zone.curbs)
            {
                VehicleFactory.Box(root, "CurbL", new Vector3(-(hw + 2.5f), 0.1f, mid), new Vector3(3f, 0.3f, SegLen), curb);
                VehicleFactory.Box(root, "CurbR", new Vector3(hw + 2.5f, 0.1f, mid), new Vector3(3f, 0.3f, SegLen), curb);
            }

            if (zone.guardrail)
            {
                VehicleFactory.Box(root, "RailL", new Vector3(-(hw + 1.1f), 0.5f, mid), new Vector3(0.12f, 0.6f, SegLen), rail);
                VehicleFactory.Box(root, "RailR", new Vector3(hw + 1.1f, 0.5f, mid), new Vector3(0.12f, 0.6f, SegLen), rail);
            }

            if (zone.laneLines)
            {
                VehicleFactory.Box(root, "EdgeL", new Vector3(-(hw - 0.25f), 0.01f, mid), new Vector3(0.12f, 0.02f, SegLen), dash);
                VehicleFactory.Box(root, "EdgeR", new Vector3(hw - 0.25f, 0.01f, mid), new Vector3(0.12f, 0.02f, SegLen), dash);
                for (int d = 0; d < Track.Lanes - 1; d++)
                {
                    float x = Track.LaneX(d) + Track.LaneWidth * 0.5f;
                    for (int k = 0; k < 10; k++)
                        VehicleFactory.Box(root, "Dash", new Vector3(x, 0.01f, k * 8f + 4f), new Vector3(0.14f, 0.02f, 3.2f), dash);
                }
            }
            else
            {
                // Off-road: two dark wheel ruts instead of painted lines
                var rut = VehicleFactory.Mat(zone.surface * 0.6f, 0.05f);
                for (int d = 0; d < Track.Lanes; d++)
                {
                    float x = Track.LaneX(d);
                    VehicleFactory.Box(root, "Rut", new Vector3(x - 0.9f, 0.005f, mid), new Vector3(0.5f, 0.01f, SegLen), rut);
                    VehicleFactory.Box(root, "Rut", new Vector3(x + 0.9f, 0.005f, mid), new Vector3(0.5f, 0.01f, SegLen), rut);
                }
            }

            if (zone.lamps)
                for (int k = 0; k < 4; k++)
                    for (int side = -1; side <= 1; side += 2)
                        AddLamp(root, side, k * 20f + 10f, ((k + (side > 0 ? 1 : 0)) % 2) == 0);

            var decor = new GameObject("Decor").transform;
            decor.SetParent(root, false);
            FillDecor(decor);
            return root;
        }

        void AddLamp(Transform root, int side, float z, bool withLight)
        {
            float hw = Track.HalfWidth;
            float px = side * (hw + 1.6f);
            VehicleFactory.Box(root, "Pole", new Vector3(px, 3.5f, z), new Vector3(0.18f, 7f, 0.18f), pole);
            VehicleFactory.Box(root, "Arm", new Vector3(px - side * 0.9f, 7f, z), new Vector3(1.8f, 0.12f, 0.12f), pole);
            VehicleFactory.Box(root, "Head", new Vector3(px - side * 1.7f, 6.92f, z), new Vector3(0.9f, 0.12f, 0.4f), lampHead);
            if (!withLight) return;

            var g = new GameObject("LampLight");
            g.transform.SetParent(root, false);
            g.transform.localPosition = new Vector3(px - side * 1.7f, 6.4f, z);
            var l = g.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = zone.lamp;
            l.range = 34f;
            l.intensity = 3f;
            l.shadows = LightShadows.None;
        }

        void FillDecor(Transform decor)
        {
            switch (zone.decor)
            {
                case DecorKind.Desert: DecorDesert(decor); break;
                case DecorKind.Highway: DecorHighway(decor); break;
                case DecorKind.Forest: DecorForest(decor); break;
                default: DecorCity(decor); break;
            }
        }

        // ---------------------------------------------------------------- city

        void DecorCity(Transform decor)
        {
            float hw = Track.HalfWidth;
            for (int side = -1; side <= 1; side += 2)
            {
                float z = Random.Range(0f, 4f);
                while (true)
                {
                    float w = Random.Range(8f, 16f);
                    if (z + w > SegLen) break;

                    if (Random.value < zone.buildingDensity)
                    {
                        float d = Random.Range(8f, 14f);
                        float h = Random.Range(zone.buildingHeight.x, zone.buildingHeight.y);
                        float xc = side * (hw + 5f + d * 0.5f + Random.Range(0f, 2f));
                        float zc = z + w * 0.5f;
                        VehicleFactory.Box(decor, "Building", new Vector3(xc, h * 0.5f, zc), new Vector3(d, h, w), building);

                        float face = xc - side * d * 0.5f; // wall facing the road
                        int bands = Mathf.Clamp(Mathf.FloorToInt(h / 5f), 1, 4);
                        for (int b = 0; b < bands; b++)
                        {
                            float r = Random.value;
                            Material m = r < 0.4f ? warmWin : (r < 0.7f ? neonA : neonB);
                            float y = 2.5f + b * Mathf.Max(3.5f, (h - 4f) / bands);
                            if (y > h - 1f) break;
                            VehicleFactory.Box(decor, "Window", new Vector3(face - side * 0.06f, y, zc), new Vector3(0.12f, 0.55f, w * 0.85f), m);
                        }
                    }

                    if (Random.value < 0.2f)
                        VehicleFactory.Box(decor, "Neon", new Vector3(side * (hw + 3.2f), Random.Range(3f, 5f), z + w * 0.5f),
                            new Vector3(0.2f, 1.1f, 3f), Random.value < 0.5f ? neonA : neonB);

                    z += w + Random.Range(1f, 4f);
                }
            }
        }

        // ---------------------------------------------------------------- desert

        void DecorDesert(Transform decor)
        {
            float hw = Track.HalfWidth;
            var cactus = VehicleFactory.Mat(new Color(0.15f, 0.4f, 0.18f), 0.1f);
            for (int side = -1; side <= 1; side += 2)
            {
                float z = Random.Range(0f, 8f);
                while (z < SegLen)
                {
                    float r = Random.value;
                    if (r < 0.4f)
                    {
                        float sx = Random.Range(30f, 60f), sy = Random.Range(6f, 14f), sz = Random.Range(30f, 50f);
                        float x = side * (hw + 4f + sx * 0.5f + Random.Range(0f, 15f));
                        VehicleFactory.Shape(PrimitiveType.Sphere, decor, "Dune", new Vector3(x, 0f, z), new Vector3(sx, sy, sz), decorA, Vector3.zero);
                    }
                    else if (r < 0.75f)
                    {
                        float h = Random.Range(2.5f, 5f);
                        float x = side * (hw + Random.Range(4f, 14f));
                        VehicleFactory.Shape(PrimitiveType.Cylinder, decor, "Cactus", new Vector3(x, h * 0.5f, z), new Vector3(0.55f, h * 0.5f, 0.55f), cactus, Vector3.zero);
                        VehicleFactory.Box(decor, "Arm", new Vector3(x + 0.6f, h * 0.6f, z), new Vector3(0.9f, 0.28f, 0.28f), cactus);
                        VehicleFactory.Box(decor, "ArmUp", new Vector3(x + 1.0f, h * 0.8f, z), new Vector3(0.28f, 1.2f, 0.28f), cactus);
                    }
                    else
                    {
                        float x = side * (hw + Random.Range(30f, 70f));
                        float h = Random.Range(10f, 25f);
                        VehicleFactory.Box(decor, "Mesa", new Vector3(x, h * 0.5f, z), new Vector3(Random.Range(8f, 16f), h, Random.Range(8f, 16f)), decorB);
                    }
                    z += Random.Range(12f, 28f);
                }
            }
        }

        // ---------------------------------------------------------------- highway

        void DecorHighway(Transform decor)
        {
            float hw = Track.HalfWidth;
            for (int side = -1; side <= 1; side += 2)
            {
                if (Random.value < 0.8f)
                {
                    float x = side * (hw + Random.Range(70f, 120f));
                    VehicleFactory.Shape(PrimitiveType.Sphere, decor, "Hill", new Vector3(x, 0f, Random.Range(10f, 70f)),
                        new Vector3(Random.Range(70f, 120f), Random.Range(15f, 35f), Random.Range(60f, 90f)), decorA, Vector3.zero);
                }
            }
            if (Random.value < 0.25f)
            {
                float z = Random.Range(10f, 70f);
                VehicleFactory.Box(decor, "GantryL", new Vector3(-(hw + 1.8f), 3.5f, z), new Vector3(0.4f, 7f, 0.4f), rail);
                VehicleFactory.Box(decor, "GantryR", new Vector3(hw + 1.8f, 3.5f, z), new Vector3(0.4f, 7f, 0.4f), rail);
                VehicleFactory.Box(decor, "Sign", new Vector3(0f, 6.4f, z), new Vector3(hw * 2f + 3.6f, 2.2f, 0.3f), decorB);
            }
        }

        // ---------------------------------------------------------------- forest (off-road)

        void DecorForest(Transform decor)
        {
            float hw = Track.HalfWidth;
            for (int side = -1; side <= 1; side += 2)
            {
                float z = Random.Range(0f, 4f);
                while (z < SegLen)
                {
                    if (Random.value < 0.9f)
                    {
                        float h = Random.Range(5f, 9f);
                        float x = side * (hw + Random.Range(3f, 28f));
                        VehicleFactory.Shape(PrimitiveType.Cylinder, decor, "Trunk", new Vector3(x, h * 0.5f, z), new Vector3(0.5f, h * 0.5f, 0.5f), decorA, Vector3.zero);
                        float c = Random.Range(4f, 6f);
                        VehicleFactory.Shape(PrimitiveType.Sphere, decor, "Canopy", new Vector3(x, h + 1f, z), new Vector3(c, c, c), decorB, Vector3.zero);
                    }
                    if (Random.value < 0.25f)
                    {
                        float x = side * (hw + Random.Range(2f, 8f));
                        VehicleFactory.Shape(PrimitiveType.Sphere, decor, "Boulder", new Vector3(x, 0.4f, z + 2f),
                            new Vector3(Random.Range(2f, 5f), Random.Range(1.5f, 3f), Random.Range(2f, 5f)), rockMat, Vector3.zero);
                    }
                    z += Random.Range(5f, 12f);
                }
            }
        }

        void Update()
        {
            if (player == null || segs.Count == 0) return;
            // Recycle the rearmost segment to the front once the player is a full segment past it.
            while (player.position.z - (segs[0].position.z + SegLen) > SegLen)
            {
                var s = segs[0];
                segs.RemoveAt(0);
                s.position += Vector3.forward * (SegLen * SegCount);
                var decor = s.Find("Decor");
                foreach (Transform c in decor) { c.gameObject.SetActive(false); Destroy(c.gameObject); }
                FillDecor(decor);
                segs.Add(s);
            }
        }
    }
}
