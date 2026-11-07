
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

    public float maxDistance = 100f;
    private float smoothedDistance;
    private bool distanceInitialized = false;
    private const float Alpha = 0.3f;


    private Vector2 previousPosition;
    private float stationaryTimer = 0f;

    private const float MinDisplacement = 1f;
    private const float StationaryTime = 5f;    

    private GameObject targetMarker;
    private LineRenderer targetBeam;
    private LineRenderer[] targetRings;

    private const float MarkerHeight = 7f;
    private const float MarkerRadius = 3f;

    public override void Initialize()
    {
        if (carController == null)
            carController = GetComponent<car>();
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        if (target != null)
            CreateTargetMarker();

    }

    public override void OnEpisodeBegin()
    {
        carController.SetAgentInput(0f, 0f);
        carController.ResetVelocity();
        transform.SetPositionAndRotation(initialPosition, initialRotation);


        distanceInitialized = false;
        smoothedDistance = 0f;

        previousPosition = new Vector2(
            transform.position.x,
            transform.position.z
        );

        stationaryTimer = 0f;
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

        // local orientation
        sensor.AddObservation(transform.up.x);
        sensor.AddObservation(transform.up.z);

        // local velocity
        Vector3 velocity = transform.InverseTransformDirection(carController.Velocity);
        sensor.AddObservation(velocity);

        // angular velocity
        Vector3 angularVelocity = transform.InverseTransformDirection(carController.AngularVelocity);
        sensor.AddObservation(angularVelocity);

        // raycasts
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
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore
            );

            float distance = hit
                ? hitInfo.distance / carController.rayDistance
                : 1f;

            bool isObstacle = false;
            bool isMine = false;

            if (hit)
            {
                int layer = hitInfo.collider.gameObject.layer;
                string layerName = LayerMask.LayerToName(layer);

                isObstacle = layer == LayerMask.NameToLayer("Obstacle");
                isMine = layer == LayerMask.NameToLayer("Mine");                
            }
            sensor.AddObservation(distance);
            sensor.AddObservation(isObstacle);
            sensor.AddObservation(isMine);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float steering = actions.ContinuousActions[0];
        float throttle = actions.ContinuousActions[1];

        float speed = carController.Velocity.magnitude;


        Debug.Log(
            $"[ACTION] Steering: {steering:F4} | " +
            $"Throttle: {throttle:F4} | " +
            $"Velocity: {carController.Velocity.magnitude:F4}"
        );

        carController.SetAgentInput(steering, throttle);

        if (speed > 2f)
            AddReward(-(speed - 2f) * 0.01f);

        if (CheckStationary())
            return;
        CalculateProgressReward();
        CheckTarget();
        CheckFall();
    }

    private void CalculateProgressReward()
    {
        Vector2 currentPos = new Vector2(
            transform.position.x,
            transform.position.z
        );

        Vector2 targetPos = new Vector2(
            target.position.x,
            target.position.z
        );

        float currentDistance = Vector2.Distance(currentPos, targetPos);

        if (!distanceInitialized)
        {
            smoothedDistance = currentDistance;
            distanceInitialized = true;
            return;
        }

        float previousSmoothedDistance = smoothedDistance;

        smoothedDistance =
            Alpha * currentDistance +
            (1f - Alpha) * smoothedDistance;

        float progress = previousSmoothedDistance - smoothedDistance;

        float reward = progress * 1000f;

        AddReward(reward);

        Debug.Log(
            $"[EMA] Current: {currentDistance:F5} | " +
            $"Smoothed: {smoothedDistance:F5} | " +
            $"Progress: {progress:F6} | " +
            $"Reward: {reward:F5}"
        );
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

    private void CheckFall()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        bool groundDetected = Physics.Raycast(
            origin,
            Vector3.down,
            out RaycastHit hit,
            20f,
            LayerMask.GetMask("Mine"),
            QueryTriggerInteraction.Ignore
        );

        if (!groundDetected)
        {
            AddReward(-5f);
            EndEpisode();
        }
    }

    private bool CheckStationary()
    {
        Vector2 currentPosition = new Vector2(
            transform.position.x,
            transform.position.z
        );

        float displacement = Vector2.Distance(
            currentPosition,
            previousPosition
        );

        if (displacement >= MinDisplacement)
        {
            stationaryTimer = 0f;
            previousPosition = currentPosition;
            return false;
        }

        stationaryTimer += Time.fixedDeltaTime;

        if (stationaryTimer >= StationaryTime)
        {
            AddReward(-2f);

            Debug.Log(
                $"[STATIONARY] Displacement: {displacement:F4}m | " +
                $"Time: {stationaryTimer:F2}s | Ending episode"
            );

            EndEpisode();
            return true;
        }

        return false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            AddReward(-5f);
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var actions = actionsOut.ContinuousActions;
        Vector2 movement = UnityEngine.InputSystem.InputSystem
                .actions["Move"].ReadValue<Vector2>();
        actions[0] = movement.x;
        actions[1] = movement.y;
    }


    private void Update()
    {
        if (target == null || targetMarker == null)
            return;

        targetMarker.transform.position = target.position;

        float time = Time.time;

        // Haz vertical
        targetBeam.positionCount = 2;
        targetBeam.SetPosition(0, Vector3.up * 0.2f);
        targetBeam.SetPosition(1, Vector3.up * MarkerHeight);

        // Anillos animados
        for (int i = 0; i < targetRings.Length; i++)
        {
            float height = 0.2f + i * 0.12f;
            float radius = MarkerRadius + Mathf.Sin(time * 2f + i) * 0.15f;

            DrawRing(targetRings[i], radius, height);
        }

        // Diamante flotante
        Transform diamond = targetMarker.transform.Find("TargetDiamond");

        if (diamond != null)
        {
            diamond.localPosition = Vector3.up *
                (MarkerHeight + Mathf.Sin(time * 2f) * 0.4f);

            diamond.Rotate(Vector3.up * 90f * Time.deltaTime);
        }
    }

    private void DrawRing(LineRenderer line, float radius, float height)
    {
        for (int i = 0; i < 64; i++)
        {
            float angle = i * Mathf.PI * 2f / 64f;

            line.SetPosition(i, new Vector3(
                Mathf.Cos(angle) * radius,
                height,
                Mathf.Sin(angle) * radius
            ));
        }
    }

    private void CreateTargetMarker()
    {
        targetMarker = new GameObject("CrazyTaxi_TargetMarker");

        targetBeam = CreateLine(
            "TargetBeam",
            Color.yellow,
            0.25f,
            targetMarker.transform
        );

        targetRings = new LineRenderer[3];

        for (int i = 0; i < targetRings.Length; i++)
        {
            targetRings[i] = CreateLine(
                $"TargetRing_{i}",
                i == 1 ? Color.yellow : new Color(1f, 0.65f, 0f),
                0.12f,
                targetMarker.transform
            );
        }

        // Diamante flotante
        GameObject diamond = GameObject.CreatePrimitive(PrimitiveType.Cube);
        diamond.name = "TargetDiamond";
        diamond.transform.SetParent(targetMarker.transform);

        diamond.transform.localPosition = Vector3.up * MarkerHeight;
        diamond.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
        diamond.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);

        Destroy(diamond.GetComponent<Collider>());

        Material material = new Material(Shader.Find("Standard"));
        material.color = Color.yellow;
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", Color.yellow * 2f);

        diamond.GetComponent<Renderer>().material = material;
    }

    private LineRenderer CreateLine(
        string name,
        Color color,
        float width,
        Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);

        LineRenderer line = obj.AddComponent<LineRenderer>();

        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 64;
        line.startWidth = width;
        line.endWidth = width;

        Material material = new Material(
            Shader.Find("Sprites/Default")
        );

        material.color = color;
        line.material = material;
        line.startColor = color;
        line.endColor = color;

        return line;
    }
}
