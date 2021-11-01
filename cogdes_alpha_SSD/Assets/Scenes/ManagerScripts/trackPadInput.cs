using System;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class TrackPadInput : MonoBehaviour {
    private static TrackPadInput _instance;
    public static TrackPadInput instance => _instance;

    private void Awake() {
        _instance = this;
        Debug.Log("Trackpad has awoken");
    }



    public delegate void triggerCallback(bool b, ExpeControl.lateralisation l);
    public static Dictionary<string, triggerCallback> triggerCallbacks = new Dictionary<string, triggerCallback>();

    public delegate void touchCallback(bool pressed, ExpeControl.lateralisation l);
    public static Dictionary<string, touchCallback> touchCallbacks = new Dictionary<string, touchCallback>();

    public delegate void pressCallback(bool pressed, ExpeControl.lateralisation l);
    public static Dictionary<string, pressCallback> pressCallbacks = new Dictionary<string, pressCallback>();

    public SteamVR_Input_Sources anyHand;
    public SteamVR_Input_Sources leftHand;
    public SteamVR_Input_Sources rightHand;
    public SteamVR_Action_Boolean sideButtonAction;
    public SteamVR_Action_Boolean trackpadClickAction;
    public SteamVR_Action_Boolean trackpadTouchAction;


    private InstructBehaviour _instructBehaviour;

    // public bool GetAction () {
    // 	return sideButtonAction.GetState (handType);
    // }

    public void TestUp(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource) {
        Debug.Log("Action up!");
    }
    public void TestDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource) {
        Debug.Log("Action down!");
    }

    public void DisableRightController(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource) {
        _instructBehaviour.DeactivateController(false);
    }

    public void DisableLeftController(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource) {
        _instructBehaviour.DeactivateController(true);
    }


    int counter = 0;


    void Start() {

        _instructBehaviour = GetComponent<InstructBehaviour>();

        sideButtonAction.AddOnStateDownListener(TestDown, anyHand);

        sideButtonAction.AddOnStateDownListener(DisableLeftController, rightHand);
        sideButtonAction.AddOnStateDownListener(DisableRightController, leftHand);
        sideButtonAction.AddOnStateUpListener(TestUp, anyHand);
        Debug.Log("Added trackpad listener scripts");
    }


    /// <summary>
    /// Update is called every frame, if the MonoBehaviour is enabled.
    /// </summary>
    void Update() {
        // if (counter++ < 10)
        // 	Debug.Log ("Counting... " + counter);
        // if (GetAction ())
        // 	Debug.Log ("New action!");
    }

    public TrackPadInput() { }

    private bool _pressedL;
    private bool _pressedR;

    private bool _touchedPadL;
    private bool _touchedPadR;

    private bool _pressedPadL;
    private bool _pressedPadR;

    public bool Pressed() {
        return sideButtonAction.GetState(anyHand);
        // return _pressedL || _pressedR;
    }

    public bool DisplayTouched() {
        return trackpadTouchAction.GetState(anyHand);
        // return _touchedPadL || _touchedPadR;
    }

    public bool DisplayPressed() {

        return trackpadClickAction.GetState(anyHand);
        // return _pressedPadL || _pressedPadR;
    }

    public void TriggerPressL(bool state) { TriggerPress(state, ExpeControl.lateralisation.left); }
    public void TriggerPressR(bool state) { TriggerPress(state, ExpeControl.lateralisation.right); }

    public void padTouchL(bool state) { PadTouch(state, ExpeControl.lateralisation.left); }
    public void padTouchR(bool state) { PadTouch(state, ExpeControl.lateralisation.right); }

    public void padPressR(bool state) { PadPress(state, ExpeControl.lateralisation.left); }
    public void padPressL(bool state) { PadPress(state, ExpeControl.lateralisation.right); }

    public void TriggerPress(bool state, ExpeControl.lateralisation hand) {
        if (hand == ExpeControl.lateralisation.left) _pressedL = state;
        else _pressedR = state;

        foreach (triggerCallback func in TrackPadInput.triggerCallbacks.Values) {
            func(state, hand);
        }
    }

    public void PadTouch(bool state, ExpeControl.lateralisation hand) {
        if (hand == ExpeControl.lateralisation.left) _touchedPadL = state;
        else _touchedPadR = state;

        foreach (touchCallback func in TrackPadInput.touchCallbacks.Values) {
            func(state, hand);
        }
    }

    public void PadPress(bool state, ExpeControl.lateralisation hand) {
        if (hand == ExpeControl.lateralisation.left) _pressedPadL = state;
        else _pressedPadR = state;

        foreach (pressCallback func in TrackPadInput.pressCallbacks.Values) {
            func(state, hand);
        }
    }
}