using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// One-lap time trial: drive a full lap of the road and cross the finish line before the timer runs out.
// Checkpoints (index 1..n) must all be passed first, so reversing over the finish line doesn't count.
public class LapGameManager : MonoBehaviour
{
    enum State { Setup, Countdown, Racing, Won, Lost }

    [Header("References")]
    public CarController car;
    [Tooltip("\"FINISH\" labels on the finish arch; only the one facing the camera is drawn.")]
    public List<MeshRenderer> finishLabels = new List<MeshRenderer>();

    [Header("Rules")]
    public float timeLimit = 30f;
    public float countdownTime = 3f;
    [Tooltip("Number of checkpoints along the road, not counting the finish line.")]
    public int checkpointCount = 7;

    [Header("Start speed controller")]
    [Tooltip("Show the start-speed slider before each race. The default value is the car's Launch Speed Kmh.")]
    public bool showStartSpeedSetup = true;
    public float minStartSpeedKmh = 0f;
    public float maxStartSpeedKmh = 200f;
    public float startSpeedStepKmh = 10f;

    // Remembered across restarts (R reloads the scene) for the rest of this play session.
    static float chosenStartSpeedKmh = -1f;

    State state = State.Setup;
    float timer;
    float finishTime;
    readonly HashSet<int> passed = new HashSet<int>();
    GUIStyle bigStyle, smallStyle;

    void Start()
    {
        car.inputEnabled = false;
        if (chosenStartSpeedKmh >= 0f) car.launchSpeedKmh = chosenStartSpeedKmh;
        if (showStartSpeedSetup) state = State.Setup;
        else BeginCountdown();
    }

    void BeginCountdown()
    {
        chosenStartSpeedKmh = car.launchSpeedKmh;
        state = State.Countdown;
        timer = countdownTime;
    }

    void Update()
    {
        var kb = Keyboard.current;
        switch (state)
        {
            case State.Setup:
                if (kb == null) break;
                float step = 0f;
                if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) step += startSpeedStepKmh;
                if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) step -= startSpeedStepKmh;
                if (step != 0f) car.launchSpeedKmh = Mathf.Clamp(car.launchSpeedKmh + step, minStartSpeedKmh, maxStartSpeedKmh);
                if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) BeginCountdown();
                break;
            case State.Countdown:
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    state = State.Racing;
                    timer = 0f;
                    car.inputEnabled = true;
                    car.Launch();
                }
                break;
            case State.Racing:
                timer += Time.deltaTime;
                if (timer >= timeLimit || car.transform.position.y < -20f) EndRace(false);
                break;
        }

        if (kb != null && kb.rKey.wasPressedThisFrame)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnCheckpoint(Checkpoint cp, Rigidbody body)
    {
        if (state != State.Racing || body.gameObject != car.gameObject) return;

        if (cp.index == 0)
        {
            if (passed.Count >= checkpointCount) EndRace(true);
        }
        else
        {
            passed.Add(cp.index);
        }
    }

    void EndRace(bool won)
    {
        state = won ? State.Won : State.Lost;
        finishTime = Mathf.Min(timer, timeLimit);
        car.inputEnabled = false;
        Debug.Log($"[LapGameManager] {(won ? "WIN" : "LOSE")} - time {finishTime:0.00}s, checkpoints {passed.Count}/{checkpointCount}");
    }

    // 3D text draws on top of everything, so only show the label whose front faces the camera.
    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        foreach (var label in finishLabels)
        {
            if (!label) continue;
            Vector3 toCam = cam.transform.position - label.transform.position;
            label.enabled = Vector3.Dot(toCam, label.transform.forward) < 0f;
        }
    }

    void OnGUI()
    {
        if (bigStyle == null)
        {
            bigStyle = new GUIStyle(GUI.skin.label) { fontSize = 64, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold };
        }
        bigStyle.normal.textColor = Color.white;
        smallStyle.normal.textColor = Color.white;

        float timeLeft = state == State.Racing ? Mathf.Max(0f, timeLimit - timer) : (state <= State.Countdown ? timeLimit : timeLimit - finishTime);
        if (state == State.Racing && timeLeft < 10f) smallStyle.normal.textColor = new Color(1f, 0.35f, 0.3f);
        GUI.Label(new Rect(20, 15, 600, 40), $"Time left: {timeLeft:0.00}s", smallStyle);
        smallStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(20, 50, 600, 40), $"Checkpoints: {passed.Count}/{checkpointCount}", smallStyle);

        Rect centerRect = new Rect(0, Screen.height * 0.3f, Screen.width, 100);
        Rect subRect = new Rect(0, Screen.height * 0.3f + 90, Screen.width, 50);
        var sub = new GUIStyle(smallStyle) { alignment = TextAnchor.MiddleCenter };
        switch (state)
        {
            case State.Setup:
                DrawStartSpeedPanel(sub);
                break;
            case State.Countdown:
                GUI.Label(centerRect, Mathf.CeilToInt(timer).ToString(), bigStyle);
                GUI.Label(subRect, $"Finish one lap in {timeLimit:0} seconds!  W/A/S/D to drive", sub);
                break;
            case State.Racing:
                if (timer < 1f) GUI.Label(centerRect, "GO!", bigStyle);
                break;
            case State.Won:
                bigStyle.normal.textColor = Color.green;
                GUI.Label(centerRect, "YOU WIN!", bigStyle);
                GUI.Label(subRect, $"Lap time: {finishTime:0.00}s   -   Press R to play again", sub);
                break;
            case State.Lost:
                bigStyle.normal.textColor = Color.red;
                GUI.Label(centerRect, "YOU LOSE!", bigStyle);
                GUI.Label(subRect, "Time's up   -   Press R to try again", sub);
                break;
        }
    }

    // Slider panel shown before the race to pick the speed the car launches with at GO.
    void DrawStartSpeedPanel(GUIStyle sub)
    {
        float w = 560f, h = 230f;
        var panel = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.25f, w, h);
        GUI.color = new Color(0f, 0f, 0f, 0.75f);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(panel.x, panel.y + 15f, w, 40f), "START SPEED", sub);
        var valueStyle = new GUIStyle(sub) { fontSize = 44 };
        valueStyle.normal.textColor = new Color(1f, 0.8f, 0.1f);
        GUI.Label(new Rect(panel.x, panel.y + 55f, w, 55f), $"{car.launchSpeedKmh:0} km/h", valueStyle);

        float v = GUI.HorizontalSlider(new Rect(panel.x + 40f, panel.y + 125f, w - 80f, 20f), car.launchSpeedKmh, minStartSpeedKmh, maxStartSpeedKmh);
        car.launchSpeedKmh = Mathf.Round(v);

        var hint = new GUIStyle(sub) { fontSize = 18 };
        GUI.Label(new Rect(panel.x, panel.y + 145f, w, 30f), "A/D or Left/Right to change  -  Enter/Space to start", hint);
        if (GUI.Button(new Rect(panel.x + w * 0.5f - 80f, panel.y + 180f, 160f, 38f), "START")) BeginCountdown();
    }
}
