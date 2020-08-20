using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Valve.VR;

public interface ITrackInput
{
	bool Pressed();
}

// https://medium.com/@sarthakghosh/a-complete-guide-to-the-steamvr-2-0-input-system-in-unity-380e3b1b3311

public class TrackPadInput: ITrackInput
{
	public static TrackPadInput instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new TrackPadInput();
			}
			return _instance;
		}
	}
	private static TrackPadInput _instance;
	
	public SteamVR_Action_Boolean PointTarget;
	public SteamVR_Action_Boolean DisplayInstruct;
	public SteamVR_Input_Sources handType;
	
	public delegate void triggerCallback(bool pressed);
	public Dictionary<string, triggerCallback> triggerCallbacks = new Dictionary<string, triggerCallback>();
	
	public delegate void touchCallback(bool pressed);
	public Dictionary<string, touchCallback> touchCallbacks = new Dictionary<string, touchCallback>();

	public TrackPadInput()
	{
		PointTarget = SteamVR_Actions.default_InteractUI;
		DisplayInstruct = SteamVR_Actions.default_ShowInstuct;
		handType = SteamVR_Input_Sources.Any;
		
		PointTarget.AddOnStateDownListener(TriggerDown, handType);
		PointTarget.AddOnStateUpListener(TriggerUp, handType);
		
		DisplayInstruct.AddOnStateDownListener(heldToDisplay, handType);
		DisplayInstruct.AddOnStateUpListener(releaseToDisplay, handType);
	}

	private bool pressed;
	public void TriggerDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
	{
		pressed = true;
		
		foreach (triggerCallback func in triggerCallbacks.Values)
		{
			func(pressed);
		}
	}

	public void TriggerUp(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
	{
		pressed = false;
		
		foreach (triggerCallback func in triggerCallbacks.Values)
		{
			func(pressed);
		}
	}
	
	public bool Pressed()
	{
		return pressed;
	}

	private bool heldDisplay;
	public void heldToDisplay(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
	{
		heldDisplay = true;
		
		foreach (touchCallback func in touchCallbacks.Values)
		{
			func(heldDisplay);
		}
	}
	public void releaseToDisplay(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
	{
		heldDisplay = false;
		
		foreach (touchCallback func in touchCallbacks.Values)
		{
			func(heldDisplay);
		}
	}
	
	public bool HeldDown()
	{
		return heldDisplay;
	}
}

public class MockTrackPadInput: ITrackInput
{
	public bool Pressed()
	{
		return Input.GetKeyUp(KeyCode.Space);
	}
}