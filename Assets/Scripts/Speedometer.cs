using UnityEngine;
using UnityEngine.UI;

// Rotates the needle over the SM.png dial: 0 km/h at +120°, 240 km/h at -120° (Z rotation).
public class Speedometer : MonoBehaviour
{
    public CarController car;
    public RectTransform needle;
    public Text speedText;
    public float maxDialSpeed = 240f;
    public float zeroAngle = 120f;
    public float maxAngle = -120f;
    public float needleSmoothing = 10f;

    float shownSpeed;

    void Update()
    {
        if (!car || !needle) return;
        shownSpeed = Mathf.Lerp(shownSpeed, car.SpeedKmh, 1f - Mathf.Exp(-needleSmoothing * Time.deltaTime));
        float t = Mathf.Clamp01(shownSpeed / maxDialSpeed);
        needle.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(zeroAngle, maxAngle, t));
        if (speedText) speedText.text = Mathf.RoundToInt(shownSpeed) + "\nkm/h";
    }
}
