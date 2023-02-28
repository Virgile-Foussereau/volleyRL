using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents.Actuators;
public class TwoVersusTwoBaseline : VolleyballHeuristic
{
    Vector3 ballPosition;
    Vector3 teammatePosition;
    Vector3 ballVelocity;
    Team lastHitterTeam;
    Role lastHitterRole;
    Team playerTeam;
    Role playerRole;

    Vector3 playerPosition;
    Vector3 playerVelocity;
    Vector3 netPosition;
    float playerMass;
    float playerDrag;

    public Vector3 aimTarget;

    Vector3 setPoint1 = new Vector3(0, 3, -3);
    Vector3 setPoint2 = new Vector3(0, 3, -4);
    bool grounded = false;
    public void SetObservations(Dictionary<ObservationTypes, float> observations)
    {

        ballPosition = new Vector3(observations[ObservationTypes.BALL_POSITION_X], observations[ObservationTypes.BALL_POSITION_Y], observations[ObservationTypes.BALL_POSITION_Z]);
        teammatePosition = new Vector3(observations[ObservationTypes.TEAMMATE_POSITION_X], observations[ObservationTypes.TEAMMATE_POSITION_Y], observations[ObservationTypes.TEAMMATE_POSITION_Z]);
        ballVelocity = new Vector3(observations[ObservationTypes.BALL_VELOCITY_X], observations[ObservationTypes.BALL_VELOCITY_Y], observations[ObservationTypes.BALL_VELOCITY_Z]);
        lastHitterTeam = (Team)(int)observations[ObservationTypes.LAST_HITTER_TEAM];
        lastHitterRole = (Role)(int)observations[ObservationTypes.LAST_HITTER_ROLE];
        playerTeam = (Team)(int)observations[ObservationTypes.PLAYER_TEAM];
        playerRole = (Role)(int)observations[ObservationTypes.PLAYER_ROLE];
        playerPosition = new Vector3(observations[ObservationTypes.PLAYER_POSITION_X], observations[ObservationTypes.PLAYER_POSITION_Y], observations[ObservationTypes.PLAYER_POSITION_Z]);
        playerVelocity = new Vector3(observations[ObservationTypes.PLAYER_VELOCITY_X], observations[ObservationTypes.PLAYER_VELOCITY_Y], observations[ObservationTypes.PLAYER_VELOCITY_Z]);
        playerMass = observations[ObservationTypes.PLAYER_MASS];
        playerDrag = observations[ObservationTypes.PLAYER_DRAG];
        netPosition = new Vector3(observations[ObservationTypes.NET_POSITION_X], observations[ObservationTypes.NET_POSITION_Y], observations[ObservationTypes.NET_POSITION_Z]);
        grounded = observations[ObservationTypes.PLAYER_GROUNDED] == 1;
    }

    public (Vector3, float) CalculateBestMoveTarget(Vector3 ballPosition, Vector3 ballVelocity, Vector3 aimTarget, VolleyballSettings settings)
    {
        Vector3 maxTouchPoint = TrajectoryUtility.PredictImpactPoint(ballPosition, ballVelocity, -Physics.gravity.y, settings.agentRange);
        Vector3 maxTouchPointSpeed = TrajectoryUtility.PredictImpactVelocity(ballPosition, ballVelocity, -Physics.gravity.y, settings.agentRange);
        Vector3[] trajectory = TrajectoryUtility.CalculateTrajectory(maxTouchPoint, maxTouchPointSpeed, -Physics.gravity.y, 100, 0.005f);
        for (int i = 0; i < trajectory.Length - 1; i++)
        {
            Debug.DrawLine(trajectory[i], trajectory[i + 1], Color.red);

        }
        for (int i = 0; i < trajectory.Length; i++)
        {
            Vector3 ur = TrajectoryUtility.FindInitialDirection3D(aimTarget, trajectory[i], settings.ballTouchSpeed, -Physics.gravity.y);
            if (Mathf.Abs((trajectory[i].y - playerPosition.y) / ur.y) <= settings.agentRange)
            {
                Vector3 moveTarget = trajectory[i] - ur * (trajectory[i].y - playerPosition.y) / ur.y;
                Debug.DrawLine(trajectory[i], trajectory[i] + Vector3.up, Color.green);
                Vector3[] trajFromMoveTarget = TrajectoryUtility.CalculateTrajectory(moveTarget, ur * settings.ballTouchSpeed, -Physics.gravity.y, 100, 0.1f);
                //Debug.DrawLine(moveTarget, moveTarget + ur * settings.ballTouchSpeed, Color.blue, 0.5f);
                for (int j = 0; j < trajFromMoveTarget.Length - 1; j++)
                {

                    Debug.DrawLine(trajFromMoveTarget[j], trajFromMoveTarget[j + 1], Color.white);
                }
                return (moveTarget, trajectory[i].y);
            }
        }
        return (Vector3.zero, 0);
    }

