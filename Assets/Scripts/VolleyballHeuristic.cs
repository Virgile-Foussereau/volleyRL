using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;

public enum ObservationTypes
{
    BALL_DIRECTION_X,
    BALL_DIRECTION_Y,
    BALL_DIRECTION_Z,
    BALL_DISTANCE,
    TEAMMATE_DIRECTION_X,
    TEAMMATE_DIRECTION_Y,
    TEAMMATE_DIRECTION_Z,
    TEAMMATE_DISTANCE,
    BALL_VELOCITY_X,
    BALL_VELOCITY_Y,
    BALL_VELOCITY_Z,
    LAST_HITTER_TEAM,
    LAST_HITTER_ROLE,
    PLAYER_TEAM,
    PLAYER_ROLE,
    //debug only, don't use for actual heuristics
    PLAYER_POSITION_X,
    PLAYER_POSITION_Y,
    PLAYER_POSITION_Z,


}
public enum ActionTypes
{
    X_MOVEMENT,
    Z_MOVEMENT,
    JUMP,
    TOUCH

}
public interface VolleyballHeuristic
{
    public Dictionary<ActionTypes, int> MakeDecisions(Dictionary<ObservationTypes, float> observations, VolleyballSettings settings);
}

