
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class MineRobotAgent : Agent
{
    [Header("References")]
    public car carController;

    public override void Initialize()
    {
        if (carController == null)
            carController = GetComponent<car>();
    }

    public override void OnEpisodeBegin()
    {
        carController.SetAgentInput(0f, 0f);
        Debug.Log("ON EPISODE BEGIN");
    }

    public override void CollectObservations(
        VectorSensor sensor)
    {
        Vector3 velocity =
            carController.GetComponent<Rigidbody>()
            .transform.InverseTransformDirection(
                carController.GetComponent<Rigidbody>().linearVelocity
            );

        sensor.AddObservation(velocity.x);
        sensor.AddObservation(velocity.z);

        Debug.Log("COLLECT OBSERVATIONS");
    }

    public override void OnActionReceived(
        ActionBuffers actions)
    {
        float steering =
            actions.ContinuousActions[0];
        float throttle =
            actions.ContinuousActions[1];
        carController.SetAgentInput(steering, throttle);

        Debug.Log("ON ACTION RECEIVED");
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var actions = actionsOut.ContinuousActions;

        Vector2 movement = UnityEngine.InputSystem.InputSystem
                .actions["Move"].ReadValue<Vector2>();

        actions[0] = movement.x;
        actions[1] = movement.y;

        Debug.Log("HEURISTIC");
    }
}