    private void MoveTo(Vector3 moveTarget, Dictionary<ActionTypes, int> actionsOut)
    {
        Vector3 stopPointIfNoControl = playerMass / playerDrag * playerVelocity + playerPosition;

        Debug.DrawLine(playerPosition, stopPointIfNoControl, Color.red);
        if (stopPointIfNoControl.z < moveTarget.z && moveTarget.z > playerPosition.z)
        {
            actionsOut.Add(ActionTypes.Z_MOVEMENT, 1);
        }
        else if (stopPointIfNoControl.z > moveTarget.z && moveTarget.z < playerPosition.z)
        {
            actionsOut.Add(ActionTypes.Z_MOVEMENT, 2);
        }
        else
        {
            actionsOut.Add(ActionTypes.Z_MOVEMENT, 0);
        }
        if (stopPointIfNoControl.x < moveTarget.x && moveTarget.x > playerPosition.x)
        {
            actionsOut.Add(ActionTypes.X_MOVEMENT, 1);
        }
        else if (stopPointIfNoControl.x > moveTarget.x && moveTarget.x < playerPosition.x)
        {
            actionsOut.Add(ActionTypes.X_MOVEMENT, 2);
        }
        else
        {
            actionsOut.Add(ActionTypes.X_MOVEMENT, 0);
        }
    }


    private Dictionary<ActionTypes, int> SetterStrategy(VolleyballSettings settings)
    {

        Dictionary<ActionTypes, int> actionsOut = new Dictionary<ActionTypes, int>();
        if (lastHitterTeam != playerTeam)
        {
            Vector3 landPoint = TrajectoryUtility.PredictImpactPoint(ballPosition, ballVelocity, -Physics.gravity.y, 5);
            int agentRot = playerTeam == Team.Purple ? 1 : -1;
            if (landPoint.z <= netPosition.z)
            {

                //Debug.Log(ballPosition);

                //aimTarget = netPosition + new Vector3(agentRot * setPoint1.x, setPoint1.y, agentRot * setPoint1.z);
                aimTarget = netPosition + setPoint1;
                (Vector3, float) moveTargetAndTouchY = CalculateBestMoveTarget(ballPosition, ballVelocity, aimTarget, settings);
                Vector3 moveTarget = moveTargetAndTouchY.Item1;
                float touchY = moveTargetAndTouchY.Item2;

                if (Mathf.Abs(ballPosition.y + ballVelocity.y * Time.fixedDeltaTime - touchY) < Mathf.Abs(ballPosition.y - touchY) || ballVelocity.y > 0)
                {

                    MoveTo(moveTarget, actionsOut);
                    actionsOut.Add(ActionTypes.TOUCH, 0);

                }
                else
                {
                    Debug.DrawLine(playerPosition, playerPosition + (ballPosition - playerPosition).normalized * settings.ballTouchSpeed, Color.yellow, 0.5f);
                    Debug.DrawLine(moveTarget, moveTarget + (ballPosition - moveTarget).normalized * settings.ballTouchSpeed, Color.black, 0.5f);
                    actionsOut.Add(ActionTypes.TOUCH, 1);
                    actionsOut.Add(ActionTypes.X_MOVEMENT, 0);
                    actionsOut.Add(ActionTypes.Z_MOVEMENT, 0);
                }

                actionsOut.Add(ActionTypes.JUMP, 0);
            }
            else
            {
                MoveTo(netPosition - Vector3.forward * 8, actionsOut);
                actionsOut.Add(ActionTypes.TOUCH, 0);
                actionsOut.Add(ActionTypes.JUMP, 0);

            }
        }
        else
        {
            actionsOut.Add(ActionTypes.TOUCH, 0);
            actionsOut.Add(ActionTypes.X_MOVEMENT, 0);
            actionsOut.Add(ActionTypes.Z_MOVEMENT, 0);
            actionsOut.Add(ActionTypes.JUMP, 0);
        }

        return actionsOut;

    }

