using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Parking challenge: drive into any empty gallery gap and park inside its bay before the time runs out.
// The car must be fully inside the bay and (almost) stopped for Hold Time seconds.
public class ParkingGameManager : MonoBehaviour
{
    enum State { Countdown, Playing, Won, Lost }

    [Header("References")]
    public CarController car;
    public List<ParkingSpot> spots = new List<ParkingSpot>();

    [Header("Rules")]
    public float timeLimit = 30f;
    public float countdownTime = 3f;
    [Tooltip("Seconds the car must stay parked to win.")]
    public float holdTime = 1.5f;
    [Tooltip("The car counts as stopped below this speed.")]
    public float maxParkSpeedKmh = 3f;
    [Tooltip("Half width / half length of the car footprint (metres).")]
    public Vector2 carHalfSize = new Vector2(0.9f, 2.1f);
    [Tooltip("Launch the car at its Launch Speed on GO (like the race).")]
    public bool launchOnGo = true;

    State state = State.Countdown;
    float timer;
    float parkedFor;
    GUIStyle bigStyle, smallStyle;

    void Start()
    {
        car.inputEnabled = false;
        timer = countdownTime;
        foreach (var s in spots) s.SetHighlight(false);
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
                    state = State.Playing;
                    timer = 0f;
                    car.inputEnabled = true;
                    if (launchOnGo) car.Launch();
                }
                break;
            case State.Playing:
                timer += Time.deltaTime;
                bool inside = false;
                foreach (var s in spots)
                {
                    bool here = s.Contains(car.transform, carHalfSize);
                    s.SetHighlight(here);
                    inside |= here;
                }
                parkedFor = inside && car.SpeedKmh < maxParkSpeedKmh ? parkedFor + Time.deltaTime : 0f;
                if (parkedFor >= holdTime) End(true);
                else if (timer >= timeLimit || car.transform.position.y < -20f) End(false);
                break;
        }

        if (kb != null && kb.rKey.wasPressedThisFrame)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void End(bool won)
    {
        state = won ? State.Won : State.Lost;
        car.inputEnabled = false;
        Debug.Log($"[ParkingGameManager] {(won ? "WIN" : "LOSE")} - time {timer:0.00}s");
    }

    void OnGUI()
    {
        if (bigStyle == null)
        {
            bigStyle = new GUIStyle(GUI.skin.label) { fontSize = 64, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold };
        }
        bigStyle.normal.textColor = Color.white;

        float timeLeft = state == State.Countdown ? timeLimit : Mathf.Max(0f, timeLimit - timer);
        smallStyle.normal.textColor = state == State.Playing && timeLeft < 5f ? new Color(1f, 0.35f, 0.3f) : Color.white;
        GUI.Label(new Rect(20, 15, 600, 40), $"Time left: {timeLeft:0.00}s", smallStyle);
        smallStyle.normal.textColor = Color.white;
        if (state == State.Playing && parkedFor > 0f)
            GUI.Label(new Rect(20, 50, 600, 40), $"Parking... {Mathf.Clamp01(parkedFor / holdTime) * 100f:0}%", smallStyle);

        Rect centerRect = new Rect(0, Screen.height * 0.3f, Screen.width, 100);
        Rect subRect = new Rect(0, Screen.height * 0.3f + 90, Screen.width, 50);
        var sub = new GUIStyle(smallStyle) { alignment = TextAnchor.MiddleCenter };
        switch (state)
        {
            case State.Countdown:
                GUI.Label(centerRect, Mathf.CeilToInt(timer).ToString(), bigStyle);
                GUI.Label(subRect, $"Park in a green bay (gap in the gallery) within {timeLimit:0} seconds!", sub);
                break;
            case State.Playing:
                if (timer < 1f) GUI.Label(centerRect, "GO!", bigStyle);
                break;
            case State.Won:
                bigStyle.normal.textColor = Color.green;
                GUI.Label(centerRect, "PARKED! YOU WIN!", bigStyle);
                GUI.Label(subRect, $"Time: {timer:0.00}s   -   Press R to play again", sub);
                break;
            case State.Lost:
                bigStyle.normal.textColor = Color.red;
                GUI.Label(centerRect, "YOU LOSE!", bigStyle);
                GUI.Label(subRect, "Time's up   -   Press R to try again", sub);
                break;
        }
    }
}
