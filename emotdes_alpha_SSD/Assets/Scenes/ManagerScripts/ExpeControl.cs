using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
// using Tilia.Locomotors.Teleporter;
using Tobii.Research;
using Tobii.Research.Unity;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;

using ViveSR.anipal.Eye;

public class ExpeControl : MonoBehaviour {
    public static ExpeControl instance { get; private set; }
    public PluxDeviceManager PluxDevManager;
    public List<List<int>> MultiThreadList = null;
    public List<int> MultiThreadSubList = null;

    public List<int> ActiveChannels;
    public List<string> ListDevices;
    [SerializeField] private int sampleRate;
    [SerializeField] private int bitDepth;
    public string SelectedDevice = "";
    private int batteryLevel = -1;
    public Scrollbar batteryMeter, batteryMeterInfo;
    public Text batteryText;

    [SerializeField] private bool debugging;
    [SerializeField] private bool controllerTutorial, questionnaireTutorial;
    [SerializeField] private bool deleteOldData = false;
    [SerializeField] private bool eyeTracking = true;
    [SerializeField] private bool preTesting = true;
    [SerializeField] private bool lightSync = true;

    [SerializeField]
    private int max_idx = 100;

    [SerializeField]
    private Button controllerCalButton, trackerCalButton, hfgButton, sglButton, enButton, deButton, preButton, studyButton,
    startButton, continueButton, syncButton, syncConfirmButton, scanButton, connectButton, acqButton, sampleButton;
    [SerializeField] private Toggle chan1Toggle, chan2Toggle, chan3Toggle, chan4Toggle, eTToggle;
    [SerializeField] private InputField chan1Field, chan2Field, chan3Field, chan4Field, sampleRateField;
    public Image connectionIndicator, signal1Indicator, signal2Indicator, signal3Indicator, signal4Indicator;

    [SerializeField] private List<int> durations = new List<int>();

    // Playlist data
    private readonly List<playlistElement> playlist = new List<playlistElement>(90);
    private readonly List<EmotPlaylistElement> emotPlaylist = new List<EmotPlaylistElement>(90);
    private List<List<EmotPlaylistElement>> allPlaylists = new List<List<EmotPlaylistElement>>(90);
    public int m_currentTrialIdx = 0;
    public int m_currentQuestionIdx = 0;
    public int currentTrialIdx => m_currentTrialIdx;
    public EmotPlaylistElement currentEmotTrial => emotPlaylist[currentTrialIdx];
    public EmotPlaylistElement nextEmotTrial => emotPlaylist[currentTrialIdx + 1];
    // public playlistElement currentTrial => playlist[currentTrialIdx];
    // private string currentRoom => playlist[currentTrialIdx].room_name;
    // private int currentRoomIdx => playlist[currentTrialIdx].room_idx;

    private Transform cameraRig;
    public Camera mainCam;

    public enum lateralisation {
        left,
        right,
        comb
    }

    public enum lang {
        english,
        german
    }

    public enum instruction {
        hfgSit,
        hfgLean,
        sglSit,
        sglStand,
        calibrate,
        chill
    }

    public struct LightStruct {
        public bool orientation;
        public bool landmark;
    }
    static public List<LightStruct> LightConditions = new List<LightStruct>(4) {
        // 0: No lightning (ctrl)
        new LightStruct { orientation = false, landmark = false },
        // 1: set light on orientation markers
        new LightStruct { orientation = true, landmark = false },
        // 2: set light on landmarks
        new LightStruct { orientation = false, landmark = true },
        // 3: set light on landmarks and orientation markers
        new LightStruct { orientation = true, landmark = true },
    };

    // UI go
    public GameObject setupPanel, infoPanel, promptPanel, pluxPanel;

    public Image syncPanel;

    // User data
    private int m_userId = -1;
    private string m_labID = "Space Lab";
    private string m_basePath;
    public string m_userdataPath { get; private set; }

    // Record data
    private StreamWriter m_recorder_ET = StreamWriter.Null;
    public StreamWriter m_recorder_HMD = StreamWriter.Null;
    private StreamWriter m_recorder_info = StreamWriter.Null;
    private StreamWriter m_recorder_question = StreamWriter.Null;
    private StreamWriter m_recorder_plux = StreamWriter.Null;

    public GameObject pausePanel;
    public GameObject questionPanel;

    public lang language = lang.english;

    private TrainSpawner trainSpawner;

    private readonly Dictionary<string, string> questionsEN = new Dictionary<string, string> {
        {
            "vrLife",
            "How many hours have you spent in VR so far in your life?"
        },
        {
            "height",
            "How tall are you (in cm)?"
        },
        {
            "age",
            "How old are you?"
        },
        {
            "sex",
            // "Which gender do you identify with?"
            "To which gender do you identify?" // Modern convention
        },
        {
            "occupation",
            "Which occupation describes yours best?"
        },
        {
            "transportFreq",
            "How often do you normally use public transport?"
        },
        {
            "patience",
            "How patient would you consider yourself to be?"
        },
        {
            "waitEstimation",
            "How long do you think you have been waiting from the START of the waiting period until NOW?"
        },
        {
            "valence",
            "Which of these pictures best represents your emotional state during the waiting time?"
        },
        {
            "arousal",
            "Which of these pictures best represents your excitement during the waiting time?"
        },
        {
            "thinkPast",
            "How much did you think about\nYOUR PAST while waiting?"
        },
        {
            "thinkPresent",
            "How much did you think about\nYOUR PRESENT while waiting?"
        },
        {
            "thinkFuture",
            "How much did you think about\nYOUR FUTURE while waiting?"
        },
        {
            "experienceBody",
            "How intensively did you experience\nYOUR BODY most of the time while waiting?"
        },
        {
            "experienceSpace",
            "How intensively did you experience \nTHE SURROUNDING SPACE most of the time while waiting?"
        },
        {
            "thinkTime",
            "How often did you think about time while waiting?"
        },
        {
            "timePass",
            "How fast did time pass for you while waiting?"
        },
        {
            "realism",
            "How realistic did you find the virtual subway station?"
        },
        {
            "differences",
            "Do you think the scenes you've experienced were similar or different from one another?"
        },
        {
            "preferPosture",
            "Which POSTURE did you prefe while waiting?"
        },
        {
            "preferMaterial",
            "Which SEAT MATERIAL did you prefer?"
        },
        {
            "preferLighting",
            "Which kind of ILLUMINATION did you prefer?"
        },
        {
            "comfort",
            "How comfortable were you during the waiting period?"
        },
        {
            "relax",
            "How relaxed or tense did you feel during the waiting period?"
        },
        {
            "tired",
            "How tired or awake did you feel during the waiting period?"
        },
        {
            "totalTime",
            "How long did you estimate the experiment took in total?"
        }
    };

    private readonly Dictionary<string, string> questionsDE = new Dictionary<string, string> {
        {
            "vrLife",
            "Wie viele Stunden hast du bisher in deinem Leben in VR verbracht?"
        },
        {
            "height",
            "Wie groß bist du (in cm)?"
        },
        {
            "age",
            "Wie alt bist du?"
        },
        {
            "sex",
            "Als welches Geschlecht identifizierst du dich?"
        },
        {
            "occupation",
            "Welche Beschäftigung stimmt am ehesten mit deiner überein?"
        },
        {
            "transportFreq",
            "Wie oft benutzt du normalerweise öffentliche Verkehrsmittel?"
        },
        {
            "patience",
            "Wie geduldig würdest du dich selbst einschätzen?"
        },
        {
            "waitEstimation",
            "Wie lange, glaubst du, hast du seit BEGINN der Wartezeit bis JETZT gewartet?"
        },
        {
            "valence",
            "Welches dieser Bilder stellt deine Gefühlslage während der Wartezeit am besten dar?"
        },
        {
            "arousal",
            "Welches dieser Bilder stellt deine Aufregung während der Wartezeit am besten dar?"
        },
        {
            "thinkPast",
            "Wie oft hast du während des Wartens über \ndeine VERGANGENHEIT nachgedacht?"
        },
        {
            "thinkPresent",
            "Wie oft hast du während des Wartens über \ndeine GEGENWART nachgedacht?"
        },
        {
            "thinkFuture",
            "Wie oft hast du während des Wartens über \ndeine ZUKUNFT nachgedacht?"
        },
        {
            "experienceBody",
            "Wie intensiv hast du deinen KÖRPER während des Wartens wahrgenommen?"
        },
        {
            "experienceSpace",
            "Wie intensiv hast du deine UMGEBUNG während des Wartens wahrgenommen?"
        },
        {
            "thinkTime",
            "Wie oft hast du beim Warten über Zeit an sich nachgedacht?"
        },
        {
            "timePass",
            "Wie schnell ist die Zeit für dich vergangen beim Warten?"
        },
        {
            "realism",
            "Wie realistisch fandest du die virtuelle U-Bahn-Station?"
        },
        {
            "differences",
            "Wie unterschiedlich oder ähnlich kamen dir die einzelnen Szenarios vor?"
        },
        {
            "preferPosture",
            "Welche KÖRPERHALTUNG hast du bevorzugt?"
        },
        {
            "preferMaterial",
            "Welches SITZMATERIAL hast du bevorzugt?"
        },
        {
            "preferLighting",
            "Welche Art von BELEUCHTUNG hat dir besser gefallen?"
        },
        {
            "comfort",
            "Wie bequem hast du die Wartezeit empfunden?"
        },
        {
            "relax",
            "Wie entspannt oder angespannt hast du dich während der Wartezeit gefühlt?"
        },
        {
            "tired",
            "Wie müde oder wach hast du dich während der Wartezeit gefühlt?"
        },
        {
            "totalTime",
            "Wie lange glaubst du hat das gesamte Experiment gedauert?"
        }
    };

    private readonly Dictionary<string, string> messagesEN = new Dictionary<string, string> {
        {
            "calibrateVSR",
            "Now we need to callibrate the EYE TRACKER.\n\n" +
            "Please pull the trigger to start the callibration!    "
        },
        {
            "calibrateTob",
            "Please start the EYE TRACKER calibration.\n" +
            "FOLLOW the RED DOT as closely as you can!\n\n" +
            "Pull the trigger to continue."
        },
        {
            "takeBreak",
            "You can now take a quick break.\n\n" +
            "Please remove the headset and follow the instructions of the assistant."
        },
        {
            "endBreak",
            "Pull the trigger to continue with the next scenes!"
        },
        {
            "pleaseLean",
            "Please lean on the high bench.\n\n" +
            "Once you're seated, pull the trigger to continue."
        },
        {
            "pleaseSit",
            "Please sit down on the low bench.\n\n" +
            "Once you're seated, pull the trigger to continue."
        },
        {
            "pleaseStand",
            "Please stand up and take one step forward, away from the bench.\n\n" +
            "Once you're standing, pull the trigger to continue."
        },
        {
            "pleaseCalibrateTobii",
            "To begin, we need to calibrate the eye tracker.\n" +
            "With your eyes, follow the red dot, then the gray disks as closely as you can.\n\n" +
            "Pull the trigger to continue."
        },
        {
            "pleaseCalibrateVive",
            "To begin, we need to calibrate the eye tracker.\n" +
            "Press the MENU button on your controller and select the eye-tracking procedure.\n\n" +
            "Pull the trigger ONLY when you've completed it to continue."
        },
        {
            "start",
            "The training phase has ended.\n\n" +
            "Pull the trigger to start the experiment."
        },
        {
            "pause",
            "Take off the headset if you wish.\n\n" +
            "Take a moment to rest before continuing with the experiment.\n" +
            "Pull the trigger to start the experiment."
        },
        {
            "loading",
            "The next room is being loaded..."
        },
        {
            "unloading",
            "The room is being unloaded..."
        },
        {
            "end",
            "This is the end of the experiment!\n\nThank you very much for your participation.\n\nYou can take off the headset."
        },
        {
            "beginWaitingSit",
            "The waiting period will begin soon!\n" +
            "You will get notified once it's over, and you will be asked questions about your experience then. Please sit down, and pull the trigger to begin!"
        },
        {
            "instructionEndSitLean",
            "The waiting time is now over!\n\n" +
            "Please stand up and take a step forward,\n" +
            "away from the seats.\n\n" +
            "Pull the trigger to continue with the questionnaires."
        },
        {
            "instructionEndStand",
            "The waiting time is now over!\n\n" +
            "You can now sit down if you like.\n\n" +
            "Pull the trigger to continue with the questionnaires."
        },
        // instruction.hfgLean instruction.hfgSit instruction.sglSit instruction.sglStand,
        {
            "instructionSitSGL",
            "The waiting period will begin soon!\n" +
            "You will get notified once it's over, and you will be asked questions about your experience then.\n\n" +
            "Please sit down, and pull the trigger to begin!"
        },
        {
            "instructionStandSGL",
            "The waiting period will begin soon!\n" +
            "You will get notified once it's over, and you will be asked questions about your experience then.\n\n" +
            "Please stand a bit away from the seat, and pull the trigger to begin!"
        },
        {
            "instructionSitHFG",
            "The waiting period will begin soon!\n" +
            "You will get notified once it's over, and you will be asked questions about your experience then.\n\n" +
            "Please sit down on the low bench, and pull the trigger to begin!"
        },
        {
            "instructionLeanHFG",
            "The waiting period will begin soon!\n" +
            "You will get notified once it's over, and you will be asked questions about your experience then.\n\n" +
            "Please lean on the high bench, and pull the trigger to begin!"
        },
        {
            "three",
            "3..."
        },
        {
            "two",
            "2..."
        },
        {
            "one",
            "1..."
        },
        {
            "beginQuestions",
            "The waiting time is now over!\n\n" +
            "Please stand up now and step a way a bit from the chairs.\n\n" +
            "Pull the trigger to continue with the questionnaires."
        },
        {
            "endQuestions",
            "Thank you!\n\n" +
            "The questionnaire is now finished. Get ready for the next trial!"
        },
        {
            "outroQuestions",
            "The experiment is almost over!\n\n" +
            "Here are some final questions for you.\n\n" +
            "Pull the trigger to continue."
        }
    };
    private readonly Dictionary<string, string> messagesDE = new Dictionary<string, string> {
        {
            "calibrateVSR",
            "Starte bitte die EYE TRACKER Kalibrierung.\n" +
            "Du kannst die Assistenten dabei jederzeit um Hilfe bitten.\n\n" +
            "Halte den Trigger gedrückt um die Kallibrierung zu starten!"
        },
        {
            "calibrateTob",
            "Starte bitte die EYE TRACKER Kalibrierung.\n" +
            "FOLGE dem ROTEN PUNKT mit den Augen so genau wie möglich!\n\n" +
            "Halte den Trigger gedrückt um fortzufahren!"
        },
        {
            "takeBreak",
            "Du kannst jetzt eine kurze Pause machen.\n\n" +
            "Bitte nehme das Heaset ab und wende dich an die Assistenten."
        },
        {
            "endBreak",
            "Halte den Trigger gedrückt um fortzufahren!"
        },
        {
            "pleaseLean",
            "Bitte lehne dich an den Anlehner an.\n\n" +
            "Sobald du Platz genommen hast, halte den Trigger gedrückt, um fortzufahren."
        },
        {
            "pleaseSit",
            "Bitte setze dich auf die Bank.\n\n" +
            "Sobald du Platz genommen hast, halte den Trigger gedrückt, um fortzufahren."
        },
        {
            "pleaseStand",
            "Bitte stehe auf und trete einen Schritt nach vorne, weg von der Bank.\n\n" +
            "Sobald du stehst, halte den Trigger gedrückt, um fortzufahren."
        },
        {
            "pleaseCalibrateTobii",
            "Sobald du stehst, müssen wir den Eye-Tracker kalibrieren.\n" +
            "Verfolge erst den roten Punkt und dann die grauen Scheiben so genau wie möglich mit deinen Augen.\n\n" +
            "Halte den Trigger gedrückt um fortzufahren!"
        },
        {
            "pleaseCalibrateVive",
            "Zunächst müssen wir den Eyetracker kalibrieren.\n" +
            "Drücke die MENÜ-TASTE am Controller und wähle das Eye-Tracking-Verfahren aus.\n\n" +
            "Drücke den Auslöser ERST WENN die Aufgabe abgeschlossen ist, um fortzufahren."
        },
        {
            "start",
            "Die Trainingsphase ist beendet.\n\n" +
            "Halte den Trigger gedrückt, um das Experiment zu starten."
        },
        {
            "pause",
            "Wenn du möchtest, nehme das Headset ab.\n\n" +
            "Nimm dir einen Moment Zeit, um dich auszuruhen, bevor du mit dem Experiment fortfährst.\n" +
            "Halte den Trigger gedrückt, um das Experiment zu starten."
        },
        {
            "loading",
            "Raum wird geladen..."
        },
        {
            "unloading",
            "Raum wird geladen..."
        },
        {
            "end",
            "Das ist das Ende des Experiments!\n\nVielen Danke für deine Teilnahme.\n\nDu kannst das Headset abnehmen."
        },
        {
            "beginWaitingSit",
            "Die Wartezeit wird gleich beginnen!\n" +
            "Wenn diese vorüber ist, wirst du benachrichtigt und dir werden Fragen gestellt.\n\n" +
            "Bitte setze dich, und halte den Trigger gedrückt, um zu beginnen!"
        },
        {
            "instructionEndSitLean",
            "Die Wartezeit ist nun vorbei!\n\n" +
            "Bitte stehe auf und trete ein Stück von der Bank weg.\n\n" +
            "Halte den Trigger gedrückt, um mit den Fragen fortzufahren."
        },
        {
            "instructionEndStand",
            "Die Wartezeit ist nun vorbei!\n\n" +
            "Wenn du möchtest, kannst Du dich jetzt setzen.\n\n" +
            "Halte den Trigger gedrückt, um mit den Fragen fortzufahren."
        },
        // instruction.hfgLean instruction.hfgSit instruction.sglSit instruction.sglStand,
        {
            "instructionSitSGL",
            "Die Wartezeit wird gleich beginnen!\n" +
            "Wenn diese vorüber ist, wirst du benachrichtigt und dir werden Fragen gestellt.\n\n" +
            "Bitte setze dich, und halte den Trigger gedrückt, um zu beginnen!"
        },
        {
            "instructionStandSGL",
            "Die Wartezeit wird gleich beginnen!\n" +
            "Wenn diese vorüber ist, wirst du benachrichtigt und dir werden Fragen gestellt.\n\n" +
            "Bitte stehe etwas vom Sitz entfernt, und halte den Trigger gedrückt, um zu beginnen!"
        },
        {
            "instructionSitHFG",
            "Die Wartezeit wird gleich beginnen!\n" +
            "Wenn diese vorüber ist, wirst du benachrichtigt und dir werden Fragen gestellt.\n\n" +
            "Bitte setze dich auf die niedrige Bank, und halte den Trigger gedrückt, um zu beginnen!"
        },
        {
            "instructionLeanHFG",
            "Die Wartezeit wird gleich beginnen!\n" +
            "Wenn diese vorüber ist, wirst du benachrichtigt und dir werden Fragen gestellt.\n\n" +
            "Bitte lehne dich an die hohe Bank an, und halte den Trigger gedrückt, um zu beginnen!"
        },
        {
            "three",
            "3..."
        },
        {
            "two",
            "2..."
        },
        {
            "one",
            "1..."
        },
        {
            "beginQuestions",
            "Die Wartezeit ist nun vorbei!\n\n" +
            "Bitte stehe auf und trete ein Stück von der Sitzbank weg.\n\n" +
            "Halte den Trigger gedrückt, um mit den Fragen fortzufahren."
        },
        {
            "endQuestions",
            "Vielen Dank!\n\n" +
            "Der Fragebogen ist nun beendet. Halte dich bereit für die nächste Szene!"
        },
        {
            "outroQuestions",
            "Das Experiment ist fast vorbei!\n\n" +
            "Es gibt nur noch ein paar abschließende Fragen.\n\n" +
            "Halte den Trigger gedrückt, um fortzufahren"
        }
    };


