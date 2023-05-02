using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
public class CustomAgent : Agent
{   
    public GameObject goal;
    public float moveSpeed = 10.0f;
    public float rotationSpeed = 300.0f;
    public float jumpForce = 2.0f;
    public Rigidbody agentRigidbody;
    public float groundCheckDistance = 0.6f;

    private bool onPlatform = false;
    public GameObject[] platforms;
    private RayPerceptionSensorComponent3D rayPerceptionSensor;
    int maxAttempts = 100;
    int attempts = 0;
    public float previousDistanceToGoal = 0f;
    public float previousDistanceToDropOff = 0f;
    public float previousDistancetoInitialDropOff = 0f;
    public GameObject goalHolder;
    public GameObject dropOff;
    
    //set object tagname to team variable
    public string team;

    public bool hasFlag = false;
    private CaptureTheFlagEnvController envController;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 previousPosition;
    private Vector3 goalInitialPosition;
    private Vector3 dropOffInitialPosition;
    private float timeInSamePosition = 0.0f;
    public float positionThreshold = 0.5f; // Threshold distance to consider a new position
    public float maxTimeInSamePosition = 3.0f; // Maximum time allowed in the same position
    public float timeColliding = 0.0f;
    public int gridSize = 10; // The number of grid cells in each dimension
    private Vector2Int lastGridPosition; // The last grid cell the agent was in
    public LayerMask groundLayer; // Assign the ground layer in the Unity Inspector
    bool isCurrentlyColliding;
    public Vector3 flagPosition = Vector3.zero;
    public float dropOffThreshold = 1.5f;
    public float positionDifference = 0.0f;
    private void Awake()
    {
        agentRigidbody = GetComponent<Rigidbody>();
        rayPerceptionSensor = GetComponent<RayPerceptionSensorComponent3D>();
        team = gameObject.tag;
        envController = GetComponentInParent<CaptureTheFlagEnvController>();
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
        previousPosition = transform.localPosition;
        timeInSamePosition = 0.0f;
        goalInitialPosition = goal.transform.localPosition;
        dropOffInitialPosition = dropOff.transform.localPosition;
        flagPosition = dropOff.transform.localPosition;
    }

