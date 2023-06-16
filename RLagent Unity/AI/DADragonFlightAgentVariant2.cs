using System;
using System.Collections;
using System.Collections.Generic;
using DA.BattleAI;
using DA.Data.AI;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Analytics;
using Random = UnityEngine.Random;

namespace DA.AI
{
    public class DADragonFlightAgentVariant2 : DADragonAIAgentBase
    {
        public DADataDragonFlightAIVariant2 ActorData;



        public float moveForce = 10f;

        public float pitchSpeed = 150f;

        public float yawSpeed = 150f;

        private float smoothPitchChange = 0f;
        private float smoothYawChange = 0f;
        private const float MaxPitchAngle = 80f;
        private const float velocityStall = 0.1f;
        private bool velocityStalled;

        public float InterestUpdateInterval = 5f;
        public float NestUpdateInterval = 5f;
        public bool ConstantUpdateIntervals = true;

        private float interestUpdateTimer;
        private float nestUpdateTimer;
        private bool collideNest;
        private bool collideInterest;
        private bool collideBarrier;

        public override void Initialize()
        {
            RB = GetComponent<Rigidbody>();
            trainingArea = GetComponentInParent<DAMLTrainingArea>();

            isTraining = trainingArea.CurrentTrainingMode == DAMLTrainingArea.TrainingMode.DRAGON_FLIGHT;

            if (!isTraining) MaxStep = 0;
            UpdateNearestInterest(true);
            UpdateNearestNest(true);
        }

        public override void OnEpisodeBegin()
        {
            GetComponent<Rigidbody>().velocity = Vector3.zero;
            GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

            MoveToSpawnPosition();

            UpdateNearestInterest(true);
            UpdateNearestNest(true);
        }


        /// <summary>
        /// is this for debugging purpose?
        /// </summary>
        private void OnDrawGizmos()
        {
            var gizmoPos = transform.position;
            gizmoPos += transform.up * 0.5f;
            if (collideInterest)
            {
                Gizmos.color = new Color(0.8f, 0.8f, 0.2f, 0.5f);
                Gizmos.DrawSphere(gizmoPos, 6f);
            }
            if (collideNest)
            {
                Gizmos.color = new Color(0.2f, 0.2f, 0.8f, 0.5f);
                Gizmos.DrawSphere(gizmoPos, 6f);
            }
            if (collideBarrier)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.8f);
                Gizmos.DrawSphere(gizmoPos, 6f);
            }