    private EyeTrackingSampler _eyeTrack => EyeTrackingSampler.instance;
    private ProgressBar _progressBar => ProgressBar.instance;
    private RadialProgress _radialProgress => RadialProgress.instance;
    private QuestionSlider _questionSlider => QuestionSlider.instance;
    private bool isTracking => (_eyeTrack.ready);
    private InstructBehaviour _instructBehaviour;
    public ObjectManager condObjects { get; private set; }
    // private TeleporterFacade _teleporter;

    [Tooltip("Time in seconds needed to click trigger to continue")] public float durationToContinue;
    [Tooltip("Time in seconds needed wait between messages")] public float messageWaitDuration;
    [Tooltip("Time in seconds the screen will flash for sync")] public float syncFlashDuration;

    void Awake() {

        instance = this;

        if (mainCam == null)
            mainCam = Camera.main;

        syncPanel.gameObject.SetActive(false);
        syncPanel.color = Color.black;

        tobiiTracking = PlayerPrefs.GetInt("tobii", 0) != 0;
        language = PlayerPrefs.GetInt("german") == 1 ? lang.german : lang.english;
        preTesting = PlayerPrefs.GetInt("pretesting") == 1 ? true : false;
        eyeTracking = PlayerPrefs.GetInt("eyetracking") == 1 ? true : false;
        m_labID = tobiiTracking ? "SGL" : "HfG";

        if (eyeTracking) {
            GetComponent<VREyeTracker>().enabled = true;
            SRAnipal.SetActive(true);
        }

        if (tobiiTracking) {
            Debug.Log("Adding Tobii callback");
            shaderBehavior.validationCallback = (success) => {
                this.m_validationSuccess = success;
                this.m_validationDone = true;
            };
        }

        string[] RoomNames = RoomManager.RoomNames;
        cameraRig = transform.GetChild(0);
        Debug.Log("Found camera rig: " + cameraRig.name);
        Debug.Log("CamRig pos: " + cameraRig.position + " CamRig rot: " + cameraRig.rotation);
        _instructBehaviour = GetComponent<InstructBehaviour>();

        // if (_instructBehaviour.leftControllerActive) {
        //     controllerBasePoint = GameObject.Find("BasePointL");
        _questionSlider.controllerMainPoint = GameObject.Find("ControlPointL");
        // } else {
        //     controllerBasePoint = GameObject.Find("BasePointR");
        //     _questionSlider.controllerMainPoint = GameObject.Find("ControlPointR");
        // }

        // controllerBasePoint.SetActive(false);
        // _questionSlider.controllerMainPoint.SetActive(false);

        // condObjects = new ObjectManager();

        // _teleporter = gameObject.GetComponentInChildren<TeleporterFacade>();

        // Disable panels
        setupPanel.SetActive(false);
        // infoPanel.SetActive(false);
        pausePanel.SetActive(false);

        grayArea.SetActive(false);
        // questionPanel.SetActive(false);

        LoadPlaylistsFromCSVs();

        GetPreviousParticipant();
        // GetLastUserNumber ();
        // TestParticipantID ();
        // TestTrialID ();

    }


    public void ScanResults(List<string> listDevices) {
        // Store list of devices in a global variable.
        this.ListDevices = listDevices;
        pluxScanned = true;
        // Info message for development purposes.
        Debug.Log("Number of Detected Devices: " + this.ListDevices.Count);
        for (int i = 0; i < this.ListDevices.Count; i++) {
            Debug.Log("Device--> " + this.ListDevices[i]);
        }

        if (this.ListDevices.Count != 0) {
            List<string> dropDevices = new List<string>();

            // Convert array to list format.
            dropDevices.AddRange(this.ListDevices);

            // A check into the list of devices.
            dropDevices = dropDevices.GetRange(0, dropDevices.Count);
            for (int i = dropDevices.Count - 1; i >= 0; i--) {
                // Accept only strings containing "BTH" or "BLE" substrings "flagging" a PLUX Bluetooth device.
                if (!dropDevices[i].Contains("BTH") && !dropDevices[i].Contains("BLE")) {
                    dropDevices.RemoveAt(i);
                }
            }

            Debug.Log("Number of flagged devices: " + dropDevices.Count);
            for (int i = dropDevices.Count - 1; i >= 0; i--) {
                Debug.Log("Device--> " + dropDevices[i]);
            }

            this.ListDevices = dropDevices;

            // Raise an exception if none device was detected.
            if (dropDevices.Count == 0) {
                connectButton.interactable = false;
                connectionIndicator.color = Color.HSVToRGB(0, 0.75f, 0.75f);
                throw new ArgumentException();
            } else {
                connectButton.interactable = true;
                connectionIndicator.color = Color.HSVToRGB(0.5f, 0.75f, 0.75f);
            }
        }

        scanButton.interactable = true;
    }

    public void UpdateBatteryLevel() {
        if (pluxConnected) {
            batteryLevel = PluxDevManager.GetBatteryUnity();
            batteryMeterInfo.GetComponentInChildren<Text>().text = batteryLevel.ToString();
            batteryMeterInfo.size = (float)batteryLevel / 100f;
            ColorBlock batteryColors = batteryMeterInfo.colors;
            batteryColors.disabledColor = Color.HSVToRGB((float)batteryLevel / 240f, 1, 1);
            batteryMeterInfo.colors = batteryColors;
        }
    }

    public void ConnectionDone() {
        Debug.Log("Connection with device " + this.SelectedDevice + " established with success!");

        connectionIndicator.color = Color.green;

        string devType = PluxDevManager.GetDeviceTypeUnity();
        Debug.Log("Product ID: " + PluxDevManager.GetProductIdUnity());
        Debug.Log("DevType: " + devType);


        if (devType != "BioPlux") {
            batteryLevel = PluxDevManager.GetBatteryUnity();
        }

        if (batteryLevel > -1) {
            Debug.Log("Battery level at " + batteryLevel + "%");
            batteryText.text = batteryLevel.ToString();
            batteryMeter.size = (float)batteryLevel / 100f;
            ColorBlock batteryColors = batteryMeter.colors;
            batteryColors.disabledColor = Color.HSVToRGB((float)batteryLevel / 240f, 1, 1);
            batteryMeter.colors = batteryColors;
        }

        UpdateBatteryLevel();
        connectButton.interactable = true;
        connectButton.GetComponentInChildren<Text>().text = "<i>Disconnect</i>";

        ColorBlock buttonColors = connectButton.colors;
        buttonColors.normalColor = Color.red;
        connectButton.colors = buttonColors;

        // scanButton.interactable = false;
        acqButton.interactable = true;
        pluxConnected = true;

        usingPlux = true;
    }


    IEnumerator ColorPhase(float startHue = 0.75f, string procedure = "scan") {
        float hue = startHue;
        float startTime = Time.time;
        bool flag = false;
        while (!flag) {
            if (procedure == "scan")
                flag = pluxScanned;
            else
                flag = pluxConnected;
            hue = startHue + (Mathf.Abs(((Time.time - startTime) % 2) - 1) - 0.5f) / 2;
            // Debug.Log(hue);
            // 0.75f + Mathf.Cos(Time.time - startTime) / 4f;
            connectionIndicator.color = Color.HSVToRGB(hue, 0.75f, 0.75f);
            yield return null;
        }
        if (pluxConnected) {
            connectionIndicator.color = Color.green;
        } else {
            if (connectButton.interactable) {
                connectionIndicator.color = Color.HSVToRGB(0.5f, 0.75f, 0.75f);
            } else {
                connectionIndicator.color = Color.HSVToRGB(0, 0.75f, 0.75f);
            }
        }
    }

    private bool autoConnect = false;
    private bool pluxConnected = false;
    private bool pluxScanned = false;
    public void ScanFunction(bool auto = false) {

        pluxScanned = false;

        if (!pluxConnected)
            connectButton.interactable = false;
        connectionIndicator.color = Color.HSVToRGB(0.5f, 0.75f, 0.75f);

        StartCoroutine(ColorPhase(0.75f, "scan"));

        try {
            // List of available Devices.
            List<string> listOfDomains = new List<string>();
            listOfDomains.Add("BTH");

            PluxDevManager.GetDetectableDevicesUnity(listOfDomains);

            // Disable scan button.
            scanButton.interactable = false;
            Debug.Log("Started scanning");
        } catch (Exception e) {
            // Show info message.
            // BluetoothInfoPanel.SetActive(true);

            // Hide object after 5 seconds.
            // StartCoroutine(RemoveAfterSeconds(5, BluetoothInfoPanel));

            // Disable Drop-down.
            // DeviceDropdown.interactable = false;
        }
    }

    public void Disconnect() {
        try {
            // Disconnect device.
            PluxDevManager.DisconnectPluxDev();
        } catch (Exception exception) {
            Debug.Log("Trying to disconnect from an unconnected device...");
        }
        connectButton.GetComponentInChildren<Text>().text = "Connect";

        ColorBlock buttonColors = connectButton.colors;
        buttonColors.normalColor = Color.white;
        connectButton.colors = buttonColors;

        acqButton.interactable = false;
        sampleButton.interactable = false;

        sampleRateField.interactable = true;

        chan1Field.interactable = true;
        chan2Field.interactable = true;
        chan3Field.interactable = true;
        chan4Field.interactable = true;

        chan1Toggle.interactable = true;
        chan2Toggle.interactable = true;
        chan3Toggle.interactable = true;
        chan4Toggle.interactable = true;

        pluxConnected = false;
    }

    public void ConnectFunction() {

        if (pluxConnected) {
            Disconnect();
            return;
        }

        pluxConnected = false;
        connectButton.interactable = false;

        connectionIndicator.color = Color.HSVToRGB(0.5f, 0.75f, 0.75f);
        StartCoroutine(ColorPhase(0.5f, "connect"));
        try {
            this.SelectedDevice = this.ListDevices[0];
            // Connection with the device.
            Debug.Log("Trying to establish a connection with device " + this.SelectedDevice);
            PluxDevManager.PluxDev(this.SelectedDevice);
        } catch (Exception e) {
            // Print information about the exception.
            Debug.Log(e);

            // Show info message.
            // ConnectInfoPanel.SetActive(true);

            // Hide object after 5 seconds.
            // StartCoroutine(RemoveAfterSeconds(5, ConnectInfoPanel));
        }
    }

    private bool pluxSampling = false;
    private bool usingPlux = false;
    private bool pluxperimenting = false;
    public void TogglePluxAcquisition(string special = "") {

        if (pluxSampling) {

            PluxDevManager.StopAcquisitionUnity();
            Debug.Log("Stopped Plux data acquisition.");

            stopPluxRecord();

            acqButton.GetComponentInChildren<Text>().text = "Acquisition";

            sampleButton.interactable = false;

            sampleRateField.interactable = true;

            chan1Field.interactable = true;
            chan2Field.interactable = true;
            chan3Field.interactable = true;
            chan4Field.interactable = true;

            chan1Toggle.interactable = true;
            chan2Toggle.interactable = true;
            chan3Toggle.interactable = true;
            chan4Toggle.interactable = true;

            pluxSampling = false;

            if (startButtonReady) {
                startButton.interactable = true;
                continueButton.interactable = true;
            }

            return;
        }

        ActiveChannels = new List<int>();
        if (chan1Toggle.isOn) ActiveChannels.Add(1);
        if (chan2Toggle.isOn) ActiveChannels.Add(2);
        if (chan3Toggle.isOn) ActiveChannels.Add(3);
        if (chan4Toggle.isOn) ActiveChannels.Add(4);

        if (ActiveChannels.Count > 0) {
            PluxDevManager.StartAcquisitionUnity(sampleRate, ActiveChannels, bitDepth);
            if (PluxDevManager.GetNbrChannelsUnity() > 0) {

                Debug.Log("Commenced Plux acquisition with " + PluxDevManager.GetNbrChannelsUnity() + " active channels.");

                startNewPluxRecord(special);

                pluxSampling = true;

                startButton.interactable = false;
                continueButton.interactable = false;


                acqButton.GetComponentInChildren<Text>().text = "Stop Sampling";
                acqButton.interactable = true;

                sampleButton.interactable = true;

                sampleRateField.interactable = false;

                chan1Field.interactable = false;
                chan2Field.interactable = false;
                chan3Field.interactable = false;
                chan4Field.interactable = false;

                chan1Toggle.interactable = false;
                chan2Toggle.interactable = false;
                chan3Toggle.interactable = false;
                chan4Toggle.interactable = false;
            }
        }
    }

    public void ValidateSampleRate() {
        bool parsed = int.TryParse(sampleRateField.text, out int sRate);
        if (parsed) {
            if (sRate < 1 || sRate > 16000) {
                sampleRateField.text = "100";
                sampleRate = 100;
            } else {
                sampleRate = sRate;
            }
            PlayerPrefs.SetInt("sampleRate", sampleRate);
        }
    }

    void WritePluxLines(long[][] package) {

        for (int i = 0; i < package.Length; i++) {
            string dataLine = package[i][0].ToString();
            // string dataLine = getTimeStamp().ToString();
            for (int j = 1; j < package[i].Length; j++) {
                dataLine += "," + package[i][j];
            }
            if (m_recorder_plux.BaseStream.CanWrite) {
                m_recorder_plux.WriteLine(dataLine);
            }
        }
    }
    void WritePluxLinesOwnTS(long[][] package, long dumpTime) {

        float timeStepMillis = (1000.0f / (float)sampleRate);
        long firstSampleTime = dumpTime - Mathf.RoundToInt(timeStepMillis * (float)package.Length);
        long sampleTime = firstSampleTime;

        for (int i = 0; i < package.Length; i++) {
            sampleTime = firstSampleTime + Mathf.RoundToInt(timeStepMillis * ((float)(i + 1)));
            string dataLine = (sampleTime).ToString();
            // string dataLine = getTimeStamp().ToString();
            for (int j = 0; j < package[i].Length; j++) {
                dataLine += "," + package[i][j];
            }
            if (m_recorder_plux.BaseStream.CanWrite) {
                m_recorder_plux.WriteLine(dataLine);
            }
        }
    }

    public void GetSample() {
        // Debug.Log("Getting samples from the Plux!");


        long[][] testPackage = PluxDevManager.GetPackageOfData(true);
        // WritePluxLinesOwnTS(testPackage, getTimeStamp());
        WritePluxLines(testPackage);


        // foreach (int chan in ActiveChannels) {
        //     int[] testPackage = PluxDevManager.GetPackageOfData(chan, ActiveChannels, true);
        //     if (testPackage != null) {
        //         Debug.Log(testPackage.Length + " samples from channel " + chan + " :");
        //         for (int i = 0; i < testPackage.Length; i++) {
        //             // Debug.Log(testPackage[i]);
        //         }
        //     }
        // }

        // int[][] testPackage = PluxDevManager.GetPackageOfData(false);
        // if (testPackage[0] != null) {
        //     for (int i = 0; i < testPackage[0].Length; i++) {
        //         Debug.Log("Data from channel " + i + 1 + ": ");
        //         for (int j = 0; j < testPackage.Length; j++) {
        //             Debug.Log(testPackage[i][j]);
        //         }
        //     }
        // }
    }

    bool lightSynced = false;
    bool syncButtonPressed = false;

    public void SyncButtonPress() {
        lightSynced = false;
        syncButtonPressed = true;
    }
    public void SyncConfirmPress() {
        if (!lightSync)
            return;
        syncButtonPressed = false;
        lightSynced = true;

        syncButton.gameObject.SetActive(false);
        syncButton.gameObject.SetActive(false);
        syncConfirmButton.interactable = false;
        syncPanel.gameObject.SetActive(false);
    }

    public void TestParticipantID() {

        int.TryParse(participantIDField.text, out int participantID);

        if (participantID < 1)
            participantID = 1;
        if (participantID > allPlaylists.Count)
            participantID = allPlaylists.Count;

        participantIDField.text = participantID.ToString();

        m_userId = participantID;

        m_userdataPath = m_basePath + "/Subj_" + m_userId;

        GetPreviousTrial();

        // if (Directory.Exists (m_userdataPath)) {
        //     Debug.Log ("Participant data already exists at " + m_userdataPath);

        //     int count = Directory.GetFiles (m_userdataPath, "*.csv", SearchOption.AllDirectories).Length;
        //     if (count > 2) {
        //         m_currentTrialIdx = (count - 1) / 2;

        //         // if (m_currentTrialIdx)

        //         trialIDField.text = (m_currentTrialIdx + 1).ToString ();
        //     }

        // } else {
        //     Debug.Log ("Participant data does not exist yet for " + m_userdataPath);
        //     trialIDField.text = "1";
        // }

        // TestTrialID ();

    }

    public void TestTrialID() {
        int.TryParse(trialIDField.text, out int trialID);

        int maxTrials = allPlaylists[m_userId - 1].Count;

        if (trialID < 1)
            trialID = 1;
        if (trialID > maxTrials)
            trialID = maxTrials;

        trialIDField.text = trialID.ToString();

        m_currentTrialIdx = trialID - 1;
        continueButton.interactable = true;
    }

