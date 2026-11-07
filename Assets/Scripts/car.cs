using System;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public class WheelProperties
{
    public int wheelState = 1;  // 1 = steerable wheel, 0 = free wheel
    [HideInInspector] public float biDirectional = 0; // optional advanced usage
    public Vector3 localPosition;        // wheel anchor in the car's local space
    public float turnAngle = 30f;        // max steer angle for this wheel

    [HideInInspector] public float lastSuspensionLength = 0.0f;
    [HideInInspector] public Vector3 localSlipDirection;
    [HideInInspector] public Vector3 worldSlipDirection;
    [HideInInspector] public Vector3 suspensionForceDirection;
    [HideInInspector] public Vector3 wheelWorldPosition;
    [HideInInspector] public float wheelCircumference;
    [HideInInspector] public float torque = 0.0f;
    [HideInInspector] public Rigidbody parentRigidbody;
    [HideInInspector] public GameObject wheelObject;
    [HideInInspector] public float hitPointForce;
    [HideInInspector] public Vector3 localVelocity;
}

public class car : MonoBehaviour
{
    [Header("Wheel Setup")]
    public GameObject wheelPrefab;
    public WheelProperties[] wheels;
    public float wheelSize = 0.53f;        // radius of the wheel
    public float maxTorque = 450f;         // maximum engine torque
    public float wheelGrip = 12f;          // how strongly it resists sideways slip
    public float maxGrip = 12f;          // how strongly it resists sideways slip
    public float frictionCoWheel = 0.022f; // rolling friction

    [Header("Suspension")]
    public float suspensionForce = 90f;       // spring constant
    public float dampAmount = 2.5f;           // damping constant
    public float suspensionForceClamp = 200f; // cap on total suspension force

    [Header("Car Mass")]
    public float massInKg = 100f; // (not strictly used, but you might incorporate it)

    // These are updated each frame
    [HideInInspector] public Vector2 input = Vector2.zero;  // horizontal=steering, vertical=gas/brake
    [HideInInspector] public bool Forwards = false;

    private Rigidbody rb;
    public Vector3 Velocity => rb.linearVelocity;
    public Vector3 AngularVelocity => rb.angularVelocity;

    [Header("Differential Steering")]
    public float turnSpeed = 1.5f;
    public float minTurnSpeed = 0.25f;

    [Header("Control")]
    public bool usePlayerInput = true;

    [Header("Raycast Sensors")]
    public float rayDistance = 30f;
    public Transform[] sensors;
    
    void Start()
    {
        // Grab or add a Rigidbody
        rb = GetComponent<Rigidbody>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody>();

        // Slight tweak to inertia if desired
        rb.inertiaTensor = 1.0f * rb.inertiaTensor;
 
        rb.centerOfMass = new Vector3(0f, -0.25f, 0f);

        // Create each wheel
        if (wheels != null)
        {
            for (int i = 0; i < wheels.Length; i++)
            {
                WheelProperties w = wheels[i];

                // Convert localPosition consistently
                Vector3 parentRelativePosition = transform.InverseTransformPoint(transform.TransformPoint(w.localPosition));
                w.localPosition = parentRelativePosition;

                // Instantiate the visual wheel
                w.wheelObject = Instantiate(wheelPrefab, transform);
                w.wheelObject.transform.localPosition = w.localPosition;
                w.wheelObject.transform.eulerAngles   = transform.eulerAngles;
                w.wheelObject.transform.localScale    = 2f * new Vector3(wheelSize, wheelSize, wheelSize);

                // Calculate wheel circumference for rotation logic
                w.wheelCircumference = 2f * Mathf.PI * wheelSize;

                w.parentRigidbody = rb;
            }
        }
    }

    void Update()
    {
        // Gather inputs
        if (usePlayerInput)
        {
            input = InputSystem.actions["Move"].ReadValue<Vector2>();
        }
        foreach (Transform ray in sensors)
        {
            bool hit = Physics.Raycast(
                ray.position,
                ray.forward,
                out RaycastHit hitInfo,
                rayDistance
            );

            Debug.DrawRay(
                ray.position,
                ray.forward * (hit ? hitInfo.distance : rayDistance),
                hit ? Color.red : Color.green
            );
        }
    }

