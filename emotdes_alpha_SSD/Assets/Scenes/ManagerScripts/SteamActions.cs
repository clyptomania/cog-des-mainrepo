using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class SteamActions : MonoBehaviour {

    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Boolean testAction;



    // Start is called before the first frame update
    void Start () {
        testAction.AddOnStateDownListener(TestDown, handType);
        testAction.AddOnStateUpListener(TestUp, handType);
        Debug.Log("Started ActionTests script");
    }

    // Update is called once per frame
    void Update () {

    }

    public void TestUp(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource) {
        Debug.Log("Action up!");
    }
    public void TestDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource) {
        Debug.Log("Action down!");
    }


}