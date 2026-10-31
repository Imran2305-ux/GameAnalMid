using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// One-lap game with two modes (pick "Mode" on the Game Manager in the Inspector):
//  RaceVsRival - you race the rival (blue) car; whoever crosses the finish first wins. Other NPC cars are hidden.
//  TimeTrial   - NPC cars drive around as traffic; finish the lap within Time Limit to win.
// Checkpoints (index 1..n) must all be passed first, so reversing over the finish line doesn't count.
public class LapGameManager : MonoBehaviour
{
    public enum GameMode { RaceVsRival, TimeTrial }
    enum State { Countdown, Racing, Won, Lost }

    [Header("Mode")]
    public GameMode mode = GameMode.RaceVsRival;

    [Header("References")]
    public CarController car;
    [Tooltip("RaceVsRival: the car you race. It waits on the start line until GO.")]
    public NPCCar rival;
    [Tooltip("Parent of all NPC traffic cars (hidden in RaceVsRival mode, except the rival).")]
    public Transform npcTraffic;

    [Header("Rules")]
    [Tooltip("TimeTrial: seconds to finish the lap.")]
    public float timeLimit = 15f;
    public float countdownTime = 3f;
    [Tooltip("Number of checkpoints along the road, not counting the finish line.")]
    public int checkpointCount = 7;

    State state = State.Countdown;
    float timer;
    float finishTime;
    // Checkpoints passed by each racer (the player, and the rival in RaceVsRival).
    readonly Dictionary<GameObject, HashSet<int>> passed = new Dictionary<GameObject, HashSet<int>>();
    GUIStyle bigStyle, smallStyle;

    bool RivalRace => mode == GameMode.RaceVsRival && rival != null;

    void Start()
    {
        car.inputEnabled = false;
        passed[car.gameObject] = new HashSet<int>();

        if (RivalRace)
        {
            // Only the rival is on track; it stays parked until GO.
            if (npcTraffic)
                foreach (var npc in npcTraffic.GetComponentsInChildren<NPCCar>())
                    if (npc != rival) npc.gameObject.SetActive(false);
            rival.enabled = false;
            passed[rival.gameObject] = new HashSet<int>();
        }

        timer = countdownTime;
    }

    void Update()
    {
        var kb = Keyboard.current;
        switch (state)
        {
            case State.Countdown:
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    state = State.Racing;
                    timer = 0f;
                    car.inputEnabled = true;
                    car.Launch();
                    if (RivalRace) rival.enabled = true;
                }
                break;
            case State.Racing:
                timer += Time.deltaTime;
                if (mode == GameMode.TimeTrial && timer >= timeLimit) EndRace(false);
                if (car.transform.position.y < -20f) EndRace(false);
                break;
        }

        if (kb != null && kb.rKey.wasPressedThisFrame)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnCheckpoint(Checkpoint cp, Rigidbody body)
    {
        if (state != State.Racing || body == null || !passed.TryGetValue(body.gameObject, out var set)) return;

        if (cp.index != 0) { set.Add(cp.index); return; }
        if (set.Count < checkpointCount) return;

        // Somebody finished the lap: the player wins only if it's them.
        bool playerFinished = body.gameObject == car.gameObject;
        EndRace(playerFinished);
        if (!playerFinished) rival.enabled = false;
    }

    void EndRace(bool won)
    {
        state = won ? State.Won : State.Lost;
        finishTime = mode == GameMode.TimeTrial ? Mathf.Min(timer, timeLimit) : timer;
        car.inputEnabled = false;
        Debug.Log($"[LapGameManager] {mode} {(won ? "WIN" : "LOSE")} - time {finishTime:0.00}s");
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

        if (mode == GameMode.TimeTrial)
        {
            float timeLeft = state == State.Racing ? Mathf.Max(0f, timeLimit - timer) : (state == State.Countdown ? timeLimit : timeLimit - finishTime);
            if (state == State.Racing && timeLeft < 5f) smallStyle.normal.textColor = new Color(1f, 0.35f, 0.3f);
            GUI.Label(new Rect(20, 15, 600, 40), $"Time left: {timeLeft:0.00}s", smallStyle);
            smallStyle.normal.textColor = Color.white;
        }
        else
        {
            GUI.Label(new Rect(20, 15, 600, 40), $"Time: {(state == State.Racing ? timer : finishTime):0.00}s", smallStyle);
        }
        GUI.Label(new Rect(20, 50, 600, 40), $"Checkpoints: {passed[car.gameObject].Count}/{checkpointCount}", smallStyle);
        if (RivalRace && passed.TryGetValue(rival.gameObject, out var rs))
            GUI.Label(new Rect(20, 85, 600, 40), $"Blue car: {rs.Count}/{checkpointCount}", smallStyle);

        Rect centerRect = new Rect(0, Screen.height * 0.3f, Screen.width, 100);
        Rect subRect = new Rect(0, Screen.height * 0.3f + 90, Screen.width, 50);
        var sub = new GUIStyle(smallStyle) { alignment = TextAnchor.MiddleCenter };
        switch (state)
        {
            case State.Countdown:
                GUI.Label(centerRect, Mathf.CeilToInt(timer).ToString(), bigStyle);
                string goal = RivalRace ? "Beat the blue car to the finish!" : $"Finish one lap in {timeLimit:0} seconds!";
                GUI.Label(subRect, goal + "  W/A/S/D to drive", sub);
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
                GUI.Label(subRect, (RivalRace ? "The blue car finished first" : "Time's up") + "   -   Press R to try again", sub);
                break;
        }
    }
}
