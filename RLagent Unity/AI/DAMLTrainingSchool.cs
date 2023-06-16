using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using DA.BattleAI;
using Unity.MLAgents;
using UnityEngine;

namespace DA.AI
{
    public class DAMLTrainingSchool : MonoBehaviour
    {
        public bool MakeCopies;
        public GameObject TrainingAreaTemplate;
        public Vector3 WorldOffset = new Vector3(500f, 0f, 500f);
        public Vector2 Copies = new Vector2(2, 2);
        public static bool CanTrain;

        public static bool SchoolInit;
        
        private void Awake()
        {
            if (!MakeCopies)
            {
                return;
            }

            StartCoroutine(CheckAcademyStatus());
        }

        IEnumerator CheckAcademyStatus(int retries = 5)
        {
            if (CanTrain || retries <= 0)
            {
                yield return null;
            }
            yield return new WaitForSeconds(0.2f);

            
            
            Debug.Log($"CanTrain: {CanTrain}");
            
            if (!Academy.IsInitialized)
            {
                StartCoroutine(CheckAcademyStatus(retries-1));
            }
            else
            {
                CanTrain = Academy.Instance.IsCommunicatorOn;
            }
            
            yield return null;
        }

        private void DoCopies()
        {
            if (SchoolInit)
            {
                return;
            }
            for (var x = 1; x <= Copies.x; x++)
            {
                for (var y = 1; y <= Copies.y; y++)
                {
                    var GO = Instantiate(TrainingAreaTemplate, this.transform);
                    GO.transform.Translate(x * WorldOffset.x, 0f, y * WorldOffset.z);
                }
            }
            SchoolInit = true;
        }
        
        private void Update()
        {
            if (!CanTrain)
            {
                return;
            }
            DoCopies();
        }
    }
}