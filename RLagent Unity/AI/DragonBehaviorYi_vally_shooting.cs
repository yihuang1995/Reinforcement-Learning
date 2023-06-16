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
    public class DragonBehaviorYi_vally_shooting : DADragonAIAgentBase
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

        /// <summary>
        /// target object
        /// </summary>
        
        private GameObject vally_target;
        private bool collidetarget;

        public float x;
        public float y;
        public float z;
        Vector3 pos;

        /// shooting 
        public Transform shootingPoint;
        public int minStepsBetweenShots = 50;
        public int damage = 20;
        public Projectile projectile;
        private bool ShotAvaliable = true;
        private int StepsUntilShotIsAvaliable = 0;   
        //private bool enemy_status;
        

        //shooting
        public void givereward(float rewardvalue)
        {
            AddReward(rewardvalue);
        }

        private void Shoot()
        {
            if (!ShotAvaliable)
                return;
            
            var layerMask = 1 << LayerMask.NameToLayer("Enemy");
            var direction = transform.forward;

            var spawnedProjectile = Instantiate(projectile, shootingPoint.position, Quaternion.Euler(0f, -90f, 0f));
            spawnedProjectile.Init(direction,this);
            
            Debug.DrawRay(transform.position, direction, Color.blue, 1f);
            
            // if (Physics.Raycast(shootingPoint.position, direction, out var hit, 50f, layerMask))
            // {
            //     //hit.transform.GetComponent<Enemy>().GetShot(damage);
            //     Debug.Log("----------------------get shot--------------------");
            //     AddReward(5f);
            // }
            // else
            // {
            //     AddReward(-0.1f);
            // }
            if (!vally_target.activeSelf){
                Invoke("EndEpisode",10);
            }
            // if (enemy_status == false)
            // {
            //     EndEpisode();
            // }
            //Debug.Log("----------------------shoot--------------------");
            //ShotAvaliable = false;
            StepsUntilShotIsAvaliable = minStepsBetweenShots;
        }     



        public override void Initialize()
        {
            RB = GetComponent<Rigidbody>();
            trainingArea = GetComponentInParent<DAMLTrainingArea>();

            isTraining = trainingArea.CurrentTrainingMode == DAMLTrainingArea.TrainingMode.DRAGON_FLIGHT;

            vally_target = GameObject.FindGameObjectWithTag("Enemy");
            //enemy_status = vally_target.GetComponent<Enemy>().status;

        }

        public override void OnEpisodeBegin()
        {
            vally_target.SetActive(true);
            GetComponent<Rigidbody>().velocity = Vector3.zero;
            GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

            MoveToSpawnPosition();
            collidetarget = true;
            //UpdateNearestInterest(true);
            //UpdateNearestNest(true);
            ShotAvaliable = true;
            //vally_target.getComponent("Enemy").CurrentHealth = vally_target.getComponent("Enemy").startingHealth;
            vally_target.GetComponent<Enemy>().Start();
        }



        /// is this for debugging purpose?
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
 
            Shoot();        
        }

        public override void CollectObservations(VectorSensor sensor)//4 observation values
        {

            //observation - 7
            sensor.AddObservation(transform.localRotation.normalized);
            sensor.AddObservation(transform.position.normalized);

            //observation - 3
            sensor.AddObservation(vally_target.transform.position.normalized);
            //observation - 3
            Vector3 toInterest = vally_target.transform.position - transform.position;
            sensor.AddObservation(toInterest.normalized);


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



        // initialize to spawn position, how? Maybe we need to record the initial nest
        private void MoveToSpawnPosition(bool chooseRandom = false)
        {
            x = 96.6f;
            y = 10f;
            z = 149.1f;
            pos = new Vector3(x, y, z);
            transform.position = pos;
            // DADragonFlightAISpawner spawnNest = chooseRandom ?
            //     trainingArea.Nests[UnityEngine.Random.Range(0, trainingArea.Nests.Length)] :
            //     MyDragonNest;

            // if (spawnNest == null)
            // {
            //     return;
            // }

            // transform.position = spawnNest.SpawnPoint.position;
            // transform.rotation = spawnNest.SpawnPoint.rotation;
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
                //Debug.Log("----------------------entered barrier--------------------");
            }
            if (other.CompareTag("Enemy"))
            {
                collidetarget = true;
                Debug.Log("----------------------entered--------------------");
            }
        }

        private void OnTriggerStay(Collider other)
        {
            TriggerEnterOrStay(other);
        }

        private void TriggerEnterOrStay(Collider collider)
        {
            if (collider.CompareTag("target") && collidetarget == true)
            {
                if (isTraining)
                {
                    AddReward(50f);
                    collidetarget = false;
                }
            }
            if (collider.CompareTag("Terrain") || collider.CompareTag("PathBlocker"))
            {
                if (isTraining)
                {
                    AddReward(-0.1f);
                }
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

        private void Update()
        {
            if (RB != null)
            {
                velocityStalled = RB.velocity.magnitude < velocityStall;
            }

            if (velocityStalled && isTraining)
            {
                AddReward(-5f);
            }
        }

        private void FixedUpdate()
        {
            if (!ConstantUpdateIntervals)
            {
                return;
            }
        }


        // what is this function used for
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