    void FixedUpdate()
    {
        if (wheels == null || wheels.Length == 0) return;

        foreach (var wheel in wheels)
        {
            if (!wheel.wheelObject) continue;

            Transform wheelObj = wheel.wheelObject.transform;
            Transform wheelVisual = wheelObj.GetChild(0);

            // DIFFERENTIAL STEERING
            float steering = Mathf.Clamp(input.x, -1f, 1f);
            float throttle = Mathf.Clamp(input.y, -1f, 1f);

            // X < 0 = left, X > 0 = right
            float side = Mathf.Sign(wheel.localPosition.x);

            // Opposite speed adjustment on each side
            float differential = -steering * side * turnSpeed;
            
            float wheelSpeed = throttle + differential;
            wheelSpeed = Mathf.Clamp(wheelSpeed, -1f, 1f);

            wheel.torque = wheelSpeed * maxTorque / massInKg;

            // Determine world position and velocity
            wheel.wheelWorldPosition = transform.TransformPoint(wheel.localPosition);

            Vector3 velocityAtWheel =
                rb.GetPointVelocity(wheel.wheelWorldPosition);

            // Local velocity
            wheel.localVelocity =
                wheelObj.InverseTransformDirection(velocityAtWheel);

            // ROLLING FRICTION
            float rollingFrictionForce =
                -frictionCoWheel * wheel.localVelocity.z;

            // LATERAL FRICTION
            float lateralFriction =
                -wheelGrip * wheel.localVelocity.x;

            lateralFriction = Mathf.Clamp(
                lateralFriction,
                -maxGrip,
                maxGrip
            );

            // ENGINE FORCE
            float engineForce = wheel.torque / wheelSize;

            Vector3 totalLocalForce = new Vector3(
                lateralFriction,
                0f,
                rollingFrictionForce + engineForce
            );

            wheel.localSlipDirection = totalLocalForce;

            Vector3 totalWorldForce =
                wheelObj.TransformDirection(totalLocalForce);

            wheel.worldSlipDirection = totalWorldForce;

            Forwards = wheel.localVelocity.z > 0f;

            // SUSPENSION
            RaycastHit hit;

            if (Physics.Raycast(
                wheel.wheelWorldPosition,
                -transform.up,
                out hit,
                wheelSize * 2f))
            {
                float rayLen = wheelSize * 2f;

                float compression = rayLen - hit.distance;

                float damping =
                    (wheel.lastSuspensionLength - hit.distance) * dampAmount;

                float springForce =
                    (compression + damping) * suspensionForce;

                springForce = Mathf.Clamp(
                    springForce,
                    0f,
                    suspensionForceClamp
                );

                Vector3 springDir = hit.normal * springForce;

                wheel.suspensionForceDirection = springDir;

                rb.AddForceAtPosition(
                    springDir + totalWorldForce,
                    hit.point
                );

                // Keep wheel visible and positioned at suspension contact
                wheelObj.position = hit.point + transform.up * wheelSize;

                wheel.lastSuspensionLength = hit.distance;
            }
            else
            {
                wheelObj.position =
                    wheel.wheelWorldPosition - transform.up * wheelSize;
            }

            // VISUAL WHEEL ROTATION
            Vector3 forwardInWheelSpace =
                wheelObj.InverseTransformDirection(
                    rb.GetPointVelocity(wheel.wheelWorldPosition)
                );

            float wheelRotationSpeed =
                forwardInWheelSpace.z * 360f / wheel.wheelCircumference;

            wheelVisual.Rotate(
                Vector3.right,
                wheelRotationSpeed * Time.fixedDeltaTime,
                Space.Self
            );
        }
    }

    public void ResetVelocity()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public void SetAgentInput(float steering, float throttle)
    {
        input = new Vector2(
            Mathf.Clamp(steering, -1f, 1f),
            Mathf.Clamp(throttle, -1f, 1f)
        );
    }

    void OnDrawGizmos()
    {
        if (wheels == null) return;

        foreach (var wheel in wheels)
        {
            // Mark the wheel center
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(wheel.wheelWorldPosition, 0.08f);

            // Suspension force
            if (wheel.suspensionForceDirection != Vector3.zero)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(
                    wheel.wheelWorldPosition,
                    wheel.wheelWorldPosition + wheel.suspensionForceDirection * 0.01f
                );
            }

            // Slip/friction force
            if (wheel.worldSlipDirection != Vector3.zero)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(
                    wheel.wheelWorldPosition,
                    wheel.wheelWorldPosition + wheel.worldSlipDirection * 0.01f
                );
            }
        }
    }

}