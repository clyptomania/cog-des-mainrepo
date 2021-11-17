using UnityEngine;
using static Trainstate;

public class TrainSpawner : MonoBehaviour {

    [Header("Spawner Settings")]
    [SerializeField]
    private bool spawnAtIntervals = false;
    [SerializeField]
    private int firstSpawnDelay;

    [SerializeField]
    private int spawnInterval;

    [SerializeField]
    private bool spawnOnBEnabled;


    [Header("Train Settings")]
    [SerializeField]
    private GameObject trainModel;
    [SerializeField]
    private bool automaticDeparture = false;

    [SerializeField]
    private Vector3 stopLocation;

    [SerializeField]
    private Vector3 despawnLocation;

    [SerializeField]
    private Vector3 spawnRotation;

    [SerializeField]
    private int totalStopTime;

    // public int TravelTime { get { return travelTime; } private set { travelTime = value; } }
    // [SerializeField]
    // private int travelTime = 15;

    // public int DoorDelay { get { return doorDelay; } private set { doorDelay = value; } }
    // [SerializeField]
    // private int doorDelay = 1;

    [SerializeField]
    private int travelTime = 15;

    [SerializeField]
    private int doorDelay = 1;

    [SerializeField]
    private AnimationCurve positionCurve;


    void Start() {
        if (this.trainModel) {
            if (spawnAtIntervals)
                this.InvokeRepeating("SpawnTrain", this.firstSpawnDelay, this.spawnInterval);
        } else {
            Debug.Log("Train model missing.");
        }
    }

    private GameObject train;
    private TrainControl trainControl;

    public float DelayToOpenDoors() {
        return travelTime + doorDelay;
    }

    public float DelayToArrival() {
        return travelTime;
    }

    public void SpawnTrain() {
        if (train) {
            Debug.Log("A train is already spawned, wait for it to depart first!");
            return;
        }
        Debug.Log("Train is on its way.");
        train = Instantiate(this.trainModel, this.transform.position, Quaternion.Euler(this.spawnRotation.x, this.spawnRotation.y, this.spawnRotation.z));
        trainControl = train.GetComponent<TrainControl>();

        if (trainControl) {
            trainControl.StopLocation = this.stopLocation;
            trainControl.DespawnLocation = this.despawnLocation;
            trainControl.departsAutomatically = this.automaticDeparture;
            trainControl.TotalStopTime = this.totalStopTime;
            trainControl.TravelTime = this.travelTime;
            trainControl.DoorDelay = this.doorDelay;
            trainControl.PositionCurve = this.positionCurve;
            trainControl.State = TrainState.Arriving;
        }
    }

    public void DepartTrain() {
        if (trainControl) {
            trainControl.setForDeparture = true;
            Debug.Log("Train is set to depart now.");
        }
    }

    void FixedUpdate() {
        if (this.spawnOnBEnabled && Input.GetKeyDown(KeyCode.B)) {
            if (this.trainModel) {
                this.SpawnTrain();
            } else {
                Debug.Log("Train model missing.");
            }
        }
        if (Input.GetKeyDown(KeyCode.D)) {
            DepartTrain();
        }
    }

    private void OnDrawGizmosSelected() {
        if (this.stopLocation != null) {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(this.transform.position, this.stopLocation);
        }

        if (this.despawnLocation != null) {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(this.stopLocation, this.despawnLocation);
        }
    }
}
