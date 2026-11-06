using System.Collections.Generic;
using UnityEngine;

namespace NightRush
{
    public enum VehicleType { Car, Sedan, Auto, Bus, Bike, Truck, Rock, Log }

    /// <summary>
    /// Builds every vehicle and prop from primitive cubes so the project runs with zero imported assets.
    /// Swap Create() for prefab instantiation when you have real 3D models.
    /// </summary>
    public static class VehicleFactory
    {
        static Shader shader;
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        public static Material Mat(Color c, float smooth = 0.4f, float emit = 0f)
        {
            string key = ColorUtility.ToHtmlStringRGB(c) + smooth.ToString("F2") + emit.ToString("F2");
            Material cached;
            if (cache.TryGetValue(key, out cached) && cached != null) return cached;

            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                if (shader == null) shader = Shader.Find("Unlit/Color");
            }
            var m = new Material(shader);
            m.color = c;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
            if (emit > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * emit);
            }
            cache[key] = m;
            return m;
        }

        public static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            Object.Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localScale = size;
            g.GetComponent<Renderer>().sharedMaterial = mat;
            return g;
        }

        public static GameObject Shape(PrimitiveType pt, Transform parent, string name, Vector3 pos, Vector3 size, Material mat, Vector3 euler)
        {
            var g = GameObject.CreatePrimitive(pt);
            g.name = name;
            Object.Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localEulerAngles = euler;
            g.transform.localScale = size;
            g.GetComponent<Renderer>().sharedMaterial = mat;
            return g;
        }

        static void Wheels(Transform t, float halfW, float z, float radius, Material m)
        {
            Box(t, "Wheel", new Vector3(halfW, radius, z), new Vector3(0.28f, radius * 2f, radius * 2f), m);
            Box(t, "Wheel", new Vector3(-halfW, radius, z), new Vector3(0.28f, radius * 2f, radius * 2f), m);
        }

        static void Lights(Transform t, float y, float halfSpan, float zFront, float zBack, Material head, Material tail, float s)
        {
            Box(t, "Head", new Vector3(halfSpan, y, zFront), new Vector3(s * 1.4f, s, 0.1f), head);
            Box(t, "Head", new Vector3(-halfSpan, y, zFront), new Vector3(s * 1.4f, s, 0.1f), head);
            Box(t, "Tail", new Vector3(halfSpan, y, zBack), new Vector3(s * 1.4f, s, 0.1f), tail);
            Box(t, "Tail", new Vector3(-halfSpan, y, zBack), new Vector3(s * 1.4f, s, 0.1f), tail);
        }

        /// <summary>Creates a vehicle facing +Z with a box collider. Add your own Rigidbody / scripts.</summary>
        public static GameObject Create(VehicleType type, Color paint, out float halfWidth, out float length)
        {
            var root = new GameObject(type.ToString());
            var t = root.transform;
            var body = Mat(paint, 0.7f);
            var glass = Mat(new Color(0.03f, 0.04f, 0.06f), 0.9f);
            var rubber = Mat(new Color(0.02f, 0.02f, 0.02f), 0.2f);
            var head = Mat(new Color(1f, 0.95f, 0.8f), 0.5f, 3f);
            var tail = Mat(new Color(1f, 0.05f, 0.05f), 0.5f, 3f);

            float w; float l; Vector3 cc; Vector3 cs;

            switch (type)
            {
                case VehicleType.Auto:
                    Box(t, "Body", new Vector3(0f, 0.75f, 0f), new Vector3(1.4f, 0.9f, 2.6f), body);
                    Box(t, "Roof", new Vector3(0f, 1.55f, -0.1f), new Vector3(1.5f, 0.08f, 2.2f), rubber);
                    Wheels(t, 0.7f, -0.9f, 0.3f, rubber);
                    Box(t, "Wheel", new Vector3(0f, 0.3f, 1.0f), new Vector3(0.2f, 0.6f, 0.6f), rubber);
                    Lights(t, 0.7f, 0.45f, 1.3f, -1.3f, head, tail, 0.2f);
                    w = 0.7f; l = 2.6f; cc = new Vector3(0f, 0.8f, 0f); cs = new Vector3(1.4f, 1.6f, 2.6f);
                    break;

                case VehicleType.Bus:
                    Box(t, "Body", new Vector3(0f, 1.65f, 0f), new Vector3(2.5f, 2.5f, 11f), body);
                    Box(t, "Windows", new Vector3(0f, 2.25f, 0f), new Vector3(2.55f, 0.8f, 10.4f), glass);
                    Wheels(t, 1.2f, 3.4f, 0.5f, rubber);
                    Wheels(t, 1.2f, -3.4f, 0.5f, rubber);
                    Lights(t, 0.9f, 0.9f, 5.5f, -5.5f, head, tail, 0.3f);
                    w = 1.25f; l = 11f; cc = new Vector3(0f, 1.75f, 0f); cs = new Vector3(2.5f, 3f, 11f);
                    break;

                case VehicleType.Truck:
                    Box(t, "Cab", new Vector3(0f, 1.55f, 3.2f), new Vector3(2.4f, 2.3f, 2.2f), body);
                    Box(t, "Windshield", new Vector3(0f, 2.0f, 4.3f), new Vector3(2.0f, 0.8f, 0.1f), glass);
                    Box(t, "Trailer", new Vector3(0f, 2.0f, -1.2f), new Vector3(2.5f, 2.9f, 6.6f), Mat(new Color(0.55f, 0.57f, 0.6f), 0.3f));
                    Wheels(t, 1.15f, 3.2f, 0.5f, rubber);
                    Wheels(t, 1.15f, -2.5f, 0.5f, rubber);
                    Wheels(t, 1.15f, -3.9f, 0.5f, rubber);
                    Lights(t, 0.9f, 0.9f, 4.3f, -4.5f, head, tail, 0.3f);
                    w = 1.25f; l = 8.8f; cc = new Vector3(0f, 1.75f, -0.1f); cs = new Vector3(2.5f, 3.5f, 8.8f);
                    break;

                case VehicleType.Rock:
                    Shape(PrimitiveType.Sphere, t, "Rock", new Vector3(0f, 0.7f, 0f), new Vector3(2.2f, 1.6f, 2.2f), Mat(paint, 0.15f), Vector3.zero);
                    Shape(PrimitiveType.Sphere, t, "RockB", new Vector3(0.6f, 0.35f, 0.5f), new Vector3(1.2f, 0.9f, 1.2f), Mat(paint * 0.8f, 0.15f), Vector3.zero);
                    w = 1.1f; l = 2.2f; cc = new Vector3(0f, 0.7f, 0f); cs = new Vector3(2.1f, 1.4f, 2.1f);
                    break;

                case VehicleType.Log:
                    Shape(PrimitiveType.Cylinder, t, "Log", new Vector3(0f, 0.45f, 0f), new Vector3(0.9f, 1.7f, 0.9f), Mat(paint, 0.1f), new Vector3(0f, 0f, 90f));
                    w = 1.7f; l = 1.0f; cc = new Vector3(0f, 0.45f, 0f); cs = new Vector3(3.4f, 0.9f, 0.9f);
                    break;

                case VehicleType.Bike:
                    Box(t, "Body", new Vector3(0f, 0.55f, 0f), new Vector3(0.35f, 0.55f, 1.9f), body);
                    Box(t, "Rider", new Vector3(0f, 1.25f, -0.1f), new Vector3(0.45f, 0.85f, 0.4f), Mat(paint * 0.5f, 0.3f));
                    Box(t, "Helmet", new Vector3(0f, 1.8f, 0f), new Vector3(0.3f, 0.3f, 0.3f), body);
                    Box(t, "Wheel", new Vector3(0f, 0.35f, 0.75f), new Vector3(0.12f, 0.7f, 0.7f), rubber);
                    Box(t, "Wheel", new Vector3(0f, 0.35f, -0.75f), new Vector3(0.12f, 0.7f, 0.7f), rubber);
                    Box(t, "Head", new Vector3(0f, 0.8f, 0.97f), new Vector3(0.25f, 0.2f, 0.1f), head);
                    Box(t, "Tail", new Vector3(0f, 0.8f, -0.97f), new Vector3(0.25f, 0.2f, 0.1f), tail);
                    w = 0.35f; l = 1.9f; cc = new Vector3(0f, 0.9f, 0f); cs = new Vector3(0.7f, 1.8f, 1.9f);
                    break;

                default: // Car (player / rivals, with spoiler) and Sedan (traffic)
                    Box(t, "Body", new Vector3(0f, 0.6f, 0f), new Vector3(1.8f, 0.6f, 4.2f), body);
                    Box(t, "Cabin", new Vector3(0f, 1.12f, -0.15f), new Vector3(1.5f, 0.5f, 2.1f), glass);
                    Wheels(t, 0.85f, 1.3f, 0.33f, rubber);
                    Wheels(t, 0.85f, -1.3f, 0.33f, rubber);
                    Lights(t, 0.7f, 0.6f, 2.1f, -2.1f, head, tail, 0.22f);
                    if (type == VehicleType.Car)
                        Box(t, "Spoiler", new Vector3(0f, 1.15f, -1.95f), new Vector3(1.6f, 0.07f, 0.35f), body);
                    w = 0.9f; l = 4.2f; cc = new Vector3(0f, 0.75f, 0f); cs = new Vector3(1.8f, 1.5f, 4.2f);
                    break;
            }

            var bc = root.AddComponent<BoxCollider>();
            bc.center = cc;
            bc.size = cs;
            halfWidth = w;
            length = l;
            return root;
        }
    }
}
