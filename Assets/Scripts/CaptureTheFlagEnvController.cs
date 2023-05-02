using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
public class CaptureTheFlagEnvController : MonoBehaviour
{
    [System.Serializable]
     public class PlayerInfo
    {
        public CustomAgent Agent;
        [HideInInspector]
        public Vector3 StartingPos;
        [HideInInspector]
        public Quaternion StartingRot;
        [HideInInspector]
        public Rigidbody Rb;

    }
    public GameObject redFlag;
    public GameObject blueFlag;
    Vector3 startingRedFlagPos;
    Vector3 startingBlueFlagPos;
    public List<PlayerInfo> Agents = new List<PlayerInfo>();
    private SimpleMultiAgentGroup blueTeam;
    private SimpleMultiAgentGroup redTeam;
    private int blueTeamCount = 0;
    private int redTeamCount = 0;
    [Tooltip("Max Environment Steps")] public int MaxEnvironmentSteps = 5000;
    public int environmentStepCount = 0;
    [SerializeField] private Material blueTeamWinMaterial;
    [SerializeField] private Material redTeamWinMaterial;
    [SerializeField] private MeshRenderer floorMesh;
    private float tagCooldown = 0f;
    // Start is called before the first frame update
    void Start()
    {
        blueTeam = new SimpleMultiAgentGroup();
        redTeam = new SimpleMultiAgentGroup();
        startingBlueFlagPos = blueFlag.transform.localPosition;
        startingRedFlagPos = redFlag.transform.localPosition;
        foreach (var agent in Agents)
        {
            agent.StartingPos = agent.Agent.transform.localPosition;
            agent.StartingRot = agent.Agent.transform.localRotation;
            agent.Rb = agent.Agent.GetComponent<Rigidbody>();
            if (agent.Agent.team == "BlueAgent")
            {
                blueTeamCount++;
                blueTeam.RegisterAgent(agent.Agent);
            }
            else
            {
                redTeamCount++;
                redTeam.RegisterAgent(agent.Agent);
            }
        }
        
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        environmentStepCount++;
        if (environmentStepCount >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
        {
            ResetEnvironment();
            blueTeam.GroupEpisodeInterrupted();
            redTeam.GroupEpisodeInterrupted();
        };
        
    }
    public bool IsInRedSide(Vector3 pos)
    {
        return pos.z > 1;
    }
    public bool IsInBlueSide(Vector3 pos)
    {
        return pos.z < 1;
    }
    public void CheckTag(CustomAgent agent, CustomAgent taggedAgent)
    {
        if (Time.time < tagCooldown) return;
        tagCooldown = Time.time + 0.00001f; // Adjust the cooldown duration as needed
        bool shouldTag = false;
        if (!agent.hasFlag) {
            if (taggedAgent.tag == "BlueAgent")
            {
                if (taggedAgent.hasFlag || IsInRedSide(taggedAgent.transform.localPosition))
                {
                    shouldTag = true;
                }
            }
            else // agentTeam == "RedAgent"
            {
                if (taggedAgent.hasFlag || IsInBlueSide(taggedAgent.transform.localPosition))
                {
                    shouldTag = true;
                }
        }
        }

        if (shouldTag)
        {
            taggedAgent.Tagged(taggedAgent.hasFlag, shouldTag);
            agent.AddReward(0.0001f); // Reward for tagging
            taggedAgent.AddReward(-0.0001f); // Penalty for being tagged
        }
        //extra reward for tagging someone with the flag
        //if (taggedAgent.hasFlag)
                //{
                    //agent.AddReward(0.001f);
                //}
    }
    /*private void CheckGoalDistance()
    {  
        float blueTeamDistance = 0;
        float redTeamDistance = 0;

        //Add negative reward for each step, to make them finish quicker
        //blueTeam.AddGroupReward(-0.01f);
        //redTeam.AddGroupReward(-0.01f);

        foreach (var agentInfo in Agents)
        {
            CustomAgent agent = agentInfo.Agent;
            float distanceDifference;
            //ADd negative reward for each step, to make them finish quicker
            //agent.AddReward(-0.01f);
            //Add a reward for the agent to stay alive on enemy side
            if (agent.team == "BlueAgent")
            {
                if (IsInRedSide(agent.transform.localPosition))
                {
                    agent.AddReward(0.05f);
                }
            }
            else // team == "RedAgent"
            {
                if (IsInBlueSide(agent.transform.localPosition))
                {
                    agent.AddReward(0.05f);
                }
            }
            
            //Add a reward for the agent to defend teammate if they have the flag
            if (agent.TeammateHasFlag())
            {
                if (agent.team == "BlueAgent")
                {
                    blueTeam.AddGroupReward(0.01f);
                    // Add a negative reward for the agent if their flag was taken
                    redTeam.AddGroupReward(-0.01f);
                }
                else // team == "RedAgent"
                {
                    redTeam.AddGroupReward(0.01f);
                    // Add a negative reward for the agent if their flag was taken
                    blueTeam.AddGroupReward(-0.01f);
                }
            }

            if (agent.hasFlag)
            {
                //agent.AddReward(-0.01f);
                float currentDistanceToDropOff = Vector3.Distance(agent.transform.localPosition, agent.dropOff.transform.localPosition);
                distanceDifference = agent.previousDistanceToDropOff - currentDistanceToDropOff;
                agent.previousDistanceToDropOff = currentDistanceToDropOff;
            }
            else
            {
                //agent.AddReward(-0.05f);
                float currentDistanceToGoal = Vector3.Distance(agent.transform.localPosition, agent.goal.transform.localPosition);
                distanceDifference = agent.previousDistanceToGoal - currentDistanceToGoal;
                agent.previousDistanceToGoal = currentDistanceToGoal;
            }
            //agent.AddReward(distanceDifference * 0.1f); // Experiment with different scaling factors.
            if (agent.team == "BlueAgent")
            {
                blueTeamDistance += distanceDifference;
            }
            else // team == "RedAgent"
            {
                redTeamDistance += distanceDifference;
            } 

            //Add negative reward if agent fell off the map
            if (agent.transform.localPosition.y < 0.0f)
            {
                //agent.AddReward(-1.0f);
                //FellOffMap(agent.team);
                if (agent.hasFlag) {
                    ResetFlag(agent.team);
                }
            }


            //add a negative reward for unnecessary jumping
            if (Mathf.Abs(agent.agentRigidbody.velocity.y) > 0.01f) // The agent is in the air
            {
                if (agent.team == "BlueAgent")
                {
                    blueTeam.AddGroupReward(-0.02f);
                }
                else // team == "RedAgent"
                {
                    redTeam.AddGroupReward(-0.02f);
                }
                
                agent.AddReward(-0.01f);
            }
            
    
        }


        /*float averageBlueTeamDistance = blueTeamDistance / blueTeamCount;
        float averageRedTeamDistance = redTeamDistance / redTeamCount;
        float blueReward = averageBlueTeamDistance * 0.1f; // Experiment with different scaling factors.
        float redReward = averageRedTeamDistance * 0.1f; // Experiment with different scaling factors.
        // Add the group reward
        blueTeam.AddGroupReward(blueReward);
        redTeam.AddGroupReward(redReward);
    }
    */
    public void ResetFlag(string team)
    {
        // Reset the flag's position and parent
        if (team == "BlueAgent")
        {
            redFlag.transform.parent = transform;
            redFlag.transform.localPosition = startingRedFlagPos;
            redFlag.transform.localRotation = Quaternion.identity;
        }
        else
        {
            blueFlag.transform.parent = transform;
            blueFlag.transform.localPosition = startingBlueFlagPos;
            blueFlag.transform.localRotation = Quaternion.identity;
        }
        
    }

    public void ResetEnvironment(){
        environmentStepCount = 0;
        foreach (var agent in Agents)
        {
            agent.Agent.transform.localPosition = agent.StartingPos;
            agent.Agent.transform.localRotation = agent.StartingRot;
            agent.Rb.velocity = Vector3.zero;
            agent.Agent.hasFlag = false;
            agent.Agent.goal.transform.parent = agent.Agent.transform.parent;
            if (agent.Agent.transform.localPosition.y < 0.5f)
            {
                agent.Agent.transform.localPosition = new Vector3(agent.Agent.transform.localPosition.x, 0.5f, agent.Agent.transform.localPosition.z);
            }

        }
        blueFlag.transform.localPosition = startingBlueFlagPos;
        redFlag.transform.localPosition = startingRedFlagPos;
    }

    public void FlagPickedUp(string team)
    {
        if (team == "BlueAgent")
        {
            blueTeam.AddGroupReward(1f);
            redTeam.AddGroupReward(-1f);
        }
        else
        {
            blueTeam.AddGroupReward(-1f);
            redTeam.AddGroupReward(1f);
        }
    }
    public void FlagCaptured(string team)
    {
        if (team == "BlueAgent")
        {
            //blueTeam.AddGroupReward(2f - 1.0f * (environmentStepCount / MaxEnvironmentSteps));
            //blueTeam.AddGroupReward(2f - (environmentStepCount / MaxEnvironmentSteps));
            blueTeam.AddGroupReward(5f);
            redTeam.AddGroupReward(-5f);
            floorMesh.material = blueTeamWinMaterial;
        }
        else
        {
            blueTeam.AddGroupReward(-5f);
            //redTeam.AddGroupReward(2f - 1.0f * (environmentStepCount / MaxEnvironmentSteps));
            //redTeam.AddGroupReward(2f - (environmentStepCount / MaxEnvironmentSteps));
            redTeam.AddGroupReward(5f);
            floorMesh.material = redTeamWinMaterial;
        }
        ResetEnvironment();
        blueTeam.EndGroupEpisode();
        redTeam.EndGroupEpisode();
    }

    public void agentHitWallWithFlag(string team)
    {
        if (team == "BlueAgent")
        {
            blueTeam.AddGroupReward(-1f);
        }
        else
        {
            redTeam.AddGroupReward(-1f);
        }
        ResetFlag(team);
    }
    public void agentHitWallWithoutFlag(string team)
    {
        if (team == "BlueAgent")
        {
            blueTeam.AddGroupReward(-2f);
        }
        else{
            redTeam.AddGroupReward(-2f);
        }
    }

    public void FellOffMap(string team)
    {
        if (team == "BlueAgent")
        {
            blueTeam.AddGroupReward(-1f);
        }
        else
        {
            redTeam.AddGroupReward(-1f);
        }
    }
}
