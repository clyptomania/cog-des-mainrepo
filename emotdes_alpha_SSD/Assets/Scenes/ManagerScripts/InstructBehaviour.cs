using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InstructBehaviour : MonoBehaviour {
    public static InstructBehaviour instance;
    static private ExpeControl _expeControl;
    [SerializeField]
    private GameObject instructionGeneral;
    [SerializeField]
    private GameObject instructionControllerL;
    [SerializeField]
    private GameObject instructionControllerR;

    private List<Text> _texts;

    private RadialProgress[] _radialProgresses;
    public bool isInstructGeneralDisplayed => instructionGeneral.activeSelf;

    public bool requested { get; private set; }

    [SerializeField] private bool oneControllerOnly = true;
    public bool leftControllerActive { get; private set; }

    public bool deactivatedOtherController { get; private set; }

    void OnEnable() {
        instance = this;
        requested = false;

        _radialProgresses = FindObjectsOfType<RadialProgress>();
        ResetRadialProgresses();

        // Already turned off by the CameraRig
        instructionGeneral.SetActive(false);
        // instructionControllerL.SetActive(false);
        // instructionControllerR.SetActive(false);

        deactivatedOtherController = false;

        _texts = new List<Text>(3);
        _texts.Add(instructionGeneral.GetComponentInChildren<Text>());
        _texts.Add(instructionControllerL.GetComponentInChildren<Text>());
        _texts.Add(instructionControllerR.GetComponentInChildren<Text>());

        TrackPadInput.touchCallbacks.Add("showCtrlInstruct",
            (state, lat) => {
                if (lat == ExpeControl.lateralisation.left)
                    instructionControllerL.SetActive(state);
                else
                    instructionControllerR.SetActive(state);
            }
        );

        TrackPadInput.triggerCallbacks.Add("HideGeneralInstruct",
            (state, lat) => {
                if (isInstructGeneralDisplayed)
                    instructionGeneral.SetActive(false);
            }
        );
    }

    private float holdTime = 0;
    private float requestTime = 0;

    public void RequestConfirmation(float time) {
        ResetRadialProgresses();
        holdTime = 0;
        requestTime = time;
        requested = true;
        StartCoroutine(Request());
    }

    IEnumerator Request() {
        // wait for user to release previously held trigger to start filling process
        while (_expeControl.userClickedTrigger)
            yield return null;
        while (holdTime < requestTime) {
            if (_expeControl.userClickedTrigger) {
                holdTime += Time.deltaTime;
                SetRadialProgresses(holdTime / requestTime);
            } else {
                holdTime = 0;
                ResetRadialProgresses();
            }
            yield return null;
        }
        SetRadialProgresses(1);
        requested = false;

        yield return new WaitUntil(() => !_expeControl.userTouchedTrigger);
        ResetRadialProgresses();
    }

    public void SetRadialProgresses(float fill) {
        foreach (var rP in _radialProgresses) {
            rP.SetProgress(fill);
        }
    }
    public void ResetRadialProgresses() {
        foreach (var rP in _radialProgresses) {
            rP.ResetFill();
            // Debug.Log("Reset radials of: " + rP.gameObject.name);
        }
    }

    /// <summary>
    /// Start is called on the frame when a script is enabled just before
    /// any of the Update methods is called the first time.
    /// </summary>
    void Start() {
        _expeControl = ExpeControl.instance;
    }

    void Update() {
        if (requested && Input.GetKeyDown("space")) {
            Debug.Log("Fulfilling request manually…");
            requested = false;
        }
    }

    public void positionWorldInstruction(Transform start) {
        Transform tr = instructionGeneral.transform;

        // Position and rotate on top of user
        tr.position = start.position;
        tr.rotation = start.rotation;
        // Move 2 units away from user new position
        tr.position += start.forward * 3;
        // Move up
        tr.position += tr.up * 1.1f;
        // tr.LookAt(start);
    }

    public bool isWorldInstructionShowing => instructionGeneral.activeSelf;

    public void toggleControllerInstruction(bool state, string message = "") {
        if (oneControllerOnly)
            if (leftControllerActive)
                instructionControllerL.SetActive(state);
            else
                instructionControllerR.SetActive(state);
        else {
            instructionControllerL.SetActive(state);
            instructionControllerR.SetActive(state);
        }
        if (message != "")
            setInstruction(message);
    }
    public void toggleWorldInstruction(bool state, string message = "") {
        instructionGeneral.SetActive(state);
        if (message != "")
            setInstruction(message);
    }

    public void setInstruction(string message) {
        foreach (var text in _texts) {
            text.text = message;
        }
    }

    public void DeactivateController(bool left) {
        if (!oneControllerOnly) {
            deactivatedOtherController = true;
            return;
        }
        if (!deactivatedOtherController) {
            if (left) {
                instructionControllerL.transform.parent.gameObject.SetActive(false);
                leftControllerActive = false;
                Debug.Log("Deactivated left controller");
            } else {
                instructionControllerR.transform.parent.gameObject.SetActive(false);
                leftControllerActive = true;
                Debug.Log("Deactivated right controller");
            }
            deactivatedOtherController = true;
        }
    }
}