    public void GetPreviousParticipant() {

        m_basePath = Directory.GetParent(Application.dataPath) + "/SubjectData/" + m_labID;
        if (preTesting)
            m_basePath += "/PreTests";
        // Working in D:\Maxim\cog-des-mainrepo\emotdes_alpha_SSD/SubjectData/SGL
        // Working in D:\Maxim\cog-des-mainrepo\emotdes_alpha_SSD/SubjectData/SGL/PreTests

        if (!Directory.Exists(m_basePath))
            Directory.CreateDirectory(m_basePath);

        Debug.Log("Working in " + m_basePath);

        string[] directories = Directory.GetDirectories(m_basePath);
        int lastSubjID = 1;
        string lastSubjectDir = m_basePath + "/Subj_" + lastSubjID;


        if (!Directory.Exists(lastSubjectDir))
            Directory.CreateDirectory(lastSubjectDir);

        foreach (string directory in directories) {
            string folder = directory.Split('/').Last();

            int tmpSubjID;

            string[] splitResults = folder.Split('_');
            if (splitResults.Length > 1) {
                int.TryParse(splitResults[1], out tmpSubjID);

                if (tmpSubjID > lastSubjID) {
                    lastSubjID = tmpSubjID;
                    lastSubjectDir = m_basePath + "/Subj_" + lastSubjID;
                }
            }
        }

        m_userId = lastSubjID;
        Debug.Log("Last detected participant folder " + " from " + m_labID + ": " + lastSubjID);
        Debug.Log("Looking at " + lastSubjectDir);

        // m_userdataPath = m_basePath + "/Subj_" + m_userId;

        // string[] answerFiles = Directory.GetFiles (lastSubjectDir, "*_Answers.csv", SearchOption.AllDirectories);
        int count = Directory.GetFiles(lastSubjectDir, "*_Answers.csv", SearchOption.AllDirectories).Length;
        int maxTrials = allPlaylists[m_userId - 1].Count;

        Debug.Log("Detected " + count + " answer files for participant " + lastSubjID);

        if (count == 0) {
            Debug.Log("The last participant did not complete any answers. Starting with that participant ID.");

            m_userId = lastSubjID;
            m_currentTrialIdx = 0;

            participantIDField.text = m_userId.ToString();
            trialIDField.text = (m_currentTrialIdx + 1).ToString();

        } else if (count < maxTrials) {
            Debug.Log("The last participant completed " + count + " answers. Resuming with that participant ID from trial " + (count + 1));

            m_userId = lastSubjID;
            m_currentTrialIdx = count;

            participantIDField.text = m_userId.ToString();
            trialIDField.text = (m_currentTrialIdx + 1).ToString();

        } else {
            Debug.Log("The last participant completed all answers. Starting with the next participant ID.");

            m_userId = lastSubjID + 1;
            m_currentTrialIdx = 0;

            participantIDField.text = m_userId.ToString();
            trialIDField.text = (m_currentTrialIdx + 1).ToString();

        }

        m_userdataPath = m_basePath + "/Subj_" + m_userId;

        // if (count > 2) {
        //     m_currentTrialIdx = (count - 1) / 2;
        //     // trialIDField.text = m_currentTrialIdx.ToString ();
        // } else {

        // }
    }

    void GetPreviousTrial() {

        int count = 0;

        if (!Directory.Exists(m_userdataPath)) {
            count = 0;
        } else {
            count = Directory.GetFiles(m_userdataPath, "*_Answers.csv", SearchOption.AllDirectories).Length;
        }

        int maxTrials = allPlaylists[m_userId - 1].Count;

        if (count == 0) {
            Debug.Log("This participant did not complete any answers. Starting with Trial 1.");

            m_currentTrialIdx = 0;
            trialIDField.text = (m_currentTrialIdx + 1).ToString();
            continueButton.interactable = true;

        } else if (count < maxTrials) {
            Debug.Log("This participant completed " + count + " answers. Resuming with trial " + (count + 1));

            m_currentTrialIdx = count;
            trialIDField.text = (m_currentTrialIdx + 1).ToString();
            continueButton.interactable = true;

        } else if (count >= maxTrials) {
            Debug.Log("This participant completed all answers. Choose a different participant or select any trial to re-start from there.");

            m_currentTrialIdx = 99;
            trialIDField.text = "";
            // continueButton.interactable = false;
        }
    }

    void GetLastUserNumber() {

        // Get last user number
        m_basePath = Directory.GetParent(Application.dataPath) + "/SubjectData/" + m_labID;
        if (preTesting)
            m_basePath += "/PreTests";
        if (!Directory.Exists(m_basePath))
            Directory.CreateDirectory(m_basePath);

        string[] directories = Directory.GetDirectories(m_basePath);

        int lastSubjID = 0;

        Debug.Log("Found " + directories.Length + " participant folders in the " + m_labID + " directory.");

        foreach (string directory in directories) {
            string folder = directory.Split('/').Last();

            int tmpSubjID;

            string[] splitResults = folder.Split('_');
            if (splitResults.Length > 1) {
                int.TryParse(splitResults[1], out tmpSubjID);

                if (tmpSubjID > lastSubjID)
                    lastSubjID = tmpSubjID;
            }
        }
        // m_userId = lastSubjID + 1;
        m_userId = lastSubjID;
        TestParticipantID();

        if (m_userId >= allPlaylists.Count) {
            Debug.Log("We are at the last user");
            m_userId = allPlaylists.Count;
        }

        participantIDField.text = m_userId.ToString();

        // Get last completed trial information

        // m_userdataPath = m_basePath + "/Subj_" + m_userId;
        // int count = Directory.GetFiles (m_userdataPath, "*.csv", SearchOption.AllDirectories).Length;
        // if (count > 2) {
        //     m_currentTrialIdx = (count - 1) / 2;
        //     trialIDField.text = m_currentTrialIdx.ToString ();
        // }

    }

    void GetLastTrialNumber() {

        // m_userdataPath = m_basePath + "/Subj_" + m_userId;
        // int count = Directory.GetFiles (m_userdataPath, "*.csv", SearchOption.AllDirectories).Length;
        // if (count > 2) {
        //     m_currentTrialIdx = (count - 1) / 2;
        //     trialIDField.text = m_currentTrialIdx.ToString ();
        // }
    }

    private void StartNew() {

        m_userdataPath = m_basePath + "/Subj_" + m_userId;

        if (Directory.Exists(m_userdataPath)) {
            Debug.Log("Data for participant " + m_userId + " already exists and will be deleted.");

            string[] oldFiles = Directory.GetFiles(m_userdataPath, "*.*", SearchOption.AllDirectories);
            foreach (string file in oldFiles) {
                // Debug.Log(file);
                File.Delete(file);
            }
            Debug.Log("Deleted " + oldFiles.Length + " old files from " + m_userdataPath);
        }
    }

    private void SetUp(Boolean rename = true) {

        // Turn off plux acquisition if it's already running
        if (pluxSampling)
            TogglePluxAcquisition();

        // deleteOldData = reset;
        m_userdataPath = m_basePath + "/Subj_" + m_userId;

        // Boolean renameFiles = !rename;

        // If this participant already exists: start after last trial
        if (Directory.Exists(m_userdataPath)) {
            Debug.Log("Data for participant " + m_userId + " already exists.");

            if (deleteOldData) {

                string[] oldFiles = Directory.GetFiles(m_userdataPath, "*.*", SearchOption.AllDirectories);

                foreach (string file in oldFiles) {
                    // Debug.Log(file);
                    File.Delete(file);
                }
                Debug.Log("Deleted " + oldFiles.Length + " old files from " + m_userdataPath);



            } else {

                if (rename) {

                    Debug.Log("Files from previous attempts to be renamed:");

                    int count = Directory.GetFiles(m_userdataPath, "*.csv", SearchOption.AllDirectories).Length;

                    string[] csvFiles = Directory.GetFiles(m_userdataPath, "*.csv", SearchOption.AllDirectories);
                    string[] txtFiles = Directory.GetFiles(m_userdataPath, "*.txt", SearchOption.AllDirectories);

                    string[] oldFiles = new string[csvFiles.Length + txtFiles.Length];
                    csvFiles.CopyTo(oldFiles, 0);
                    txtFiles.CopyTo(oldFiles, csvFiles.Length);

                    foreach (string file in oldFiles) {
                        Debug.Log(file);
                        File.Move(file, file + $"_{getTimeStamp()}.bak");
                    }

                    // if (count > 1) {
                    //     m_currentTrialIdx = count / 2;
                    // }
                } else {

                    // Rename userdata file before creating a new one
                    // if (File.Exists(m_userdataPath + "/UserData.txt")) {
                    // File.Move(m_userdataPath + "/UserData.txt", m_userdataPath + $"/UserData_{getTimeStamp()}.txt");
                    // }
                }
            }

        } else {


            // Create new folder with subject ID
            Directory.CreateDirectory(m_userdataPath);

        }

        Debug.Log("Writing experiment data on:");
        Debug.Log(m_userdataPath);

        // get playlist for user ID --- we begin with user ID 1, but start with element 0 of the list of playlists.
        // SetUserPlaylist (m_userId - 1);
        SetUserPlaylistFromCSVs(m_userId - 1);


        // User information: basic data + playlist
        m_recorder_info = new StreamWriter(m_userdataPath + "/UserData.txt", !rename);

        if (rename) {
            // Record some protocol information
            WriteInfo("User_ID: " + m_userId);
            WriteInfo("lab,participant,trial_idx,roomName,instruction,duration");
            // writeInfo("Stimuli order, room name, target idx, scotoma condition:");
            // foreach (playlistElement elp in playlist)
            //     writeInfo($"{elp.expName} - quest_{elp.task_idx}");
            foreach (EmotPlaylistElement ple in emotPlaylist)
                WriteInfo($"{ple.expNameCSV}");
            WriteInfo("Started experiment");
        } else {
            WriteInfo("Continued experiment");
        }
        FlushInfo();

        // Plux things

        pluxPanel.SetActive(false);

        // setTaskList ();
    }

    // private bool calibrating = false;
    private bool conCal = false;
    private bool trackCal = false;
    Coroutine runningRoutine;

    public void CalibrateByController() {
        if (!trackCal) {

            if (!conCal) {
                conCal = true;
                controllerCalButton.colors = SwapColors(controllerCalButton.colors);
                runningRoutine = StartCoroutine(ControllerPositioning());
            } else {
                StopCoroutine(runningRoutine);
                _instructBehaviour.ResetRadialProgresses();
                _instructBehaviour.toggleControllerInstruction(false);
                controllerCalButton.colors = SwapColors(controllerCalButton.colors);
                conCal = false;
                LoadCamRigCal();
                controllerBasePoint.SetActive(false);
                DisableCalPoints();
            }

            // conCal = !conCal;
            // controllerCalButton.colors = SwapColors(controllerCalButton.colors);
            // calibrating = conCal ? true : false;
            // if (conCal && calibrating) {
            //     StartCoroutine(ControllerPositioning());
            // }
            // if (conCal) calibrating = true; else calibrating = false;
        }
        // SaveCamRigCal();
        // Debug.Log("Calibrating: " + conCal);
    }
    public void CalibrateByTracker() {
        // if (!conCal) {
        //     trackCal = !trackCal;
        //     trackerCalButton.colors = SwapColors(trackerCalButton.colors);
        //     calibrating = trackCal ? true : false;
        // }
        // // LoadCamRigCal();
        // Debug.Log("Calibrating: " + calibrating);
    }

    private GameObject calPointA, calPointB, calPointF;
    private GameObject controllerBasePoint, controllerMainPoint;

    IEnumerator ControllerPositioning() {

        if (calPointF != null) calPointF.SetActive(true);

        controllerBasePoint.SetActive(true);

        Vector3 newPos = new Vector3();

        bool positioned = false;

        _instructBehaviour.toggleControllerInstruction(true);
        _instructBehaviour.setInstruction("Using the trigger, place the controller's base on the floor.\n\n" + "Confirm with the touchpad.");

        // Get first point: floor
        while (!positioned) {
            if (userClickedPad) {
                positioned = true;
                break;
            }
            while (!userClickedTrigger && userTouchedTrigger) {
                // if (userTouchedTrigger) {
                newPos = cameraRig.position;
                Debug.Log("ContBasePoint: " + controllerBasePoint.transform.position);
                Debug.Log("CalPointF: " + calPointF.gameObject.transform.position);
                newPos.y = newPos.y - (controllerBasePoint.transform.position.y - calPointF.transform.position.y);
                cameraRig.position = newPos;
                // }
                yield return null;
            }
            yield return new WaitUntil(() => !userTouchedTrigger || userClickedPad);
        }
        Debug.Log("Calibrated floor!");
        _instructBehaviour.setInstruction("The floor is set!");
        calPointF.SetActive(false);
        positioned = false;

        // Wait for trigger and pad release
        yield return new WaitUntil(() => !userTouchedTrigger && !userClickedPad);
        // _instructBehaviour.setInstruction("Position calibration.\n\n" + "Place the controller's base to the front right corner of the seat, then click the trigger.");
        _instructBehaviour.setInstruction("Using the trigger, place the controller's base on the seat's front right corner.\n\n" + "Confirm with the touchpad.");

        if (calPointA != null) calPointA.SetActive(true);

        // Get second point: corner A
        while (!positioned) {
            if (userClickedPad) {
                positioned = true;
                break;
            }
            while (!userClickedTrigger && userTouchedTrigger) {
                newPos = cameraRig.position;
                newPos = newPos - (controllerBasePoint.transform.position - calPointA.transform.position);
                newPos.y = cameraRig.position.y;
                cameraRig.position = newPos;
                yield return null;
            }
            yield return new WaitUntil(() => !userTouchedTrigger || userClickedPad);
        }
        Debug.Log("Calibrated first corner!");
        _instructBehaviour.setInstruction("The seat's position is set!");
        calPointA.SetActive(false);
        positioned = false;

        // Wait for trigger and pad release
        yield return new WaitUntil(() => !userTouchedTrigger && !userClickedPad);
        // _instructBehaviour.setInstruction("Rotation calibration.\n\n" + "Place the controller's base to the front left corner of the seat, then click the trigger.");
        _instructBehaviour.setInstruction("Using the trigger, place the controller's base on the seat's front left corner.\n\n" + "Confirm with the touchpad.");

        if (calPointB != null) calPointB.SetActive(true);

        float angleBetween;
        Vector3 firstCornerPos = calPointA.transform.position;
        Vector3 horizontalControllerPos = controllerBasePoint.transform.position;
        // Get final point: corner B
        while (!positioned) {
            if (userClickedPad) {
                positioned = true;
                break;
            }
            while (!userClickedTrigger && userTouchedTrigger) {
                newPos = cameraRig.position;

                horizontalControllerPos = controllerBasePoint.transform.position;
                horizontalControllerPos.y = 0;
                firstCornerPos.y = 0;

                // angleBetween = Vector3.SignedAngle(Vector3.right, horizontalControllerPos - firstCornerPos, Vector3.up);
                angleBetween = Vector3.SignedAngle(Vector3.left, horizontalControllerPos - firstCornerPos, Vector3.up);
                // Debug.Log("Signed angle: " + angleBetween);
                cameraRig.transform.RotateAround(firstCornerPos, Vector3.up, -angleBetween);
                yield return null;
            }
            yield return new WaitUntil(() => !userTouchedTrigger || userClickedPad);
        }
        Debug.Log("Calibrated second corner!");
        _instructBehaviour.setInstruction("The seat's rotation is set!");
        calPointB.SetActive(false);
        positioned = false;

        // Wait for trigger and pad release
        yield return new WaitUntil(() => !userTouchedTrigger && !userClickedPad);
        controllerBasePoint.SetActive(false);

        float triggerClickTime = 0.0f;

        _instructBehaviour.setInstruction("Click and hold the trigger to save the calibration, or click the touchpad to abort.");
        // _radialProgress.gameObject.SetActive(true);
        while (triggerClickTime < durationToContinue) {
            if (userClickedPad) {
                _instructBehaviour.setInstruction("Aborting!\n\n" + "Loading previous calibration.");
                yield return new WaitUntil(() => !userClickedPad);
                // LoadCamRigCal();
                // _instructBehaviour.ResetRadialProgresses();
                // _instructBehaviour.toggleControllerInstruction(false);
                CalibrateByController();

                controllerBasePoint.SetActive(false);
                DisableCalPoints();
                yield break;
            }
            if (userClickedTrigger) {
                triggerClickTime += Time.deltaTime;
                _instructBehaviour.SetRadialProgresses(triggerClickTime / durationToContinue);
            } else {
                _instructBehaviour.ResetRadialProgresses();
                triggerClickTime = 0;
            }
            yield return null;
        }

        SaveCamRigCal();

        _radialProgress.SetProgress(1);
        _instructBehaviour.setInstruction("Calibration saved!");

        yield return new WaitUntil(() => !userTouchedTrigger);
        _instructBehaviour.ResetRadialProgresses();
        // _radialProgress.gameObject.SetActive(false);
        _instructBehaviour.toggleControllerInstruction(false);
        toggleMessage(false);

        CalibrateByController();

        // yield return new WaitUntil(() => !calibrating);
        Debug.Log("Done calibrating!");
    }

    private void SaveCamRigCal() {
        PlayerPrefs.SetFloat("pX", cameraRig.position.x);
        PlayerPrefs.SetFloat("pY", cameraRig.position.y);
        PlayerPrefs.SetFloat("pZ", cameraRig.position.z);
        // PlayerPrefs.SetFloat("rW", cameraRig.rotation.w);
        PlayerPrefs.SetFloat("rX", cameraRig.eulerAngles.x);
        PlayerPrefs.SetFloat("rY", cameraRig.eulerAngles.y);
        PlayerPrefs.SetFloat("rZ", cameraRig.eulerAngles.z);
        Debug.Log("Saved Camera Rig calibration.");
    }

    private void LoadCamRigCal() {
        float pX = PlayerPrefs.GetFloat("pX");
        float pY = PlayerPrefs.GetFloat("pY");
        float pZ = PlayerPrefs.GetFloat("pZ");
        // float rW = PlayerPrefs.GetFloat("rW");
        float rX = PlayerPrefs.GetFloat("rX");
        float rY = PlayerPrefs.GetFloat("rY");
        float rZ = PlayerPrefs.GetFloat("rZ");
        cameraRig.position = new Vector3(pX, pY, pZ);
        cameraRig.eulerAngles = new Vector3(rX, rY, rZ);
    }

    private void DisableCalPoints() {

        calPointA = GameObject.Find("CalPointA");
        calPointB = GameObject.Find("CalPointB");
        calPointF = GameObject.Find("CalPointF");

        if (calPointF != null) {
            calPointF.SetActive(false);
        }
        if (calPointA != null) {
            calPointA.SetActive(false);
        }
        if (calPointB != null) {
            calPointB.SetActive(false);
        }
    }

    private ColorBlock SwapColors(ColorBlock colors) {
        Color normal = colors.normalColor;
        Color pressed = colors.pressedColor;
        colors.normalColor = pressed;
        colors.selectedColor = pressed;
        colors.highlightedColor = pressed;
        colors.pressedColor = normal;
        return colors;
    }

    public void WriteInfo(string txt) {
        // print (txt);

        if (m_recorder_info.BaseStream.CanWrite)
            m_recorder_info.WriteLine("{0}:{1}", getTimeStamp(), txt);
        Debug.Log(txt);
    }