    private void FixedUpdate() {
        CheckGoalDistance();
        // Negative reward for each step the agent takes, to encourage it to finish the task quickly
        AddReward(-0.001f);
        if (isCurrentlyColliding) {
            timeColliding += Time.deltaTime;
            if (timeColliding > 2.5f) {
                AddReward(-0.1f);
                timeColliding = 0.0f;
                isCurrentlyColliding = false;
                if (hasFlag) {
                    envController.ResetFlag(team);
                }
                ResetAgent();
            }
        }
        
    }
    private Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt(worldPosition.x / gridSize);
        int z = Mathf.FloorToInt(worldPosition.z / gridSize);
        return new Vector2Int(x, z);
    }
    private Vector3 GridToWorld(Vector2Int gridPosition)
    {
        float x = gridPosition.x * gridSize + gridSize / 2f;
        float z = gridPosition.y * gridSize + gridSize / 2f;
        return new Vector3(x, 0, z);
    }
    private Vector3 FindValidPosition(float minX, float maxX, float minZ, float maxZ) {
        Vector3 newPosition = Vector3.zero;
        bool validPosition = false;

        while (!validPosition && attempts < maxAttempts) {
            newPosition = new Vector3(Random.Range(minX, maxX), 0.5f, Random.Range(minZ, maxZ));

            // Check if the path between the agent's new position and the goal is unobstructed
            if (!Physics.Linecast(transform.localPosition, newPosition, out RaycastHit hitInfo)) {
                validPosition = true;
            }
            attempts++;
    }
        return newPosition;
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        // Agent position, velocity, and rotation
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(agentRigidbody.velocity);
        sensor.AddObservation(transform.localRotation.eulerAngles.y);
        // On platform status
        sensor.AddObservation(onPlatform);
        //
        if (hasFlag || TeammateHasFlag()) {
            float distanceToDropOff = Vector3.Distance(transform.localPosition, dropOff.transform.localPosition);
            sensor.AddObservation(distanceToDropOff);
            sensor.AddObservation(dropOff.transform.localPosition);
        }
        else {
           
            float distanceToGoal = Vector3.Distance(transform.localPosition, goal.transform.localPosition);
            sensor.AddObservation(distanceToGoal);
            // Goal position
            sensor.AddObservation(goal.transform.localPosition);
        }

        // Platform heights
        foreach (GameObject platform in platforms)
        {
            sensor.AddObservation(platform.transform.localPosition.y);
        }
        sensor.AddObservation(hasFlag);
        sensor.AddObservation(TeammateHasFlag());
        sensor.AddObservation(enemyHasFlag());
        Vector2Int currentGridCell = new Vector2Int(Mathf.FloorToInt(transform.localPosition.x / gridSize), Mathf.FloorToInt(transform.localPosition.z / gridSize));
        sensor.AddObservation(currentGridCell);
        sensor.AddObservation(timeInSamePosition);


    }

    // Call this method when the agent reaches a platform
    public void OnReachPlatform()
    {
        onPlatform = true;
        AddReward(0.5f);
    }
    public void OnFallOffPlatform()
    {
        onPlatform = false;
        AddReward(-0.1f);
    }

    public void ResetAgent() {
        transform.localPosition = initialPosition;
        transform.localRotation = initialRotation;
        agentRigidbody.velocity = Vector3.zero;
        agentRigidbody.angularVelocity = Vector3.zero;
        hasFlag = false;
        goal.transform.parent = transform.parent;
        if (transform.localPosition.y < 0.5f) {
            transform.localPosition = new Vector3(transform.localPosition.x, 0.5f, transform.localPosition.z);
        }


    }
    public bool TeammateHasFlag()
    {
        foreach (var agent in envController.Agents)
        {
            if (agent.Agent.team == team && agent.Agent != this && agent.Agent.hasFlag)
            {
                //AddReward(0.001f);
                return true;
            }
        }
        return false;
    }
    public bool enemyHasFlag()
    {
        foreach (var agent in envController.Agents)
        {
            if (agent.Agent.team != team && agent.Agent != this && agent.Agent.hasFlag)
            {  
                //AddReward(-0.001f);
                return true;
            }
        }
        return false;
    }

    public void Tagged(bool hasFlag, bool shouldReset) {
        if (shouldReset) {
            ResetAgent();
            if (hasFlag) {
                envController.ResetFlag(team);
            }

        }
    }

    private void CheckGoalDistance() {

        // Add a small negative reward every step to encourage the agent to reach the goal quickly
        float distanceDifference;
        float currentDistanceToDropOff = Vector3.Distance(transform.localPosition, dropOff.transform.localPosition);
        float distanceToInitialDropOff = Vector3.Distance(transform.localPosition, dropOffInitialPosition);
        float currentDistanceToGoal = Vector3.Distance(transform.localPosition, goal.transform.localPosition);
        bool hasMoved = false;
        //if (((hasFlag || TeammateHasFlag()) && !enemyHasFlag()) || ( (!hasFlag && !TeammateHasFlag()) && enemyHasFlag()) || (TeammateHasFlag() && enemyHasFlag()))  {
        if (hasFlag && !enemyHasFlag()) {
            distanceDifference = previousDistanceToDropOff - currentDistanceToDropOff;
            positionDifference = Mathf.Abs(currentDistanceToDropOff- previousDistanceToDropOff);
            previousDistanceToDropOff = currentDistanceToDropOff;
        }
        // If you have the flag, stay on your side till teammate tag opponent with flag
        else if ( hasFlag && enemyHasFlag()) {
            distanceDifference = previousDistancetoInitialDropOff- distanceToInitialDropOff;
            previousDistancetoInitialDropOff= distanceToInitialDropOff;
            positionDifference = Mathf.Abs(distanceToInitialDropOff - previousDistancetoInitialDropOff);
        }
        else {
            distanceDifference = previousDistanceToGoal - currentDistanceToGoal;
            positionDifference = Mathf.Abs(currentDistanceToGoal - previousDistanceToGoal);
            previousDistanceToGoal = currentDistanceToGoal; 
        }
        if (positionDifference >= 0.05f) {
            hasMoved = true;
                /*timeInSamePosition += Time.deltaTime;
                if (timeInSamePosition > 3.0f)
                {
                    AddReward(-0.2f);
                }
                if (timeInSamePosition > 6.0f)
                {
                    AddReward(-0.5f);
                    ResetAgent();
                    if (hasFlag) {
                        envController.ResetFlag(team);
                    }
                    timeInSamePosition = 0f;
                }
            }
            else {
                timeInSamePosition = 0f;
            }
            */
        float reward = distanceDifference * 0.5f; // Experiment with different scaling factors.
        AddReward(reward);
        
        if (transform.localPosition.y < 0.0f) {
            ResetAgent();
            if (hasFlag) {
                envController.ResetFlag(team);
            }
        }
        
        //Add negative reward if the agent stays in the same position for too long
        Vector2Int currentGridPosition = WorldToGrid(transform.localPosition);
        if (currentGridPosition != lastGridPosition)
        {
            hasMoved = true;
            /*float distanceToGoal = Vector3.Distance(transform.localPosition, goal.transform.localPosition);
            float distanceToDropOff = Vector3.Distance(transform.localPosition, dropOff.transform.localPosition);
            float distanceToInitialDropOff = Vector3.Distance(transform.localPosition, dropOffInitialPosition);
            float distanceToLastGridGoal = Vector3.Distance(GridToWorld(lastGridPosition), goal.transform.localPosition);
            float distanceToLastGridDropOff = Vector3.Distance(GridToWorld(lastGridPosition), dropOff.transform.localPosition);
            float distanceToLastGridInitialDropOff = Vector3.Distance(GridToWorld(lastGridPosition), dropOffInitialPosition);
            // If the agent moved closer to the goal, give a positive reward
            if ((((hasFlag || TeammateHasFlag()) && !enemyHasFlag()) || ( (!hasFlag && !TeammateHasFlag()) && enemyHasFlag()) || (TeammateHasFlag() && enemyHasFlag())) && distanceToDropOff < distanceToLastGridDropOff)
            {
                AddReward(0.1f);
            }
            else if ((hasFlag && enemyHasFlag()) && distanceToInitialDropOff < distanceToLastGridInitialDropOff) {
                AddReward(0.1f);
            }
            else if (((!hasFlag && !TeammateHasFlag()) && !enemyHasFlag()) && distanceToGoal < distanceToLastGridGoal) {
                AddReward(-0.1f);
            }
            // If the agent moved away from the goal, give a negative reward
            else
            {
                AddReward(-0.1f);
            }
            */
            lastGridPosition = currentGridPosition;
        }
        // Reset timeInSamePosition if the agent has moved
        if (hasMoved) {
            timeInSamePosition = 0f;
        } else {
            timeInSamePosition += Time.deltaTime;
            if (timeInSamePosition > 3.0f) {
                AddReward(-0.2f);
            }
            if (timeInSamePosition > 6.0f) {
                AddReward(-0.5f);
                ResetAgent();
                if (hasFlag) {
                    envController.ResetFlag(team);
                }
                timeInSamePosition = 0f;
            }
        }
    }
    }
     private bool IsGrounded()
    {
        // Cast a ray downwards from the agent's position
        Ray ray = new Ray(agentRigidbody.position, Vector3.down);
        
        // Perform a raycast and check if it hits the ground layer within the specified distance
        return Physics.Raycast(ray, groundCheckDistance, groundLayer);
    }

    private void PickUpFlag() {
        goal.transform.parent = goalHolder.transform;
        goal.transform.localPosition = new Vector3(0f, 1f, 0f);
        goal.transform.localRotation = Quaternion.identity;
        hasFlag = true;
        AddReward(1.0f);
       //envController.FlagPickedUp(team);
    }

    private void DropOffFlag() {
        envController.FlagCaptured(team);
    }

    public override void OnActionReceived(ActionBuffers actions) {
        float moveForward = actions.ContinuousActions[0];
        float moveSideways = actions.ContinuousActions[1];
        float rotate = actions.ContinuousActions[2];
        bool jump = actions.DiscreteActions[0] > 0;


        // Calculate movement direction based on the agent's rotation
        Vector3 forwardMovement = transform.forward * moveForward;
        Vector3 sidewaysMovement = transform.right * moveSideways;
        Vector3 movement = (forwardMovement + sidewaysMovement).normalized * moveSpeed;
        // Adjust the agent's velocity directly
        Vector3 newVelocity = new Vector3(movement.x, agentRigidbody.velocity.y, movement.z);
        agentRigidbody.velocity = newVelocity;
        // negative reward for always jumping
        //if (jump) {
            //AddReward(-0.01f);
        //}
        // Apply rotation
        transform.Rotate(new Vector3(0, rotate, 0) * rotationSpeed * Time.deltaTime);
        if (jump && IsGrounded())
        {
            agentRigidbody.AddForce(new Vector3(0, jumpForce, 0), ForceMode.Impulse);
        }

    }

    public override void Heuristic(in ActionBuffers actionsOut) {
        // Get user inputs
        float moveForward = Input.GetAxis("Vertical");
        float moveSideways = Input.GetAxis("Horizontal");
        float rotate = Input.GetAxis("Mouse X");
        bool jump = Input.GetKey(KeyCode.Space);

        // Store continuous actions
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = moveForward;
        continuousActions[1] = moveSideways;
        continuousActions[2] = rotate;

        // Store discrete actions
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = jump ? 1 : 0;
    }

    void OnTriggerEnter(Collider other)
    {
        /*if (other.tag == "Flag") {
            AddReward(1f);
            floorMesh.material = winMaterial;
            EndEpisode();
        }*/
        if (other.gameObject.CompareTag("Wall") && hasFlag) {
            //AddReward(-1f);
            //envController.agentHitWallWithFlag(team);
            //ResetAgent();
            AddReward(-0.01f);
            agentRigidbody.velocity = Vector3.zero;
            agentRigidbody.angularVelocity = Vector3.zero;
        }
        else if (other.gameObject.CompareTag("Wall") && !hasFlag) {
            //AddReward(-2f);
            //envController.agentHitWallWithoutFlag(team);
            //ResetAgent();
            AddReward(-0.01f);
            agentRigidbody.velocity = Vector3.zero;
            agentRigidbody.angularVelocity = Vector3.zero;
        }
        //if (other.CompareTag("Platform") && IsGrounded() && transform.localPosition.y > 0.5f) {
            //OnReachPlatform();
        //}
        //if (onPlatform && other.CompareTag("Floor")) {
            //OnFallOffPlatform();
        //}
        if (other.CompareTag("RedFlag") && this.CompareTag("BlueAgent") && !TeammateHasFlag()) {
            PickUpFlag();
        }
        if (other.CompareTag("BlueFlag") && this.CompareTag("RedAgent") && !TeammateHasFlag()) {
            PickUpFlag();
        }
        if (other.CompareTag("BlueFlag") && this.CompareTag("BlueAgent") && hasFlag && !enemyHasFlag() && Vector3.Distance(transform.position, dropOff.transform.position) < dropOffThreshold)
         {
            DropOffFlag();
        }
        if (other.CompareTag("RedFlag") && this.CompareTag("RedAgent") && hasFlag && !enemyHasFlag() && Vector3.Distance(transform.position, dropOff.transform.position) < dropOffThreshold) {
            DropOffFlag();
        }
        if (this.CompareTag("BlueAgent") && other.gameObject.CompareTag("RedAgent"))
        {
            CustomAgent taggedAgent = other.gameObject.GetComponent<CustomAgent>();
            if (!hasFlag)
            {
                envController.CheckTag(this, taggedAgent);
            }
            else if (taggedAgent.hasFlag)
            {
                envController.CheckTag(taggedAgent, this);
            }
        }
        if (this.CompareTag("RedAgent") && other.gameObject.CompareTag("BlueAgent"))
        {
            CustomAgent taggedAgent = other.gameObject.GetComponent<CustomAgent>();
            if (!hasFlag)
            {
                envController.CheckTag(this, taggedAgent);
            }
            else if (taggedAgent.hasFlag)
            {
                envController.CheckTag(taggedAgent, this);
            }
        }
    }

    void OnCollisionStay(Collision other) {
        if (other.gameObject.CompareTag("Wall") && !isCurrentlyColliding) {
            //AddReward(-1f);
            //envController.agentHitWallWithFlag(team);
            //ResetAgent();
            isCurrentlyColliding = true;
            agentRigidbody.velocity = Vector3.zero;
            agentRigidbody.angularVelocity = Vector3.zero;
        }
        if (other.gameObject.CompareTag("Obstacle")) {
            AddReward(-0.5f);
            if (hasFlag) {
                envController.ResetFlag(team);
            }
            ResetAgent();
        }
    }
    void OnCollisionExit(Collision other) {
        if (other.gameObject.CompareTag("Wall")) {
            isCurrentlyColliding = false;
        }
    }
}