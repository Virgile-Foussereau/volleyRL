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
        Vector3 ballPosition = ball.transform.position;
        observations.Add(ObservationTypes.BALL_POSITION_X, ballPosition.x);
        observations.Add(ObservationTypes.BALL_POSITION_Y, ballPosition.y);
        observations.Add(ObservationTypes.BALL_POSITION_Z, ballPosition.z);
        observations.Add(ObservationTypes.TEAMMATE_POSITION_X, 0);
        observations.Add(ObservationTypes.TEAMMATE_POSITION_Y, 0);
        observations.Add(ObservationTypes.TEAMMATE_POSITION_Z, 0);
        observations.Add(ObservationTypes.BALL_VELOCITY_X, ball.velocity.x);
        observations.Add(ObservationTypes.BALL_VELOCITY_Y, ball.velocity.y);
        observations.Add(ObservationTypes.BALL_VELOCITY_Z, ball.velocity.z);
        observations.Add(ObservationTypes.LAST_HITTER_TEAM, 0);
        observations.Add(ObservationTypes.LAST_HITTER_ROLE, 0);
        observations.Add(ObservationTypes.PLAYER_TEAM, 0);
        observations.Add(ObservationTypes.PLAYER_ROLE, (int)role);
        observations.Add(ObservationTypes.PLAYER_POSITION_X, GetComponent<Rigidbody>().position.x);
        observations.Add(ObservationTypes.PLAYER_POSITION_Y, GetComponent<Rigidbody>().position.y);
        observations.Add(ObservationTypes.PLAYER_POSITION_Z, GetComponent<Rigidbody>().position.z);
        observations.Add(ObservationTypes.PLAYER_VELOCITY_X, GetComponent<Rigidbody>().velocity.x);
        observations.Add(ObservationTypes.PLAYER_VELOCITY_Y, GetComponent<Rigidbody>().velocity.y);
        observations.Add(ObservationTypes.PLAYER_VELOCITY_Z, GetComponent<Rigidbody>().velocity.z);
        observations.Add(ObservationTypes.PLAYER_MASS, GetComponent<Rigidbody>().mass);
        observations.Add(ObservationTypes.PLAYER_DRAG, GetComponent<Rigidbody>().drag);
        return observations;

    }
    void Set()
    {
        //calculate vector from agent to ball
        Vector3 agentToBall = ball.position - GetComponent<Rigidbody>().position;

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
        agentRb.AddForce(controlSignal * settings.agentRunSpeed, ForceMode.VelocityChange);
        if (actions[ActionTypes.TOUCH] == 1)
        {
            Set();
        }

    }
    // Update is called once per frame
    void FixedUpdate()
    {
        Dictionary<ObservationTypes, float> observations = MakeObservations();
        Dictionary<ActionTypes, int> actions = baseline.MakeDecisions(observations, settings);
        if (actions.Count > 0)
            MoveAgent(actions);

        baseline.aimTarget = target.position;
        /*baseline.SetObservations(observations);

        (Vector3, float) moveTarget = baseline.CalculateBestMoveTarget(ball.position, ball.velocity, target.position, settings);

        transform.position = moveTarget.Item1;

        if (Mathf.Abs(ball.position.y - moveTarget.Item2) <= Mathf.Abs(ball.position.y - Time.fixedDeltaTime * ball.velocity.y))
        {
            Set();
        }*/
    }
}