    public string CondenseString(string input) {
        string condensedString = "";
        string[] stringParts = input.Split(' ');
        if (stringParts.Length > 1) {
            foreach (string part in stringParts) {
                if (part == " " || part == "_" || part == "-" || part == "?" || part == "," || part == "." || part == "\n")
                    continue;
                else
                    condensedString += part;
            }
        } else {
            condensedString = input;
        }
        return condensedString;
    }

    public void WriteAnswer(string question, string answer) {
        if (m_recorder_question.BaseStream.CanWrite) {
            m_recorder_question.WriteLine("{0},{1},{2},{3}", getTimeStamp(), currentEmotTrial.expNameCSV, CondenseString(csvQuestionText), answer);
            // RoomManager.instance.currentRoomName, currentEmotTrial.duration, m_currentTrialIdx, txt);
            m_recorder_question.Flush();
            Debug.Log("Wrote answer: " + answer);
        }
    }
    public void FlushInfo() {
        if (m_recorder_info.BaseStream.CanWrite)
            m_recorder_info.Flush();
    }

    private IDictionary<string, string> tasks;

    // public string currentTaskString => tasks[$"{currentTrial.room_idx}.{currentTrial.task_idx}"];
    // public string currentTaskString => tasks[$"{currentEmotTrial.roomName}.{currentEmotTrial.task_idx}"];

    public string currentTaskString = "Example task string.";

    private void setTaskList() {
        tasks = new Dictionary<string, string>();
        var lines = File.ReadLines(
            Directory.GetParent(Application.dataPath) + "/SubjectData/questions.csv");
        foreach (var line in lines) {
            string[] linesplit = line.Split(',');
            tasks.Add(linesplit[0], linesplit[1]);
            print(line);
        }
    }
    private void setTaskListErwan() {
        tasks = new Dictionary<string, string>();
        var lines = File.ReadLines(
            Directory.GetParent(Application.dataPath) + "/SubjectData/questions.csv");
        foreach (var line in lines) {
            string[] linesplit = line.Split(',');
            tasks.Add(linesplit[0], linesplit[1]);
            print(line);
        }
    }

    private void SetUserPlaylistFromCSVs(int idx) {

        if (idx > allPlaylists.Count) {
            Debug.LogError($"No playlist exists for user {idx}. The currently loaded file only holds {allPlaylists.Count} playlists.", this);
            Quit();
        }

        foreach (EmotPlaylistElement eElement in allPlaylists[idx]) {
            emotPlaylist.Add(eElement);
        }
    }

    private void LoadPlaylistsFromCSVs() {

        allPlaylists = new List<List<EmotPlaylistElement>>(90);

        string playlistName;
        string testKind;

        if (preTesting)
            testKind = "Pre";
        else
            testKind = "Full";

        if (tobiiTracking) {
            Debug.Log("We are at SGL.");
            playlistName = $"/SubjectData/Playlists/{testKind}-1-Training.csv";
        } else {
            Debug.Log("We are at HfG.");
            playlistName = $"/SubjectData/Playlists/{testKind}-2-Training.csv";
        }

        // StreamReader file = new StreamReader (Directory.GetParent (Application.dataPath) + playlistName, Encoding.UTF8);

        List<List<string[]>> participants = new List<List<string[]>>(90);

        var lines = File.ReadLines(Directory.GetParent(Application.dataPath) + playlistName);
        // Debug.Log ("Number of lines: " + lines.Count ());
        int lineCounter = 0;
        List<string[]> participant = new List<string[]>(90);
        string toPrint = "";
        foreach (var line in lines) {
            switch (lineCounter % 3) {
                case 0:
                    // Debug.Log ("Creating a new playlist for participant " + ((lineCounter / 3) + 1));
                    participant = new List<string[]>(90);
                    // string[] linesplit = line.Split (',');
                    participant.Add(line.Split(','));
                    toPrint = "Printing var1 line ";
                    break;
                case 1:
                    toPrint = "Printing var2 line ";
                    // linesplit = line.Split (',');
                    participant.Add(line.Split(','));
                    break;
                case 2:
                    toPrint = "Printing time line ";
                    // string[] linesplit = line.Split (',');
                    participant.Add(line.Split(','));
                    participants.Add(participant);
                    // Debug.Log ("List of participants is now " + participants.Count + " long");
                    break;
            }
            // toPrint += line;

            toPrint += $"of participant {(lineCounter / 3) + 1}: {line}";
            lineCounter++;
            // Debug.Log (toPrint);

            // string[] linesplit = line.Split (',');
            // tasks.Add (linesplit[0], linesplit[1]);
            // print (line);
        }

        for (int j = 0; j < participants.Count; j++) {
            List<string[]> person = participants[j];

            // }
            // foreach (List<string[]> person in participants) {

            List<EmotPlaylistElement> ePlaylist = new List<EmotPlaylistElement>(90);
            for (int i = 0; i < person[0].Length; i++) {

                // Durations from array defined in Editor

                int.TryParse(person[2][i], out int durationIdx);
                int duration = durations[durationIdx - 1];

                string room = "BreakRoom";
                // string inst = "Chill";
                instruction inst = instruction.chill;
                // inst = instruction.hfgLean.ToString ();
                // Debug.Log ("\n\nTESTING: toString() of enums: " + inst + "\n\n");
                string labo = "Lab";

                // SGL-specific playlist generation
                if (tobiiTracking) {
                    labo = "SGL";
                    // TO DO: replace hard-coded room names with dynamically assigned ones from the Editor
                    // Variable one, first level: warmly lit tunnel
                    if (person[0][i] == "a") {
                        room = "Version 8 - warmes Licht";
                        // Variable one, second level: coldly lit tunnel
                    } else {
                        room = "Version 9 - kaltes Licht";
                    }

                    // Variable two, first level: sit down (specifically for SGL)
                    if (person[1][i] == "x") {
                        inst = instruction.sglSit;
                        // Variable two, second level: stand up (specifically for SGL)
                    } else {
                        inst = instruction.sglStand;
                    }

                    // HfG-specific playlist generation
                } else {
                    labo = "HfG";
                    // TO DO: replace hard-coded room names with dynamically assigned ones from the Editor
                    // Variable one, first level: mesh benches
                    if (person[0][i] == "a") {
                        room = "Version 1 - Bank Testbed Mesh";
                        // Variable one, second level: wooden benches
                    } else {
                        room = "Version 2 - Bank Testbed Wood";
                    }

                    // Variable two, first level: sit on the low bench (specifically for HfG)
                    if (person[1][i] == "x") {
                        // inst = "sit-HfG";
                        inst = instruction.hfgSit;
                        // Variable two, second level: lean on the high bench (specifically for HfG)
                    } else {
                        inst = instruction.hfgLean;
                        // inst = "lean-HfG";
                    }
                }
                ePlaylist.Add(new EmotPlaylistElement(room, duration, i, inst, labo, j));
                // Debug.Log("Created playlist element with instruction: " + inst);
                // EmotPlaylistElement pElement = new EmotPlaylistElement (room, duration, i, inst, labo);
            }
            allPlaylists.Add(ePlaylist);
        }
        Debug.Log("Length of allPlaylist: " + allPlaylists.Count());

        // foreach (var pEl in allPlaylists[0]) {
        //     Debug.Log (pEl.expName);
        // }
    }

    private void setUserPlaylistErwan(int idx) {
        // int max_idx = 100;
        if (idx > max_idx) {
            Debug.LogError($"User index cannot be over {max_idx}.", this);
            Quit();
        }

        StreamReader file = new StreamReader(Directory.GetParent(Application.dataPath) +
            "/SubjectData/playlist.csv", Encoding.UTF8);

        int nrep = 14;
        int linesize = 104 + 1;
        // Number of characters per line plus line return

        // Read line according to the user ID number
        char[] lineChar = new char[linesize - 1];
        file.BaseStream.Position = idx * linesize;
        file.Read(lineChar, 0, lineChar.Length);
        file.Close();

        // Convert line from char[] to string
        string line = new string(lineChar);
        // Split line by commas
        string[] ell = line.Split(',');
        // For all element in list
        for (int i = 0; i < ell.Length; i++) {
            // Split by '-' 
            string[] els = ell[i].Split('-');

            // Debug.Log (ell[i]);

            // 0: Scene, 1: light cond, 2: Task
            int.TryParse(els[0], out var room_idx);
            int.TryParse(els[1], out var light_cond);
            int.TryParse(els[2], out var quest_idx);

            // new playlistElement to insert in playlist
            playlist.Add(new playlistElement(room_idx, light_cond, quest_idx, i));
        }

        print($"playlist.Count: {playlist.Count}");
    }

    private bool m_isPresenting;

    public bool userGrippedControl => TrackPadInput.instance.SideGripped();
    public bool userTouchedPad => TrackPadInput.instance.TrackpadTouched();
    public bool userClickedPad => TrackPadInput.instance.TrackpadClicked();
    public bool userTouchedTrigger => TrackPadInput.instance.TriggerTouched();
    public bool userClickedTrigger => TrackPadInput.instance.TriggerClicked();

    float taskTime = 0;
    float padPressedTime = 0;

    public void SetTobiiTracking(bool tobTrack) {

        // Debug.Log ("Requesting Lab change. SGL? " + tobTrack);

        if (tobiiTracking == tobTrack) {
            Debug.Log("Pressed already active Lab button");
            GetPreviousParticipant();
            // GetLastUserNumber ();
            return;
        }

        tobiiTracking = tobTrack;
        sglButton.colors = SwapColors(sglButton.colors);
        hfgButton.colors = SwapColors(hfgButton.colors);
        PlayerPrefs.SetInt("tobii", (tobiiTracking ? 1 : 0));

        m_labID = tobiiTracking ? "SGL" : "HfG";
        LoadPlaylistsFromCSVs();
        GetPreviousParticipant();
        // GetLastUserNumber ();
    }

    public void SetEyeTracking() {
        eyeTracking = eTToggle.isOn ? true : false;
        PlayerPrefs.SetInt("eyetracking", (eTToggle.isOn ? 1 : 0));
    }

    public void SaveChannelToggle(int chan) {

        switch (chan) {
            case 1:
                PlayerPrefs.SetInt("chan1", (chan1Toggle.isOn ? 1 : 0));
                Debug.Log("Saved channel " + chan + " as " + chan1Toggle.isOn);
                break;
            case 2:
                PlayerPrefs.SetInt("chan2", (chan2Toggle.isOn ? 1 : 0));
                Debug.Log("Saved channel " + chan + " as " + chan2Toggle.isOn);
                break;
            case 3:
                PlayerPrefs.SetInt("chan3", (chan3Toggle.isOn ? 1 : 0));
                Debug.Log("Saved channel " + chan + " as " + chan3Toggle.isOn);
                break;
            case 4:
                PlayerPrefs.SetInt("chan4", (chan4Toggle.isOn ? 1 : 0));
                Debug.Log("Saved channel " + chan + " as " + chan4Toggle.isOn);
                break;
            default:
                PlayerPrefs.SetInt("chan1", (chan1Toggle.isOn ? 1 : 0));
                PlayerPrefs.SetInt("chan2", (chan2Toggle.isOn ? 1 : 0));
                PlayerPrefs.SetInt("chan3", (chan3Toggle.isOn ? 1 : 0));
                PlayerPrefs.SetInt("chan4", (chan4Toggle.isOn ? 1 : 0));
                Debug.Log("Saved all channel toggles");
                break;
        }
        // string channelName = "chan" + chan.ToString();
        // PlayerPrefs.SetInt(channelName, (state ? 1 : 0));
        // PlayerPrefs.SetInt("chan1", (chan1Toggle.isOn ? 1 : 0));
        // PlayerPrefs.SetInt("chan2", (chan2Toggle.isOn ? 1 : 0));
        // PlayerPrefs.SetInt("chan3", (chan3Toggle.isOn ? 1 : 0));
        // PlayerPrefs.SetInt("chan4", (chan4Toggle.isOn ? 1 : 0));

        // if (PlayerPrefs.GetInt("chan1") == 1) Debug.Log("chan 1 on"); else Debug.Log("chan 1 off");
        // if (PlayerPrefs.GetInt("chan2") == 1) Debug.Log("chan 2 on"); else Debug.Log("chan 2 off");
        // if (PlayerPrefs.GetInt("chan3") == 1) Debug.Log("chan 3 on"); else Debug.Log("chan 3 off");
        // if (PlayerPrefs.GetInt("chan4") == 1) Debug.Log("chan 4 on"); else Debug.Log("chan 4 off");
    }
    public void SaveChannelNames(int chan) {

        switch (chan) {
            case 1:
                PlayerPrefs.SetString("chan1name", chan1Field.text);
                // Debug.Log("Saved name of channel " + chan + " as " + chan1Field.text);
                break;
            case 2:
                PlayerPrefs.SetString("chan2name", chan2Field.text);
                // Debug.Log("Saved name of channel " + chan + " as " + chan2Field.text);
                break;
            case 3:
                PlayerPrefs.SetString("chan3name", chan3Field.text);
                // Debug.Log("Saved name of channel " + chan + " as " + chan3Field.text);
                break;
            case 4:
                PlayerPrefs.SetString("chan4name", chan4Field.text);
                // Debug.Log("Saved name of channel " + chan + " as " + chan4Field.text);
                break;
            default:
                PlayerPrefs.SetString("chan1name", chan1Field.text);
                PlayerPrefs.SetString("chan2name", chan2Field.text);
                PlayerPrefs.SetString("chan3name", chan3Field.text);
                PlayerPrefs.SetString("chan4name", chan4Field.text);
                // Debug.Log("Saved all channel names");
                break;
        }

        signal1Indicator.GetComponentInChildren<Text>().text = chan1Field.text;
        signal2Indicator.GetComponentInChildren<Text>().text = chan2Field.text;
        signal3Indicator.GetComponentInChildren<Text>().text = chan3Field.text;
        signal4Indicator.GetComponentInChildren<Text>().text = chan4Field.text;
        // string channelName = "chan" + chan.ToString();
        // PlayerPrefs.SetInt(channelName, (state ? 1 : 0));
        // PlayerPrefs.SetInt("chan1", (chan1Toggle.isOn ? 1 : 0));
        // PlayerPrefs.SetInt("chan2", (chan2Toggle.isOn ? 1 : 0));
        // PlayerPrefs.SetInt("chan3", (chan3Toggle.isOn ? 1 : 0));
        // PlayerPrefs.SetInt("chan4", (chan4Toggle.isOn ? 1 : 0));

        // if (PlayerPrefs.GetInt("chan1") == 1) Debug.Log("chan 1 on"); else Debug.Log("chan 1 off");
        // if (PlayerPrefs.GetInt("chan2") == 1) Debug.Log("chan 2 on"); else Debug.Log("chan 2 off");
        // if (PlayerPrefs.GetInt("chan3") == 1) Debug.Log("chan 3 on"); else Debug.Log("chan 3 off");
        // if (PlayerPrefs.GetInt("chan4") == 1) Debug.Log("chan 4 on"); else Debug.Log("chan 4 off");
    }

    // public void SetChannel1Toggle() {
    //     PlayerPrefs.SetInt("chan1", (chan1Toggle.isOn ? 1 : 0));
    // }
    // public void SetChannel2Toggle(bool state) {
    //     PlayerPrefs.SetInt("chan2", (state ? 1 : 0));
    // }
    // public void SetChannel3Toggle(bool state) {
    //     PlayerPrefs.SetInt("chan3", (state ? 1 : 0));
    // }
    // public void SetChannel4Toggle(bool state) {
    //     PlayerPrefs.SetInt("chan4", (state ? 1 : 0));
    // }
    public void SetPreTest(bool pre) {

        // Debug.Log ("Requesting Lab change. SGL? " + tobTrack);

        if (pre) {
            if (preTesting) {
                Debug.Log("It's already Pre-Testing...");
                return;
            } else {
                preTesting = true;
                preButton.colors = SwapColors(preButton.colors);
                studyButton.colors = SwapColors(studyButton.colors);
                PlayerPrefs.SetInt("pretesting", 1);
                GetPreviousParticipant();
            }
        } else {
            if (!preTesting) {
                Debug.Log("It's already the full study...");
                return;
            } else {
                preTesting = false;
                preButton.colors = SwapColors(preButton.colors);
                studyButton.colors = SwapColors(studyButton.colors);
                PlayerPrefs.SetInt("pretesting", 0);
                GetPreviousParticipant();
            }
        }
    }

    public void SetGerman(bool german) {

        // Debug.Log ("Requesting Lab change. SGL? " + tobTrack);

        if (german) {
            if (language == lang.german) {
                Debug.Log("It's already German...");
                return;
            } else {
                language = lang.german;
                deButton.colors = SwapColors(deButton.colors);
                enButton.colors = SwapColors(enButton.colors);
                PlayerPrefs.SetInt("german", 1);
            }
        } else {
            if (language == lang.english) {
                Debug.Log("It's already English...");
                return;
            } else {
                language = lang.english;
                deButton.colors = SwapColors(deButton.colors);
                enButton.colors = SwapColors(enButton.colors);
                PlayerPrefs.SetInt("german", 0);
            }
        }
    }

    public string SecondsToTime(float totalSeconds) {
        int minutes = Mathf.FloorToInt(totalSeconds / 60F);
        int seconds = Mathf.FloorToInt(totalSeconds - minutes * 60);
        string output = string.Format("{0:0}:{1:00}", minutes, seconds);
        // Debug.Log(output);
        return output;
    }

    // THE ACTUAL GAME LOOP!

    bool startButtonReady = false;

