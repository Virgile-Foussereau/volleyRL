using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using System.Collections.Generic;

public class VolleyballAgent : Agent
{
    public GameObject area;
    Rigidbody agentRb;
    public GameObject teamMate;

    Rigidbody teamMateRb;
    BehaviorParameters behaviorParameters;
    public Team teamId;

    public Role roleId;

    // To get ball's location for observations
    public GameObject ball;
    Rigidbody ballRb;

    // To get net's location for observations
    public GameObject net;

    Vector3 netPos;

    VolleyballSettings volleyballSettings;
    public VolleyballEnvController envController;

    // Controls jump behavior
    float jumpingTime;
    Vector3 jumpTargetPos;
    Vector3 jumpStartingPos;
    float agentRot;

    public Collider[] hitGroundColliders = new Collider[3];
    EnvironmentParameters resetParams;

    int[] lastActions;

    private VolleyballHeuristic heuristic;
    private Dictionary<ObservationTypes, float> observationsForHeuristic = new Dictionary<ObservationTypes, float>();
    void Start()
    {
        heuristic = new TwoVersusTwoBaseline();
        if (area != null)
            envController = area.GetComponent<VolleyballEnvController>();
    }

    public override void Initialize()
    {
        volleyballSettings = FindObjectOfType<VolleyballSettings>();
        behaviorParameters = GetComponent<BehaviorParameters>();

        agentRb = GetComponent<Rigidbody>();
        if (teamMate != null)
            teamMateRb = teamMate.GetComponent<Rigidbody>();
        ballRb = ball.GetComponent<Rigidbody>();

        if (net != null)
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
    void Set()
    {
        //calculate vector from agent to ball
        Vector3 agentToBall = ball.transform.position - transform.position;

        if (agentToBall.magnitude < volleyballSettings.agentRange)
        {
            ballRb.velocity = agentToBall.normalized * volleyballSettings.ballTouchSpeed;
            envController.UpdateLastHitter(teamId);
            envController.UpdateLastRole(roleId);
            envController.UpdateLastTouch(Touch.Set);
        }
    }

    void Smash()
    {
        //calculate vector from agent to ball
        Vector3 agentToBall = ball.transform.position - transform.position;
        if (agentToBall.magnitude < volleyballSettings.agentRange)
        {
            if (envController.GetLastHitter() == teamId &&
                envController.GetLastRole() == Role.Setter && roleId == Role.Hitter)
            {
                // add reward to teamMate
                teamMate.GetComponent<VolleyballAgent>().AddReward(0.5f);
                // add reward to agent
                AddReward(0.3f);
            }
            Vector3 planeNormal = Vector3.Cross(Vector3.up, agentToBall.normalized);
            Vector3 smashDir = Vector3.Cross(planeNormal, Vector3.up);
            ballRb.velocity = smashDir * volleyballSettings.ballSmashSpeed;

            envController.UpdateLastHitter(teamId);
            envController.UpdateLastRole(roleId);
            envController.UpdateLastTouch(Touch.Smash);
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
            envController.UpdateLastRole(roleId);
        }
    }

    /// <summary>
    /// Starts the jump sequence
    /// </summary>
    public void Jump()
    {
        if (roleId == Role.Setter)
        {
            AddReward(-0.1f);
        }
        jumpingTime = 0.2f;
        jumpStartingPos = agentRb.position;
    }

    /// <summary>
    /// Resolves the agent movement
    /// </summary>
    public void MoveAgent(ActionSegment<int> act)
    {
        var grounded = CheckIfGrounded();
        var dirToGo = Vector3.zero;
        var dirToGoForwardAction = act[0];
        var dirToGoSideAction = act[1];
        var jumpAction = act[2];
        var touchAction = act[3];

        //Debug.Log(teamId + " " + transform.forward);

        if (dirToGoForwardAction == 1)
            dirToGo += (grounded ? 1f : 0.5f) * Vector3.forward * 1f;
        else if (dirToGoForwardAction == 2)
            dirToGo += (grounded ? 1f : 0.5f) * Vector3.forward * -1f;
        if (dirToGoSideAction == 1)
            dirToGo += (grounded ? 1f : 0.5f) * Vector3.right;
        else if (dirToGoSideAction == 2)
            dirToGo += (grounded ? 1f : 0.5f) * Vector3.right * -1f;


        dirToGo = agentRot * dirToGo.normalized;

        if (jumpAction == 1)
            if (((jumpingTime <= 0f) && grounded))
            {
                Jump();
            }

        agentRb.AddForce(dirToGo * volleyballSettings.agentRunSpeed, ForceMode.VelocityChange);
        // Rotate the agent towards the direction it is moving
        if (dirToGo.magnitude != 0f)
        {
            agentRb.transform.rotation = Quaternion.LookRotation(dirToGo);
        }


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

        if (touchAction == 1)
        {
            if (grounded)
            {
                Set();
            }
            else
            {
                Smash();
            }
        }

    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        MoveAgent(actionBuffers.DiscreteActions);
        lastActions = actionBuffers.DiscreteActions.Array;
    }

    private void MakeHeuristicObservations()
    {
        observationsForHeuristic.Clear();
        observationsForHeuristic.Add(ObservationTypes.BALL_POSITION_X, ballRb.position.x * agentRot);
        observationsForHeuristic.Add(ObservationTypes.BALL_POSITION_Y, ballRb.position.y);
        observationsForHeuristic.Add(ObservationTypes.BALL_POSITION_Z, ballRb.position.z * agentRot);
        observationsForHeuristic.Add(ObservationTypes.TEAMMATE_POSITION_X, teamMateRb.position.x * agentRot);
        observationsForHeuristic.Add(ObservationTypes.TEAMMATE_POSITION_Y, teamMateRb.position.y);
        observationsForHeuristic.Add(ObservationTypes.TEAMMATE_POSITION_Z, teamMateRb.position.z * agentRot);
        observationsForHeuristic.Add(ObservationTypes.PLAYER_POSITION_X, this.transform.position.x * agentRot);
        observationsForHeuristic.Add(ObservationTypes.PLAYER_POSITION_Y, this.transform.position.y);
        observationsForHeuristic.Add(ObservationTypes.PLAYER_POSITION_Z, this.transform.position.z * agentRot);
        observationsForHeuristic.Add(ObservationTypes.BALL_VELOCITY_X, ballRb.velocity.x * agentRot);
        observationsForHeuristic.Add(ObservationTypes.BALL_VELOCITY_Y, ballRb.velocity.y);
        observationsForHeuristic.Add(ObservationTypes.BALL_VELOCITY_Z, ballRb.velocity.z * agentRot);
        observationsForHeuristic.Add(ObservationTypes.PLAYER_MASS, agentRb.mass);
        observationsForHeuristic.Add(ObservationTypes.PLAYER_DRAG, agentRb.drag);
        observationsForHeuristic.Add(ObservationTypes.LAST_HITTER_ROLE, (float)envController.GetLastRole());
        observationsForHeuristic.Add(ObservationTypes.LAST_HITTER_TEAM, (float)envController.GetLastHitter());
        observationsForHeuristic.Add(ObservationTypes.PLAYER_ROLE, (float)roleId);
        observationsForHeuristic.Add(ObservationTypes.PLAYER_TEAM, (float)teamId);
        observationsForHeuristic.Add(ObservationTypes.PLAYER_VELOCITY_X, agentRb.velocity.x * agentRot);
        observationsForHeuristic.Add(ObservationTypes.PLAYER_VELOCITY_Y, agentRb.velocity.y);
        observationsForHeuristic.Add(ObservationTypes.PLAYER_VELOCITY_Z, agentRb.velocity.z * agentRot);
        observationsForHeuristic.Add(ObservationTypes.NET_POSITION_X, netPos.x * agentRot);
        observationsForHeuristic.Add(ObservationTypes.NET_POSITION_Y, netPos.y);
        observationsForHeuristic.Add(ObservationTypes.NET_POSITION_Z, netPos.z * agentRot);
        observationsForHeuristic.Add(ObservationTypes.PLAYER_GROUNDED, CheckIfGrounded() ? 1f : 0f);



    }

    public override void CollectObservations(VectorSensor sensor)
    {

        observationsForHeuristic.Clear();
        // vector to ball (vector3)  
        Vector3 toBall = new Vector3((ballRb.transform.position.x - this.transform.position.x) * agentRot,
        (ballRb.transform.position.y - this.transform.position.y),
        (ballRb.transform.position.z - this.transform.position.z) * agentRot);

        sensor.AddObservation(toBall.normalized);

        // Distance to ball (float)
        sensor.AddObservation(toBall.magnitude);
        // vector to teammate (vector3)
        Vector3 toTeamMate = new Vector3((teamMateRb.transform.position.x - this.transform.position.x) * agentRot,
        (teamMateRb.transform.position.y - this.transform.position.y),
        (teamMateRb.transform.position.z - this.transform.position.z) * agentRot);

        sensor.AddObservation(toTeamMate.normalized);
        // Distance to teammate (float)
        sensor.AddObservation(toTeamMate.magnitude);

        // Ball velocity (3 floats)
        sensor.AddObservation(ballRb.velocity.y);
        sensor.AddObservation(ballRb.velocity.z * agentRot);
        sensor.AddObservation(ballRb.velocity.x * agentRot);
        // last player touch (int)
        Team lastHitter = envController.GetLastHitter();
        Role lastRole = envController.GetLastRole();
        if (lastHitter == teamId)
        {
            if (lastRole == roleId)
            {
                sensor.AddObservation(0);
            }
            else
            {
                sensor.AddObservation(1);
            }
        }
        else
        {
            sensor.AddObservation(2);
        }

    }

    // For human controller
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        /*var discreteActionsOut = actionsOut.DiscreteActions;
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
        discreteActionsOut[2] = Input.GetKey(KeyCode.Space) ? 1 : 0;*/
        MakeHeuristicObservations();
        Dictionary<ActionTypes, int> actions = heuristic.MakeDecisions(observationsForHeuristic, volleyballSettings);
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = actions[ActionTypes.Z_MOVEMENT];
        discreteActionsOut[1] = actions[ActionTypes.X_MOVEMENT];
        discreteActionsOut[2] = actions[ActionTypes.JUMP];
        discreteActionsOut[3] = actions[ActionTypes.TOUCH];

    }

    public int[] GetLastActions()
    {
        return lastActions;
    }
}
