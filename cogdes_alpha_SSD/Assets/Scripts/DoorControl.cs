using UnityEngine;

public class DoorControl : MonoBehaviour
{
    enum DoorState
    {
        Closed,
        Opening,
        Open,
        Closing
    }

    [SerializeField]
    private AnimationCurve positionCurve;

    [SerializeField]
    private Vector3 positionOpen;

    [SerializeField]
    private int speed;

    private float currentTime;
    private DoorState state = DoorState.Closed;
    private Vector3 startingPos;

    void Start()
    {
        this.startingPos = this.transform.localPosition;
    }

    void Update()
    {
        if (this.state == DoorState.Closed || this.state == DoorState.Open)
        {
            return;
        }

        this.currentTime += Time.deltaTime;
        var curveProgress = this.positionCurve.Evaluate(this.currentTime);

        switch (this.state)
        {
            case DoorState.Closing:
                this.transform.localPosition = this.positionOpen - this.positionOpen * curveProgress;
                if (Vector3.Distance(this.transform.localPosition, this.startingPos) < 0.001f)
                {
                    this.state = DoorState.Closed;
                }

                break;
            case DoorState.Opening:
                this.transform.localPosition = this.positionOpen * curveProgress;
                if (Vector3.Distance(this.transform.localPosition, this.positionOpen) < 0.001f)
                {
                    this.state = DoorState.Open;
                }

                break;
        }
    }


    public void ToggleDoors()
    {
        switch (this.state)
        {
            case DoorState.Closed:
                this.state = DoorState.Opening;
                break;
            case DoorState.Open:
                this.state = DoorState.Closing;
                break;
        }

        this.currentTime = 0.0f;
    }
}