
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
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 velocity = transform.InverseTransformDirection(carController.Velocity);
        sensor.AddObservation(velocity.x);
        sensor.AddObservation(velocity.z);
        CollectRaycasts(sensor);
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


    public override void OnActionReceived(ActionBuffers actions)
    {
        float steering = actions.ContinuousActions[0];
        float throttle = actions.ContinuousActions[1];
        carController.SetAgentInput(steering, throttle);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var actions = actionsOut.ContinuousActions;
        Vector2 movement = UnityEngine.InputSystem.InputSystem
                .actions["Move"].ReadValue<Vector2>();
        actions[0] = movement.x;
        actions[1] = movement.y;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(
            collision.gameObject.CompareTag("Machinery") ||
            collision.gameObject.CompareTag("Workers") ||
            collision.gameObject.CompareTag("Rocks")
        )
        {
            AddReward(-10f);
            EndEpisode();
        }
    }
}