    IEnumerator Start() {

        taskTime = 0;
        padPressedTime = 0;

        shaderBehavior.phase = ShaderBehaviour.shaderPhase.none;

        tobiiTracking = PlayerPrefs.GetInt("tobii", 0) != 0;

        Debug.Log("Read from prefs: Tobii Tracking = " + tobiiTracking);

        if (tobiiTracking) {
            sglButton.colors = SwapColors(sglButton.colors);
            Debug.Log("Swapped SGL Button");
        } else {
            hfgButton.colors = SwapColors(hfgButton.colors);
            Debug.Log("Swapped HFG Button");
        }

        if (language == lang.german) {
            deButton.colors = SwapColors(deButton.colors);
        } else {
            enButton.colors = SwapColors(enButton.colors);
        }

        if (preTesting) {
            preButton.colors = SwapColors(preButton.colors);
        } else {
            studyButton.colors = SwapColors(studyButton.colors);
        }

        if (eyeTracking) {
            eTToggle.isOn = true;
        } else {
            eTToggle.isOn = false;
        }

        currentRoomInfo.text = "";
        currentInstructionInfo.text = "";
        nextRoomInfo.text = "";
        nextInstructionInfo.text = "";
        durationInfo.text = "";
        elapsedInfo.text = "";

        // Debug.Log("Passed _eyeTrack.ready check");

        // Show SubjInfo panel
        setupPanel.SetActive(true);
        // infoPanel.SetActive(false);
        pausePanel.SetActive(false);
        // questionPanel.SetActive(false);
        _progressBar.gameObject.SetActive(false);
        // _radialProgress.gameObject.SetActive(false);
        _questionSlider.gameObject.SetActive(false);


        startButton.interactable = false;
        startButtonReady = false;
        continueButton.interactable = false;
        controllerCalButton.interactable = false;



        if (PlayerPrefs.GetInt("chan1") == 1) chan1Toggle.isOn = true; else chan1Toggle.isOn = false;
        if (PlayerPrefs.GetInt("chan2") == 1) chan2Toggle.isOn = true; else chan2Toggle.isOn = false;
        if (PlayerPrefs.GetInt("chan3") == 1) chan3Toggle.isOn = true; else chan3Toggle.isOn = false;
        if (PlayerPrefs.GetInt("chan4") == 1) chan4Toggle.isOn = true; else chan4Toggle.isOn = false;

        if (PlayerPrefs.GetInt("chan1") == 1) Debug.Log("chan 1 on"); else Debug.Log("chan 1 off");
        if (PlayerPrefs.GetInt("chan2") == 1) Debug.Log("chan 2 on"); else Debug.Log("chan 2 off");
        if (PlayerPrefs.GetInt("chan3") == 1) Debug.Log("chan 3 on"); else Debug.Log("chan 3 off");
        if (PlayerPrefs.GetInt("chan4") == 1) Debug.Log("chan 4 on"); else Debug.Log("chan 4 off");

        chan1Field.text = PlayerPrefs.GetString("chan1name");
        chan2Field.text = PlayerPrefs.GetString("chan2name");
        chan3Field.text = PlayerPrefs.GetString("chan3name");
        chan4Field.text = PlayerPrefs.GetString("chan4name");

        signal1Indicator.GetComponentInChildren<Text>().text = chan1Field.text;
        signal2Indicator.GetComponentInChildren<Text>().text = chan2Field.text;
        signal3Indicator.GetComponentInChildren<Text>().text = chan3Field.text;
        signal4Indicator.GetComponentInChildren<Text>().text = chan4Field.text;

        sampleRateField.text = PlayerPrefs.GetInt("sampleRate").ToString();




        trainSpawner = GameObject.FindObjectOfType<TrainSpawner>();
        if (trainSpawner != null) {
            // trainSpawner.SpawnTrain();
        }

        LoadCamRigCal();
        Debug.Log("Loaded Camera Rig Position");


        if (eyeTracking)
            yield return new WaitUntil(() => _eyeTrack.ready);
        else
            yield return new WaitForSeconds(1);


        _instructBehaviour.toggleControllerInstruction(true);

        _instructBehaviour.setInstruction("Press any button on this controller (trigger, side button, or trackpad).");


        // PLUX connections
        PluxDevManager = new PluxDeviceManager(ScanResults, ConnectionDone);

        // ScanFunction();

        // Initialization of Variables.      
        MultiThreadList = new List<List<int>>();
        ActiveChannels = new List<int>();



        yield return new WaitUntil(() => _instructBehaviour.deactivatedOtherController || Input.GetKeyDown("space"));
        _instructBehaviour.setInstruction("The other controller has been disabled!");

        if (_instructBehaviour.leftControllerActive) {
            controllerBasePoint = GameObject.Find("BasePointL");
            _questionSlider.controllerMainPoint = GameObject.Find("ControlPointL");
        } else {
            controllerBasePoint = GameObject.Find("BasePointR");
            _questionSlider.controllerMainPoint = GameObject.Find("ControlPointR");
        }

        controllerBasePoint.SetActive(false);
        _questionSlider.controllerMainPoint.SetActive(false);


        promptPanel.SetActive(false);
        startButton.interactable = true;
        startButtonReady = true;
        continueButton.interactable = true;

        // _questionSlider.gameObject.SetActive(true);
        RoomManager.instance.SaveManagerSceneNum();
        RoomManager.instance.LoadBreakRoom();
        yield return new WaitUntil(() => !(RoomManager.instance.actionInProgress));

        // if (!debugging) {
        //     if (userClickedPad)
        //         yield return new WaitUntil(() => !userClickedPad);
        //     if (userClickedTrigger)
        //         yield return new WaitUntil(() => !userTouchedTrigger);
        //     if (userGrippedControl)
        //         yield return new WaitUntil(() => !userGrippedControl);
        // }
        _instructBehaviour.toggleControllerInstruction(false);

        calPointA = GameObject.Find("CalPointA");
        calPointB = GameObject.Find("CalPointB");
        calPointF = GameObject.Find("CalPointF");

        if (calPointA != null) {
            calPointA.SetActive(false);
            calPointB.SetActive(false);
            calPointF.SetActive(false);

            controllerCalButton.interactable = true;
        }

        //
        //
        // Wait for user ID --- Setup() happens here!
        //
        //
        yield return new WaitUntil(() => !setupPanel.activeSelf);
        _instructBehaviour.toggleControllerInstruction(false);

        DisableCalPoints();

        infoPanel.SetActive(true);
        UpdateBatteryLevel();
        participantIDInfo.text = m_userId.ToString();

        trialIDInfo.text = (m_currentTrialIdx + 1).ToString();
        currentRoomInfo.text = currentEmotTrial.roomName;


        // Light sync with the sensor pack
        if (lightSync) {
            syncPanel.gameObject.SetActive(true);
            syncButton.gameObject.SetActive(true);
            syncConfirmButton.gameObject.SetActive(true);
            syncConfirmButton.interactable = false;
            syncButtonPressed = false;
            lightSynced = false;
            while (lightSynced != true) {
                yield return new WaitUntil(() => syncButtonPressed || lightSynced);
                if (lightSynced)
                    break;
                syncButtonPressed = false;
                WriteInfo("startedLightSync");
                syncButton.gameObject.SetActive(false);
                syncConfirmButton.gameObject.SetActive(false);
                for (int i = 0; i < 5; i++) {
                    syncPanel.color = Color.black;
                    yield return new WaitForSecondsRealtime(syncFlashDuration);
                    syncPanel.color = Color.white;
                    yield return new WaitForSecondsRealtime(syncFlashDuration);
                }
                WriteInfo("finishedLightSync");
                syncButton.gameObject.SetActive(true);
                syncConfirmButton.gameObject.SetActive(true);
                syncConfirmButton.interactable = true;
            }
            WriteInfo("confirmedLightSync");
        }



        // Settings from the setup panel have been submitted: start of the trial

        // TO DO: Integrate this with generated / read conditions of the playlist
        // HfG: Request to stand or lean depending on user ID
        // SGL: Request to stand or sit depending on user ID (tobiiTracking means SGL)
        // if (m_userId % 2 != 0) {
        //     Debug.Log("Odd user ID (" + m_userId + ").");
        //     if (tobiiTracking)
        //         toggleMessage(true, "pleaseStand");
        //     else
        //         toggleMessage(true, "pleaseLean");
        //     // _instructBehaviour.toggleWorldInstruction(true, "Please lean on the bench.");
        // } else {
        //     Debug.Log("Even user ID (" + m_userId + ").");
        //     toggleMessage(true, "pleaseSit");
        // }

        if (m_currentTrialIdx == 0) {

            switch (currentEmotTrial.instruction) {

                case instruction.hfgLean:
                    nextInstructionInfo.text = "Leaning HFG";
                    break;

                case instruction.hfgSit:
                    nextInstructionInfo.text = "Sitting HFG";
                    break;

                case instruction.sglSit:
                    nextInstructionInfo.text = "Sitting SGL";
                    break;

                case instruction.sglStand:
                    nextInstructionInfo.text = "Standing SGL";
                    break;
            }

            currentInstructionInfo.text = "Intro";

            toggleMessage(true, "pleaseSit");
            // _instructBehaviour.toggleWorldInstruction(true, "This is a test instruction.\n\nPress trigger.");
            _instructBehaviour.RequestConfirmation(durationToContinue);
            yield return new WaitUntil(() => !_instructBehaviour.requested);
            yield return new WaitForSecondsRealtime(messageWaitDuration);
            _instructBehaviour.toggleWorldInstruction(false);
            yield return new WaitForSecondsRealtime(messageWaitDuration);

            // First questions: demographics

            startNewAnswerRecord("_IntroQuestions");

            ToggleQuestion(true, "age");
            _questionSlider.UpdateSliderRange(18, 76, false, false, "18", "", "75");
            yield return new WaitUntil(() => _questionSlider.confirmed);
            yield return new WaitForSecondsRealtime(messageWaitDuration);
            ToggleQuestion(false);
            yield return new WaitForSecondsRealtime(messageWaitDuration);

            ToggleQuestion(true, "height");
            _questionSlider.UpdateSliderRange(120, 220, false, false, "120 cm", "", "220 cm");
            yield return new WaitUntil(() => _questionSlider.confirmed);
            yield return new WaitForSecondsRealtime(messageWaitDuration);
            ToggleQuestion(false);
            yield return new WaitForSecondsRealtime(messageWaitDuration);

            ToggleQuestion(true, "sex");
            if (language == lang.german)
                _questionSlider.UpdateSliderRange(0, 2, true, false, "Männlich", "Divers", "Weiblich");
            else
                _questionSlider.UpdateSliderRange(0, 2, true, false, "male", "diverse", "female");
            yield return new WaitUntil(() => _questionSlider.confirmed);
            yield return new WaitForSecondsRealtime(messageWaitDuration);
            ToggleQuestion(false);
            yield return new WaitForSecondsRealtime(messageWaitDuration);

            ToggleQuestion(true, "occupation");
            if (language == lang.german)
                _questionSlider.UpdateSliderRange(0, 4, true, false, "arbeitslos", "angestellt", "selbstständig", "",
                    "studierend", "andere");
            else
                _questionSlider.UpdateSliderRange(0, 4, true, false, "unemployed", "employee", "self-employed", "",
                    "student", "other");
            yield return new WaitUntil(() => _questionSlider.confirmed);
            yield return new WaitForSecondsRealtime(messageWaitDuration);
            ToggleQuestion(false);
            yield return new WaitForSecondsRealtime(messageWaitDuration);

            ToggleQuestion(true, "vrLife");
            _questionSlider.UpdateSliderRange(0, 4, true, false, "0", "1-5h", ">20h", "",
                "<1h", "5-20h");
            yield return new WaitUntil(() => _questionSlider.confirmed);
            yield return new WaitForSecondsRealtime(messageWaitDuration);
            ToggleQuestion(false);
            yield return new WaitForSecondsRealtime(messageWaitDuration);

            ToggleQuestion(true, "transportFreq");
            if (language == lang.german)
                _questionSlider.UpdateSliderRange(0, 4, true, false, "nie", "monatlich", "täglich", "",
                    "jährlich", "wöchentlich");
            else
                _questionSlider.UpdateSliderRange(0, 4, true, false, "never", "monthly", "daily", "",
                    "yearly", "weekly");
            yield return new WaitUntil(() => _questionSlider.confirmed);
            yield return new WaitForSecondsRealtime(messageWaitDuration);
            ToggleQuestion(false);
            yield return new WaitForSecondsRealtime(messageWaitDuration);

            ToggleQuestion(true, "patience");
            if (language == lang.german)
                _questionSlider.UpdateSliderRange(0, 99, true, false, "gar nicht geduldig", " ", "sehr geduldig");
            else
                _questionSlider.UpdateSliderRange(0, 99, true, false, "not at all", " ", "very patient");
            yield return new WaitUntil(() => _questionSlider.confirmed);
            yield return new WaitForSecondsRealtime(messageWaitDuration);
            ToggleQuestion(false);
            yield return new WaitForSecondsRealtime(messageWaitDuration);

            stopAnswerRecord();

            toggleMessage(true, "pleaseStand");
            _instructBehaviour.RequestConfirmation(durationToContinue);
            yield return new WaitUntil(() => !_instructBehaviour.requested);
            yield return new WaitForSecondsRealtime(messageWaitDuration);
            _instructBehaviour.toggleWorldInstruction(false);
            yield return new WaitForSecondsRealtime(messageWaitDuration);

            // if (!eyeTracking) {
            //     shaderBehavior.gameObject.SetActive(false);

            //     // Eye Tracking Setup
            //     //
            //     // TO DO: separate calibration from validation; add validation to VivePro Eye routine.
            //     //
            // } else {
            //     if (!tobiiTracking) {
            //         toggleMessage(true, "pleaseCalibrateVive");
            //         yield return new WaitForSeconds(5);
            //         _instructBehaviour.RequestConfirmation(durationToContinue);
            //         yield return new WaitUntil(() => !_instructBehaviour.requested);
            //         yield return new WaitForSecondsRealtime(messageWaitDuration);
            //         _instructBehaviour.toggleWorldInstruction(false);
            //         yield return new WaitForSecondsRealtime(messageWaitDuration);

            //         // Tobii eye tracker
            //     } else {
            //         eyeValidated = false;
            //         eyeCalibrated = false;
            //         StartCoroutine(TobiiCalibration());
            //         yield return new WaitUntil(() => eyeValidated);
            //     }
            // }

            // Introduce Interaction
            if (controllerTutorial) {

                // toggleMessage(false);
                yield return new WaitForSecondsRealtime(0.5f);
                toggleMessage(true, "Let's learn the VR controls.\n\nTake a look at your controller, and pull its trigger.");
                yield return new WaitForSecondsRealtime(messageWaitDuration);

                // _instructBehaviour.setInstruction("Click the controller's trigger, then release it.");
                yield return new WaitUntil(() => userClickedTrigger);
                Debug.Log("Successfully triggered.");
                _instructBehaviour.setInstruction("Good!\n\nNow fully release the trigger.");
                yield return new WaitUntil(() => !userTouchedTrigger);
                _instructBehaviour.setInstruction("You did it!");
                yield return new WaitForSecondsRealtime(1f);
                // _instructBehaviour.toggleControllerInstruction(false);
                _instructBehaviour.toggleWorldInstruction(false);
                yield return new WaitForSecondsRealtime(0.25f);

                // _instructBehaviour.setInstruction("Press the controller's side buttons.");
                // yield return new WaitUntil(() => userGrippedControl);
                // Debug.Log("Successfully gripped.");
                // _instructBehaviour.setInstruction("You did it!");
                // yield return new WaitForSecondsRealtime(1f);
                // _instructBehaviour.toggleControllerInstruction(false);
                // yield return new WaitForSecondsRealtime(0.25f);

                // _instructBehaviour.toggleControllerInstruction(true);
                // _instructBehaviour.setInstruction("Touch the controller's touch pad.");
                // yield return new WaitUntil(() => userTouchedPad);
                // Debug.Log("Successfully touched touchpad.");
                // _instructBehaviour.setInstruction("Well done!");
                // yield return new WaitForSecondsRealtime(1f);
                // _instructBehaviour.toggleControllerInstruction(false);
                // yield return new WaitForSecondsRealtime(0.25f);

                // _instructBehaviour.toggleControllerInstruction(true);
                toggleMessage(true, "Click the controller's touch pad, then release it.");
                yield return new WaitUntil(() => userClickedPad);
                Debug.Log("Successfully clicked touchpad.");
                _instructBehaviour.setInstruction("Good!\n\nNow let go of the touchpad.");
                yield return new WaitUntil(() => !userTouchedPad);
                _instructBehaviour.setInstruction("Well done!");
                yield return new WaitForSecondsRealtime(1f);
                _instructBehaviour.toggleWorldInstruction(false);
                // _instructBehaviour.toggleControllerInstruction(false);

                yield return new WaitForSecondsRealtime(0.25f);

                // _instructBehaviour.toggleControllerInstruction(true);
                toggleMessage(true, "Press and hold the touch pad until the bar fills, then release it.");
                while (padPressedTime < durationToContinue) {
                    if (userClickedPad) {
                        padPressedTime += Time.deltaTime;
                        _progressBar.gameObject.SetActive(true);
                        _progressBar.SetProgress(padPressedTime / durationToContinue);
                    } else {
                        _progressBar.gameObject.SetActive(false);
                        padPressedTime = 0;
                    }
                    yield return null;
                }
                yield return new WaitUntil(() => !userTouchedPad);
                _progressBar.gameObject.SetActive(false);
                // _instructBehaviour.toggleControllerInstruction(false);
                toggleMessage(false);

                toggleMessage(false);
                yield return new WaitForSecondsRealtime(0.5f);
                toggleMessage(true, "Now let's practice the questionnaires.\n\nPress the touch pad again to continue.");
                yield return new WaitForSecondsRealtime(0.25f);
                yield return new WaitUntil(() => userClickedPad);
                toggleMessage(false);
                yield return new WaitForSecondsRealtime(0.5f);
            }

            // Introduce slider interaction
            if (questionnaireTutorial) {
                // Time Format
                ToggleQuestion(true, "How long have you been here, in VR, so far?");
                _questionSlider.UpdateSliderRange(10, 300, false, true);
                yield return new WaitUntil(() => _questionSlider.confirmed);
                yield return new WaitForSecondsRealtime(messageWaitDuration);
                ToggleQuestion(false);
                yield return new WaitForSecondsRealtime(messageWaitDuration);

                // Visual Analog Scale
                ToggleQuestion(true, "What is your feeling toward this environment?");
                _questionSlider.UpdateSliderRange(1, 100, true, false, "bad", "indifferent", "good");
                yield return new WaitUntil(() => _questionSlider.confirmed);
                yield return new WaitForSecondsRealtime(messageWaitDuration);
                ToggleQuestion(false);
                yield return new WaitForSecondsRealtime(messageWaitDuration);

                // SAM Scale: valence
                ToggleQuestion(true, "What is your valence toward this environment?");
                _questionSlider.UpdateSliderRange(1, 100, true, false, "does", "not", "matter", "v");
                yield return new WaitUntil(() => _questionSlider.confirmed);
                yield return new WaitForSecondsRealtime(messageWaitDuration);
                ToggleQuestion(false);
                yield return new WaitForSecondsRealtime(messageWaitDuration);

                // SAM Scale: arousal
                ToggleQuestion(true, "What is your arousal toward this environment?");
                _questionSlider.UpdateSliderRange(1, 100, true, false, "does", "not", "matter", "a");
                yield return new WaitUntil(() => _questionSlider.confirmed);
                yield return new WaitForSecondsRealtime(messageWaitDuration);
                ToggleQuestion(false);
                yield return new WaitForSecondsRealtime(messageWaitDuration);

                // Discrete Scale
                // ToggleQuestion(true, "How would you rate this experience on a 1-to-5 scale?");
                // _questionSlider.UpdateSliderRange(1, 5);
                // yield return new WaitUntil(() => _questionSlider.confirmed);
                // yield return new WaitForSecondsRealtime(messageWaitDuration);
                // ToggleQuestion(false);

                yield return new WaitForSecondsRealtime(0.5f);
                toggleMessage(true, "You did great!\n\nPress the side button again to start.");
            }
        }

        Debug.Log("Currently playing trial " + (m_currentTrialIdx + 1) + " out of " + emotPlaylist.Count);
        Debug.Log(currentEmotTrial.expName);

        while (m_currentTrialIdx < emotPlaylist.Count) {

            UpdateBatteryLevel();

            switch (m_currentTrialIdx) {

                case 0:
                    durationToContinue = 0.75f;
                    messageWaitDuration = 0.75f;
                    break;
                case 1:
                    durationToContinue = 0.6f;
                    messageWaitDuration = 0.6f;
                    break;
                case 2:
                    durationToContinue = 0.5f;
                    messageWaitDuration = 0.5f;
                    break;
                case 3:
                    durationToContinue = 0.4f;
                    messageWaitDuration = 0.4f;
                    break;
                case 4:
                    durationToContinue = 0.35f;
                    messageWaitDuration = 0.35f;
                    break;
                default:
                    durationToContinue = 0.3f;
                    messageWaitDuration = 0.3f;
                    break;

            }

            participantIDInfo.text = m_userId.ToString();
            trialIDInfo.text = (m_currentTrialIdx + 1).ToString();
            currentRoomInfo.text = currentEmotTrial.roomName;
            currentInstructionInfo.text = currentEmotTrial.instruction.ToString();
            durationInfo.text = SecondsToTime(currentEmotTrial.duration);
            elapsedInfo.text = SecondsToTime(0);

            toggleMessage(true, "unloading");
            Debug.Log("Starting room unload...");

            // RoomManager.instance.UnloadScene();
            RoomManager.instance.UnloadRoom();
            yield return new WaitUntil(() => !(RoomManager.instance.actionInProgress));
            toggleMessage(false);
            _instructBehaviour.toggleControllerInstruction(false);
            Debug.Log("Room unload finished.");



            int trialIDX = currentEmotTrial.trial_idx;

            // condObjects.Clear();

            // Skip trial if the station scene is not finished
            // if (!RoomManager.instance.isRoomAvailable(currentTrial.room_idx)) { m_currentTrialIdx++; continue; }

            //
            // BREAK ROOM: do calibration and continue to next level
            //
            if (trialIDX % 5 == 0 && trialIDX > 1) {
                Debug.Log("Break room needed");

                currentRoomInfo.text = "Break Room";
                currentInstructionInfo.text = "Take Break";

                RoomManager.instance.LoadRoom(RoomManager.instance.breakRoomName);

                // WriteInfo(RoomManager.instance.currSceneName);
                yield return new WaitUntil(() =>
                   !RoomManager.instance.actionInProgress &&
                   RoomManager.instance.currentScene.isLoaded);
                yield return null;

                WriteInfo(currentEmotTrial.condensedRoomName);
                FlushInfo();

                DisableCalPoints();

                toggleMessage(true, "takeBreak");


                // Light sync with the sensor pack
                if (lightSync) {
                    syncPanel.gameObject.SetActive(true);
                    syncButton.gameObject.SetActive(true);
                    syncConfirmButton.gameObject.SetActive(true);
                    syncConfirmButton.interactable = false;
                    syncButtonPressed = false;
                    lightSynced = false;
                    while (lightSynced != true) {
                        yield return new WaitUntil(() => syncButtonPressed || lightSynced);
                        if (lightSynced)
                            break;
                        syncButtonPressed = false;
                        WriteInfo("startedLightSync");
                        syncButton.gameObject.SetActive(false);
                        syncConfirmButton.gameObject.SetActive(false);
                        for (int i = 0; i < 5; i++) {
                            syncPanel.color = Color.black;
                            yield return new WaitForSecondsRealtime(syncFlashDuration);
                            syncPanel.color = Color.white;
                            yield return new WaitForSecondsRealtime(syncFlashDuration);
                        }
                        WriteInfo("finishedLightSync");
                        syncButton.gameObject.SetActive(true);
                        syncConfirmButton.gameObject.SetActive(true);
                        syncConfirmButton.interactable = true;
                    }
                    WriteInfo("confirmedLightSync");
                }


                yield return new WaitForSecondsRealtime(30);
                toggleMessage(true, "endBreak");

                _instructBehaviour.RequestConfirmation(durationToContinue);
                yield return new WaitUntil(() => !_instructBehaviour.requested);
                yield return new WaitForSecondsRealtime(messageWaitDuration);
                _instructBehaviour.toggleWorldInstruction(false);
                yield return new WaitForSecondsRealtime(messageWaitDuration);



                toggleMessage(true, "unloading");
                Debug.Log("Starting room unload...");

                // RoomManager.instance.UnloadScene();
                RoomManager.instance.UnloadRoom();

                yield return new WaitUntil(() => !(RoomManager.instance.actionInProgress));
                toggleMessage(false);
                Debug.Log("Room unload finished.");

            }


            if (eyeTracking) {
                // HfG Calibration
                if (!tobiiTracking) {

                    toggleMessage(true, "calibrateVSR");
                    _instructBehaviour.RequestConfirmation(durationToContinue);
                    yield return new WaitUntil(() => !_instructBehaviour.requested);
                    yield return new WaitForSecondsRealtime(messageWaitDuration);
                    _instructBehaviour.toggleWorldInstruction(false);
                    yield return new WaitForSecondsRealtime(messageWaitDuration);

                    bool calibrationSuccess = false;
                    while (!calibrationSuccess) {
                        // Contrary to the old API this function is not async
                        int calibReturnCode = SRanipal_Eye_API.LaunchEyeCalibration(IntPtr.Zero);
                        // Their API kinda suck so right now you cannot pass a delegate to the calibration call (to check if it failed or succeeded) and "calibReturnCode" always returns a positive outcome.
                        //  So if somebody fails the calibration, they have to do it again but by triggering it manually...
                        calibrationSuccess = calibReturnCode == (int)ViveSR.Error.WORK;
                    }

                    // SGL Calibration
                } else {

                    toggleMessage(true, "calibrateTob");
                    _instructBehaviour.RequestConfirmation(durationToContinue);
                    yield return new WaitUntil(() => !_instructBehaviour.requested);
                    yield return new WaitForSecondsRealtime(messageWaitDuration);
                    _instructBehaviour.toggleWorldInstruction(false);
                    yield return new WaitForSecondsRealtime(messageWaitDuration);

                    eyeCalibrated = false;
                    StartCoroutine(FullTobiiCalibration());

                    yield return new WaitUntil(() => eyeCalibrated);


                }
            }


            yield return new WaitForSecondsRealtime(1.0f);

            long start_time = getTimeStamp();
            // Start new gaze record (record name = stimulus name)
            if (eyeTracking) {
                if (tobiiTracking)
                    startNewRecord(true);
                else
                    _eyeTrack.startNewRecord(true);
                Debug.Log("Started eye tracking.");
            }

            // Already start a plux record for the baseline and the transition to the next scene
            if (pluxConnected && !pluxSampling)
                TogglePluxAcquisition();

            if (usingPlux)
                pluxperimenting = true;


            WriteInfo("startedBlankScene");
            yield return new WaitForSecondsRealtime(2.0f);
            WriteInfo("endedBlankScene");


            // Stop recording gaze
            if (eyeTracking)
                if (tobiiTracking)
                    stopRecord(getTimeStamp() - start_time);
                else
                    _eyeTrack.stopRecord(getTimeStamp() - start_time);


            long timeSpentLoading = getTimeStamp();
            toggleMessage(true, "loading");
            // RoomManager.instance.LoadScene(currentTrial.room_idx);
            RoomManager.instance.LoadRoom(currentEmotTrial.roomName);

            // WriteInfo(RoomManager.instance.currSceneName);
            WriteInfo(currentEmotTrial.condensedRoomName);
            yield return new WaitUntil(() =>
               !RoomManager.instance.actionInProgress &&
               RoomManager.instance.currentScene.isLoaded);
            yield return null;
            toggleMessage(false);
            FlushInfo();

            trainSpawner = GameObject.FindObjectOfType<TrainSpawner>();
            DisableCalPoints();

            taskTime = 0;
            padPressedTime = 0;

            //
            // displaying info
            //
            participantIDInfo.text = m_userId.ToString();
            trialIDInfo.text = (m_currentTrialIdx + 1).ToString();
            currentRoomInfo.text = currentEmotTrial.roomName;
            currentInstructionInfo.text = "Intro";
            durationInfo.text = SecondsToTime(currentEmotTrial.duration);
            elapsedInfo.text = "";

            // REGULAR TRIAL ROOM

            // _instructBehaviour.toggleWorldInstruction(false);

            // Update all info panels with the new trial question (there can be more than one question for a same scene)
            // _instructBehaviour.setInstruction(currentTaskString);

            switch (currentEmotTrial.instruction) {

                case instruction.hfgLean:
                    toggleMessage(true, "instructionLeanHFG");
                    currentInstructionInfo.text = "Leaning HFG";
                    break;

                case instruction.hfgSit:
                    toggleMessage(true, "instructionSitHFG");
                    currentInstructionInfo.text = "Sitting HFG";
                    break;

                case instruction.sglSit:
                    toggleMessage(true, "instructionSitSGL");
                    currentInstructionInfo.text = "Sitting SGL";
                    break;

                case instruction.sglStand:
                    toggleMessage(true, "instructionStandSGL");
                    currentInstructionInfo.text = "Standing SGL";
                    break;
            }

            // Show next trial information
            if (m_currentTrialIdx < emotPlaylist.Count - 1) {

                nextRoomInfo.text = nextEmotTrial.roomName;

                switch (nextEmotTrial.instruction) {

                    case instruction.hfgLean:
                        nextInstructionInfo.text = "Leaning HFG";
                        break;

                    case instruction.hfgSit:
                        nextInstructionInfo.text = "Sitting HFG";
                        break;

                    case instruction.sglSit:
                        nextInstructionInfo.text = "Sitting SGL";
                        break;

                    case instruction.sglStand:
                        nextInstructionInfo.text = "Standing SGL";
                        break;
                }
                // Show that this is the last trial
            } else {
                nextRoomInfo.text = "End of session";
                nextInstructionInfo.text = "Outro";
            }

            // toggleMessage(true, "beginWaitingSit");

            _instructBehaviour.RequestConfirmation(durationToContinue);
            yield return new WaitUntil(() => !_instructBehaviour.requested);
            yield return new WaitForSecondsRealtime(messageWaitDuration);
            _instructBehaviour.toggleWorldInstruction(false);
            yield return new WaitForSecondsRealtime(messageWaitDuration);


            toggleMessage(true, "three");
            yield return new WaitForSeconds(1);
            toggleMessage(true, "two");
            yield return new WaitForSeconds(1);
            toggleMessage(true, "one");
            yield return new WaitForSeconds(1);
            toggleMessage(false);


            WriteInfo("Started trial: " + currentEmotTrial.expName);
            FlushInfo();

            // Start new gaze record (record name = stimulus name)
            if (eyeTracking) {
                if (tobiiTracking)
                    startNewRecord();
                else
                    _eyeTrack.startNewRecord();
                Debug.Log("Started eye tracking.");
            }
            // Start trial
            m_isPresenting = true;
            start_time = getTimeStamp();

            if (debugging) {
                // foreach (var lightCond in LightConditions) {
                //     // yield return new WaitForSecondsRealtime(1);
                //     yield return new WaitUntil (() => userGrippedControl || Input.GetKeyUp (KeyCode.Space));
                //     yield return null; // Leave time for key up event to disappear
                //     // setLights (lightCond);
                // }
                // yield return new WaitUntil (() => userGrippedControl || Input.GetKeyUp (KeyCode.Space));
            } else {
                // setLights (LightConditions[currentTrial.light_cond]);
            }

            // Teleport user to starting position
            // Transform startTr = GameObject.Find("Start").transform;
            // _teleporter.Teleport(startTr);
            // startTr.gameObject.SetActive(false);

            // Give time to teleport before relocating world instruction panel
            // yield return new WaitForSecondsRealtime(.5f);

            // Position instruction panel relative to new user location
            // _instructBehaviour.positionWorldInstruction(startTr);
            // Set instruction panel visible
            if (trainSpawner)
                trainSpawner.Invoke("SpawnTrain", currentEmotTrial.duration - trainSpawner.DelayToOpenDoors());

            // Wait until trial time runs out or touchpad pressed
            padPressedTime = 0;
            while (taskTime < currentEmotTrial.duration && padPressedTime < durationToContinue && !Input.GetKeyDown("space")) {
                taskTime += Time.deltaTime;

                elapsedInfo.text = SecondsToTime(taskTime);

                // if (userClickedPad) {
                //     padPressedTime += Time.deltaTime;

                //     _progressBar.gameObject.SetActive(true);
                //     _progressBar.SetProgress(padPressedTime / durationPressToLeave);
                // } else {
                //     _progressBar.gameObject.SetActive(false);
                //     padPressedTime = 0;
                // }

                // trainSpawner.SpawnTrain();
                yield return null;
            }
            _progressBar.gameObject.SetActive(false);

            if (padPressedTime >= durationToContinue)
                Debug.Log("Finished from pad press abort.");
            else if (taskTime >= currentEmotTrial.duration)
                Debug.Log("Finished from expired waiting duration.");
            else
                Debug.Log("Finished from space bar abort.");

            // Stop recording gaze
            if (eyeTracking)
                if (tobiiTracking)
                    stopRecord(getTimeStamp() - start_time);
                else
                    _eyeTrack.stopRecord(getTimeStamp() - start_time);

            m_isPresenting = false;

            // Stop recording Plux data
            if (pluxConnected && pluxSampling)
                TogglePluxAcquisition();

            pluxperimenting = false;

            _instructBehaviour.setInstruction("Please wait");


            WriteInfo("Finished trial: " + currentEmotTrial.expName);

            Debug.Log($"Finished: {currentEmotTrial.expName} - {trialIDX}");
            FlushInfo();

            //
            // Initiate Questioning
            //
            startNewAnswerRecord();
            switch (currentEmotTrial.instruction) {

                case instruction.hfgLean:
                    toggleMessage(true, "instructionEndSitLean");
                    currentInstructionInfo.text = "Standing Questions";
                    break;

                case instruction.hfgSit:
                    toggleMessage(true, "instructionEndSitLean");
                    currentInstructionInfo.text = "Standing Questions";
                    break;

                case instruction.sglSit:
                    toggleMessage(true, "instructionEndSitLean");
                    currentInstructionInfo.text = "Standing Questions";
                    break;

                case instruction.sglStand:
                    toggleMessage(true, "instructionEndStand");
                    currentInstructionInfo.text = "Sitting Questions";
                    break;
            }



            // toggleMessage (true, "beginQuestions");

            _instructBehaviour.RequestConfirmation(durationToContinue);
            yield return new WaitUntil(() => !_instructBehaviour.requested);
            yield return new WaitForSecondsRealtime(messageWaitDuration);
            _instructBehaviour.toggleWorldInstruction(false);
            yield return new WaitForSecondsRealtime(messageWaitDuration);
            toggleMessage(false);
            yield return new WaitForSecondsRealtime(messageWaitDuration);


            // THIS IS THE QUESTION BLOCK

            WriteInfo("Started questions: " + currentEmotTrial.expName);
            FlushInfo();

            ToggleQuestion(true, "waitEstimation");
            _questionSlider.UpdateSliderRange(0, 600, false, true, "", "mm:ss");
            yield return new WaitUntil(() => _questionSlider.confirmed);
            yield return new WaitForSecondsRealtime(messageWaitDuration);
            ToggleQuestion(false);
            yield return new WaitForSecondsRealtime(messageWaitDuration);

            ToggleQuestion(true, "timePass");
            if (language == lang.german)
                _questionSlider.UpdateSliderRange(1, 100, true, false, "extrem langsam", " ", "extrem schnell");
            else
                _questionSlider.UpdateSliderRange(1, 100, true, false, "extremely slowly", " ", "extremely fast");
            yield return new WaitUntil(() => _questionSlider.confirmed);
            yield return new WaitForSecondsRealtime(messageWaitDuration);
            ToggleQuestion(false);
            yield return new WaitForSecondsRealtime(messageWaitDuration);


            ToggleQuestion(true, "comfort");
            if (language == lang.german)
                _questionSlider.UpdateSliderRange(1, 100, true, false, "sehr unbequem", " ", "sehr bequem");
            else
                _questionSlider.UpdateSliderRange(1, 100, true, false, "very uncomfortable", " ", "very comfortable");
            yield return new WaitUntil(() => _questionSlider.confirmed);
            yield return new WaitForSecondsRealtime(messageWaitDuration);
            ToggleQuestion(false);
            yield return new WaitForSecondsRealtime(messageWaitDuration);


            ToggleQuestion(true, "relax");
            if (language == lang.german)
                _questionSlider.UpdateSliderRange(1, 100, true, false, "extrem angespannt", " ", "extrem entspannt");
            else
                _questionSlider.UpdateSliderRange(1, 100, true, false, "extremely tense", " ", "extremely relaxed");
            yield return new WaitUntil(() => _questionSlider.confirmed);
            yield return new WaitForSecondsRealtime(messageWaitDuration);
            ToggleQuestion(false);
            yield return new WaitForSecondsRealtime(messageWaitDuration);


            ToggleQuestion(true, "tired");
            if (language == lang.german)
                _questionSlider.UpdateSliderRange(1, 100, true, false, "sehr müde", " ", "sehr wach");
            else
                _questionSlider.UpdateSliderRange(1, 100, true, false, "very tired", " ", "very awake");
            yield return new WaitUntil(() => _questionSlider.confirmed);
            yield return new WaitForSecondsRealtime(messageWaitDuration);
            ToggleQuestion(false);
            yield return new WaitForSecondsRealtime(messageWaitDuration);

            // SAM Scale: valence
            ToggleQuestion(true, "valence");
            _questionSlider.UpdateSliderRange(1, 100, true, false, "does", "not", "matter", "v");
            yield return new WaitUntil(() => _questionSlider.confirmed);
            yield return new WaitForSecondsRealtime(messageWaitDuration);
            ToggleQuestion(false);
            yield return new WaitForSecondsRealtime(messageWaitDuration);

            // SAM Scale: arousal
            ToggleQuestion(true, "arousal");
            _questionSlider.UpdateSliderRange(1, 100, true, false, "does", "not", "matter", "a");
            yield return new WaitUntil(() => _questionSlider.confirmed);
            yield return new WaitForSecondsRealtime(messageWaitDuration);
            ToggleQuestion(false);
            yield return new WaitForSecondsRealtime(messageWaitDuration);

            ToggleQuestion(true, "thinkPast");
            if (language == lang.german)
                _questionSlider.UpdateSliderRange(1, 100, true, false, "überhaupt nicht", " ", "die ganze Zeit");
            else
                _questionSlider.UpdateSliderRange(1, 100, true, false, "not at all", " ", "all the time");
            yield return new WaitUntil(() => _questionSlider.confirmed);
            yield return new WaitForSecondsRealtime(messageWaitDuration);
            ToggleQuestion(false);
            yield return new WaitForSecondsRealtime(messageWaitDuration);

            ToggleQuestion(true, "thinkPresent");
            if (language == lang.german)
                _questionSlider.UpdateSliderRange(1, 100, true, false, "überhaupt nicht", " ", "die ganze Zeit");
            else
                _questionSlider.UpdateSliderRange(1, 100, true, false, "not at all", " ", "all the time");
            yield return new WaitUntil(() => _questionSlider.confirmed);
            yield return new WaitForSecondsRealtime(messageWaitDuration);
            ToggleQuestion(false);
            yield return new WaitForSecondsRealtime(messageWaitDuration);

            ToggleQuestion(true, "thinkFuture");
            if (language == lang.german)
                _questionSlider.UpdateSliderRange(1, 100, true, false, "überhaupt nicht", " ", "die ganze Zeit");
            else
                _questionSlider.UpdateSliderRange(1, 100, true, false, "not at all", " ", "all the time");
            yield return new WaitUntil(() => _questionSlider.confirmed);
            yield return new WaitForSecondsRealtime(messageWaitDuration);
            ToggleQuestion(false);
            yield return new WaitForSecondsRealtime(messageWaitDuration);

            ToggleQuestion(true, "experienceBody");
            if (language == lang.german)
                _questionSlider.UpdateSliderRange(1, 100, true, false, "überhaupt nicht", " ", "sehr intensiv");
            else
                _questionSlider.UpdateSliderRange(1, 100, true, false, "not at all", " ", "very intensively");
            yield return new WaitUntil(() => _questionSlider.confirmed);
            yield return new WaitForSecondsRealtime(messageWaitDuration);
            ToggleQuestion(false);
            yield return new WaitForSecondsRealtime(messageWaitDuration);

            ToggleQuestion(true, "experienceSpace");
            if (language == lang.german)
                _questionSlider.UpdateSliderRange(1, 100, true, false, "überhaupt nicht", " ", "sehr intensiv");
            else
                _questionSlider.UpdateSliderRange(1, 100, true, false, "not at all", " ", "very intensively");
            yield return new WaitUntil(() => _questionSlider.confirmed);
            yield return new WaitForSecondsRealtime(messageWaitDuration);
            ToggleQuestion(false);
            yield return new WaitForSecondsRealtime(messageWaitDuration);

            ToggleQuestion(true, "thinkTime");
            if (language == lang.german)
                _questionSlider.UpdateSliderRange(1, 100, true, false, "überhaupt nicht", " ", "sehr intensiv");
            else
                _questionSlider.UpdateSliderRange(1, 100, true, false, "not at all", " ", "extremely often");
            yield return new WaitUntil(() => _questionSlider.confirmed);
            ToggleQuestion(false);
            WriteInfo("Finished questions: " + currentEmotTrial.expName);
            FlushInfo();
            yield return new WaitForSecondsRealtime(messageWaitDuration);



            // THIS IS THE END OF THE QUESTION BLOCK

            trainSpawner.DepartTrain();
            yield return new WaitForSecondsRealtime(2.0f);

            toggleMessage(true, "endQuestions");
            stopAnswerRecord();
            yield return new WaitForSecondsRealtime(3.0f);

            m_currentTrialIdx++;
        }

        m_currentTrialIdx--;

        currentInstructionInfo.text = "Outro Questions";

        toggleMessage(true, "outroQuestions");

        _instructBehaviour.RequestConfirmation(durationToContinue);
        yield return new WaitUntil(() => !_instructBehaviour.requested);
        yield return new WaitForSecondsRealtime(messageWaitDuration);
        _instructBehaviour.toggleWorldInstruction(false);
        yield return new WaitForSecondsRealtime(messageWaitDuration);
        toggleMessage(false);


        toggleMessage(true, "unloading");
        Debug.Log("Starting room unload...");

        // RoomManager.instance.UnloadScene();
        RoomManager.instance.UnloadRoom();
        yield return new WaitUntil(() => !(RoomManager.instance.actionInProgress));
        toggleMessage(false);
        _instructBehaviour.toggleControllerInstruction(false);
        Debug.Log("Room unload finished.");


        RoomManager.instance.LoadRoom(RoomManager.instance.breakRoomName);

        // WriteInfo(RoomManager.instance.currSceneName);

        yield return new WaitUntil(() =>
           !RoomManager.instance.actionInProgress &&
           RoomManager.instance.currentScene.isLoaded);
        yield return null;
        DisableCalPoints();

        currentRoomInfo.text = "Break Room";



        // Last questions: comparisons

        startNewAnswerRecord("_OutroQuestions");

        WriteInfo("Started outro");


        yield return new WaitForSecondsRealtime(messageWaitDuration);
        ToggleQuestion(true, "totalTime");
        _questionSlider.UpdateSliderRange(0, 300, false, true, "", "hh:mm");
        yield return new WaitUntil(() => _questionSlider.confirmed);
        yield return new WaitForSecondsRealtime(messageWaitDuration);
        ToggleQuestion(false);

        // SGL outro questions

        if (tobiiTracking) {

            ToggleQuestion(true, "preferPosture");
            if (language == lang.german)
                _questionSlider.UpdateSliderRange(0, 99, true, false, "sitzend", "weder noch", "stehend");
            else
                _questionSlider.UpdateSliderRange(0, 99, true, false, "sitting", "neither", "standing");
            yield return new WaitUntil(() => _questionSlider.confirmed);
            yield return new WaitForSecondsRealtime(messageWaitDuration);
            ToggleQuestion(false);
            yield return new WaitForSecondsRealtime(messageWaitDuration);

            ToggleQuestion(true, "preferLighting");
            if (language == lang.german)
                _questionSlider.UpdateSliderRange(0, 99, true, false, "kalt", "weder noch", "warm");
            else
                _questionSlider.UpdateSliderRange(0, 99, true, false, "cold", "neither", "warm");
            yield return new WaitUntil(() => _questionSlider.confirmed);
            yield return new WaitForSecondsRealtime(messageWaitDuration);
            ToggleQuestion(false);
            yield return new WaitForSecondsRealtime(messageWaitDuration);

            // HfG outro questions
        } else {

            ToggleQuestion(true, "preferPosture");
            if (language == lang.german)
                _questionSlider.UpdateSliderRange(0, 99, true, false, "sitzend", "weder noch", "lehnend");
            else
                _questionSlider.UpdateSliderRange(0, 99, true, false, "sitting", "neither", "leaning");
            yield return new WaitUntil(() => _questionSlider.confirmed);
            yield return new WaitForSecondsRealtime(messageWaitDuration);
            ToggleQuestion(false);
            yield return new WaitForSecondsRealtime(messageWaitDuration);

            ToggleQuestion(true, "preferMaterial");
            if (language == lang.german)
                _questionSlider.UpdateSliderRange(0, 99, true, false, "Holz", "weder noch", "Metallgitter");
            else
                _questionSlider.UpdateSliderRange(0, 99, true, false, "wood", "neither", "metal mesh");
            yield return new WaitUntil(() => _questionSlider.confirmed);
            yield return new WaitForSecondsRealtime(messageWaitDuration);
            ToggleQuestion(false);
            yield return new WaitForSecondsRealtime(messageWaitDuration);
        }



        ToggleQuestion(true, "differences");
        if (language == lang.german)
            _questionSlider.UpdateSliderRange(0, 99, true, false, "extrem ähnlich", " ", "extrem unterschiedlich");
        else
            _questionSlider.UpdateSliderRange(0, 99, true, false, "extremely similar", " ", "extremely different");
        yield return new WaitUntil(() => _questionSlider.confirmed);
        yield return new WaitForSecondsRealtime(messageWaitDuration);
        ToggleQuestion(false);
        yield return new WaitForSecondsRealtime(messageWaitDuration);

        ToggleQuestion(true, "realism");
        if (language == lang.german)
            _questionSlider.UpdateSliderRange(0, 99, true, false, "extrem unrealistisch", " ", "extrem realistisch");
        else
            _questionSlider.UpdateSliderRange(0, 99, true, false, "extremely unrealistic", " ", "extremely realistic");
        yield return new WaitUntil(() => _questionSlider.confirmed);
        yield return new WaitForSecondsRealtime(messageWaitDuration);
        ToggleQuestion(false);
        yield return new WaitForSecondsRealtime(messageWaitDuration);

        stopAnswerRecord();



        toggleMessage(true, "end");

        yield return new WaitForSecondsRealtime(15);

        Debug.Log("Experiment concluded. Quitting...");

        WriteInfo("Ended experiment");

        FlushInfo();
        Quit();
    }

