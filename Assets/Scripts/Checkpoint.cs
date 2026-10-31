using UnityEngine;

// Trigger placed across the road; reports to the LapGameManager when the car drives through.
public class Checkpoint : MonoBehaviour
{
    public int index;
    public LapGameManager manager;

    void OnTriggerEnter(Collider other)
    {
        if (manager != null && other.attachedRigidbody != null)
            manager.OnCheckpoint(this, other.attachedRigidbody);
    }
}
