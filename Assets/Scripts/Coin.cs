using UnityEngine;

// Spinning coin; the CoinGameManager counts it when the player's car touches it.
public class Coin : MonoBehaviour
{
    public CoinGameManager manager;
    public float spinSpeed = 180f;

    void Update() { transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World); }

    void OnTriggerEnter(Collider other)
    {
        if (manager) manager.OnCoinTouched(this, other.attachedRigidbody);
    }
}