    bool paused;
    private void toggleMessage(bool state, string message = "") {

        Dictionary<string, string> mDictionary = messagesEN;

        if (language == lang.german)
            mDictionary = messagesDE;

        paused = state;
        pausePanel.SetActive(paused);
        Text msgHolder = pausePanel.transform.Find("ContentTxt").GetComponent<Text>();

        if (!mDictionary.ContainsKey(message)) {
            // message = "pause";
            msgHolder.text = message;
        } else {
            msgHolder.text = mDictionary[message];
        }
        // paused = state;
        // pausePanel.SetActive(paused);
        // Text msgHolder = pausePanel.transform.Find("ContentTxt").GetComponent<Text>();
        // string messageText = messages[message];
        // msgHolder.text = messageText;
        Debug.Log(msgHolder.text);
    }

    private string csvQuestionText;

    private void ToggleQuestion(bool state, string question = "") {

        string qText = question;

        Dictionary<string, string> qDictionary = questionsEN;

        if (language == lang.german)
            qDictionary = questionsDE;

        _questionSlider.gameObject.SetActive(state);

        if (!qDictionary.ContainsKey(question)) {
            // message = "pause";
            _questionSlider.UpdateQuestionText(question);
        } else {
            _questionSlider.UpdateQuestionText(qDictionary[question]);
        }
        csvQuestionText = question;

        _questionSlider.RequestConfirmation();
        _questionSlider.confirmed = false;
    }

