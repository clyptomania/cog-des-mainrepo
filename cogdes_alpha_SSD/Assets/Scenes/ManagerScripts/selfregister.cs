using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface ILightable
{
    void ToggleLight(bool state);
    bool GetLightState();
}

public class selfregister : MonoBehaviour, ILightable
{
    public enum objectType
    {
        Orientation,
        Landmark
    }

    public objectType currentObjectType;

    // Start is called before the first frame update
    void Awake()
    {
        ExpeControl.instance.condObjects.Add(this);
    }

    public void ToggleLight(bool state)
    {
        gameObject.SetActive(state);
    }

    public bool GetLightState()
    {
        return isActiveAndEnabled;
    }

    public objectType GetObjectType()
    {
        return currentObjectType;
    }
}