            if (velocityStalled)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.8f);
                Gizmos.DrawSphere(gizmoPos, 6f);
            }
        }

        /// vectorAction[i] represents:
        /// Index 0: move vector z (+1 = forward, -1 = backward) - shift to always forward
        /// Index 1: pitch angle (+1 = pitch up, -1 = pitch down)
        /// Index 2: yaw angle (+1 = turn right, -1 = turn left)

        public override void OnActionReceived(float[] vectorAction)
        {
            if (frozenAgent) return;

            var zValue = vectorAction[0] * 0.5f + 0.5f;

            Vector3 move = new Vector3(0f, 0f, zValue);

            move = transform.TransformDirection(move * moveForce);

            GetComponent<Rigidbody>().AddForce(move);

            Vector3 rotationVector = transform.rotation.eulerAngles;

            float pitchChange = vectorAction[1];
            float yawChange = vectorAction[2];

            smoothPitchChange = Mathf.MoveTowards(smoothPitchChange, pitchChange, 2f * Time.fixedDeltaTime);
            smoothYawChange = Mathf.MoveTowards(smoothYawChange, yawChange, 2f * Time.fixedDeltaTime);

            float pitch = rotationVector.x + smoothPitchChange * Time.fixedDeltaTime * pitchSpeed;
            if (pitch > 180f) pitch -= 360f;
            pitch = Mathf.Clamp(pitch, -MaxPitchAngle, MaxPitchAngle);

            float yaw = rotationVector.y + smoothYawChange * Time.fixedDeltaTime * yawSpeed;

            //yaw += (velocityStalled ? 180f : 0f);

            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        public override void CollectObservations(VectorSensor sensor)//10 observation values + 6
        {
            if (nearestNest == null || nearestInterest == null)
            {
                sensor.AddObservation(new float[16]);
                return;
            }

            //observation - 4
            sensor.AddObservation(transform.localRotation.normalized);

            Vector3 toNest = nearestNest.NestCollider.transform.position - transform.position;

            //observation - 3
            sensor.AddObservation(toNest.normalized);

            //observation - 1
            sensor.AddObservation(Vector3.Dot(toNest.normalized, -nearestNest.NestCollider.transform.up.normalized));

            //observation - 1
            sensor.AddObservation(Vector3.Dot(transform.forward.normalized, -nearestNest.NestCollider.transform.up.normalized));

            var obvRatio = toNest.magnitude / DAMLTrainingArea.AreaDiameter;
            //observation - 1
            sensor.AddObservation(obvRatio);



            Vector3 toInterest = nearestInterest.position - transform.position;

            //observation - 3
            sensor.AddObservation(toInterest.normalized);

            //observation - 1
            sensor.AddObservation(Vector3.Dot(toInterest.normalized, -nearestInterest.up.normalized));

            //observation - 1
            sensor.AddObservation(Vector3.Dot(transform.forward.normalized, -nearestInterest.up.normalized));

            var obvRatioInterest = toInterest.magnitude / DAMLTrainingArea.AreaDiameter;
            //observation - 1
            sensor.AddObservation(obvRatioInterest);
        }

        public void FreezeAgent()
        {
            Debug.Assert(isTraining == false, "Freeze/Unfreeze not supported in training");
            frozenAgent = true;
            RB.Sleep();
        }

        public void UnfreezeAgent()
        {
            Debug.Assert(isTraining == false, "Freeze/Unfreeze not supported in training");
            frozenAgent = false;
            RB.WakeUp();
        }

        private void MoveToSpawnPosition(bool chooseRandom = false)
        {
            DADragonFlightAISpawner spawnNest = chooseRandom ?
                trainingArea.Nests[UnityEngine.Random.Range(0, trainingArea.Nests.Length)] :
                MyDragonNest;

            if (spawnNest == null)
            {
                return;
            }

            transform.position = spawnNest.SpawnPoint.position;
            transform.rotation = spawnNest.SpawnPoint.rotation;
        }

        private void UpdateNearestInterest(bool chooseRandom = false)
        {
            interestUpdateTimer += Time.fixedDeltaTime;
            if (interestUpdateTimer < InterestUpdateInterval && chooseRandom)
            {
                return;
            }

            interestUpdateTimer = 0f;
            Transform thisInterest = chooseRandom ? trainingArea.Interests[Random.Range(0, trainingArea.Interests.Length)] : nearestInterest;

            foreach (Transform interest in trainingArea.Interests)
            {
                if (thisInterest == null)
                {
                    thisInterest = interest;
                }
                else
                {
                    float distanceToInterest = Vector3.Distance(interest.position, transform.position);
                    float distanceToCurrentNearestInterest = Vector3.Distance(thisInterest.position, transform.position);

                    if (distanceToInterest < distanceToCurrentNearestInterest)
                    {
                        thisInterest = interest;
                    }
                }
            }

            nearestInterest = thisInterest;
        }

        private void UpdateNearestNest(bool chooseRandom = false)
        {
            nestUpdateTimer += Time.fixedDeltaTime;
            if (nestUpdateTimer < NestUpdateInterval && chooseRandom)
            {
                return;
            }

            nestUpdateTimer = 0f;

            DADragonFlightAISpawner thisNest = chooseRandom ? trainingArea.Nests[Random.Range(0, trainingArea.Nests.Length)] : nearestNest;
            if(!chooseRandom)
            {
                foreach (DADragonFlightAISpawner nest in trainingArea.Nests)
                {
                    if (thisNest == null)
                    {
                        thisNest = nest;
                    }
                    else
                    {
                        float distanceToNest = Vector3.Distance(nest.transform.position, transform.position);
                        float distanceToCurrentNearestNest = Vector3.Distance(thisNest.transform.position, transform.position);

                        if (distanceToNest < distanceToCurrentNearestNest)
                        {
                            thisNest = nest;
                        }
                    }
                }

            }

            nearestNest = thisNest;
        }

        private void OnTriggerEnter(Collider other)
        {
            TriggerEnterOrStay(other);
            if (other.CompareTag("DragonNest"))
            {
                collideNest = true;
            }
            if (other.CompareTag("PathAttractor"))
            {
                collideInterest = true;
            }
            if (other.CompareTag("Terrain") || other.CompareTag("PathBlocker"))
            {
                collideBarrier = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("DragonNest"))
            {
                collideNest = false;
            }
            if (other.CompareTag("PathAttractor"))
            {
                collideInterest = false;
            }
            if (other.CompareTag("Terrain") || other.CompareTag("PathBlocker"))
            {
                collideBarrier = false;
            }
        }

        private void OnTriggerStay(Collider other)
        {
            TriggerEnterOrStay(other);
        }

        private void TriggerEnterOrStay(Collider collider)
        {
            if (collider.CompareTag("DragonNest"))
            {
                if (isTraining)
                {
                    AddReward(0.2f);
                }
                UpdateNearestNest(true);
            }
            else if (collider.CompareTag("PathAttractor"))
            {
                if (isTraining)
                {
                    AddReward(0.1f);
                }
                UpdateNearestInterest(true);
            }
            if (collider.CompareTag("Terrain") || collider.CompareTag("PathBlocker"))
            {
                if (isTraining)
                {
                    AddReward(-0.5f);
                }
                UpdateNearestNest(true);
                UpdateNearestInterest(true);
            }
        }


        private void Update()
        {
            if (nearestNest != null)
                Debug.DrawLine(transform.position, nearestNest.transform.position, Color.blue);
            if (nearestInterest != null)
                Debug.DrawLine(transform.position, nearestInterest.transform.position, Color.blue);
            if (RB != null)
            {
                velocityStalled = RB.velocity.magnitude < velocityStall;
            }

            if (velocityStalled && isTraining)
            {
                AddReward(-0.01f);
            }
        }

        private void FixedUpdate()
        {
            if (!ConstantUpdateIntervals)
            {
                return;
            }
            UpdateNearestInterest();
            UpdateNearestNest();
        }


        public void SetState(State state, bool force = false)
        {
            if (state == currentState && !force)
            {
                return;
            }

            currentState = state;
        }

        public void LeaveNest(DADragonFlightAISpawner nest)
        {
            if (MyDragonNest == nest)
            {
                MyDragonNest = null;
            }
        }
    }
}
