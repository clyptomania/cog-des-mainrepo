using UnityEngine;
using static Trainstate;

public class TrainSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [SerializeField]
    private int firstSpawnDelay;

    [SerializeField]
    private int spawnIntervall;

    [SerializeField]
    private bool spawnOnBEnabled;


    [Header("Train Settings")]
    [SerializeField]
    private GameObject trainModel;

    [SerializeField]
    private Vector3 stopLocation;

    [SerializeField]
    private Vector3 despawnLocation;

    [SerializeField]
    private Vector3 spawnRotation;

    [SerializeField]
    private int totalStopTime;

    [SerializeField]
    private int travelTime;

    [SerializeField]
    private int doorDelay;

    [SerializeField]
    private AnimationCurve positionCurve;


    void Start()
    {
        if(this.trainModel)
        {
            this.InvokeRepeating("SpawnTrain", this.firstSpawnDelay, this.spawnIntervall);
        } else
        {
            Debug.Log("Train model missing.");
        }
    }

    void SpawnTrain()
    {
        var train = Instantiate(this.trainModel, this.transform.position, Quaternion.Euler(this.spawnRotation.x, this.spawnRotation.y, this.spawnRotation.z));
        var trainControl = train.GetComponent<TrainControl>();

        if (trainControl)
        {
            trainControl.StopLocation = this.stopLocation;
            trainControl.DespawnLocation = this.despawnLocation;
            trainControl.TotalStopTime = this.totalStopTime;
            trainControl.TravelTime = this.travelTime;
            trainControl.DoorDelay = this.doorDelay;
            trainControl.PositionCurve = this.positionCurve;
            trainControl.State = TrainState.Arriving;
        }
    }

    void FixedUpdate()
    {
        if(this.spawnOnBEnabled && Input.GetKeyUp(KeyCode.B))
        {
            if (this.trainModel)
            {
                this.SpawnTrain();
            }
            else
            {
                Debug.Log("Train model missing.");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (this.stopLocation != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(this.transform.position, this.stopLocation);
        }

        if (this.despawnLocation != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(this.stopLocation, this.despawnLocation);
        }
    }
}
