using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Coin challenge: collect every coin on the road before the time runs out.
// Coins are found automatically: every Coin in the scene counts (add/remove coins freely).
public class CoinGameManager : MonoBehaviour
{
    enum State { Countdown, Playing, Won, Lost }

    [Header("References")]
    public CarController car;

    [Header("Rules")]
    public float timeLimit = 30f;
    public float countdownTime = 3f;
    [Tooltip("Launch the car at its Launch Speed on GO.")]
    public bool launchOnGo = true;

    State state = State.Countdown;
    float timer;
    int total, collected;
    GUIStyle bigStyle, smallStyle;

    void Start()
    {
        car.inputEnabled = false;
        timer = countdownTime;
        var coins = FindObjectsByType<Coin>(FindObjectsSortMode.None);
        foreach (var c in coins) c.manager = this;
        total = coins.Length;
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
                if (timer >= timeLimit || car.transform.position.y < -20f) End(false);
                break;
        }

        if (kb != null && kb.rKey.wasPressedThisFrame)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnCoinTouched(Coin coin, Rigidbody body)
    {
        if (state != State.Playing || body == null || body.gameObject != car.gameObject) return;
        coin.gameObject.SetActive(false);
        collected++;
        if (collected >= total) End(true);
    }

    void End(bool won)
    {
        state = won ? State.Won : State.Lost;
        car.inputEnabled = false;
        Debug.Log($"[CoinGameManager] {(won ? "WIN" : "LOSE")} - {collected}/{total} coins, time {timer:0.00}s");
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
        smallStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
        GUI.Label(new Rect(20, 50, 600, 40), $"Coins: {collected}/{total}", smallStyle);
        smallStyle.normal.textColor = Color.white;

        Rect centerRect = new Rect(0, Screen.height * 0.3f, Screen.width, 100);
        Rect subRect = new Rect(0, Screen.height * 0.3f + 90, Screen.width, 50);
        var sub = new GUIStyle(smallStyle) { alignment = TextAnchor.MiddleCenter };
        switch (state)
        {
            case State.Countdown:
                GUI.Label(centerRect, Mathf.CeilToInt(timer).ToString(), bigStyle);
                GUI.Label(subRect, $"Collect all {total} coins in {timeLimit:0} seconds!", sub);
                break;
            case State.Playing:
                if (timer < 1f) GUI.Label(centerRect, "GO!", bigStyle);
                break;
            case State.Won:
                bigStyle.normal.textColor = Color.green;
                GUI.Label(centerRect, "YOU WIN!", bigStyle);
                GUI.Label(subRect, $"All coins in {timer:0.00}s   -   Press R to play again", sub);
                break;
            case State.Lost:
                bigStyle.normal.textColor = Color.red;
                GUI.Label(centerRect, "YOU LOSE!", bigStyle);
                GUI.Label(subRect, $"Only {collected}/{total} coins   -   Press R to try again", sub);
                break;
        }
    }
}