    private void setLights(LightStruct cond) {
        // condObjects.ToggleLight (cond.orientation, selfregister.objectType.Orientation);
        // condObjects.ToggleLight (cond.landmark, selfregister.objectType.Landmark);
    }

    private void OnGUI() {
        string strInfo = string.Format("FPS: {0:0.00}", 1 / Time.deltaTime);

        GUI.TextArea(new Rect(0, 200, 100, 35), strInfo);
    }

    public static long getTimeStamp() {
        // USE solution I used in Olivier Z. 's project
        return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
    }

    private void OnApplicationQuit() {

        // Disconnect from plux device.
        PluxDevManager.DisconnectPluxDev();

        stopPluxRecord();

        if (eyeTracking && isSampling)
            if (tobiiTracking)
                stopRecord(-1);
            else
                _eyeTrack.stopRecord(-1);

        if (m_recorder_info.BaseStream.CanWrite)
            m_recorder_info.Close();

        if (m_recorder_question != StreamWriter.Null && m_recorder_question.BaseStream != null && m_recorder_question.BaseStream.CanWrite)
            m_recorder_question.Close();

        if (m_recorder_HMD.BaseStream.CanWrite)
            m_recorder_HMD.Close();

        if (m_recorder_ET.BaseStream.CanWrite)
            m_recorder_ET.Close();
    }

    public static void Quit() {
        print("Quitting gracefully");
        if (instance.tobiiTracking)
            EyeTrackingOperations.Terminate();
        Application.Quit();

#if UNITY_EDITOR
        //Stop playing the scene
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public InputField participantIDField, participantIDInfo;
    public InputField trialIDField, trialIDInfo;
    public InputField durationInfo, elapsedInfo;
    public InputField currentRoomInfo, currentInstructionInfo;
    public InputField nextRoomInfo, nextInstructionInfo;
    private readonly Enum _localEnum;

    public void ContinueButtonClick() {

        // TestParticipantID ();
        // TestTrialID ();

        if (conCal || trackCal) {
            return;
            // m_userId = Int16.Parse (txt);
            // Debug.Log ("User Input: " + txt);
        }
        SetUp(false);
        setupPanel.SetActive(false);
    }

    public void StartButtonClick() {
        // string txt = participantIDField.text;\

        trialIDField.text = "0";

        TestParticipantID();
        // TestTrialID();


        m_currentTrialIdx = 0;

        if (conCal || trackCal) {
            return;
            // m_userId = Int16.Parse (txt);
            // Debug.Log ("User Input: " + txt);
        }

        SetUp();
        // yield return new WaitForSecondsRealtime (messageWaitDuration);
        setupPanel.SetActive(false);
        // yield return null;
    }

    // SGL Tobii Eyetracker Additions

    private bool tobiiTracking = false;

    public class GazePoint {
        public GazePoint() // Empty ctor
        {
            LeftGaze = new VRGazeDataEye();
            RightGaze = new VRGazeDataEye();
            data = new VRGazeData();
        }

        public GazePoint(IVRGazeData gaze) {
            LeftGaze = gaze.Left;
            RightGaze = gaze.Right;
            data = gaze;

            LeftCollide = null;
            RightCollide = null;

            LeftWorldRay = getGazeRay(lateralisation.left);
            RightWorldRay = getGazeRay(lateralisation.right);

            LeftLocalRay = new Ray(LeftGaze.GazeOrigin, LeftGaze.GazeDirection);
            RightLocalRay = new Ray(RightGaze.GazeOrigin, RightGaze.GazeDirection);

            LeftViewportPos = getViewportPos(lateralisation.left);
            RightViewportPos = getViewportPos(lateralisation.right);
        }

        public readonly IVRGazeData data;
        public readonly IVRGazeDataEye LeftGaze;
        public readonly IVRGazeDataEye RightGaze;
        public readonly Vector2 LeftViewportPos;
        public readonly Vector2 RightViewportPos;

        public readonly Ray LeftWorldRay;
        public readonly Ray RightWorldRay;

        public readonly Ray LeftLocalRay;
        public readonly Ray RightLocalRay;

        public Transform LeftCollide;
        public Transform RightCollide;

        public bool valid(lateralisation later) {
            return later == lateralisation.left ? LeftGaze != null && LeftGaze.GazeRayWorldValid : RightGaze != null && RightGaze.GazeRayWorldValid;
        }

        public Vector2 getPor(lateralisation later) {
            return lateralisation.left == later ? LeftViewportPos : RightViewportPos;
        }

        public Ray getGazeRay(lateralisation later) {
            IVRGazeDataEye gp = later == lateralisation.left ? LeftGaze : RightGaze;

            return new Ray(data.Pose.Position + gp.GazeOrigin, data.Pose.Rotation * gp.GazeDirection);
        }

        public Vector2 getViewportPos(lateralisation later) {
            IVRGazeDataEye gaze = later == lateralisation.left ? LeftGaze : RightGaze;

            if (!gaze.GazeDirectionValid) return new Vector2(Single.NaN, Single.NaN);

            Vector3 worldPosition = (later == lateralisation.left ? LeftWorldRay : RightWorldRay).GetPoint(FillCamFoV.m_distance * 20);

            Vector2 screenPos;
            if (Thread.CurrentThread != mainThread) {
                screenPos = WorldToVP(worldPosition,
                    later == lateralisation.left ? Camera.StereoscopicEye.Left : Camera.StereoscopicEye.Right);
            } else {
                screenPos = ExpeControl.instance.mainCam.WorldToViewportPoint(worldPosition,
                    later == lateralisation.left ? Camera.MonoOrStereoscopicEye.Left : Camera.MonoOrStereoscopicEye.Right);
            }

            return new Vector2(screenPos.x, screenPos.y);
        }

        private static Vector2 WorldToVP(Vector3 worldpos, Camera.StereoscopicEye eye) {
            Matrix4x4 proj = eye == Camera.StereoscopicEye.Left ? ExpeControl.instance.camStereoProjLeft : ExpeControl.instance.camStereoProjRight;

            Vector4 worldPos = new Vector4(worldpos.x, worldpos.y, worldpos.z, 1.0f);
            Vector4 viewPos = ExpeControl.instance.camViewMat * worldPos;
            Vector4 projPos = proj * viewPos; // ExpeControl.instance.mainCam.projectionMatrix * viewPos;
            Vector3 ndcPos = new Vector3(projPos.x / projPos.w, projPos.y / projPos.w, projPos.z / projPos.w);
            Vector3 viewportPos = new Vector3(ndcPos.x * 0.5f + 0.5f, ndcPos.y * 0.5f + 0.5f, -viewPos.z);

            return viewportPos;
        }
    }

    public GameObject SRAnipal;
    public VREyeTracker trackr;
    public GazePoint gazePoint = new GazePoint();
    public ShaderBehaviour shaderBehavior;
    public GameObject grayArea;
    public delegate void samplingCallback(GazePoint gaze);
    public Dictionary<string, samplingCallback> SamplingCallbacks = new Dictionary<string, samplingCallback>();

    private VREyeTracker _eyeTrackerTobii => (VREyeTracker.Instance);
    private bool isTobiiTracking => (_eyeTrackerTobii.isActiveAndEnabled);

    private VRCalibration _calibration => (VRCalibration.Instance);
    private bool m_validationSuccess;
    private bool m_validationDone;

    private bool m_calibrationSuccess;
    private bool m_calibrationDone;
    // private bool m_isPresenting;

    [SerializeField]
    private bool b_validate = true;
    [SerializeField]
    private bool b_calibrate = true;

    private bool m_ETsubscribed = false;

    // CALIBRATION BEG
    private void startCalibration() {
        _calibration.StartCalibration(null, CalibrationCallback);
    }

    private void CalibrationCallback(bool calibrationResult) {
        m_calibrationSuccess = calibrationResult;
        m_calibrationDone = true;
    }
    // CALIBRATION END

    // GAZE TRACKING SAMPLING
    private Vector3 cameraRotation;
    private Vector3 cameraPosition;
    private Vector3 cameraLocalScale;
    private Quaternion cameraQuaternion;
    private Matrix4x4 camStereoProjLeft;
    private Matrix4x4 camStereoProjRight;
    private Matrix4x4 camViewMat;

    private Vector3 torsoPosition;
    private Quaternion torsoRotation;
    public long UnityTimeStamp;

    private static readonly Thread mainThread = Thread.CurrentThread;

    float passedTime = 0f;
    float batteryMeterTime = 0f;
    bool sensorUpdateFail = false;

    public void UpdateSensorReadouts() {
        if (!pluxSampling)
            return;

        long[][] dataPackage = PluxDevManager.GetPackageOfData(false);
        long[] lastSamples = new long[ActiveChannels.Count];

        if (dataPackage != null && dataPackage[0] != null) {

            lastSamples = dataPackage.Last();

            float maxSample = Mathf.Pow(2, bitDepth);

            foreach (int chan in ActiveChannels) {
                switch (chan) {
                    case 1:
                        signal1Indicator.color = Color.HSVToRGB(0, 1, lastSamples[1] / maxSample);
                        break;
                    case 2:
                        signal2Indicator.color = Color.HSVToRGB(0.55f, 1, lastSamples[2] / maxSample);
                        break;
                    case 3:
                        signal3Indicator.color = Color.HSVToRGB(0.25f, 1, lastSamples[3] / maxSample);
                        break;
                    case 4:
                        signal4Indicator.color = Color.HSVToRGB(0.15f, 1, lastSamples[4] / maxSample);
                        break;
                }
            }
        } else {
            sensorUpdateFail = true;
        }

    }

    [SerializeField] private bool realTimeReadout = false;

    private void Update() {

        if (pluxSampling) {
            passedTime += Time.deltaTime;
            if (passedTime > 100.0f / (float)sampleRate) {
                passedTime = 0;
                GetSample();
            }
            if (realTimeReadout)
                UpdateSensorReadouts();
        }

        if (pluxperimenting) {
            if (!pluxConnected || !pluxSampling || sensorUpdateFail) {
                pluxperimenting = false;
                syncPanel.gameObject.SetActive(true);
            }
        }

        if (pluxConnected) {
            batteryMeterTime += Time.deltaTime;
            if (batteryMeterTime > 30.0f) {
                batteryMeterTime = 0;
                // UpdateBatteryLevel();
            }
        }

        // return;
        // To be used in this component - Coroutines are called back between "Update" and "LateUpdate"
        if (tobiiTracking) {
            RetrieveCameraData();
            if (isSampling) {
                m_recorder_HMD.WriteLine(
                    $"{gazePoint.data.TimeStamp},{UnityTimeStamp}," +
                    $"{(gazePoint.LeftCollide != null ? gazePoint.LeftCollide.name : "None")}," +
                    $"{(gazePoint.RightCollide != null ? gazePoint.RightCollide.name : "None")}");
                // $"{(gazePoint.CombinedCollide != null ? gazePoint.CombinedCollide.name : "None")}");
                // m_recorder_HMD.Flush();
            }
        }
    }
    public Vector2[] validationHit = new Vector2[2];

    public void RetrieveCameraData() {
        Transform camTrans = mainCam.transform;

        cameraRotation = camTrans.eulerAngles;
        cameraQuaternion = camTrans.rotation;
        cameraPosition = camTrans.position;
        cameraLocalScale = camTrans.localScale;

        camStereoProjLeft = mainCam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
        camStereoProjRight = mainCam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);

        gazePoint.LeftCollide = Physics.Raycast(gazePoint.LeftWorldRay, out RaycastHit hitL) ? hitL.transform : null;
        gazePoint.RightCollide = Physics.Raycast(gazePoint.RightWorldRay, out RaycastHit hitR) ? hitR.transform : null;

        if (gazePoint.LeftCollide != null && gazePoint.LeftCollide.name.Contains("mesh"))
            if (gazePoint.LeftCollide.parent != null)
                gazePoint.LeftCollide = gazePoint.LeftCollide.parent;
        if (gazePoint.RightCollide != null && gazePoint.RightCollide.name.Contains("mesh"))
            if (gazePoint.RightCollide.parent != null)
                gazePoint.RightCollide = gazePoint.RightCollide.parent;

        if (Physics.Raycast(gazePoint.LeftWorldRay, out RaycastHit vL)) {
            validationHit[0] = shaderBehavior.transform.InverseTransformPoint(vL.point);
            validationHit[0].x = (validationHit[0].x + .5f) * Utils.Cam_FOV_hori;
            validationHit[0].y = (validationHit[0].y + .5f) * Utils.Cam_FOV_vert;

            //            Vector3 aa = shB.transform.InverseTransformPoint(vL.point);
            //            print($"{aa.x},{aa.y},{aa.z} -- {validationHit[0].x},{validationHit[0].y}");
        } else {
            validationHit[0] = new Vector2(float.NaN, float.NaN);
        }
        if (Physics.Raycast(gazePoint.RightWorldRay, out RaycastHit vR)) {
            validationHit[1] = shaderBehavior.transform.InverseTransformPoint(vR.point);
            validationHit[1].x = (validationHit[1].x + .5f) * Utils.Cam_FOV_hori;
            validationHit[1].y = (validationHit[1].y + .5f) * Utils.Cam_FOV_vert;
        } else {
            validationHit[1] = new Vector2(float.NaN, float.NaN);
        }

        camViewMat = mainCam.worldToCameraMatrix;

        UnityTimeStamp = getTimeStamp();
    }

