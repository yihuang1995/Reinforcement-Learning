using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using DA.BattleAI;
using UnityEngine;

namespace DA.AI
{
    public class DAMLTrainingArea : MonoBehaviour
    {
        public enum TrainingMode
        {
            NO_TRAINING,
            DRAGON_FLIGHT
        }
        public TrainingMode CurrentTrainingMode;
        public CinemachineVirtualCamera TrainingVCam;
        public const float AreaDiameter = 350f;
        
        private Dictionary<Collider, DADragonFlightAISpawner> dragonNestsLookup;
        //[HideInInspector]
        public DADragonFlightAISpawner[] Nests;
        //[HideInInspector]
        public Transform[] Interests;
        private Dictionary<Collider, Transform> interestsLookup;

        public Transform GetInterest(Collider col)
        {
            Debug.Log($"Log: {col.transform.parent.name} found");
            return interestsLookup[col];
        }
        
        public DADragonFlightAISpawner GetDragonNest(Collider col)
        {
            Debug.Log($"Log: {col.transform.parent.name} found");
            return dragonNestsLookup[col];
        }

        private void Awake()
        {
            dragonNestsLookup = new Dictionary<Collider, DADragonFlightAISpawner>();
            interestsLookup = new Dictionary<Collider, Transform>();
            TrainingVCam.Priority = CurrentTrainingMode == TrainingMode.DRAGON_FLIGHT ? 1000 : 0;
        }

        private void Start()
        {
            InitDragonNests();
            InitInterests();
        }

        private void InitDragonNests()
        {
            dragonNestsLookup.Clear();

            Nests = GetComponentsInChildren<DADragonFlightAISpawner>();
            
            foreach (var spawner in Nests)
            {
                var col = spawner.NestCollider;
                if (col == null)
                {
                    continue;
                }
                if (dragonNestsLookup.ContainsKey(col))
                {
                    return;
                }
                dragonNestsLookup[col] = spawner;
            }
        }

        private Transform[] GetInterestList()
        {
            var allPathTargets = GetComponentsInChildren<Transform>();
            var tempList = new List<Transform>();
            tempList.Clear();

            foreach (var target in allPathTargets)
            {
                if (target.gameObject.CompareTag("PathAttractor"))
                {
                    tempList.Add(target);
                }
            }

            return tempList.ToArray();
        }
        
        private void InitInterests()
        {
            interestsLookup.Clear();

            Interests = GetInterestList();
            
            foreach (var interest in Interests)
            {
                var col = interest.GetComponentInChildren<Collider>();
                if (col == null)
                {
                    continue;
                }
                if (interestsLookup.ContainsKey(col))
                {
                    return;
                }
                interestsLookup[col] = interest;
            }
        }
    }
}