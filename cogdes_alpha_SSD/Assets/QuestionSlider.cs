using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestionSlider : MonoBehaviour {
    public static QuestionSlider instance;
    private Slider slider;

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// </summary>
    void Awake() {
        instance = this;

        slider = GetComponentInChildren<Slider>();
        Debug.Log("Slider found with value: " + slider.value);
    }

    // Start is called before the first frame update
    void Start() {

    }

    // Update is called once per frame
    void Update() {

    }
}
