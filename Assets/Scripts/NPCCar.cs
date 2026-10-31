using System.Collections.Generic;
using UnityEngine;

// AI traffic car that drives laps along a RoadPath in its own lane.
// Kinematic, so the player bumps off it; slows down behind a slower car in the same lane.
[RequireComponent(typeof(Rigidbody))]
public class NPCCar : MonoBehaviour
{
    public RoadPath path;
    public float distance;               // start position along the path
    public float speedKmh = 90f;
    public float laneOffset = 4.5f;      // metres right of the road centre
    public float followGap = 14f;
    public Transform[] wheels;
    public float wheelRadius = 0.35f;

    static readonly List<NPCCar> all = new List<NPCCar>();
    Rigidbody rb;
    float currentSpeed;                  // m/s

    void OnEnable() { all.Add(this); }
    void OnDisable() { all.Remove(this); }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        currentSpeed = speedKmh / 3.6f;
    }

    void FixedUpdate()
    {
        float target = speedKmh / 3.6f;
        foreach (var other in all)
        {
            if (other == this || Mathf.Abs(other.laneOffset - laneOffset) > 1f) continue;
            float gap = Mathf.Repeat(other.distance - distance, path.Length);
            if (gap < followGap) target = Mathf.Min(target, other.currentSpeed * (gap / followGap));
        }
        currentSpeed = Mathf.MoveTowards(currentSpeed, target, 8f * Time.fixedDeltaTime);
        distance = Mathf.Repeat(distance + currentSpeed * Time.fixedDeltaTime, path.Length);

        path.Sample(distance, out var pos, out var fwd);
        Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
        rb.MovePosition(pos + right * laneOffset);
        rb.MoveRotation(Quaternion.LookRotation(fwd, Vector3.up));

        float spin = currentSpeed * Time.fixedDeltaTime / wheelRadius * Mathf.Rad2Deg;
        foreach (var w in wheels) if (w) w.Rotate(spin, 0f, 0f, Space.Self);
    }
}
