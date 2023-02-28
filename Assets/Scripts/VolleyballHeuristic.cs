using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;

public enum ObservationTypes
{
    PLAYER_POSITION_X,
    PLAYER_POSITION_Y,
    PLAYER_POSITION_Z,
    PLAYER_VELOCITY_X,
    PLAYER_VELOCITY_Y,
    PLAYER_VELOCITY_Z,
    PLAYER_MASS,
    PLAYER_DRAG,
    BALL_POSITION_X,
    BALL_POSITION_Y,
    BALL_POSITION_Z,
    TEAMMATE_POSITION_X,
    TEAMMATE_POSITION_Y,
    TEAMMATE_POSITION_Z,
    BALL_VELOCITY_X,
    BALL_VELOCITY_Y,
    BALL_VELOCITY_Z,
    LAST_HITTER_TEAM,
    LAST_HITTER_ROLE,
    PLAYER_TEAM,
    PLAYER_ROLE,

    NET_POSITION_X,
    NET_POSITION_Y,
    NET_POSITION_Z,

    PLAYER_GROUNDED,


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

