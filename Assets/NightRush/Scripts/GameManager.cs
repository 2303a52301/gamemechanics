using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NightRush
{
    public enum GameState { Menu, Countdown, Racing, Finished }
    public enum RaceMode { CircuitSprint, TimeAttack }

    /// <summary>
    /// Owns the whole game: builds the world for a chosen zone, runs the race state machine,
    /// scoring (near-miss combos x1..x8), the night-time traffic curve, and the IMGUI menu / HUD.
    /// Add this to any scene (or just press Play: it bootstraps itself).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        [Header("Race")]
        public float finishDistance = 6000f; // metres, roughly 2 to 3 minutes
        public int rivalCount = 3;

        public GameState State { get; private set; }
        public bool CanDrive { get { return State == GameState.Racing; } }
        public TrafficSpawner Traffic { get; private set; }
        public CarController Player { get; private set; }
        public ZoneProfile Zone { get; private set; }
        public int Score { get { return Mathf.RoundToInt(scoreF); } }
        public int Multiplier { get { return Mathf.Clamp(1 + combo / 2, 1, 8); } }
        public float Progress { get { return Player == null ? 0f : Mathf.Clamp01(Player.transform.position.z / finishDistance); } }

        // Traffic density by hour of night, 8 PM to 2 AM (from the deck's Traffic System chart)
        static readonly float[] DensityCurve = { 1.00f, 0.90f, 0.75f, 0.55f, 0.40f, 0.30f, 0.25f };

        int zoneIndex;
        RaceMode mode = RaceMode.CircuitSprint;
        GameObject world, arch;
        ChaseCamera chase;
        Camera cam;
        Light moon;
        readonly List<CarController> rivals = new List<CarController>();
        readonly Dictionary<CarController, float> rivalFinish = new Dictionary<CarController, float>();

        float countdown, raceTime, finishTime, scoreF, comboTimer, popupTimer;
        int combo, finalPlace;
        string popupText = "";

        // Simulation speed (Time.timeScale while racing). Change in the menu, or with [ ] / - = during the race.
        static readonly float[] SimSpeeds = { 0.5f, 0.75f, 1f, 1.5f, 2f };
        int simIndex = 2;
        public float SimSpeed { get { return SimSpeeds[simIndex]; } }

        GUIStyle sBig, sMid, sMidL, sMidR, sSmall, sSpeed, sBtn, sBtnBig;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoBootstrap()
        {
            if (I == null) new GameObject("NightRushGame").AddComponent<GameManager>();
        }

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            State = GameState.Menu;

            cam = Camera.main;
            if (cam == null)
            {
                var g = new GameObject("Main Camera");
                g.tag = "MainCamera";
                cam = g.AddComponent<Camera>();
                g.AddComponent<AudioListener>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.farClipPlane = 600f;
            chase = cam.GetComponent<ChaseCamera>();
            if (chase == null) chase = cam.gameObject.AddComponent<ChaseCamera>();

            // Disable any default sun from the template scene; this is a night game.
            foreach (var l in FindObjectsOfType<Light>())
                if (l.type == LightType.Directional) l.enabled = false;

            BuildWorld(zoneIndex); // live preview behind the menu
        }

        void OnDestroy() { Time.timeScale = 1f; if (I == this) I = null; }

        // ------------------------------------------------------------------ world

        void BuildWorld(int zi)
        {
            if (world != null) { world.SetActive(false); Destroy(world); }
            Zone = ZoneProfile.All[zi];
            Track.Lanes = Zone.lanes;
            world = new GameObject("World");
            arch = null;
            ApplyLighting(Zone);

            int playerLane = Track.Lanes / 2;
            float hw, len;
            var pgo = VehicleFactory.Create(VehicleType.Car, new Color(0.9f, 0.08f, 0.15f), out hw, out len);
            pgo.name = "Player";
            pgo.transform.SetParent(world.transform, false);
            pgo.transform.position = new Vector3(Track.LaneX(playerLane), 0f, 0f);
            Player = pgo.AddComponent<CarController>();
            Player.halfWidth = hw;
            Player.isPlayer = true;
            Player.Crashed = OnPlayerCrash;
            Player.maxSpeed *= Zone.carSpeed;
            Player.grip *= Zone.carGrip;
            Player.driftGrip *= Zone.carGrip;
            pgo.AddComponent<PlayerInput>();
            pgo.AddComponent<NearMissScorer>();

            var roadGo = new GameObject("Road");
            roadGo.transform.SetParent(world.transform, false);
            roadGo.AddComponent<RoadGenerator>().Build(Zone, pgo.transform);

            var tGo = new GameObject("Traffic");
            tGo.transform.SetParent(world.transform, false);
            Traffic = tGo.AddComponent<TrafficSpawner>();
            Traffic.density = 1f;
            Traffic.Begin(pgo.transform, Zone);

            rivals.Clear();
            rivalFinish.Clear();
            if (mode == RaceMode.CircuitSprint)
            {
                var others = new List<int>();
                for (int l = 0; l < Track.Lanes; l++) if (l != playerLane) others.Add(l);
                Color[] cols = { new Color(0.1f, 0.4f, 1f), new Color(0.95f, 0.8f, 0.1f), new Color(0.1f, 0.85f, 0.4f) };
                float[] skill = { 0.88f, 0.93f, 0.97f };
                for (int i = 0; i < rivalCount; i++)
                {
                    int lane = others[i % others.Count];
                    int row = i / others.Count;
                    float rhw, rlen;
                    var rgo = VehicleFactory.Create(VehicleType.Car, cols[i % cols.Length], out rhw, out rlen);
                    rgo.name = "Rival " + (i + 1);
                    rgo.transform.SetParent(world.transform, false);
                    rgo.transform.position = new Vector3(Track.LaneX(lane), 0f, -9f * row);
                    var rc = rgo.AddComponent<CarController>();
                    rc.halfWidth = rhw;
                    rc.maxSpeed = 55f * Zone.carSpeed * skill[i % skill.Length];
                    rc.grip *= Zone.carGrip;
                    rc.driftGrip *= Zone.carGrip;
                    rc.passiveNitro = 0.04f;
                    rgo.AddComponent<RivalAI>();
                    rivals.Add(rc);
                }
            }

            chase.SetTarget(pgo.transform, Player);
            scoreF = 0f; combo = 0; comboTimer = 0f; raceTime = 0f; popupTimer = 0f;
        }

        void ApplyLighting(ZoneProfile z)
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = z.ambient;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = z.fog;
            RenderSettings.fogDensity = z.fogDensity;
            cam.backgroundColor = z.fog;

            if (moon == null)
            {
                var g = new GameObject("Sun");
                g.transform.SetParent(transform, false);
                moon = g.AddComponent<Light>();
                moon.type = LightType.Directional;
                moon.shadows = LightShadows.None;
            }
            moon.transform.rotation = Quaternion.Euler(z.sunAngle, -30f, 0f);
            moon.color = z.sun;
            moon.intensity = z.sunIntensity;
        }

        void StartRace()
        {
            BuildWorld(zoneIndex);
            State = GameState.Countdown;
            countdown = 4f; // 1 s of READY, then 3, 2, 1
        }

        // ------------------------------------------------------------------ game flow

        void Update()
        {
            if (Player == null) return;
            float dt = Time.deltaTime;
            if (popupTimer > 0f) popupTimer -= dt;
            if (Traffic != null) Traffic.density = Zone.nightCurve ? Density(Progress) : 1f;

            switch (State)
            {
                case GameState.Menu:
                    if (InputBridge.SubmitPressed()) StartRace();
                    break;
                case GameState.Countdown:
                    countdown -= dt;
                    if (countdown <= 0f) { State = GameState.Racing; raceTime = 0f; }
                    break;
                case GameState.Racing:
                    raceTime += dt;
                    scoreF += Player.Speed * dt * 0.05f * Multiplier;
                    if (combo > 0) { comboTimer -= dt; if (comboTimer <= 0f) combo = 0; }
                    if (Zone.roughness > 0f && Player.Speed > 8f && chase != null) chase.Shake(Zone.roughness * Player.Speed01);
                    TrackFinish();
                    break;
            }

            if (State != GameState.Menu)
            {
                if (InputBridge.RestartPressed()) StartRace();
                else if (InputBridge.MenuPressed()) { State = GameState.Menu; BuildWorld(zoneIndex); }
            }
            MaybeSpawnFinishArch();

            HandleSimSpeedKeys();
            // The countdown, menu and results always run at normal speed; only the race itself is scaled.
            Time.timeScale = State == GameState.Racing ? SimSpeed : 1f;
        }

        void HandleSimSpeedKeys()
        {
            if (State != GameState.Racing && State != GameState.Countdown) return;
            if (InputBridge.SpeedUpPressed()) simIndex = Mathf.Min(simIndex + 1, SimSpeeds.Length - 1);
            if (InputBridge.SpeedDownPressed()) simIndex = Mathf.Max(simIndex - 1, 0);
        }

        void TrackFinish()
        {
            foreach (var r in rivals)
                if (!rivalFinish.ContainsKey(r) && r.transform.position.z >= finishDistance) rivalFinish[r] = raceTime;

            if (Player.transform.position.z >= finishDistance)
            {
                State = GameState.Finished;
                finishTime = raceTime;
                finalPlace = 1 + rivalFinish.Count;
                scoreF += (rivals.Count + 1 - finalPlace) * 1000f;
            }
        }

        static float Density(float p)
        {
            float f = Mathf.Clamp01(p) * (DensityCurve.Length - 1);
            int i = Mathf.Min((int)f, DensityCurve.Length - 2);
            return Mathf.Lerp(DensityCurve[i], DensityCurve[i + 1], f - i);
        }

        void MaybeSpawnFinishArch()
        {
            if (arch != null || world == null || Player == null) return;
            if (Player.transform.position.z < finishDistance - 400f) return;

            arch = new GameObject("FinishArch");
            arch.transform.SetParent(world.transform, false);
            float hw = Track.HalfWidth + 1.5f;
            var white = VehicleFactory.Mat(Color.white, 0.4f, 3f);
            var red = VehicleFactory.Mat(new Color(1f, 0.1f, 0.2f), 0.4f, 3f);
            VehicleFactory.Box(arch.transform, "PillarL", new Vector3(-hw, 4f, finishDistance), new Vector3(0.6f, 8f, 0.6f), white);
            VehicleFactory.Box(arch.transform, "PillarR", new Vector3(hw, 4f, finishDistance), new Vector3(0.6f, 8f, 0.6f), white);
            VehicleFactory.Box(arch.transform, "Bar", new Vector3(0f, 8.2f, finishDistance), new Vector3(hw * 2f + 1.2f, 0.8f, 0.6f), red);
            VehicleFactory.Box(arch.transform, "Line", new Vector3(0f, 0.03f, finishDistance), new Vector3(hw * 2f, 0.03f, 1.2f), white);
        }

        // ------------------------------------------------------------------ scoring hooks

        public void RegisterNearMiss()
        {
            combo++;
            comboTimer = 4f;
            int pts = 100 * Multiplier;
            scoreF += pts;
            Player.AddNitro(0.12f);
            Popup("NEAR MISS  +" + pts);
        }

        void OnPlayerCrash(CarController c, float impact)
        {
            combo = 0;
            Popup("CRASH!");
            if (chase != null) chase.Shake(Mathf.Clamp(impact / 15f, 0.3f, 1.2f));
        }

        void Popup(string t) { popupText = t; popupTimer = 1.2f; }

        int Place()
        {
            int ahead = 0;
            foreach (var r in rivals) if (r.transform.position.z > Player.transform.position.z) ahead++;
            return ahead + 1;
        }

        // ------------------------------------------------------------------ UI (IMGUI, zero dependencies)

        void EnsureStyles()
        {
            if (sBig != null) return;
            sBig = new GUIStyle(GUI.skin.label) { fontSize = 56, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            sMid = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            sMidL = new GUIStyle(sMid) { alignment = TextAnchor.MiddleLeft };
            sMidR = new GUIStyle(sMid) { alignment = TextAnchor.MiddleRight };
            sSmall = new GUIStyle(GUI.skin.label) { fontSize = 15, alignment = TextAnchor.MiddleCenter };
            sSpeed = new GUIStyle(GUI.skin.label) { fontSize = 52, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight };
            sBtn = new GUIStyle(GUI.skin.button) { fontSize = 16, wordWrap = true };
            sBtnBig = new GUIStyle(GUI.skin.button) { fontSize = 24, fontStyle = FontStyle.Bold };
        }

        static void Fill(Rect r, Color c)
        {
            var old = GUI.color; GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        static void Label(Rect r, string t, GUIStyle st, Color c)
        {
            st.normal.textColor = new Color(0f, 0f, 0f, c.a * 0.7f);
            GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), t, st);
            st.normal.textColor = c;
            GUI.Label(r, t, st);
        }

        void OnGUI()
        {
            if (Player == null) return;
            EnsureStyles();
            float s = Screen.height / 720f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            float W = Screen.width / s, H = 720f;

            if (State == GameState.Menu) { DrawMenu(W, H); return; }
            DrawHud(W, H);
            if (State == GameState.Countdown)
            {
                bool ready = countdown > 3f;
                Label(new Rect(0f, H * 0.3f, W, 120f), ready ? "READY" : Mathf.CeilToInt(countdown).ToString(), sBig,
                    ready ? Color.white : new Color(1f, 0.8f, 0.3f));
                Label(new Rect(0f, H * 0.3f + 92f, W, 30f), Zone.name + "   |   SIM SPEED " + SimSpeed.ToString("0.##") + "x   ( [ ] to change )", sSmall, Color.white);
            }
            if (State == GameState.Racing && raceTime < 0.8f)
                Label(new Rect(0f, H * 0.3f, W, 120f), "GO!", sBig, new Color(0.4f, 1f, 0.5f));
            if (State == GameState.Finished) DrawResults(W, H);
        }

        void DrawMenu(float W, float H)
        {
            Fill(new Rect(0f, 0f, W, H), new Color(0f, 0f, 0f, 0.5f));
            Label(new Rect(0f, 26f, W, 80f), "NIGHT RUSH", sBig, new Color(1f, 0.78f, 0.25f));
            Label(new Rect(0f, 100f, W, 30f), "Weave through traffic. Chase the lights. Own the road.", sSmall, Color.white);
            Label(new Rect(0f, 146f, W, 24f), "SELECT THEME", sSmall, new Color(0.7f, 0.7f, 0.8f));

            float bw = 450f, bh = 84f, gap = 14f;
            float x0 = (W - (2f * bw + gap)) * 0.5f;
            for (int i = 0; i < ZoneProfile.All.Length; i++)
            {
                var r = new Rect(x0 + (i % 2) * (bw + gap), 174f + (i / 2) * (bh + gap), bw, bh);
                GUI.backgroundColor = i == zoneIndex ? new Color(1f, 0.7f, 0.2f) : Color.white;
                if (GUI.Button(r, ZoneProfile.All[i].name + "\n" + ZoneProfile.All[i].blurb, sBtn) && i != zoneIndex)
                {
                    zoneIndex = i;
                    BuildWorld(zoneIndex);
                }
            }

            string[] names = { "CIRCUIT SPRINT\nRace 3 rivals through traffic", "TIME ATTACK\nSolo run against the clock" };
            float mw = 450f;
            float mx = (W - (2f * mw + gap)) * 0.5f;
            for (int i = 0; i < 2; i++)
            {
                GUI.backgroundColor = (int)mode == i ? new Color(0.3f, 0.9f, 1f) : Color.white;
                if (GUI.Button(new Rect(mx + i * (mw + gap), 388f, mw, 62f), names[i], sBtn) && (int)mode != i) { mode = (RaceMode)i; BuildWorld(zoneIndex); }
            }
            GUI.backgroundColor = Color.white;

            Label(new Rect(0f, 458f, W, 22f), "SIMULATION SPEED", sSmall, new Color(0.7f, 0.7f, 0.8f));
            float sw = 120f, sg = 10f;
            float sx = (W - (SimSpeeds.Length * sw + (SimSpeeds.Length - 1) * sg)) * 0.5f;
            for (int i = 0; i < SimSpeeds.Length; i++)
            {
                GUI.backgroundColor = i == simIndex ? new Color(0.5f, 1f, 0.5f) : Color.white;
                if (GUI.Button(new Rect(sx + i * (sw + sg), 482f, sw, 44f), SimSpeeds[i].ToString("0.##") + "x", sBtn)) simIndex = i;
            }
            GUI.backgroundColor = Color.white;

            if (GUI.Button(new Rect(W * 0.5f - 170f, 540f, 340f, 66f), "START RACE  (Enter)", sBtnBig)) StartRace();

            Label(new Rect(0f, 624f, W, 26f), "W / S  throttle + brake    A / D  steer    SPACE  drift    SHIFT  nitro    [ ]  sim speed    R  restart    ESC  menu", sSmall, Color.white);
            Label(new Rect(0f, 650f, W, 26f), "Skim past obstacles for near-miss combos (up to x8). Tuck behind trucks for a free slipstream boost.", sSmall, new Color(0.7f, 0.7f, 0.8f));
        }

        void DrawHud(float W, float H)
        {
            // Score and combo (top left)
            Label(new Rect(24f, 14f, 420f, 36f), "SCORE  " + Score.ToString("N0"), sMidL, Color.white);
            if (combo > 0)
            {
                Label(new Rect(24f, 48f, 300f, 34f), "x" + Multiplier + " COMBO", sMidL, new Color(1f, 0.78f, 0.25f));
                Fill(new Rect(24f, 82f, 200f, 6f), new Color(0f, 0f, 0f, 0.5f));
                Fill(new Rect(24f, 82f, 200f * Mathf.Clamp01(comboTimer / 4f), 6f), new Color(1f, 0.78f, 0.25f));
            }

            // Night clock + progress (top centre)
            float hourF = Zone.startHour + Zone.hourSpan * Progress;
            int h24 = (int)hourF % 24;
            int min = (int)((hourF - Mathf.Floor(hourF)) * 60f);
            int h12 = h24 % 12 == 0 ? 12 : h24 % 12;
            Label(new Rect(W * 0.5f - 150f, 10f, 300f, 34f), h12 + ":" + min.ToString("00") + (h24 >= 12 ? " PM" : " AM"), sMid, new Color(0.7f, 0.85f, 1f));
            Fill(new Rect(W * 0.5f - 200f, 48f, 400f, 8f), new Color(0f, 0f, 0f, 0.5f));
            Fill(new Rect(W * 0.5f - 200f, 48f, 400f * Progress, 8f), new Color(0.3f, 0.9f, 1f));

            // Position + time (top right)
            string pos = mode == RaceMode.TimeAttack ? "TIME ATTACK" : "P" + Place() + " / " + (rivals.Count + 1);
            Label(new Rect(W - 324f, 14f, 300f, 36f), pos, sMidR, Color.white);
            int tm = Mathf.FloorToInt(raceTime);
            Label(new Rect(W - 324f, 48f, 300f, 30f), (tm / 60).ToString("00") + ":" + (tm % 60).ToString("00"), sMidR, new Color(0.8f, 0.8f, 0.9f));

            // Speed + nitro (bottom right)
            Label(new Rect(W - 384f, H - 140f, 360f, 70f), Mathf.RoundToInt(Player.SpeedKmh) + " km/h", sSpeed, Color.white);
            Fill(new Rect(W - 364f, H - 58f, 340f, 16f), new Color(0f, 0f, 0f, 0.55f));
            Fill(new Rect(W - 364f, H - 58f, 340f * Player.Nitro01, 16f), Player.NitroActive ? new Color(0.3f, 0.9f, 1f) : new Color(1f, 0.6f, 0.15f));
            Label(new Rect(W - 364f, H - 40f, 340f, 22f), Player.Drifting ? "DRIFT  charging nitro" : (Player.InSlipstream ? "SLIPSTREAM" : "NITRO"), sSmall, new Color(0.85f, 0.85f, 0.9f));

            // Simulation speed (bottom left)
            Label(new Rect(24f, H - 44f, 420f, 30f), "SIM SPEED  " + SimSpeed.ToString("0.##") + "x   ( [ ] to change )", sSmall,
                SimSpeed == 1f ? new Color(0.8f, 0.8f, 0.9f) : new Color(0.5f, 1f, 0.5f));

            // Popup (centre)
            if (popupTimer > 0f)
            {
                float a = Mathf.Clamp01(popupTimer / 0.4f);
                Label(new Rect(0f, H * 0.2f - (1.2f - popupTimer) * 20f, W, 50f), popupText, sMid, new Color(1f, 0.85f, 0.3f, a));
            }
        }

        void DrawResults(float W, float H)
        {
            Fill(new Rect(W * 0.5f - 260f, 180f, 520f, 300f), new Color(0f, 0f, 0f, 0.75f));
            string[] ord = { "1st", "2nd", "3rd", "4th", "5th", "6th" };
            string place = mode == RaceMode.TimeAttack ? "FINISH!" : "FINISH!  " + ord[Mathf.Clamp(finalPlace - 1, 0, ord.Length - 1)] + " place";
            Label(new Rect(W * 0.5f - 260f, 196f, 520f, 60f), place, sMid, new Color(1f, 0.78f, 0.25f));
            int tm = Mathf.FloorToInt(finishTime);
            Label(new Rect(W * 0.5f - 260f, 270f, 520f, 36f), "TIME  " + (tm / 60).ToString("00") + ":" + (tm % 60).ToString("00"), sMid, Color.white);
            Label(new Rect(W * 0.5f - 260f, 316f, 520f, 36f), "SCORE  " + Score.ToString("N0"), sMid, Color.white);
            Label(new Rect(W * 0.5f - 260f, 400f, 520f, 30f), "R  race again        ESC  menu", sSmall, new Color(0.8f, 0.8f, 0.9f));
        }
    }
}
