using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;

public class QuestionSlider : MonoBehaviour {
    public static QuestionSlider instance;
    private Slider slider;
    public Text questionTextField, sliderText;
    public string questionText;
    public float sliderValue { get; private set; }
    public Text minText, midText, maxText;
    private float minVal, maxVal;
    // private int minutes, seconds;
    [SerializeField] private bool discrete = true;
    [SerializeField] private bool timeFormat = true;
    [SerializeField] [Range(0.1f, 1)] private float swipeSpeed = 0.5f;


    public SteamVR_Input_Sources anyHand;
    public SteamVR_Action_Boolean sideButtonAction;
    public SteamVR_Action_Boolean trackpadClickAction;
    public SteamVR_Action_Boolean trackpadTouchAction;

    public SteamVR_Action_Vector2 trackpadVector;

    public bool confirmed = false;
    private bool confirming = false;


    float deltaX, range, divisor, rounder, discreteVal;
    bool nonSwipe = true;

    [SerializeField] private string confirmationText = "\n\n\nPress the side button to confirm your answer, or continue sliding to change it.";
    [SerializeField] private string confirmingText = "\n\n\nPress the side button to confirm your answer, or continue sliding to change it.";


    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// </summary>
    void Awake() {
        instance = this;

        slider = GetComponentInChildren<Slider>();

        UpdateSliderRange(30, 300f);
        // Debug.Log("Slider found with value: " + sliderValue);
    }

    public void PadSwipe(SteamVR_Action_Vector2 fromAction, SteamVR_Input_Sources fromSource, Vector2 axis, Vector2 delta) {
        // Debug.Log("Swiped the pad by: " + deltaX + " : " + delta.y);
        if (nonSwipe) {
            deltaX = 0;
            // Debug.Log("Executed non-swipe");
            nonSwipe = false;
        } else
            deltaX = trackpadVector.delta.x * swipeSpeed * range;

        if (deltaX > 0)
            if (sliderValue + deltaX < maxVal)
                sliderValue += deltaX;
            else sliderValue = maxVal;
        else
            if (sliderValue + deltaX > minVal)
            sliderValue += deltaX;
        else sliderValue = minVal;

        UpdateSlider();
    }

    public void UpdateSliderRange(float min, float max, bool visualAnalog = false, string minLabel = "", string midLabel = "", string maxLabel = "") {
        minVal = min;
        maxVal = max;
        if (maxVal >= 5.0f) rounder = 1.0f;
        else rounder = 10.0f;
        range = max - min;
        divisor = 1 / range;
        if (timeFormat) {
            minText.text = SecondsToTime(minVal);
            midText.text = "";
            maxText.text = SecondsToTime(maxVal);
        } else {
            minText.text = Mathf.RoundToInt(minVal).ToString();
            midText.text = (minVal + range / 2f).ToString();
            maxText.text = Mathf.RoundToInt(maxVal).ToString();
        }
        if (minLabel != "") {
            midText.text = minLabel;
        }
        if (midLabel != "") {
            midText.text = midLabel;
        }
        if (maxLabel != "") {
            maxText.text = maxLabel;
        }
        sliderValue = minVal + range / 2f;
        UpdateSlider(visualAnalog);
    }

    public string SecondsToTime(float totalSeconds) {
        int minutes = Mathf.FloorToInt(totalSeconds / 60F);
        int seconds = Mathf.FloorToInt(totalSeconds - minutes * 60);
        return string.Format("{0:0}:{1:00}", minutes, seconds);
    }

    private void UpdateSlider(bool visualAnalog = false) {
        slider.value = (sliderValue - minVal) * divisor;
        discreteVal = Mathf.Round(sliderValue * rounder) / rounder;

        if (discrete)
            slider.value = (discreteVal - minVal) * divisor;
        else
            slider.value = (sliderValue - minVal) * divisor;

        if (timeFormat)
            sliderText.text = SecondsToTime(discreteVal);
        else
            sliderText.text = (discreteVal).ToString();
        if (visualAnalog)
            sliderText.text = "";
    }

    public void PadTouchStart(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource) {
        // Debug.Log("Began Touch");
        nonSwipe = true;
        if (confirming) {
            questionTextField.text = questionText + confirmationText;
            confirming = false;
            Debug.Log("Aborted confirmation from touch start.");
        }
    }
    public void PadTouchEnd(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource) {
        // Debug.Log("Ended Touch");
        nonSwipe = true;
        if (confirming) {
            questionTextField.text = questionText + confirmationText;
            confirming = false;
            Debug.Log("Aborted confirmation from touch end.");
        }
    }

    public void UpdateQuestionText(string text) {
        questionText = text;
        questionTextField.text = questionText + confirmationText;
    }

    private float timeConfirming = 0.0f;
    public void StartConfirmation() {
        confirming = true;
        timeConfirming = Time.time;
        questionTextField.text = questionText + confirmingText;
    }
    public void SideButtonGrip(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource) {
        if (!confirming) {
            StartConfirmation();
        }
        if (confirming) {
            if (Time.time - timeConfirming > 0.5) {
                confirmed = true;
                confirming = false;
                Debug.Log("Successfully confirmed answer: " + sliderValue);
            } else {
                Debug.Log("Confirmation click happened too fast.");
            }
        }
    }

    // Start is called before the first frame update
    void Start() {
        trackpadVector.AddOnChangeListener(PadSwipe, anyHand);
        trackpadTouchAction.AddOnStateDownListener(PadTouchStart, anyHand);
        trackpadTouchAction.AddOnStateUpListener(PadTouchEnd, anyHand);
        sideButtonAction.AddOnStateDownListener(SideButtonGrip, anyHand);
    }

    // Update is called once per frame
    void Update() {

    }
}
