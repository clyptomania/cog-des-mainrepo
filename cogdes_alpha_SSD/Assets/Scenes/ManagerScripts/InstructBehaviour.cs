using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InstructBehaviour : MonoBehaviour
{
    private static InstructBehaviour instance;
    [SerializeField]
    private Canvas instructionGeneral;
    [SerializeField]
    private Canvas instructionControllerL;
    [SerializeField]
    private Canvas instructionControllerR;

    private List<Text> _texts;
    public bool isInstructGeneralDisplayed => instructionGeneral.gameObject.activeSelf;
    void OnEnable()
    {
        instance = this;
        
        // Already turned off by the CameraRig
        instructionGeneral.gameObject.SetActive(false);
        instructionControllerL.gameObject.SetActive(false);
        instructionControllerR.gameObject.SetActive(false);
        
        _texts = new List<Text>(3);
        _texts.Add(instructionGeneral.GetComponentInChildren<Text>());
        _texts.Add(instructionControllerL.GetComponentInChildren<Text>());
        _texts.Add(instructionControllerR.GetComponentInChildren<Text>());
        
        TrackPadInput.instance.touchCallbacks.Add("showCtrlInstruct",
            touching =>
            {
                instructionControllerL.gameObject.SetActive(touching);
                instructionControllerR.gameObject.SetActive(touching);
            }
        );
        
        TrackPadInput.instance.triggerCallbacks.Add("HideGeneralInstruct",
            touching =>
            {
                if (isInstructGeneralDisplayed)
                    instructionGeneral.gameObject.SetActive(false);
            }
        );
    }

    public void positionWorldInstruction(Transform start)
    {
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

    public bool isWorldInstructionShowing => instructionGeneral.gameObject.activeSelf;
    public void toggleWorldInstruction(bool state)
    {
        instructionGeneral.gameObject.SetActive(state);
    }

    public void setInstruction(string message)
    {
        foreach (var text in _texts)
        {
            text.text = message;
        }
    }
}
