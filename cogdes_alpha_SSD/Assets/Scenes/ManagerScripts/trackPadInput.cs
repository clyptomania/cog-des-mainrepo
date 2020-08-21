using System;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;


public class TrackPadInput: MonoBehaviour
{
	private static TrackPadInput _instance;
	public static TrackPadInput instance => _instance;

	private void Awake()
	{
		_instance = this;
	}

	public delegate void triggerCallback(bool b, ExpeControl.lateralisation l);
	public static Dictionary<string, triggerCallback> triggerCallbacks = new Dictionary<string, triggerCallback>();
	
	public delegate void touchCallback(bool pressed, ExpeControl.lateralisation l);
	public static Dictionary<string, touchCallback> touchCallbacks = new Dictionary<string, touchCallback>();

	public TrackPadInput()
	{
	}

	private bool _pressedL;
	private bool _pressedR;
	
	private bool _heldDisplayL;
	private bool _heldDisplayR;

	public bool Pressed()
	{
		return _pressedL || _pressedR;
	}
	
	public bool HeldDisplayDown()
	{
		return _heldDisplayL || _heldDisplayR;
	}

	public void TriggerPressL(bool state)
	{ TriggerPress(state, ExpeControl.lateralisation.left); }
	public void TriggerPressR(bool state)
	{ TriggerPress(state, ExpeControl.lateralisation.right); }
	
	public void heldToDisplayL(bool state)
	{ HeldToDisplay(state, ExpeControl.lateralisation.left); }
	public void heldToDisplayR(bool state)
	{ HeldToDisplay(state, ExpeControl.lateralisation.right); }
	

	public void TriggerPress(bool state, ExpeControl.lateralisation hand)
	{
		if (hand == ExpeControl.lateralisation.left)  _pressedL = state;
		else  _pressedR = state;

		// print($"TT {Pressed()} {hand}");
		
		foreach (triggerCallback func in TrackPadInput.triggerCallbacks.Values)
		{
			func(state, hand);
		}
	}

	public void HeldToDisplay(bool state, ExpeControl.lateralisation hand)
	{
		if (hand == ExpeControl.lateralisation.left)  _heldDisplayL = state;
		else  _heldDisplayR = state;
		
		// print($"HD {HeldDisplayDown()} {hand}");
		
		foreach (touchCallback func in TrackPadInput.touchCallbacks.Values)
		{
			func(state, hand);
		}
	}
}