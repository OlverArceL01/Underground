
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class MineRobotAgent : Agent
{
    [Header("References")]
    public car carController;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    [Header("Navigation")]
    public Transform target;

    [Header("Stuck Detection")]
    public float stuckSpeedThreshold = 0.2f;
    public float stuckTimeLimit = 5f;
    private float stuckTimer;
    private float lastThrottle;


    public float maxDistance = 100f;
    private float previousDistance;

    public override void Initialize()
    {
        if (carController == null)
            carController = GetComponent<car>();
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    public override void OnEpisodeBegin()
    {
        carController.SetAgentInput(0f, 0f);
        carController.ResetVelocity();
        transform.SetPositionAndRotation(initialPosition, initialRotation);
        previousDistance = Vector3.Distance(
            transform.position,
            target.position
        );
        stuckTimer = 0f;
        lastThrottle = 0f;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // target direction
        Vector3 direction = target.position - transform.position;
        Vector3 localDirection = transform.InverseTransformDirection(direction.normalized);
        sensor.AddObservation(localDirection);
        
        // target distance
        float distance = Vector3.Distance(
            transform.position,
            target.position
        );
        sensor.AddObservation(Mathf.Clamp01(distance / maxDistance));

        // local velocity
        Vector3 velocity = transform.InverseTransformDirection(carController.Velocity);
        sensor.AddObservation(velocity);

        // local orientation
        sensor.AddObservation(transform.up.x);
        sensor.AddObservation(transform.up.z);

        // angular velocity
        Vector3 angularVelocity = transform.InverseTransformDirection(carController.AngularVelocity);
        sensor.AddObservation(angularVelocity);

        // raycasts
        CollectRaycasts(sensor);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float steering = actions.ContinuousActions[0];
        float throttle = actions.ContinuousActions[1];
        carController.SetAgentInput(steering, throttle);
        lastThrottle = throttle;
    
        CalculateProgressReward();
        CheckTarget();
        CheckTilt();
        CheckStuck();
        AddReward(-0.001f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var actions = actionsOut.ContinuousActions;
        Vector2 movement = UnityEngine.InputSystem.InputSystem
                .actions["Move"].ReadValue<Vector2>();
        actions[0] = movement.x;
        actions[1] = movement.y;
    }

    private void CollectRaycasts(VectorSensor sensor)
    {
        foreach (Transform ray in carController.sensors)
        {
            bool hit = Physics.Raycast(
                ray.position,
                ray.forward,
                out RaycastHit hitInfo,
                carController.rayDistance,
                carController.obstacleMask
            );

            float distance = hit
                ? hitInfo.distance / carController.rayDistance
                : 1f;

            sensor.AddObservation(distance);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(
            collision.gameObject.CompareTag("Machinery") ||
            collision.gameObject.CompareTag("Workers") ||
            collision.gameObject.CompareTag("Rocks")
        )
        {
            AddReward(-5f);
            EndEpisode();
        }
    }

    private void CalculateProgressReward()
    {
        float currentDistance = Vector3.Distance(
            transform.position,
            target.position
        );
        float progress = previousDistance - currentDistance;
        AddReward(progress * 0.05f);
        previousDistance = currentDistance;
    }

    private void CheckTarget()
    {
        float distance = Vector3.Distance(
            transform.position,
            target.position
        );
        if (distance < 3f)
        {
            AddReward(5f);
            EndEpisode();
        }
    }

    private void CheckTilt()
    {
        float tilt = Vector3.Angle(
            transform.up,
            Vector3.up
        );
        if (tilt > 120f)
        {
            AddReward(-3f);
            EndEpisode();
        }
    }

    private void CheckStuck()
    {
        float speed = carController.Velocity.magnitude;

        if (lastThrottle > 0.2f && speed < stuckSpeedThreshold)
            stuckTimer += Time.fixedDeltaTime;
        else
            stuckTimer = 0f;

        if (stuckTimer >= stuckTimeLimit)
        {
            AddReward(-2f);
            EndEpisode();
        }
    }
}
