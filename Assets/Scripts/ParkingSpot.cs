using UnityEngine;

// A parking bay on the ground. Local X = bay width, local Z = bay length.
// The car counts as parked when all four corners of its footprint are inside the bay.
public class ParkingSpot : MonoBehaviour
{
    public Vector2 size = new Vector2(3.6f, 6.2f);
    public Renderer fill;                    // highlighted while the car is inside
    public Color emptyColor = new Color(0.1f, 0.8f, 0.2f, 0.35f);
    public Color insideColor = new Color(1f, 0.85f, 0.1f, 0.6f);

    public bool Contains(Transform car, Vector2 carHalfSize)
    {
        Vector3[] corners =
        {
            car.TransformPoint(new Vector3(-carHalfSize.x, 0f, -carHalfSize.y)),
            car.TransformPoint(new Vector3( carHalfSize.x, 0f, -carHalfSize.y)),
            car.TransformPoint(new Vector3(-carHalfSize.x, 0f,  carHalfSize.y)),
            car.TransformPoint(new Vector3( carHalfSize.x, 0f,  carHalfSize.y)),
        };
        foreach (var c in corners)
        {
            Vector3 p = transform.InverseTransformPoint(c);
            if (Mathf.Abs(p.x) > size.x * 0.5f || Mathf.Abs(p.z) > size.y * 0.5f) return false;
        }
        return true;
    }

    public void SetHighlight(bool inside)
    {
        if (fill) fill.material.SetColor("_BaseColor", inside ? insideColor : emptyColor);
    }
}
