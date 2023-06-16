using System.Collections;
using System.Collections.Generic;
using DA.BattleAI;
using DA.Data.AI;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace DA.AI
{
    public class DADragonAIAgentBase : Agent
    {
        [HideInInspector]
        public DADragonFlightAISpawner MyDragonNest;
        public enum State
        {
            IDLE,
            FIND_NEST,
            NESTED,
            COMBAT_FLIGHT
        }

        protected State currentState = State.IDLE;
        protected bool frozenAgent = false;

        protected DAMLTrainingArea trainingArea;
        protected DADragonFlightAISpawner nearestNest;
        protected Transform nearestInterest;
        public bool isTraining;

        protected Rigidbody RB;
    }
}