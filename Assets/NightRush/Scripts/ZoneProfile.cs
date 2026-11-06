using UnityEngine;

namespace NightRush
{
    public enum DecorKind { City, Desert, Highway, Forest }

    /// <summary>One selectable theme: look, lighting, road style, traffic mix and handling.</summary>
    public class ZoneProfile
    {
        public string name, blurb;
        public DecorKind decor;
        public int lanes;

        // Look
        public Color fog;                 // horizon / sky / fog colour
        public Color ambient, sun, surface, ground, decorA, decorB;
        public Color lamp, neonA, neonB, building;
        public float sunIntensity, sunAngle, fogDensity, roadSmooth;
        public bool lamps, curbs, guardrail, laneLines;
        public Vector2 buildingHeight;
        public float buildingDensity;

        // Traffic: weights for Sedan, Auto, Bike, Bus, Truck, Rock, Log
        public float[] mix;
        public float trafficMultiplier = 1f;
        public float trafficSpeed = 1f;
        public bool nightCurve;           // thin traffic out over the night (deck's density chart)

        // Handling and feel
        public float carSpeed = 1f;
        public float carGrip = 1f;
        public float roughness;           // camera bumpiness

        // HUD clock
        public float startHour = 20f, hourSpan = 6f;

        public static readonly ZoneProfile[] All =
        {
            new ZoneProfile {
                name = "DESERT", blurb = "Sun-baked dunes, long empty straights, heat haze",
                decor = DecorKind.Desert, lanes = 3,
                fog = new Color(0.88f, 0.76f, 0.58f), ambient = new Color(0.55f, 0.5f, 0.45f),
                sun = new Color(1f, 0.94f, 0.8f), sunIntensity = 1.2f, sunAngle = 50f, fogDensity = 0.0028f,
                surface = new Color(0.2f, 0.2f, 0.21f), roadSmooth = 0.15f,
                ground = new Color(0.82f, 0.64f, 0.36f), decorA = new Color(0.86f, 0.69f, 0.41f), decorB = new Color(0.6f, 0.32f, 0.18f),
                lamps = false, curbs = false, guardrail = false, laneLines = true,
                mix = new float[] { 0.25f, 0f, 0.1f, 0.1f, 0.45f, 0.1f, 0f },
                trafficMultiplier = 0.3f, trafficSpeed = 1f, carSpeed = 1.05f, carGrip = 1f,
                startHour = 12f, hourSpan = 4f },

            new ZoneProfile {
                name = "HIGHWAY", blurb = "Five fast lanes at sunset, guardrails and sign gantries",
                decor = DecorKind.Highway, lanes = 5,
                fog = new Color(0.82f, 0.46f, 0.32f), ambient = new Color(0.3f, 0.25f, 0.32f),
                sun = new Color(1f, 0.6f, 0.3f), sunIntensity = 0.85f, sunAngle = 12f, fogDensity = 0.0032f,
                surface = new Color(0.09f, 0.09f, 0.1f), roadSmooth = 0.5f,
                ground = new Color(0.11f, 0.14f, 0.08f), decorA = new Color(0.1f, 0.16f, 0.12f), decorB = new Color(0.05f, 0.4f, 0.2f),
                lamp = new Color(1f, 0.72f, 0.35f),
                lamps = true, curbs = false, guardrail = true, laneLines = true,
                mix = new float[] { 0.45f, 0f, 0.1f, 0.1f, 0.35f, 0f, 0f },
                trafficMultiplier = 1.1f, trafficSpeed = 1.5f, carSpeed = 1.15f, carGrip = 1f,
                startHour = 18f, hourSpan = 3f },

            new ZoneProfile {
                name = "TRAFFIC CITY", blurb = "Hyderabad after dark: neon, towers, packed streets",
                decor = DecorKind.City, lanes = 4,
                fog = new Color(0.02f, 0.03f, 0.07f), ambient = new Color(0.07f, 0.08f, 0.13f),
                sun = new Color(0.35f, 0.42f, 0.7f), sunIntensity = 0.25f, sunAngle = 50f, fogDensity = 0.0035f,
                surface = new Color(0.045f, 0.047f, 0.055f), roadSmooth = 0.88f,
                ground = new Color(0.04f, 0.04f, 0.05f), decorA = Color.white, decorB = Color.white,
                lamp = new Color(1f, 0.75f, 0.4f), neonA = new Color(0.1f, 0.9f, 1f), neonB = new Color(1f, 0.2f, 0.8f),
                building = new Color(0.06f, 0.08f, 0.12f),
                buildingHeight = new Vector2(15f, 55f), buildingDensity = 0.9f,
                lamps = true, curbs = true, guardrail = false, laneLines = true,
                mix = new float[] { 0.38f, 0.2f, 0.2f, 0.12f, 0.1f, 0f, 0f },
                trafficMultiplier = 1.3f, trafficSpeed = 1f, nightCurve = true, carSpeed = 1f, carGrip = 1f,
                startHour = 20f, hourSpan = 6f },

            new ZoneProfile {
                name = "OFF ROAD", blurb = "Muddy forest trail: loose grip, rocks and fallen logs",
                decor = DecorKind.Forest, lanes = 3,
                fog = new Color(0.6f, 0.68f, 0.72f), ambient = new Color(0.4f, 0.45f, 0.45f),
                sun = new Color(0.95f, 0.95f, 0.85f), sunIntensity = 0.9f, sunAngle = 55f, fogDensity = 0.006f,
                surface = new Color(0.27f, 0.19f, 0.12f), roadSmooth = 0.05f,
                ground = new Color(0.14f, 0.2f, 0.09f), decorA = new Color(0.25f, 0.15f, 0.08f), decorB = new Color(0.1f, 0.35f, 0.12f),
                lamps = false, curbs = false, guardrail = false, laneLines = false,
                mix = new float[] { 0.1f, 0f, 0.15f, 0f, 0.1f, 0.35f, 0.3f },
                trafficMultiplier = 0.5f, trafficSpeed = 0.7f, carSpeed = 0.78f, carGrip = 0.55f, roughness = 0.1f,
                startHour = 9f, hourSpan = 3f },
        };
    }
}
