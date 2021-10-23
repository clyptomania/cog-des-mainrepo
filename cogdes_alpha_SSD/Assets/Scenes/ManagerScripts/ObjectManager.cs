using System.Collections.Generic;
using UnityEngine;

public class ObjectManager {
    private List<ILightable> _oOrientation;
    private List<ILightable> _oLandmark;

    private bool _lOrientation;
    private bool _lLandmark;

    public ObjectManager() {
        Clear();
    }

    public void Clear() {
        _oOrientation = new List<ILightable>(50);
        _oLandmark = new List<ILightable>(50);
    }

    public void Add(selfregister _o) {
        // if (_o.currentObjectType == selfregister.objectType.Orientation)
        // {
        //     _oOrientation.Add(_o);
        // }
        // else if (_o.currentObjectType == selfregister.objectType.Landmark)
        // {
        //     _oLandmark.Add(_o);
        // }
    }

    // public void ToggleLight(bool state, selfregister.objectType type)
    // {
    //     List<ILightable> list;
    //     if (type == selfregister.objectType.Orientation)
    //     {
    //         list = _oOrientation;
    //         _lOrientation = state;
    //     }
    //     else
    //     {
    //         list = _oLandmark;
    //         _lLandmark = state;
    //     }

    //     foreach (ILightable _o in list)
    //     {
    //         _o.ToggleLight(state);
    //     }

    //     // Debug.Log($"Toggle: {type} {state} (N={list.Count})");
    // }

    // public bool GetLightState(selfregister.objectType type)
    // {
    //     bool state = type == selfregister.objectType.Orientation ? _lOrientation : _lLandmark;

    //     return state;
    // }
}
