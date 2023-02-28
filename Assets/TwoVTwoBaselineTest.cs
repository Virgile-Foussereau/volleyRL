using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TwoVTwoBaselineTest : MonoBehaviour
{

    public Rigidbody ball;
    public Role role;
    public Transform target;
    TwoVersusTwoBaseline baseline;
    VolleyballSettings settings;
    Rigidbody agentRb;


    // Start is called before the first frame update
    void Start()
    {
        baseline = new TwoVersusTwoBaseline();
        settings = FindObjectOfType<VolleyballSettings>();
        agentRb = GetComponent<Rigidbody>();
    }

    Dictionary<ObservationTypes, float> MakeObservations()
    {
        Dictionary<ObservationTypes, float> observations = new Dictionary<ObservationTypes, float>();
        Vector3 ballDirection = ball.transform.position - transform.position;
        observations.Add(ObservationTypes.BALL_DIRECTION_X, ballDirection.normalized.x);
        observations.Add(ObservationTypes.BALL_DIRECTION_Y, ballDirection.normalized.y);
        observations.Add(ObservationTypes.BALL_DIRECTION_Z, ballDirection.normalized.z);
        observations.Add(ObservationTypes.BALL_DISTANCE, ballDirection.magnitude);
        observations.Add(ObservationTypes.TEAMMATE_DIRECTION_X, 0);
        observations.Add(ObservationTypes.TEAMMATE_DIRECTION_Y, 0);
        observations.Add(ObservationTypes.TEAMMATE_DIRECTION_Z, 0);
        observations.Add(ObservationTypes.TEAMMATE_DISTANCE, 0);
        observations.Add(ObservationTypes.BALL_VELOCITY_X, ball.velocity.x);
        observations.Add(ObservationTypes.BALL_VELOCITY_Y, ball.velocity.y);
        observations.Add(ObservationTypes.BALL_VELOCITY_Z, ball.velocity.z);
        observations.Add(ObservationTypes.LAST_HITTER_TEAM, 0);
        observations.Add(ObservationTypes.LAST_HITTER_ROLE, 0);
        observations.Add(ObservationTypes.PLAYER_TEAM, 0);
        observations.Add(ObservationTypes.PLAYER_ROLE, (int)role);
        observations.Add(ObservationTypes.PLAYER_POSITION_X, transform.position.x);
        observations.Add(ObservationTypes.PLAYER_POSITION_Y, transform.position.y);
        observations.Add(ObservationTypes.PLAYER_POSITION_Z, transform.position.z);
        return observations;

    }
    void Set()
    {
        //calculate vector from agent to ball
        Vector3 agentToBall = ball.position - transform.position;

        if (agentToBall.magnitude < settings.agentRange)
        {
            ball.GetComponent<Rigidbody>().velocity = agentToBall.normalized * settings.ballTouchSpeed;
        }
    }

    void MoveAgent(Dictionary<ActionTypes, int> actions)
    {
        Vector3 controlSignal = Vector3.zero;
        if (actions[ActionTypes.X_MOVEMENT] == 1)
        {
            controlSignal.x = 1;
        }
        else if (actions[ActionTypes.X_MOVEMENT] == 2)
        {
            controlSignal.x = -1;
        }
        if (actions[ActionTypes.Z_MOVEMENT] == 1)
        {
            controlSignal.z = 1;
        }
        else if (actions[ActionTypes.Z_MOVEMENT] == 2)
        {
            controlSignal.z = -1;
        }
        agentRb.velocity = controlSignal * settings.agentRunSpeed;
        if (actions[ActionTypes.TOUCH] == 1)
        {
            Set();
        }

    }
    // Update is called once per frame
    void Update()
    {
        Dictionary<ObservationTypes, float> observations = MakeObservations();
        Dictionary<ActionTypes, int> actions = baseline.MakeDecisions(observations, settings);
        if (actions.Count > 0)
            MoveAgent(actions);

        baseline.aimTarget = target.position - transform.position;
        //(Vector3, float) moveTarget = baseline.CalculateBestMoveTarget(ball.position - transform.position, ball.velocity, target.position - transform.position, settings);

        //transform.position = moveTarget.Item1 + transform.position;

        //if (ball.position.y - transform.position.y <= moveTarget.Item2)
        //{
        //    Set();
        //}

    }
}
