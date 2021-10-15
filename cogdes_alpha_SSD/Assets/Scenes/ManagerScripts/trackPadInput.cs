using System;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class TrackPadInput : MonoBehaviour {
	private static TrackPadInput _instance;
	public static TrackPadInput instance => _instance;

	private void Awake () {
		_instance = this;
		Debug.Log ("Trackpad has awoken");
	}

	public delegate void triggerCallback (bool b, ExpeControl.lateralisation l);
	public static Dictionary<string, triggerCallback> triggerCallbacks = new Dictionary<string, triggerCallback> ();

	public delegate void touchCallback (bool pressed, ExpeControl.lateralisation l);
	public static Dictionary<string, touchCallback> touchCallbacks = new Dictionary<string, touchCallback> ();

	public delegate void pressCallback (bool pressed, ExpeControl.lateralisation l);
	public static Dictionary<string, pressCallback> pressCallbacks = new Dictionary<string, pressCallback> ();

	public SteamVR_Input_Sources handType;
	public SteamVR_Action_Boolean newAction;

	public bool GetAction () {
		return newAction.GetState (handType);
	}

	int counter = 0;

	/// <summary>
	/// Update is called every frame, if the MonoBehaviour is enabled.
	/// </summary>
	void Update () {
		// if (counter++ < 10)
		// 	Debug.Log ("Counting... " + counter);
		if (GetAction ())
			Debug.Log ("New action!");
	}

	public TrackPadInput () { }

	private bool _pressedL;
	private bool _pressedR;

	private bool _touchedPadL;
	private bool _touchedPadR;

	private bool _pressedPadL;
	private bool _pressedPadR;

	public bool Pressed () {
		return _pressedL || _pressedR;
	}

	public bool DisplayTouched () {
		return _touchedPadL || _touchedPadR;
	}

	public bool DisplayPressed () {
		return _pressedPadL || _pressedPadR;
	}

	public void TriggerPressL (bool state) { TriggerPress (state, ExpeControl.lateralisation.left); }
	public void TriggerPressR (bool state) { TriggerPress (state, ExpeControl.lateralisation.right); }

	public void padTouchL (bool state) { PadTouch (state, ExpeControl.lateralisation.left); }
	public void padTouchR (bool state) { PadTouch (state, ExpeControl.lateralisation.right); }

	public void padPressR (bool state) { PadPress (state, ExpeControl.lateralisation.left); }
	public void padPressL (bool state) { PadPress (state, ExpeControl.lateralisation.right); }

	public void TriggerPress (bool state, ExpeControl.lateralisation hand) {
		if (hand == ExpeControl.lateralisation.left) _pressedL = state;
		else _pressedR = state;

		foreach (triggerCallback func in TrackPadInput.triggerCallbacks.Values) {
			func (state, hand);
		}
	}

	public void PadTouch (bool state, ExpeControl.lateralisation hand) {
		if (hand == ExpeControl.lateralisation.left) _touchedPadL = state;
		else _touchedPadR = state;

		foreach (touchCallback func in TrackPadInput.touchCallbacks.Values) {
			func (state, hand);
		}
	}

	public void PadPress (bool state, ExpeControl.lateralisation hand) {
		if (hand == ExpeControl.lateralisation.left) _pressedPadL = state;
		else _pressedPadR = state;

		foreach (pressCallback func in TrackPadInput.pressCallbacks.Values) {
			func (state, hand);
		}
	}
}