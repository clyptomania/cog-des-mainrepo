using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class InstructBehaviour : MonoBehaviour
{
    public static InstructBehaviour instance;
    public Canvas instructionGeneral;
    public Canvas instructionControllerL;
    public Canvas instructionControllerR;
    public bool isInstructGeneralDisplayed => instructionGeneral.gameObject.activeSelf;
    void OnEnable()
    {
        instance = this;
        
        // Already turned off by the CameraRig
        instructionGeneral.gameObject.SetActive(false);
        instructionControllerL.gameObject.SetActive(false);
        instructionControllerR.gameObject.SetActive(false);
        
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
}
