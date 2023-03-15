# INF581 Project - Reinforcement Learning Volleyball 2v2 competitive set-up

![Ultimate Volleyball](https://i.imgur.com/fHRSvtO.gif)

<sub><sup>*A long rally of smashes between two teams using our RL models*</sup></sub>


This project is based on the ultimate volleyball environment built on [Unity ML-Agents](https://unity.com/products/machine-learning-agents) by Joy Zhang. Original code can be found [here](https://github.com/CoderOneHQ/ultimate-volleyball). In this environment, two agents play volleyball over a net. We have two goals in our project :

1. Improve the existing environment using reward engineering to increase training speed.
2. Develop and compare several methods to implement 2v2 volleyball games.

This branch contains the code corresponding to the 2v2 case.
To access the code for 1v1 case, please go to [this branch](https://github.com/Virgile-Foussereau/volleyRL/tree/main).
 
## Contents
1. [Getting started](#getting-started)
1. [Training](#training)
1. [Environment description](#environment-description)
1. [Baselines](#baselines)

## Getting Started
1. Install the [Unity ML-Agents toolkit](https:github.com/Unity-Technologies/ml-agents) (Release 19+) by following the [installation instructions](https://github.com/Unity-Technologies/ml-agents/blob/release_18_docs/docs/Installation.md).
2. Download or clone this repo containing the `volleyRL` Unity project.
3. Open the `volleyRL` project in Unity (Unity Hub → Projects → Add → Select root folder for this repo).
4. Load the `VolleyballMain` scene (Project panel → Assets → Scenes → `VolleyballMain.unity`).
5. Click the ▶ button at the top of the window. This will run the agent in inference mode using the provided baseline model.

## Training

1. If you previously changed Behavior Type to `Heuristic Only`, ensure that the Behavior Type is set back to `Default` (see [Heuristic Mode](#heuristic-mode)).
2. Activate the virtual environment containing your installation of `ml-agents`.
3. Make a copy of the [provided training config file](config/Volleyball.yaml) in a convenient working directory.
4. Run from the command line `mlagents-learn <path to config file> --run-id=<some_id> --time-scale=1`
    - Replace `<path to config file>` with the actual path to the file in Step 3
5. When you see the message "Start training by pressing the Play button in the Unity Editor", click ▶ within the Unity GUI.
6. From another terminal window, navigate to the same directory you ran Step 4 from, and run `tensorboard --logdir results` to observe the training process. 

For more detailed instructions, check the [ML-Agents getting started guide](https://github.com/Unity-Technologies/ml-agents/blob/release_18_docs/docs/Getting-Started.md).

## Environment Description 2v2 set-up
**Goal:** Get the ball to bounce in the opponent's side of the court while preventing the ball bouncing into your own court.

**Action space:**

4 discrete action branches:
- Forward motion (3 possible actions: forward, backward, no action)
- Side motion (3 possible actions: left, right, no action)
- Jump (2 possible actions: jump, no action)
- Touch (2 possible actions:touch, no action)

Action touch will either do a smash if the agent has jumped or a set if the agent is on the ground.

**Observation space:**

Total size: 15
- Normalised directional vector from agent to ball (3)
- Distance from agent to ball (1)
- Normalised directional vector from agent to team mate (3)
- Distance from agent to team mate (1)
- Ball X, Y, Z velocity (3)
- Agent X, Y, Z velocity (3)
- Last player to touch the ball (1)

**Reward function:**

The project contains some examples of how the reward function can be defined.
The base example gives a +1 reward each time the agent hits the ball over the net.
Accordingly to our first objective, we worked to develop a more complex reward function that would increase training speed. Please read our report to know more about it.

## Teams
Trained models are available to be used directly. To use them on a team, set each of the player behavior type to `Default` and the model you want in the `Model` parameter in unity. Use a Hitter model for player 1 and Setter model for player 2. The following teams are included:

### Main RL team
- `Hitter_RL.onnx` - Agent trained to specialize in smashing 
- `Setter_RL.onnx`- Agent trained to specialize in defense and set

### RL team trained without jump penalty (for comparison purpose)
- `Hitter_RL_without_jump_penalty.onnx` - Agent trained to specialize in smashing, without jump penalty 
- `Setter_RL_without_jump_penalty.onnx`- Agent trained to specialize in defense and set, without jump penalty for the hitter

### Hard-coded baseline
To use the hard-coded baseline, set the behavior type to `Heuristic only` for each player of the team.
