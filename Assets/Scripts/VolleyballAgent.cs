using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;

public class VolleyballAgent : Agent
{

    public GameObject area;
    Rigidbody agentRb;
    public GameObject teamMate;
    BehaviorParameters behaviorParameters;
    public Team teamId;

    // To get ball's location for observations
    public GameObject ball;
    Rigidbody ballRb;

    // To get net's location for observations
    public GameObject net;

    Vector3 netPos;

    VolleyballSettings volleyballSettings;
    VolleyballEnvController envController;

    // Controls jump behavior
    float jumpingTime;
    Vector3 jumpTargetPos;
    Vector3 jumpStartingPos;
    float agentRot;

    float mobilityReductionTime;
    float touchCD;

    public Collider[] hitGroundColliders = new Collider[3];
    EnvironmentParameters resetParams;

    private float terrainWidth = 15f;
    private float terrainLength = 15f;
    private Vector2[] importantPoints = new Vector2[4];

    void Start()
    {
        envController = area.GetComponent<VolleyballEnvController>();
        importantPoints = new Vector2[4];
        importantPoints[0] = new Vector2(-terrainWidth / 4f, terrainLength / 4f);
        importantPoints[1] = new Vector2(terrainWidth / 4f, terrainLength / 4f);
        importantPoints[2] = new Vector2(terrainWidth / 4f, 3 * terrainLength / 4f);
        importantPoints[3] = new Vector2(-terrainWidth / 4f, 3 * terrainLength / 4f);
    }

    public override void Initialize()
    {
        volleyballSettings = FindObjectOfType<VolleyballSettings>();
        behaviorParameters = GetComponent<BehaviorParameters>();


        agentRb = GetComponent<Rigidbody>();
        ballRb = ball.GetComponent<Rigidbody>();

        netPos = net.transform.position;

        // for symmetry between player side
        if (teamId == Team.Blue)
        {
            agentRot = -1;
        }
        else
        {
            agentRot = 1;
        }
        mobilityReductionTime = 0f;
        touchCD = 0f;
        resetParams = Academy.Instance.EnvironmentParameters;
    }

    /// <summary>
    /// Moves  a rigidbody towards a position smoothly.
    /// </summary>
    /// <param name="targetPos">Target position.</param>
    /// <param name="rb">The rigidbody to be moved.</param>
    /// <param name="targetVel">The velocity to target during the
    ///  motion.</param>
    /// <param name="maxVel">The maximum velocity posible.</param>
    void MoveTowards(
        Vector3 targetPos, Rigidbody rb, float targetVel, float maxVel)
    {
        var moveToPos = targetPos - rb.worldCenterOfMass;
        var velocityTarget = Time.fixedDeltaTime * targetVel * moveToPos;
        if (float.IsNaN(velocityTarget.x) == false)
        {
            rb.velocity = Vector3.MoveTowards(
                rb.velocity, velocityTarget, maxVel);
        }
    }
    void Touch()
    {
        //calculate vector from agent to ball
        Vector3 agentToBall = ball.transform.position - transform.position;

        if (agentToBall.magnitude < volleyballSettings.agentRange)
        {
            AddReward(0.1f);
            ballRb.velocity = agentToBall.normalized * volleyballSettings.ballTouchSpeed;
        }
    }

    void Smash()
    {
        //calculate vector from agent to ball
        Vector3 agentToBall = ball.transform.position - transform.position;
        if (agentToBall.magnitude < volleyballSettings.agentRange)
        {
            if (ballRb.velocity.z * agentRot < volleyballSettings.maxBallSpeedForSmash)
            {
                AddReward(0.15f);
                Vector3 planeNormal = Vector3.Cross(Vector3.up, agentToBall.normalized);
                Vector3 smashDir = Vector3.Cross(planeNormal, Vector3.up);
                ballRb.velocity = smashDir * volleyballSettings.ballSmashSpeed;
            }
            else
            {
                Touch();
            }
        }
    }

    /// <summary>
    /// Check if agent is on the ground to enable/disable jumping
    /// </summary>
    public bool CheckIfGrounded()
    {
        hitGroundColliders = new Collider[3];
        Physics.OverlapBoxNonAlloc(
            transform.localPosition + new Vector3(0, -0.05f, 0),
            new Vector3(0.95f / 2f, 0.5f, 0.95f / 2f),
            hitGroundColliders,
            transform.rotation);
        var grounded = false;
        foreach (var col in hitGroundColliders)
        {
            if (col != null && col.transform != transform &&
                (col.CompareTag("walkableSurface") ||
                 col.CompareTag("purpleGoal") ||
                 col.CompareTag("blueGoal")))
            {
                grounded = true; //then we're grounded
                break;
            }
        }
        return grounded;
    }

    /// <summary>
    /// Called when agent collides with the ball
    /// </summary>
    void OnCollisionEnter(Collision c)
    {
        if (c.gameObject.CompareTag("ball"))
        {
            envController.UpdateLastHitter(teamId);
            //AddReward(0.1f);
        }
    }

    /// <summary>
    /// Starts the jump sequence
    /// </summary>
    public void Jump()
    {

        jumpingTime = 0.2f;
        jumpStartingPos = agentRb.position;
    }

