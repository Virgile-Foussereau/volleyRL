using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents.Actuators;
public class TwoVersusTwoBaseline : VolleyballHeuristic
{
    Vector3 ballDirection;
    float ballDistance;
    Vector3 teammateDirection;
    float teammateDistance;
    Vector3 ballVelocity;
    Team lastHitterTeam;
    Role lastHitterRole;
    Team playerTeam;
    Role playerRole;

    Vector3 playerPosition;

    public Vector3 aimTarget;
    private void SetObservations(Dictionary<ObservationTypes, float> observations)
    {

        ballDirection = new Vector3(observations[ObservationTypes.BALL_DIRECTION_X], observations[ObservationTypes.BALL_DIRECTION_Y], observations[ObservationTypes.BALL_DIRECTION_Z]);
        ballDistance = observations[ObservationTypes.BALL_DISTANCE];
        teammateDirection = new Vector3(observations[ObservationTypes.TEAMMATE_DIRECTION_X], observations[ObservationTypes.TEAMMATE_DIRECTION_Y], observations[ObservationTypes.TEAMMATE_DIRECTION_Z]);
        teammateDistance = observations[ObservationTypes.TEAMMATE_DISTANCE];
        ballVelocity = new Vector3(observations[ObservationTypes.BALL_VELOCITY_X], observations[ObservationTypes.BALL_VELOCITY_Y], observations[ObservationTypes.BALL_VELOCITY_Z]);
        lastHitterTeam = (Team)(int)observations[ObservationTypes.LAST_HITTER_TEAM];
        lastHitterRole = (Role)(int)observations[ObservationTypes.LAST_HITTER_ROLE];
        playerTeam = (Team)(int)observations[ObservationTypes.PLAYER_TEAM];
        playerRole = (Role)(int)observations[ObservationTypes.PLAYER_ROLE];
        playerPosition = new Vector3(observations[ObservationTypes.PLAYER_POSITION_X], observations[ObservationTypes.PLAYER_POSITION_Y], observations[ObservationTypes.PLAYER_POSITION_Z]);
    }

    public (Vector3, float) CalculateBestMoveTarget(Vector3 ballPosition, Vector3 ballVelocity, Vector3 aimTarget, VolleyballSettings settings)
    {
        Vector3 maxTouchPoint = TrajectoryUtility.PredictImpactPoint(ballPosition, ballVelocity, 9.81f, settings.agentRange);
        Vector3 maxTouchPointSpeed = TrajectoryUtility.PredictImpactVelocity(ballPosition, ballVelocity, 9.81f, settings.agentRange);
        Vector3[] trajectory = TrajectoryUtility.CalculateTrajectory(maxTouchPoint, maxTouchPointSpeed, 9.81f, 100, 0.005f);
        for (int i = 0; i < trajectory.Length - 1; i++)
        {
            Debug.DrawLine(trajectory[i] + playerPosition, trajectory[i + 1] + playerPosition, Color.red);

        }
        for (int i = 0; i < trajectory.Length; i++)
        {
            Vector3 ur = TrajectoryUtility.FindInitialDirection3D(aimTarget, trajectory[i], settings.ballTouchSpeed, 9.81f);
            if (Mathf.Abs(trajectory[i].y / ur.y) <= settings.agentRange)
            {
                Vector3 moveTarget = trajectory[i] - ur * trajectory[i].y / ur.y;
                Debug.DrawLine(trajectory[i] + playerPosition, trajectory[i] + Vector3.up + playerPosition, Color.green);
                Vector3[] trajFromMoveTarget = TrajectoryUtility.CalculateTrajectory(moveTarget, ur * settings.ballTouchSpeed, 9.81f, 100, 0.1f);
                Debug.DrawLine(moveTarget + playerPosition, moveTarget + ur * settings.ballTouchSpeed + playerPosition, Color.blue, 0.5f);
                for (int j = 0; j < trajFromMoveTarget.Length - 1; j++)
                {

                    Debug.DrawLine(trajFromMoveTarget[j] + playerPosition, trajFromMoveTarget[j + 1] + playerPosition, Color.white);
                }
                return (moveTarget, trajectory[i].y);
            }
        }
        return (Vector3.zero, 0);
    }


    private Dictionary<ActionTypes, int> SetterStrategy(VolleyballSettings settings)
    {
        Dictionary<ActionTypes, int> actionsOut = new Dictionary<ActionTypes, int>();
        Vector3 ballPosition = ballDirection * ballDistance;
        //Debug.Log(ballPosition);
        (Vector3, float) moveTargetAndTouchY = CalculateBestMoveTarget(ballPosition, ballVelocity, aimTarget, settings);
        Vector3 moveTarget = moveTargetAndTouchY.Item1;
        float touchY = moveTargetAndTouchY.Item2;

        if (Mathf.Abs(ballPosition.y + ballVelocity.y * Time.fixedDeltaTime - touchY) < Mathf.Abs(ballPosition.y - touchY) || ballVelocity.y > 0)
        {
            if (Mathf.Abs(moveTarget.z - Time.fixedDeltaTime * settings.agentRunSpeed) < Mathf.Abs(moveTarget.z))
            {
                actionsOut.Add(ActionTypes.Z_MOVEMENT, 1);
            }
            else if (Mathf.Abs(moveTarget.z + Time.fixedDeltaTime * settings.agentRunSpeed) < Mathf.Abs(moveTarget.z))
            {
                actionsOut.Add(ActionTypes.Z_MOVEMENT, 2);
            }
            else
            {
                actionsOut.Add(ActionTypes.Z_MOVEMENT, 0);
            }

            if (Mathf.Abs(moveTarget.x - Time.fixedDeltaTime * settings.agentRunSpeed) < Mathf.Abs(moveTarget.x))
            {
                actionsOut.Add(ActionTypes.X_MOVEMENT, 1);
            }
            else if (Mathf.Abs(moveTarget.x + Time.fixedDeltaTime * settings.agentRunSpeed) < Mathf.Abs(moveTarget.x))
            {
                actionsOut.Add(ActionTypes.X_MOVEMENT, 2);
            }
            else
            {
                actionsOut.Add(ActionTypes.X_MOVEMENT, 0);

            }
            actionsOut.Add(ActionTypes.TOUCH, 0);
        }
        else
        {
            Debug.Log(touchY);
            Debug.DrawLine(moveTarget + playerPosition, moveTarget + (ballPosition - moveTarget).normalized * settings.ballTouchSpeed + playerPosition, Color.yellow, 0.5f);
            actionsOut.Add(ActionTypes.TOUCH, 1);
            actionsOut.Add(ActionTypes.X_MOVEMENT, 0);
            actionsOut.Add(ActionTypes.Z_MOVEMENT, 0);
        }

        actionsOut.Add(ActionTypes.JUMP, 0);
        return actionsOut;

    }

    public Dictionary<ActionTypes, int> MakeDecisions(Dictionary<ObservationTypes, float> observations, VolleyballSettings settings)
    {
        SetObservations(observations);

        if (playerRole == Role.Setter)
        {
            return SetterStrategy(settings);
        }
        return new Dictionary<ActionTypes, int>();

    }
}
