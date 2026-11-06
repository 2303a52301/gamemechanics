using UnityEngine;

namespace NightRush
{
    /// <summary>Lane geometry shared by road, traffic, rivals and camera. The road runs along +Z.</summary>
    public static class Track
    {
        public const float LaneWidth = 3.6f;
        public static int Lanes = 4;

        public static float HalfWidth { get { return Lanes * LaneWidth * 0.5f; } }
        public static float LaneX(int lane) { return (lane - (Lanes - 1) * 0.5f) * LaneWidth; }
        public static int NearestLane(float x) { return Mathf.Clamp(Mathf.RoundToInt(x / LaneWidth + (Lanes - 1) * 0.5f), 0, Lanes - 1); }
    }
}
