using UnityEngine;
using UnityEngine.InputSystem;

// Arcade car driving on top of the prefab's WheelColliders.
// W/S or Up/Down = throttle/brake-reverse, A/D or Left/Right = steer, Space = handbrake.
[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [System.Serializable]
    public class Wheel
    {
        public WheelCollider collider;
        public Transform mesh;
        public bool steer;
        public bool motor;
    }

    public Wheel[] wheels;

    [Header("Driving")]
    public float maxSpeedKmh = 200f;
    public float acceleration = 26f;      // m/s^2
    public float brakeDeceleration = 35f; // m/s^2
    public float maxReverseKmh = 40f;
    public float turnSpeed = 75f;         // deg/s at full steer
    public float maxSteerAngle = 30f;     // visual wheel steer
    [Range(0f, 1f)] public float grip = 0.92f; // how much sideways slide is removed per physics step
    public float downforce = 0.3f;        // extra down-acceleration per m/s of speed
    public float wheelTorque = 400f;      // keeps the WheelColliders rolling

    [Header("Launch")]
    [Tooltip("Speed the car is given the moment it is launched (race start).")]
    [Range(0f, 200f)] public float launchSpeedKmh = 90f;

    [HideInInspector] public bool inputEnabled = true;

    Rigidbody rb;
    float throttle, steer;
    bool handbrake;

    public float SpeedKmh => rb ? rb.linearVelocity.magnitude * 3.6f : 0f;
    float ForwardSpeed => Vector3.Dot(rb.linearVelocity, transform.forward);

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = 1200f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.centerOfMass = new Vector3(0f, 0.3f, 0f);
    }

    // Shoots the car forward at launch speed so it is fast from the very start.
    public void Launch()
    {
        rb.linearVelocity = transform.forward * (launchSpeedKmh / 3.6f);
    }

    void Update()
    {
        throttle = 0f;
        steer = 0f;
        handbrake = false;
        var kb = Keyboard.current;
        if (!inputEnabled || kb == null) return;

        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) throttle += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) throttle -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) steer += 1f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) steer -= 1f;
        handbrake = kb.spaceKey.isPressed;
    }

    void FixedUpdate()
    {
        bool grounded = false;
        foreach (var w in wheels) grounded |= w.collider.isGrounded;
        float speed = ForwardSpeed;

        if (grounded)
        {
            if (throttle > 0f)
            {
                if (speed < maxSpeedKmh / 3.6f) rb.AddForce(transform.forward * acceleration * throttle, ForceMode.Acceleration);
            }
            else if (throttle < 0f)
            {
                if (speed > 0.5f) rb.AddForce(-transform.forward * brakeDeceleration, ForceMode.Acceleration);
                else if (speed > -maxReverseKmh / 3.6f) rb.AddForce(transform.forward * acceleration * 0.5f * throttle, ForceMode.Acceleration);
            }
            if (handbrake) rb.AddForce(-rb.linearVelocity.normalized * brakeDeceleration, ForceMode.Acceleration);

            // Steering: turn faster once moving, reversed when going backwards
            float speedFactor = Mathf.Clamp01(Mathf.Abs(speed) / 8f);
            float yaw = steer * turnSpeed * speedFactor * Mathf.Sign(speed) * Time.fixedDeltaTime;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, yaw, 0f));

            // Grip: remove sideways slide so the car follows its nose
            Vector3 v = rb.linearVelocity;
            float lateral = Vector3.Dot(v, transform.right);
            rb.linearVelocity = v - transform.right * lateral * (handbrake ? grip * 0.3f : grip);

            rb.AddForce(-transform.up * downforce * Mathf.Abs(speed), ForceMode.Acceleration);
        }

        // WheelColliders with exactly zero torque lock up at low speed and hold the car in place,
        // so a tiny torque keeps them rolling.
        float torque = throttle != 0f ? throttle * wheelTorque : 0.0001f;
        foreach (var w in wheels)
        {
            if (w.steer) w.collider.steerAngle = steer * maxSteerAngle;
            w.collider.motorTorque = torque;
            w.collider.brakeTorque = 0f;
        }
    }

    void LateUpdate()
    {
        foreach (var w in wheels)
        {
            if (!w.mesh) continue;
            w.collider.GetWorldPose(out var pos, out var rot);
            w.mesh.SetPositionAndRotation(pos, rot);
        }
    }
}
