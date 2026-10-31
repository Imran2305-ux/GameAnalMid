using UnityEngine;

// Closed loop of road centre points (in driving order) that NPC cars follow.
public class RoadPath : MonoBehaviour
{
    public Vector3[] points;

    float[] cumulative;
    public float Length { get; private set; }

    void Awake() { Build(); }

    public void Build()
    {
        cumulative = new float[points.Length + 1];
        for (int i = 0; i < points.Length; i++)
            cumulative[i + 1] = cumulative[i] + Vector3.Distance(points[i], points[(i + 1) % points.Length]);
        Length = cumulative[points.Length];
    }

    // Position and forward direction at a distance along the loop.
    public void Sample(float distance, out Vector3 pos, out Vector3 forward)
    {
        if (cumulative == null || cumulative.Length != points.Length + 1 || Length <= 0f) Build();
        distance = Mathf.Repeat(distance, Length);
        int lo = 0, hi = points.Length;
        while (hi - lo > 1)
        {
            int mid = (lo + hi) / 2;
            if (cumulative[mid] <= distance) lo = mid; else hi = mid;
        }
        Vector3 a = points[lo], b = points[(lo + 1) % points.Length];
        float t = (distance - cumulative[lo]) / Mathf.Max(0.001f, cumulative[lo + 1] - cumulative[lo]);
        pos = Vector3.Lerp(a, b, t);
        Vector3 next = points[(lo + 2) % points.Length];
        forward = Vector3.Lerp(b - a, next - b, t).normalized;
    }
}