    // Record data
    // private StreamWriter m_recorder_ET = StreamWriter.Null;
    // public StreamWriter m_recorder_HMD = StreamWriter.Null;
    // private StreamWriter m_recorder_info = StreamWriter.Null;

    public bool isSampling;
    public long lastOcuTS;
    public char phase = 'z';

    private void HMDGazeDataReceivedCallback(object sender, HMDGazeDataEventArgs rawGazeData) {
        long OcutimeStamp = EyeTrackingOperations.GetSystemTimeStamp();
        // print("in");

        lastOcuTS = OcutimeStamp;

        EyeTrackerOriginPose bestMatchingPose = new EyeTrackerOriginPose(OcutimeStamp, cameraPosition, cameraQuaternion);

        VRGazeData gazeData = new VRGazeData(rawGazeData, bestMatchingPose);
        gazePoint = new GazePoint(gazeData);

        // Viewport positions
        Vector2 leftPor = gazePoint.getPor(lateralisation.left);
        Vector2 rightPor = gazePoint.getPor(lateralisation.right);
        // 3D gaze vector
        Vector3 leftBasePoint = gazeData.Left.GazeOrigin;
        Vector3 rightBasePoint = gazeData.Right.GazeOrigin;
        Vector3 leftGazeDirection = gazeData.Left.GazeDirection;
        Vector3 rightGazeDirection = gazeData.Right.GazeDirection;
        // Cyclops 3D gaze vector
        Vector3 meanBasePoint = gazeData.CombinedGazeRayWorld.origin;
        Vector3 meanGazeDirection = gazeData.CombinedGazeRayWorld.direction;
        // Validity
        bool valL = gazeData.Left.GazeDirectionValid;
        bool valR = gazeData.Right.GazeDirectionValid;
        //Pupil Data
        float pDiameterL = gazeData.Left.PupilDiameter;
        float pDiameterR = gazeData.Right.PupilDiameter;

        // TODO: add back func startNewRecord - unblock below
        if (isSampling) {
            m_recorder_ET.WriteLine(
                $"{OcutimeStamp},{UnityTimeStamp}," +
                $"{leftPor.x},{leftPor.y}," +
                $"{rightPor.x},{rightPor.y}," +
                $"{cameraPosition.x},{cameraPosition.y},{cameraPosition.z}," +
                $"{cameraQuaternion.x},{cameraQuaternion.y},{cameraQuaternion.z},{cameraQuaternion.w}," +
                $"{torsoPosition.x},{torsoPosition.y},{torsoPosition.z}," +
                $"{torsoRotation.x},{torsoRotation.y},{torsoRotation.z},{torsoRotation.w}," +
                $"{meanBasePoint.x},{meanBasePoint.y},{meanBasePoint.z}," +
                $"{meanGazeDirection.x},{meanGazeDirection.y},{meanGazeDirection.z}," +
                $"{leftBasePoint.x},{leftBasePoint.y},{leftBasePoint.z}," +
                $"{rightBasePoint.x},{rightBasePoint.y},{rightBasePoint.z}," +
                $"{leftGazeDirection.x},{leftGazeDirection.y},{leftGazeDirection.z}," +
                $"{rightGazeDirection.x},{rightGazeDirection.y},{rightGazeDirection.z}," +
                $"{pDiameterL},{pDiameterR},{valL},{valR}"
            );
        }

        foreach (samplingCallback func in SamplingCallbacks.Values) {
            func(gazePoint);
        }
    }

    private bool fullTobiiCalibrationComplete = false;
    private bool eyeValidationComplete = false;

    bool eyeCalibrated = false;
    bool eyeValidated = false;
    IEnumerator FullTobiiCalibration() {

        Debug.Log("Inside Tobii calibration routine");


        // grayArea.SetActive(true);

        eyeCalibrated = false;
        eyeValidated = false;

        print("Waiting for the eyetracker to start");
        // Wait for ET server to start
        yield return new WaitUntil(() => _eyeTrackerTobii != null && _eyeTrackerTobii._eyeTracker != null);
        print("_eyeTrackerTobii != null");

        yield return new WaitForEndOfFrame();
        _eyeTrackerTobii._eyeTracker.HMDGazeDataReceived += HMDGazeDataReceivedCallback;

        m_ETsubscribed = true;
        print("Eyetracker started and subscribed to");

        // Tobii calibration routine

        // shaderBehavior.phase = ShaderBehaviour.shaderPhase.none;

        m_calibrationSuccess = false;

        int calCount = 0;
        while (!m_calibrationSuccess) {

            print("BEFORE CALIBRATION");
            // print("Press space to begin calibration routine!");
            // yield return new WaitUntil(() => Input.GetKeyUp(KeyCode.Space));

            m_calibrationDone = false;
            yield return null;
            startCalibration();
            yield return new WaitUntil(() => m_calibrationDone);
            print("AFTER CALIBRATION");

            if (b_validate && m_calibrationSuccess) {
                // calCount = 0;
                // Validation procedure - only if calibration was successful
                m_validationSuccess = false;
                // If fails: new calibration
                m_validationDone = false;
                yield return null;
                shaderBehavior.phase = ShaderBehaviour.shaderPhase.validation;
                yield return new WaitUntil(() => m_validationDone);
                shaderBehavior.phase = ShaderBehaviour.shaderPhase.none;

                m_calibrationSuccess = m_validationSuccess;

                if (!m_validationSuccess) {
                    print("failedVal");
                    yield return new WaitForSecondsRealtime(3f);
                } else {
                    print("succeededVal");
                }
            }
            // TODO: log calibration and validation success and precision

            if (++calCount >= 3) {
                print("failedCal");
                print("Press space to abort calibration and continue...");
                yield return new WaitUntil(() => Input.GetKeyUp(KeyCode.Space));
                m_calibrationSuccess = true;
                calCount = 0;
            }
        }
        // End of Tobii Calibration

        eyeCalibrated = true;
        eyeValidated = true;


        grayArea.SetActive(false);
    }

    private void startNewPluxRecord(string special = "") {

        string fileName;

        if (special != "") {
            fileName = Directory.GetParent(Application.dataPath) + "/Pluxing/" + special + ".csv";
        } else {
            fileName = m_userdataPath + "/" + currentEmotTrial.expName + "_Plux.csv";
        }

        m_recorder_plux = new StreamWriter(fileName);

        string channelNames = "UnityTS";

        foreach (int chan in ActiveChannels) {
            switch (chan) {
                case 1:
                    channelNames += "," + chan1Field.text;
                    break;
                case 2:
                    channelNames += "," + chan2Field.text;
                    break;
                case 3:
                    channelNames += "," + chan3Field.text;
                    break;
                case 4:
                    channelNames += "," + chan4Field.text;
                    break;
            }
        }

        if (m_recorder_plux.BaseStream.CanWrite) {
            m_recorder_plux.WriteLine(channelNames);
            m_recorder_plux.Flush();
        }
    }

    private void stopPluxRecord() {

        if (m_recorder_plux != null && m_recorder_plux.BaseStream.CanWrite)
            m_recorder_plux.Close();

    }

    private void startNewAnswerRecord(string name = "") {

        string fileName = "";


        if (name != "") {
            fileName = currentEmotTrial.participantName + name;
        } else {
            fileName = currentEmotTrial.expName + "_Answers";
        }

        fileName += ".csv";

        m_recorder_question = new StreamWriter(m_userdataPath + "/" + fileName);


        // m_recorder_question = new StreamWriter (m_userdataPath + "/Answers.csv", true); 

        if (m_recorder_question.BaseStream.CanWrite) {
            // m_recorder_question.WriteLine ("{0},{1},{2},{3}", getTimeStamp (), currentEmotTrial.expNameCSV, CondenseString (question), answer);
            //                                                                 $"{lab},{participant+1},{trial_idx},{roomName},{instruction},{duration}";
            m_recorder_question.WriteLine("UnityTS,LabID,ParticipantID,TrialID,Room,Instruction,Duration,Question,Answer");
            // RoomManager.instance.currentRoomName, currentEmotTrial.duration, m_currentTrialIdx, txt);
            m_recorder_question.Flush();
        }

    }

    private void stopAnswerRecord() {

        if (m_recorder_question != null && m_recorder_question.BaseStream.CanWrite)
            m_recorder_question.Close();
    }

    private void startNewRecord(bool baseline = false) {
        // m_recorder_ET = new StreamWriter(m_userdataPath + "/TESTname_ET.csv");
        // m_recorder_ET = new StreamWriter(m_userdataPath + "/" + currentTrial.expName + "_ET.csv");
        string baseName = m_userdataPath + "/" + currentEmotTrial.expName;

        string fileNameET;
        string fileNameHMD;

        if (baseline) {
            fileNameET = baseName + "_ET_base.csv";
            fileNameHMD = baseName + "_HMD_base.csv";
        } else {
            fileNameET = baseName + "_ET.csv";
            fileNameHMD = baseName + "_HMD.csv";
        }

        // m_recorder_ET = new StreamWriter(m_userdataPath + "/" +
        //     currentEmotTrial.expName + "_ET.csv");

        m_recorder_ET = new StreamWriter(fileNameET);
        m_recorder_ET.WriteLine(
            "OcutimeStamp,UnityTimeStamp," +
            "leftPor.x,leftPor.y," +
            "rightPor.x,rightPor.y," +
            "cameraPosition.x,cameraPosition.y,cameraPosition.z," +
            "cameraRotation.x,cameraRotation.y,cameraRotation.z,cameraRotation.w," +
            "torsoPosition.x,torsoPosition.y,torsoPosition.z," +
            "torsoRotation.x,torsoRotation.y,torsoRotation.z,torsoRotation.w," +
            "meanBasePoint.x,meanBasePoint.y,meanBasePoint.z," +
            "meanGazeDirection.x,meanGazeDirection.y,meanGazeDirection.z," +
            "leftBasePoint.x,leftBasePoint.y,leftBasePoint.z," +
            "rightBasePoint.x,rightBasePoint.y,rightBasePoint.z," +
            "leftEyeDirection.x,leftEyeDirection.y,leftEyeDirection.z," +
            "rightEyeDirection.x,rightEyeDirection.y,rightEyeDirection.z," +
            "pupilDiameterL,pupilDiameterR,valL,valR");

        // m_recorder_HMD = new StreamWriter(m_userdataPath + "/TESTname_HMD.csv");
        // m_recorder_HMD = new StreamWriter(m_userdataPath + "/" + currentTrial.expName + "_HMD.csv");
        m_recorder_HMD = new StreamWriter(fileNameHMD);
        //         m_recorder_HMD = new StreamWriter(m_userdataPath + "/" +
        //  currentEmotTrial.expName + "_HMD.csv");
        m_recorder_HMD.WriteLine(
            "OcutimeStamp,UnityTimeStamp," +
            "LeftCollide,RightCollide");

        isSampling = true;

        // writeInfo($"Started: [{currentTrial.exp_idx + 1}] {currentTrial.expName}");
    }

    private void stopRecord(long elapsedtime) {
        isSampling = false;
        if (m_recorder_ET != null && m_recorder_ET.BaseStream.CanWrite)
            m_recorder_ET.Close();
        if (m_recorder_HMD != null && m_recorder_HMD.BaseStream.CanWrite)
            m_recorder_HMD.Close();

        // writeInfo($"Elapsed time: {elapsedtime}");
        // writeInfo($"Trial ended: {(userPressed ? "Pressed trigger" : "Ran out of time")}");
    }

    IEnumerator TobiiCalibration() {
        toggleMessage(true, "pleaseCalibrateTobii");
        _instructBehaviour.RequestConfirmation(durationToContinue);
        yield return new WaitUntil(() => !_instructBehaviour.requested);
        yield return new WaitForSecondsRealtime(messageWaitDuration);
        _instructBehaviour.toggleWorldInstruction(false);
        yield return new WaitForSecondsRealtime(messageWaitDuration);

        // shaderBehavior.gameObject.SetActive(true);

        // Debug.Log("Adding Tobii callback");
        // shaderBehavior.validationCallback = (success) => {
        //     this.m_validationSuccess = success;
        //     this.m_validationDone = true;
        // };

        // shaderBehavior.phase = ShaderBehaviour.shaderPhase.none;

        print("Waiting for the eyetracker to start");
        // Wait for ET server to start
        yield return new WaitUntil(() => _eyeTrackerTobii != null && _eyeTrackerTobii._eyeTracker != null);
        print("_eyeTrackerTobii != null");

        yield return new WaitForEndOfFrame();
        _eyeTrackerTobii._eyeTracker.HMDGazeDataReceived += HMDGazeDataReceivedCallback;

        m_ETsubscribed = true;
        print("Eyetracker started and subscribed to");

        // Tobii calibration routine

        // shaderBehavior.phase = ShaderBehaviour.shaderPhase.none;

        m_calibrationSuccess = false;

        int calCount = 0;
        while (!m_calibrationSuccess) {

            print("BEFORE CALIBRATION");
            // print("Press space to begin calibration routine!");
            // yield return new WaitUntil(() => Input.GetKeyUp(KeyCode.Space));

            m_calibrationDone = false;
            yield return null;
            startCalibration();
            yield return new WaitUntil(() => m_calibrationDone);
            print("AFTER CALIBRATION");

            if (b_validate && m_calibrationSuccess) {
                // calCount = 0;
                // Validation procedure - only if calibration was successful
                m_validationSuccess = false;
                // If fails: new calibration
                m_validationDone = false;
                yield return null;
                shaderBehavior.phase = ShaderBehaviour.shaderPhase.validation;
                yield return new WaitUntil(() => m_validationDone);
                shaderBehavior.phase = ShaderBehaviour.shaderPhase.none;

                m_calibrationSuccess = m_validationSuccess;

                if (!m_validationSuccess) {
                    print("failedVal");
                    yield return new WaitForSecondsRealtime(3f);
                } else {
                    print("succeededVal");
                }
            }
            // TODO: log calibration and validation success and precision

            if (++calCount >= 3) {
                print("failedCal");
                print("Press space to abort calibration and continue...");
                yield return new WaitUntil(() => Input.GetKeyUp(KeyCode.Space));
                m_calibrationSuccess = true;
                calCount = 0;
            }
        }
        // End of Tobii Calibration
    }

    // GAZE TRACKING SAMPLING

    // bool paused;

}