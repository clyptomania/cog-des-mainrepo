using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Trainstate;

public class TrainControl : MonoBehaviour {

    public Vector3 StopLocation { get; set; }
    public Vector3 DespawnLocation { get; set; }
    public bool departsAutomatically { get; set; }
    public bool setForDeparture { get; set; }
    public int TotalStopTime { get; set; }
    public int DoorDelay { get; set; }
    public int TravelTime { get; set; }
    public AnimationCurve PositionCurve { get; set; }
    public TrainState State { get; set; }

    private float secondsStationary;
    private DoorControl[] doors;
    private Vector3 startingPos;
    private float currentTime;


    void Start() {
        this.doors = this.GetComponentsInChildren<DoorControl>();
        this.startingPos = this.transform.position;
        this.currentTime = 0.0f;
    }


    void Update() {
        switch (this.State) {
            case TrainState.Arriving:
                this.currentTime += Time.deltaTime;
                var arrivalProgress = this.PositionCurve.Evaluate(this.currentTime / this.TravelTime);
                this.transform.position = this.startingPos + (this.StopLocation - this.startingPos) * arrivalProgress;

                if (Vector3.Distance(this.transform.position, this.StopLocation) < 0.001f) {
                    this.State = TrainState.DoorsOpening;

                    //Open Doors
                    this.StartCoroutine(this.ToggleDoors(this.DoorDelay, 0, TrainState.Waiting));
                }

                break;
            case TrainState.DoorsOpening:
                break;
            case TrainState.Waiting:

                if (setForDeparture) {
                    this.State = TrainState.DoorsClosing;
                    //Close Doors
                    this.StartCoroutine(this.ToggleDoors(0, this.DoorDelay, TrainState.Departing));
                    setForDeparture = false;
                }

                if (departsAutomatically) {

                    if (this.secondsStationary >= (this.TotalStopTime - this.DoorDelay)) {

                        this.State = TrainState.DoorsClosing;
                        //Close Doors
                        this.StartCoroutine(this.ToggleDoors(0, this.DoorDelay, TrainState.Departing));
                    }
                    this.secondsStationary += Time.deltaTime;
                }

                break;
            case TrainState.DoorsClosing:
                break;
            case TrainState.Departing:
                this.currentTime -= Time.deltaTime;
                var departureProgress = this.PositionCurve.Evaluate(this.currentTime / this.TravelTime);
                this.transform.position = this.DespawnLocation - (this.DespawnLocation - this.StopLocation) * departureProgress;

                if (Vector3.Distance(this.transform.position, this.DespawnLocation) < 0.001f) {
                    Destroy(this.gameObject);
                }

                break;
        }
    }

    IEnumerator ToggleDoors(int delayBefore, int delayAfter, TrainState nextState) {
        yield return new WaitForSeconds(delayBefore);

        foreach (var door in this.doors) {
            door.ToggleDoors();
        }

        yield return new WaitForSeconds(delayAfter);
        this.State = nextState;
    }
}