    private Dictionary<ActionTypes, int> HitterStrategy(Dictionary<ObservationTypes, float> observations, VolleyballSettings settings)
    {
        Dictionary<ActionTypes, int> actionsOut = new Dictionary<ActionTypes, int>();
        if (lastHitterTeam == playerTeam && lastHitterRole == Role.Setter)
        {
            Vector3 hitPoint = TrajectoryUtility.PredictImpactPoint(ballPosition, ballVelocity, -Physics.gravity.y, 5);
            Vector3 jumpPoint = hitPoint - Vector3.forward * 0.8f;
            if (hitPoint.z > netPosition.z - 6)
            {
                if (grounded)
                {

                    Vector3 projectedJumpPoint = new Vector3(jumpPoint.x, playerPosition.y, jumpPoint.z);
                    MoveTo(projectedJumpPoint, actionsOut);
                    if (Vector3.Distance(projectedJumpPoint, playerPosition) < 0.05f && TrajectoryUtility.PredictPositionInTime(ballPosition, ballVelocity, -Physics.gravity.y, 0.2f).y < 5.5f)
                    {
                        actionsOut.Add(ActionTypes.JUMP, 1);

                    }
                    else
                    {
                        actionsOut.Add(ActionTypes.JUMP, 0);
                    }
                    actionsOut.Add(ActionTypes.TOUCH, 0);
                }
                else
                {
                    Vector3 ballDir = (ballPosition - playerPosition).normalized;
                    Vector3 smashImpact = TrajectoryUtility.PredictImpactPoint(ballPosition, ballDir * settings.ballSmashSpeed, -Physics.gravity.y, 0);
                    //Vector3 smashOverNet = TrajectoryUtility.PredictImpactPoint(ballPosition, ballDir * settings.ballSmashSpeed, -Physics.gravity.y, 3.8f);
                    if (Vector3.Distance(ballPosition, playerPosition) < settings.agentRange && smashImpact.z < netPosition.z + 15f)
                    {
                        actionsOut.Add(ActionTypes.TOUCH, 1);
                    }
                    else
                    {

                        actionsOut.Add(ActionTypes.TOUCH, 0);
                    }
                    actionsOut.Add(ActionTypes.JUMP, 0);
                    MoveTo(jumpPoint, actionsOut);
                }
            }
            else
            {
                (Vector3, float) moveTargetAndTouchY = CalculateBestMoveTarget(ballPosition, ballVelocity, netPosition + Vector3.forward * 9, settings);
                MoveTo(moveTargetAndTouchY.Item1, actionsOut);
                if (TrajectoryUtility.PredictPositionInTime(ballPosition, ballVelocity, -Physics.gravity.y, Time.fixedDeltaTime).y < moveTargetAndTouchY.Item2)
                {
                    actionsOut.Add(ActionTypes.TOUCH, 1);
                }
                else
                {
                    actionsOut.Add(ActionTypes.TOUCH, 0);
                }
                actionsOut.Add(ActionTypes.JUMP, 0);
            }
        }
        else
        {
            actionsOut.Add(ActionTypes.TOUCH, 0);
            actionsOut.Add(ActionTypes.JUMP, 0);
            MoveTo(setPoint1, actionsOut);
        }
        return actionsOut;
    }

    public Dictionary<ActionTypes, int> MakeDecisions(Dictionary<ObservationTypes, float> observations, VolleyballSettings settings)
    {
        SetObservations(observations);

        if (playerRole == Role.Setter)
        {
            return SetterStrategy(settings);
        }
        else
        {
            return HitterStrategy(observations, settings);
        }

    }
}
