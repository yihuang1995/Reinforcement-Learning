using System.Collections;
using System.Collections.Generic;
using DA.BattleAI;
using DA.Data.AI;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace DA.AI
{
    public class DADragonFlightAgent : DADragonAIAgentBase
    {
        public DADataDragonFlightAI ActorData;

        public float moveForce = 10f;
        
        public float pitchSpeed = 150f;

        public float yawSpeed = 150f;
        
        private float smoothPitchChange = 0f;
        private float smoothYawChange = 0f;
        private const float MaxPitchAngle = 80f;
        private const float InNestRadius = 0.1f;

        
        public override void Initialize()
        {
            RB = GetComponent<Rigidbody>();
            trainingArea = GetComponentInParent<DAMLTrainingArea>();

            isTraining = trainingArea.CurrentTrainingMode == DAMLTrainingArea.TrainingMode.DRAGON_FLIGHT;
            
            if (!isTraining) MaxStep = 0;
            UpdateNearestInterest();
        }
        
        public override void OnEpisodeBegin()
        {
            if (isTraining)
            {
                // reset spawners
            }
    
            GetComponent<Rigidbody>().velocity = Vector3.zero;
            GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
    
            // Default to spawning in front of a flower
            bool inFrontOfNest = true;
            if (isTraining)
            {
                inFrontOfNest = UnityEngine.Random.value > .5f;
            }

            MoveToSafeRandomPosition(inFrontOfNest);
    
            UpdateNearestInterest();
        }
    
        /// vectorAction[i] represents:
        /// Index 0: move vector x (+1 = right, -1 = left)
        /// Index 1: move vector y (+1 = up, -1 = down)
        /// Index 2: move vector z (+1 = forward, -1 = backward)
        /// Index 3: pitch angle (+1 = pitch up, -1 = pitch down)
        /// Index 4: yaw angle (+1 = turn right, -1 = turn left)
        
        public override void OnActionReceived(float[] vectorAction)
        {
            if (frozenAgent) return;
    
            Vector3 move = new Vector3(vectorAction[0], vectorAction[1], vectorAction[2]);
            
            GetComponent<Rigidbody>().AddForce(move * moveForce);
    
            Vector3 rotationVector = transform.rotation.eulerAngles;
    
            float pitchChange = vectorAction[3];
            float yawChange = vectorAction[4];
    
            smoothPitchChange = Mathf.MoveTowards(smoothPitchChange, pitchChange, 2f * Time.fixedDeltaTime);
            smoothYawChange = Mathf.MoveTowards(smoothYawChange, yawChange, 2f * Time.fixedDeltaTime);
    
            float pitch = rotationVector.x + smoothPitchChange * Time.fixedDeltaTime * pitchSpeed;
            if (pitch > 180f) pitch -= 360f;
            pitch = Mathf.Clamp(pitch, -MaxPitchAngle, MaxPitchAngle);
    
            float yaw = rotationVector.y + smoothYawChange * Time.fixedDeltaTime * yawSpeed;
    
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
        
        
        public override void Heuristic(float[] actionsOut)
        {
            // Create placeholders for all movement/turning
            Vector3 forward = Vector3.zero;
            Vector3 left = Vector3.zero;
            Vector3 up = Vector3.zero;
            float pitch = 0f;
            float yaw = 0f;

            // Convert keyboard inputs to movement and turning
            // All values should be between -1 and +1

            // Forward/backward
            if (Input.GetKey(KeyCode.W)) forward = transform.forward;
            else if (Input.GetKey(KeyCode.S)) forward = -transform.forward;

            // Left/right
            if (Input.GetKey(KeyCode.A)) left = -transform.right;
            else if (Input.GetKey(KeyCode.D)) left = transform.right;

            // Up/down
            if (Input.GetKey(KeyCode.E)) up = transform.up;
            else if (Input.GetKey(KeyCode.C)) up = -transform.up;

            // Pitch up/down
            if (Input.GetKey(KeyCode.UpArrow)) pitch = 1f;
            else if (Input.GetKey(KeyCode.DownArrow)) pitch = -1f;

            // Turn left/right
            if (Input.GetKey(KeyCode.LeftArrow)) yaw = -1f;
            else if (Input.GetKey(KeyCode.RightArrow)) yaw = 1f;

            // Combine the movement vectors and normalize
            Vector3 combined = (forward + left + up).normalized;

            // Add the 3 movement values, pitch, and yaw to the actionsOut array
            actionsOut[0] = combined.x;
            actionsOut[1] = combined.y;
            actionsOut[2] = combined.z;
            actionsOut[3] = pitch;
            actionsOut[4] = yaw;
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
        
        private void MoveToSafeRandomPosition(bool inFrontOfNest)
        {
            bool safePositionFound = false;
            int attemptsRemaining = 100; 
            Vector3 potentialPosition = Vector3.zero;
            Quaternion potentialRotation = new Quaternion();

            while (!safePositionFound && attemptsRemaining > 0)
            {
                attemptsRemaining--;
                if (inFrontOfNest)
                {
                    DADragonFlightAISpawner randomNest = trainingArea.Nests[UnityEngine.Random.Range(0, trainingArea.Nests.Length)];

                    float distanceFromNest = UnityEngine.Random.Range(4f, 8f);
                    potentialPosition = randomNest.transform.position + randomNest.transform.up * distanceFromNest;

                    Vector3 toNest = randomNest.transform.position - potentialPosition;
                    potentialRotation = Quaternion.LookRotation(toNest, Vector3.up);
                }
                else
                {
                    float height = UnityEngine.Random.Range(1.2f, 2.5f);

                    float radius = UnityEngine.Random.Range(2f, 7f);

                    Quaternion direction = Quaternion.Euler(0f, UnityEngine.Random.Range(-180f, 180f), 0f);

                    potentialPosition = trainingArea.transform.position + Vector3.up * height + direction * Vector3.forward * radius;

                    float pitch = UnityEngine.Random.Range(-60f, 60f);
                    float yaw = UnityEngine.Random.Range(-180f, 180f);
                    potentialRotation = Quaternion.Euler(pitch, yaw, 0f);
                }

                Collider[] colliders = Physics.OverlapSphere(potentialPosition, 2f);

                safePositionFound = colliders.Length == 0;
            }

            Debug.Assert(safePositionFound, "Could not find a safe position to spawn");

            transform.position = potentialPosition;
            transform.rotation = potentialRotation;
        }

        private void UpdateNearestInterest()
        {
            foreach (Transform interest in trainingArea.Interests)
            {
                if (nearestInterest == null)
                {
                    nearestInterest = interest;
                }
                else
                {
                    float distanceToInterest = Vector3.Distance(interest.position, transform.position);
                    float distanceToCurrentNearestInterest = Vector3.Distance(nearestInterest.position, transform.position);

                    if (distanceToInterest < distanceToCurrentNearestInterest)
                    {
                        nearestInterest = interest;
                    }
                }
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            TriggerEnterOrStay(other);
        }
        
        private void OnTriggerStay(Collider other)
        {
            TriggerEnterOrStay(other);
        }
        
        private void TriggerEnterOrStay(Collider collider)
        {
            if (collider.CompareTag("DragonNest"))
            {
                Vector3 closestPointToNest = collider.ClosestPoint(transform.position);

                if (Vector3.Distance(transform.position, closestPointToNest) < InNestRadius)
                {
                    if (isTraining)
                    {
                        AddReward(0.5f);
                    }

                }
            }
            else if (collider.CompareTag("PathAttractor"))
            {
                if (isTraining)
                {
                    AddReward(0.5f);
                }
            }
        }
        
        private void OnCollisionEnter(Collision collision)
        {
            if (isTraining)
            {
                if (collision.collider.CompareTag("Terrain") || collision.collider.CompareTag("PathBlocker"))
                {
                    AddReward(-0.5f);
                }
            }
        }
        
        private void Update()
        {
            if (nearestNest != null)
                Debug.DrawLine(transform.position, transform.transform.position, Color.green);
        }
        
        private void FixedUpdate()
        {
            if (nearestNest != null)
                UpdateNearestInterest();
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