    public void UpdateFallingMovement(Vector3 dirToGo, bool grounded)
    {
        if (jumpingTime > 0f)
        {
            jumpTargetPos =
                new Vector3(agentRb.position.x,
                    jumpStartingPos.y + volleyballSettings.agentJumpHeight,
                    agentRb.position.z) + dirToGo;

            MoveTowards(jumpTargetPos, agentRb, volleyballSettings.agentJumpVelocity,
                volleyballSettings.agentJumpVelocityMaxChange);
        }

        if (!(jumpingTime > 0f) && !grounded)
        {
            agentRb.AddForce(
                Vector3.down * volleyballSettings.fallingForce, ForceMode.Acceleration);
        }

        if (jumpingTime > 0f)
        {
            jumpingTime -= Time.fixedDeltaTime;
        }
    }

    void RewardTerrainControl()
    {
        float sum = 0;
        for (int i = 0; i < 4; i++)
        {
            Vector3 actualPoint = netPos + new Vector3(-importantPoints[i].x, 0, -importantPoints[i].y) * agentRot;
            float pointDist = Mathf.Min(Vector3.Distance(teamMate.transform.position, actualPoint), Vector3.Distance(transform.position, actualPoint));
            sum += 1f / (pointDist + 1);
            //show actual point in the scene
            if (teamId == Team.Blue)
            {
                Debug.DrawLine(actualPoint, actualPoint + Vector3.up * 2f, Color.red);
            }
        }
        AddReward(0.002f * sum);
    }
    void OnBallAction(bool grounded)
    {
        if (grounded)
        {
            float distToBall = Vector3.Distance(ball.transform.position, transform.position);
            if (distToBall < volleyballSettings.agentRange)
            {
                if (touchCD <= 0f)
                {
                    Touch();
                    touchCD = volleyballSettings.touchCD;
                }
            }
            else
            {
                if (jumpingTime <= 0f && mobilityReductionTime <= 0f)
                {
                    Jump();
                    mobilityReductionTime = volleyballSettings.mobilityReductionTime;
                }
            }

        }
        if (!grounded && touchCD <= 0f)
        {
            Smash();
            touchCD = volleyballSettings.touchCD;
        }
    }

    /// <summary>
    /// Resolves the agent movement
    /// </summary>
    public void MoveAgent(ActionSegment<int> act)
    {

        if (mobilityReductionTime > 0f)
        {
            mobilityReductionTime -= Time.fixedDeltaTime;
        }
        if (touchCD > 0f)
        {
            touchCD -= Time.fixedDeltaTime;
        }

        var grounded = CheckIfGrounded();
        var dirToGo = Vector3.zero;
        var dirToGoForwardAction = act[0];
        var dirToGoSideAction = act[1];
        var ballAction = act[2];


        //Debug.Log(teamId + " " + transform.forward);
        if (dirToGoForwardAction == 1)
            dirToGo += (grounded ? 1f : 0.5f) * Vector3.forward * 1f;
        else if (dirToGoForwardAction == 2)
            dirToGo += (grounded ? 1f : 0.5f) * Vector3.forward * -1f;
        if (dirToGoSideAction == 1)
            dirToGo += (grounded ? 1f : 0.5f) * Vector3.right * -1f;
        else if (dirToGoSideAction == 2)
            dirToGo += (grounded ? 1f : 0.5f) * Vector3.right;


        dirToGo = agentRot * dirToGo.normalized;
        UpdateFallingMovement(dirToGo, grounded);


        agentRb.AddForce(dirToGo * volleyballSettings.agentRunSpeed * (mobilityReductionTime > 0f ? volleyballSettings.mobilityReductionFactor : 1f),
            ForceMode.VelocityChange);

        if (ballAction == 1)
        {
            OnBallAction(grounded);
        }


        // Rotate the agent towards the direction it is moving
        if (dirToGo.magnitude != 0f)
        {
            agentRb.transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(dirToGo), 0.1f);
        }

    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        MoveAgent(actionBuffers.DiscreteActions);
        RewardTerrainControl();
    }

    public override void CollectObservations(VectorSensor sensor)
    {

        // Ball position (vector3)  
        Vector3 ballPos = new Vector3(ball.transform.position.x * agentRot, ball.transform.position.y, ball.transform.position.z * agentRot);
        Vector3 teammatePos = new Vector3(teamMate.transform.position.x * agentRot, teamMate.transform.position.y, teamMate.transform.position.z * agentRot);
        Vector3 agentPos = new Vector3(transform.position.x * agentRot, transform.position.y, transform.position.z * agentRot);

        Vector3 toBall = ballPos - agentPos;
        sensor.AddObservation(toBall.normalized);
        sensor.AddObservation(toBall.magnitude);

        //net position (vector3)
        Vector3 toNet = netPos - agentPos;
        sensor.AddObservation(toNet.normalized);
        sensor.AddObservation(toNet.magnitude);

        // teammate position (vector3)
        Vector3 toTeammate = teammatePos - agentPos;
        sensor.AddObservation(toTeammate.normalized);
        sensor.AddObservation(toTeammate.magnitude);

        // Ball velocity (3 floats)
        sensor.AddObservation(ballRb.velocity.y);
        sensor.AddObservation(ballRb.velocity.z * agentRot);
        sensor.AddObservation(ballRb.velocity.x * agentRot);

        bool grounded = CheckIfGrounded();

        sensor.AddObservation(grounded);

    }

    // For human controller
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            // forward
            discreteActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            // backward
            discreteActionsOut[0] = 2;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            // move left
            discreteActionsOut[1] = 1;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            // move right
            discreteActionsOut[1] = 2;
        }
        discreteActionsOut[2] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }
}
