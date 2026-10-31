using UnityEngine;

// Third-person camera that stays a fixed distance behind the car at any speed.
// Only the camera's swing around the car is smoothed, so it never falls behind on straights.
public class CarFollowCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 3f, -7.5f);
    [Tooltip("How quickly the camera swings round behind the car in turns.")]
    public float rotationSharpness = 5f;
    public float lookHeight = 1.2f;

    float yaw;

    void Start()
    {
        if (target) yaw = target.eulerAngles.y;
    }

    void LateUpdate()
    {
        if (!target) return;
        yaw = Mathf.LerpAngle(yaw, target.eulerAngles.y, 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime));
        transform.position = target.position + Quaternion.Euler(0f, yaw, 0f) * offset;
        transform.LookAt(target.position + Vector3.up * lookHeight);
    }
}
