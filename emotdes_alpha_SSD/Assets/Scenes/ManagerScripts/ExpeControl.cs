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

public class ExpeControl : MonoBehaviour {
    public static ExpeControl instance { get; private set; }

    [SerializeField] private bool debugging;
    [SerializeField] private bool controllerTutorial, questionnaireTutorial;
    [SerializeField] private bool resetExperiments = false;
    [SerializeField] private bool eyeTracking = true;
    [SerializeField] private bool preTesting = true;

    [SerializeField]
    private int max_idx = 100;

    [SerializeField] private Button controllerCalButton, trackerCalButton, hfgButton, sglButton;

    [SerializeField] private List<int> durations = new List<int> ();

    // Playlist data
    private readonly List<playlistElement> playlist = new List<playlistElement> (90);
    private readonly List<EmotPlaylistElement> emotPlaylist = new List<EmotPlaylistElement> (90);
    private readonly List<List<EmotPlaylistElement>> allPlaylists = new List<List<EmotPlaylistElement>> (90);
    public int m_currentTrialIdx = 0;
    public int m_currentQuestionIdx = 0;
    public int currentTrialIdx => m_currentTrialIdx;
    public EmotPlaylistElement currentEmotTrial => emotPlaylist[currentTrialIdx];
    public playlistElement currentTrial => playlist[currentTrialIdx];
    // private string currentRoom => playlist[currentTrialIdx].room_name;
    // private int currentRoomIdx => playlist[currentTrialIdx].room_idx;

    private Transform cameraRig;
    public Camera mainCam;

    public enum lateralisation {
        left,
        right,
        comb
    }

    public struct LightStruct {
        public bool orientation;
        public bool landmark;
    }
    static public List<LightStruct> LightConditions = new List<LightStruct> (4) {
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
    public GameObject setupPanel;

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

    public GameObject pausePanel;
    public GameObject questionPanel;

    private TrainSpawner trainSpawner;

    private readonly Dictionary<string, string> messages = new Dictionary<string, string> {
        {
            "calibrate",
            "You can take a quick break if you wish to remove the headset.\n\n" +
            "Then continue please with the calibration;\n" +
            "you may ask the assistant for help with that.\n\n" +
            "Hold the touchpad when you're done to continue!"
            // "Bitte beginne mit der Kalibrierung.\nFrage den Assistenten, sofern du Hilfe benötigst.\n\n"+
            // "Wenn du fertig bist, drücke bitte die Seitentaste um fortzufahren."
        },
        {
            "takeBreak",
            "You can take a quick break if you wish to remove the headset.\n\n" +
            "Hold the touchpad when you're done to continue!"
            // "Bitte beginne mit der Kalibrierung.\nFrage den Assistenten, sofern du Hilfe benötigst.\n\n"+
            // "Wenn du fertig bist, drücke bitte die Seitentaste um fortzufahren."
        },
        {
            "pleaseLean",
            "Please lean on the high bench.\n\n" +
            "Once you're seated, hold the trigger to continue."
        },
        {
            "pleaseSit",
            "Please sit down on the low bench.\n\n" +
            "Once you're seated, hold the trigger to continue."
        },
        {
            "pleaseStand",
            "Please stand up and take one step forward, away from the bench.\n\n" +
            "Once you're standing, hold the trigger to continue."
        },
        {
            "pleaseCalibrateTobii",
            "To begin, we need to calibrate the eye tracker.\n" +
            "With your eyes, follow the red dot, then the gray disks as closely as you can.\n\n" +
            "Hold the trigger to continue."
        },
        {
            "pleaseCalibrateVive",
            "To begin, we need to calibrate the eye tracker.\n" +
            "Press the MENU button on your controller and select the eye-tracking procedure.\n\n" +
            "Hold the trigger ONLY when you've completed it to continue."
        },
        {
            "start",
            "The training phase has ended.\n\n" +
            "Press the trigger to start the experiment."
        },
        {
            "pause",
            "Take off the headset if you wish.\n\n" +
            "Take a moment to rest before continuing with the experiment.\n" +
            "Press the trigger to start the experiment."
        },
        {
            "loading",
            "The next room is loading..."
            // "Nächster Raum wird geladen."
        },
        {
            "unloading",
            "The current room is being unloaded..."
            // "Unloading current room."
        },
        {
            "end",
            "This is the end of the experiment.\nThank you very much for your participation.\nYou can take off the headset."
        },
        {
            "beginWaitingSit",
            "The waiting period will begin soon!\n" +
            "You will get notified once it's over, and you will\n" +
            "be asked questions about your experience then.\n\n" +
            "Please sit down, and hold the touch pad to begin!"
            // "Die Wartezeit beginnt gleich.\n\n" +
            // "Halte das Touchpad gedrückt um damit loszulegen!"
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
            "Press the side button to continue with the questionnaires."
            // "Die Wartezeit ist jetzt vorbei!\n\n" +
            // "Betätige die Seitenknöpfe um mit den Fragebögen fortzufahren."
        },
        {
            "endQuestions",
            "Thank you!\n\n" +
            "The questionnaire is now finished. Get ready for the next trial!"
            // "Die Wartezeit ist jetzt vorbei!\n\n" +
            // "Betätige die Seitenknöpfe um mit den Fragebögen fortzufahren."
        },
    };

    private EyeTrackingSampler _eyeTrack => EyeTrackingSampler.instance;
    private ProgressBar _progressBar => ProgressBar.instance;
    private RadialProgress _radialProgress => RadialProgress.instance;
    private QuestionSlider _questionSlider => QuestionSlider.instance;
    private bool isTracking => (_eyeTrack.ready);
    private InstructBehaviour _instructBehaviour;
    public ObjectManager condObjects { get; private set; }
    // private TeleporterFacade _teleporter;

    [Tooltip ("Time in seconds needed to click trigger to continue")] public float durationToContinue;

    void Awake () {

        instance = this;

        if (mainCam == null)
            mainCam = Camera.main;

        tobiiTracking = PlayerPrefs.GetInt ("tobii", 0) != 0;
        m_labID = tobiiTracking ? "SGL" : "HfG";

        if (eyeTracking) {
            GetComponent<VREyeTracker> ().enabled = true;
            SRAnipal.SetActive (true);
        }

        if (tobiiTracking) {
            Debug.Log ("Adding Tobii callback");
            shaderBehavior.validationCallback = (success) => {
                this.m_validationSuccess = success;
                this.m_validationDone = true;
            };
        }

        string[] RoomNames = RoomManager.RoomNames;
        cameraRig = transform.GetChild (0);
        Debug.Log ("Found camera rig: " + cameraRig.name);
        Debug.Log ("CamRig pos: " + cameraRig.position + " CamRig rot: " + cameraRig.rotation);
        _instructBehaviour = GetComponent<InstructBehaviour> ();

        if (_instructBehaviour.leftControllerActive) {
            controllerBasePoint = GameObject.Find ("BasePointL");
            _questionSlider.controllerMainPoint = GameObject.Find ("ControlPointL");
        } else {
            controllerBasePoint = GameObject.Find ("BasePointR");
            _questionSlider.controllerMainPoint = GameObject.Find ("ControlPointR");
        }

        controllerBasePoint.SetActive (false);
        _questionSlider.controllerMainPoint.SetActive (false);

        // condObjects = new ObjectManager();

        // _teleporter = gameObject.GetComponentInChildren<TeleporterFacade>();

        // Disable panels
        setupPanel.SetActive (false);
        pausePanel.SetActive (false);
        // questionPanel.SetActive(false);

        LoadPlaylistsFromCSVs ();
        GetLastUserNumber ();
        // TestParticipantID ();
        TestTrialID ();

    }

    public void TestParticipantID () {

        int.TryParse (participantIDField.text, out int participantID);

        if (participantID < 1)
            participantID = 1;
        if (participantID > allPlaylists.Count)
            participantID = allPlaylists.Count;

        participantIDField.text = participantID.ToString ();

        m_userId = participantID;

        m_userdataPath = m_basePath + "/Subj_" + m_userId;

        if (Directory.Exists (m_userdataPath)) {
            Debug.Log ("Participant data already exists at " + m_userdataPath);

            int count = Directory.GetFiles (m_userdataPath, "*.csv", SearchOption.AllDirectories).Length;
            if (count > 2) {
                m_currentTrialIdx = (count - 1) / 2;
                trialIDField.text = (m_currentTrialIdx + 1).ToString ();
            }

        } else {
            Debug.Log ("Participant data does not exist yet for " + m_userdataPath);
            trialIDField.text = "1";
        }

        // TestTrialID ();

    }

    public void TestTrialID () {
        int.TryParse (trialIDField.text, out int trialID);

        int maxTrials = allPlaylists[m_userId - 1].Count;

        if (trialID < 1)
            trialID = 1;
        if (trialID > maxTrials)
            trialID = maxTrials;

        trialIDField.text = trialID.ToString ();

        m_currentTrialIdx = trialID - 1;
    }

    void GetLastUserNumber () {

        // Get last user number
        m_basePath = Directory.GetParent (Application.dataPath) + "/SubjectData/" + m_labID;
        if (!Directory.Exists (m_basePath))
            Directory.CreateDirectory (m_basePath);

        string[] directories = Directory.GetDirectories (m_basePath);

        int lastSubjID = 0;

        Debug.Log ("Found " + directories.Length + " participant folders in the " + m_labID + " directory.");

        foreach (string directory in directories) {
            string folder = directory.Split ('/').Last ();

            int tmpSubjID;

            string[] splitResults = folder.Split ('_');
            if (splitResults.Length > 1) {
                int.TryParse (splitResults[1], out tmpSubjID);

                if (tmpSubjID > lastSubjID)
                    lastSubjID = tmpSubjID;
            }
        }
        m_userId = lastSubjID + 1;

        if (m_userId >= allPlaylists.Count) {
            Debug.Log ("We are at the last user");
            m_userId = allPlaylists.Count;
        }

        participantIDField.text = m_userId.ToString ();

        TestParticipantID ();

        // Get last completed trial information

        // m_userdataPath = m_basePath + "/Subj_" + m_userId;
        // int count = Directory.GetFiles (m_userdataPath, "*.csv", SearchOption.AllDirectories).Length;
        // if (count > 2) {
        //     m_currentTrialIdx = (count - 1) / 2;
        //     trialIDField.text = m_currentTrialIdx.ToString ();
        // }

    }

    void GetLastTrialNumber () {

        m_userdataPath = m_basePath + "/Subj_" + m_userId;
        int count = Directory.GetFiles (m_userdataPath, "*.csv", SearchOption.AllDirectories).Length;
        if (count > 2) {
            m_currentTrialIdx = (count - 1) / 2;
            trialIDField.text = m_currentTrialIdx.ToString ();
        }
    }

    private void SetUp () {

        m_userdataPath = m_basePath + "/Subj_" + m_userId;

        // If this participant already exists: start after last trial
        if (Directory.Exists (m_userdataPath)) {
            Debug.Log ("Data for participant " + m_userId + " already exists.");

            if (resetExperiments) {

                string[] oldFiles = Directory.GetFiles (m_userdataPath, "*.*", SearchOption.AllDirectories);

                foreach (string file in oldFiles) {
                    // Debug.Log(file);
                    File.Delete (file);
                }
                Debug.Log ("Deleted " + oldFiles.Length + " old files from " + m_userdataPath);

            } else {

                int count = Directory.GetFiles (m_userdataPath, "*.csv", SearchOption.AllDirectories).Length;

                // Rename userdata file before creating a new one
                if (File.Exists (m_userdataPath + "/UserData.txt")) {
                    File.Move (m_userdataPath + "/UserData.txt", m_userdataPath + $"/UserData_{getTimeStamp()}.txt");
                }

                if (count > 1) {
                    m_currentTrialIdx = count / 2;
                }
            }
        }

        print (m_userdataPath);
        // Create new folder with subject ID
        Directory.CreateDirectory (m_userdataPath);
        // User information: basic data + playlist
        m_recorder_info = new StreamWriter (m_userdataPath + "/UserData.txt");
        // m_recorder_question = new StreamWriter (m_userdataPath + "/UserData.txt");
        m_recorder_question = new StreamWriter (m_userdataPath + "/Answers.csv", true);

        if (m_recorder_question.BaseStream.CanWrite) {
            m_recorder_question.WriteLine ("UnityTS,LabID,ParticipantID,TrialID,Room,Instruction,Duration,Question,Answer");
            // RoomManager.instance.currentRoomName, currentEmotTrial.duration, m_currentTrialIdx, txt);
            m_recorder_question.Flush ();
        }

        // get playlist for user ID --- we begin with user ID 1, but start with element 0 of the list of playlists.
        // SetUserPlaylist (m_userId - 1);
        SetUserPlaylistFromCSVs (m_userId - 1);

        // setTaskList ();

        // Record some protocol information
        WriteInfo ("User_ID: " + m_userId);
        // writeInfo("Stimuli order, room name, target idx, scotoma condition:");
        WriteInfo ("Room name, Duration, Order:");
        // foreach (playlistElement elp in playlist)
        //     writeInfo($"{elp.expName} - quest_{elp.task_idx}");
        foreach (EmotPlaylistElement ple in emotPlaylist)
            WriteInfo ($"{ple.expNameCSV}");
        FlushInfo ();
    }

    // private bool calibrating = false;
    private bool conCal = false;
    private bool trackCal = false;
    Coroutine runningRoutine;

    public void CalibrateByController () {
        if (!trackCal) {

            if (!conCal) {
                conCal = true;
                controllerCalButton.colors = SwapColors (controllerCalButton.colors);
                runningRoutine = StartCoroutine (ControllerPositioning ());
            } else {
                StopCoroutine (runningRoutine);
                _instructBehaviour.ResetRadialProgresses ();
                _instructBehaviour.toggleControllerInstruction (false);
                controllerCalButton.colors = SwapColors (controllerCalButton.colors);
                conCal = false;
                LoadCamRigCal ();
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
    public void CalibrateByTracker () {
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

    IEnumerator ControllerPositioning () {

        if (calPointF != null) calPointF.SetActive (true);

        controllerBasePoint.SetActive (true);

        Vector3 newPos = new Vector3 ();

        bool positioned = false;

        _instructBehaviour.toggleControllerInstruction (true);
        _instructBehaviour.setInstruction ("Using the trigger, place the controller's base on the floor.\n\n" + "Confirm with the touchpad.");

        // Get first point: floor
        while (!positioned) {
            if (userClickedPad) {
                positioned = true;
                break;
            }
            while (!userClickedTrigger && userTouchedTrigger) {
                // if (userTouchedTrigger) {
                newPos = cameraRig.position;
                newPos.y = newPos.y - (controllerBasePoint.transform.position.y - calPointF.transform.position.y);
                cameraRig.position = newPos;
                // }
                yield return null;
            }
            yield return new WaitUntil (() => !userTouchedTrigger || userClickedPad);
        }
        Debug.Log ("Calibrated floor!");
        _instructBehaviour.setInstruction ("The floor is set!");
        calPointF.SetActive (false);
        positioned = false;

        // Wait for trigger and pad release
        yield return new WaitUntil (() => !userTouchedTrigger && !userClickedPad);
        // _instructBehaviour.setInstruction("Position calibration.\n\n" + "Place the controller's base to the front right corner of the seat, then click the trigger.");
        _instructBehaviour.setInstruction ("Using the trigger, place the controller's base on the seat's front right corner.\n\n" + "Confirm with the touchpad.");

        if (calPointA != null) calPointA.SetActive (true);

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
            yield return new WaitUntil (() => !userTouchedTrigger || userClickedPad);
        }
        Debug.Log ("Calibrated first corner!");
        _instructBehaviour.setInstruction ("The seat's position is set!");
        calPointA.SetActive (false);
        positioned = false;

        // Wait for trigger and pad release
        yield return new WaitUntil (() => !userTouchedTrigger && !userClickedPad);
        // _instructBehaviour.setInstruction("Rotation calibration.\n\n" + "Place the controller's base to the front left corner of the seat, then click the trigger.");
        _instructBehaviour.setInstruction ("Using the trigger, place the controller's base on the seat's front left corner.\n\n" + "Confirm with the touchpad.");

        if (calPointB != null) calPointB.SetActive (true);

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
                angleBetween = Vector3.SignedAngle (Vector3.left, horizontalControllerPos - firstCornerPos, Vector3.up);
                // Debug.Log("Signed angle: " + angleBetween);
                cameraRig.transform.RotateAround (firstCornerPos, Vector3.up, -angleBetween);
                yield return null;
            }
            yield return new WaitUntil (() => !userTouchedTrigger || userClickedPad);
        }
        Debug.Log ("Calibrated second corner!");
        _instructBehaviour.setInstruction ("The seat's rotation is set!");
        calPointB.SetActive (false);
        positioned = false;

        // Wait for trigger and pad release
        yield return new WaitUntil (() => !userTouchedTrigger && !userClickedPad);
        controllerBasePoint.SetActive (false);

        float triggerClickTime = 0.0f;

        _instructBehaviour.setInstruction ("Click and hold the trigger to save the calibration, or click the touchpad to abort.");
        // _radialProgress.gameObject.SetActive(true);
        while (triggerClickTime < durationToContinue) {
            if (userClickedPad) {
                _instructBehaviour.setInstruction ("Aborting!\n\n" + "Loading previous calibration.");
                yield return new WaitUntil (() => !userClickedPad);
                // LoadCamRigCal();
                // _instructBehaviour.ResetRadialProgresses();
                // _instructBehaviour.toggleControllerInstruction(false);
                CalibrateByController ();
                yield break;
            }
            if (userClickedTrigger) {
                triggerClickTime += Time.deltaTime;
                _instructBehaviour.SetRadialProgresses (triggerClickTime / durationToContinue);
            } else {
                _instructBehaviour.ResetRadialProgresses ();
                triggerClickTime = 0;
            }
            yield return null;
        }

        SaveCamRigCal ();

        _radialProgress.SetProgress (1);
        _instructBehaviour.setInstruction ("Calibration saved!");

        yield return new WaitUntil (() => !userTouchedTrigger);
        _instructBehaviour.ResetRadialProgresses ();
        // _radialProgress.gameObject.SetActive(false);
        _instructBehaviour.toggleControllerInstruction (false);
        toggleMessage (false);

        CalibrateByController ();

        // yield return new WaitUntil(() => !calibrating);
        Debug.Log ("Done calibrating!");
    }

    private void SaveCamRigCal () {
        PlayerPrefs.SetFloat ("pX", cameraRig.position.x);
        PlayerPrefs.SetFloat ("pY", cameraRig.position.y);
        PlayerPrefs.SetFloat ("pZ", cameraRig.position.z);
        // PlayerPrefs.SetFloat("rW", cameraRig.rotation.w);
        PlayerPrefs.SetFloat ("rX", cameraRig.eulerAngles.x);
        PlayerPrefs.SetFloat ("rY", cameraRig.eulerAngles.y);
        PlayerPrefs.SetFloat ("rZ", cameraRig.eulerAngles.z);
        Debug.Log ("Saved Camera Rig calibration.");
    }

    private void LoadCamRigCal () {
        float pX = PlayerPrefs.GetFloat ("pX");
        float pY = PlayerPrefs.GetFloat ("pY");
        float pZ = PlayerPrefs.GetFloat ("pZ");
        // float rW = PlayerPrefs.GetFloat("rW");
        float rX = PlayerPrefs.GetFloat ("rX");
        float rY = PlayerPrefs.GetFloat ("rY");
        float rZ = PlayerPrefs.GetFloat ("rZ");
        cameraRig.position = new Vector3 (pX, pY, pZ);
        cameraRig.eulerAngles = new Vector3 (rX, rY, rZ);
    }

    private ColorBlock SwapColors (ColorBlock colors) {
        Color normal = colors.normalColor;
        Color pressed = colors.pressedColor;
        colors.normalColor = pressed;
        colors.selectedColor = pressed;
        colors.highlightedColor = pressed;
        colors.pressedColor = normal;
        return colors;
    }

    public void WriteInfo (string txt) {
        // print (txt);

        if (m_recorder_info.BaseStream.CanWrite)
            m_recorder_info.WriteLine ("{0}: {1}", getTimeStamp (), txt);
    }

    public string CondenseString (string input) {
        string condensedString = "";
        string[] stringParts = input.Split (' ');
        if (stringParts.Length > 1) {
            foreach (string part in stringParts) {
                if (part == " " || part == "_" || part == "-" || part == "?" || part == "," || part == ".")
                    continue;
                else
                    condensedString += part;
            }
        } else {
            condensedString = input;
        }
        return condensedString;
    }

    public void WriteAnswer (string question, string answer) {
        if (m_recorder_question.BaseStream.CanWrite) {
            m_recorder_question.WriteLine ("{0},{1},{2},{3}", getTimeStamp (), currentEmotTrial.expNameCSV, CondenseString (question), answer);
            // RoomManager.instance.currentRoomName, currentEmotTrial.duration, m_currentTrialIdx, txt);
            m_recorder_question.Flush ();
            Debug.Log ("Wrote answer: " + answer);
        }
    }
    public void FlushInfo () {
        if (m_recorder_info.BaseStream.CanWrite)
            m_recorder_info.Flush ();
    }

    private IDictionary<string, string> tasks;

    // public string currentTaskString => tasks[$"{currentTrial.room_idx}.{currentTrial.task_idx}"];
    // public string currentTaskString => tasks[$"{currentEmotTrial.roomName}.{currentEmotTrial.task_idx}"];

    public string currentTaskString = "Example task string.";

    private void setTaskList () {
        tasks = new Dictionary<string, string> ();
        var lines = File.ReadLines (
            Directory.GetParent (Application.dataPath) + "/SubjectData/questions.csv");
        foreach (var line in lines) {
            string[] linesplit = line.Split (',');
            tasks.Add (linesplit[0], linesplit[1]);
            print (line);
        }
    }
    private void setTaskListErwan () {
        tasks = new Dictionary<string, string> ();
        var lines = File.ReadLines (
            Directory.GetParent (Application.dataPath) + "/SubjectData/questions.csv");
        foreach (var line in lines) {
            string[] linesplit = line.Split (',');
            tasks.Add (linesplit[0], linesplit[1]);
            print (line);
        }
    }

    private void SetUserPlaylist (int idx) {
        // int max_idx = 100;
        if (idx > max_idx) {
            Debug.LogError ($"User index cannot be over {max_idx}.", this);
            Quit ();
        }

        List<string> roomNames = RoomManager.instance.ListRooms ();

        // TEMPORARY, EASY PLAYLIST CREATION (NO RANDOMIZATION)
        for (int i = 0; i < durations.Count; i++) {
            for (int j = 0; j < roomNames.Count; j++) {
                // Debug.Log(roomName + ", available: " + RoomManager.instance.isRoomAvailable(roomName));
                int trial = i * roomNames.Count + j + 1;
                emotPlaylist.Add (new EmotPlaylistElement (roomNames[j], durations[i], trial, "trial", "SGL", 0));
            }
        }
    }

    private void SetUserPlaylistFromCSVs (int idx) {

        if (idx > allPlaylists.Count) {
            Debug.LogError ($"No playlist exists for user {idx}. The currently loaded file only holds {allPlaylists.Count} playlists.", this);
            Quit ();
        }

        foreach (EmotPlaylistElement eElement in allPlaylists[idx]) {
            emotPlaylist.Add (eElement);
        }
    }

    private void LoadPlaylistsFromCSVs () {

        string playlistName;
        string testKind;

        if (preTesting)
            testKind = "Pre";
        else
            testKind = "Full";

        if (tobiiTracking) {
            Debug.Log ("We are at SGL.");
            playlistName = $"/SubjectData/Playlists/{testKind}-1-Training.csv";
        } else {
            Debug.Log ("We are at HfG.");
            playlistName = $"/SubjectData/Playlists/{testKind}-2-Training.csv";
        }

        // StreamReader file = new StreamReader (Directory.GetParent (Application.dataPath) + playlistName, Encoding.UTF8);

        List<List<string[]>> participants = new List<List<string[]>> (90);

        var lines = File.ReadLines (Directory.GetParent (Application.dataPath) + playlistName);
        // Debug.Log ("Number of lines: " + lines.Count ());
        int lineCounter = 0;
        List<string[]> participant = new List<string[]> (90);
        string toPrint = "";
        foreach (var line in lines) {
            switch (lineCounter % 3) {
                case 0:
                    // Debug.Log ("Creating a new playlist for participant " + ((lineCounter / 3) + 1));
                    participant = new List<string[]> (90);
                    // string[] linesplit = line.Split (',');
                    participant.Add (line.Split (','));
                    toPrint = "Printing var1 line ";
                    break;
                case 1:
                    toPrint = "Printing var2 line ";
                    // linesplit = line.Split (',');
                    participant.Add (line.Split (','));
                    break;
                case 2:
                    toPrint = "Printing time line ";
                    // string[] linesplit = line.Split (',');
                    participant.Add (line.Split (','));
                    participants.Add (participant);
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

            List<EmotPlaylistElement> ePlaylist = new List<EmotPlaylistElement> (90);
            for (int i = 0; i < person[0].Length; i++) {

                // Durations from array defined in Editor

                int.TryParse (person[2][i], out int durationIdx);
                int duration = durations[durationIdx - 1];

                string room = "BreakRoom";
                string inst = "Chill";
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
                        inst = "sit-SGL";
                        // Variable two, second level: stand up (specifically for SGL)
                    } else {
                        inst = "stand-SGL";
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
                        inst = "sit-HfG";
                        // Variable two, second level: lean on the high bench (specifically for HfG)
                    } else {
                        inst = "lean-HfG";
                    }
                }
                ePlaylist.Add (new EmotPlaylistElement (room, duration, i, inst, labo, j));
                // EmotPlaylistElement pElement = new EmotPlaylistElement (room, duration, i, inst, labo);
            }
            allPlaylists.Add (ePlaylist);
        }
        Debug.Log ("Length of allPlaylist: " + allPlaylists.Count ());

        // foreach (var pEl in allPlaylists[0]) {
        //     Debug.Log (pEl.expName);
        // }
    }

    private void setUserPlaylistErwan (int idx) {
        // int max_idx = 100;
        if (idx > max_idx) {
            Debug.LogError ($"User index cannot be over {max_idx}.", this);
            Quit ();
        }

        StreamReader file = new StreamReader (Directory.GetParent (Application.dataPath) +
            "/SubjectData/playlist.csv", Encoding.UTF8);

        int nrep = 14;
        int linesize = 104 + 1;
        // Number of characters per line plus line return

        // Read line according to the user ID number
        char[] lineChar = new char[linesize - 1];
        file.BaseStream.Position = idx * linesize;
        file.Read (lineChar, 0, lineChar.Length);
        file.Close ();

        // Convert line from char[] to string
        string line = new string (lineChar);
        // Split line by commas
        string[] ell = line.Split (',');
        // For all element in list
        for (int i = 0; i < ell.Length; i++) {
            // Split by '-' 
            string[] els = ell[i].Split ('-');

            // Debug.Log (ell[i]);

            // 0: Scene, 1: light cond, 2: Task
            int.TryParse (els[0], out var room_idx);
            int.TryParse (els[1], out var light_cond);
            int.TryParse (els[2], out var quest_idx);

            // new playlistElement to insert in playlist
            playlist.Add (new playlistElement (room_idx, light_cond, quest_idx, i));
        }

        print ($"playlist.Count: {playlist.Count}");
    }

    private bool m_isPresenting;

    public bool userGrippedControl => TrackPadInput.instance.SideGripped ();
    public bool userTouchedPad => TrackPadInput.instance.TrackpadTouched ();
    public bool userClickedPad => TrackPadInput.instance.TrackpadClicked ();
    public bool userTouchedTrigger => TrackPadInput.instance.TriggerTouched ();
    public bool userClickedTrigger => TrackPadInput.instance.TriggerClicked ();

    float taskTime = 0;
    float padPressedTime = 0;

    public void SetTobiiTracking (bool tobTrack) {

        // Debug.Log ("Requesting Lab change. SGL? " + tobTrack);

        if (tobiiTracking == tobTrack) {
            Debug.Log ("Pressed already active Lab button");
            GetLastUserNumber ();
            return;
        }

        tobiiTracking = tobTrack;
        sglButton.colors = SwapColors (sglButton.colors);
        hfgButton.colors = SwapColors (hfgButton.colors);
        PlayerPrefs.SetInt ("tobii", (tobiiTracking ? 1 : 0));

        m_labID = tobiiTracking ? "SGL" : "HfG";
        GetLastUserNumber ();
    }

    // THE ACTUAL GAME LOOP!

    IEnumerator Start () {

        taskTime = 0;
        padPressedTime = 0;

        shaderBehavior.phase = ShaderBehaviour.shaderPhase.none;

        tobiiTracking = PlayerPrefs.GetInt ("tobii", 0) != 0;

        Debug.Log ("Read from prefs: Tobii Tracking = " + tobiiTracking);

        if (tobiiTracking) {
            sglButton.colors = SwapColors (sglButton.colors);
            Debug.Log ("Swapped SGL Button");
        } else {
            hfgButton.colors = SwapColors (hfgButton.colors);
            Debug.Log ("Swapped HFG Button");
        }

        if (eyeTracking)
            yield return new WaitUntil (() => _eyeTrack.ready);
        else
            yield return new WaitForSeconds (1);

        // Debug.Log("Passed _eyeTrack.ready check");

        // Show SubjInfo panel
        setupPanel.SetActive (true);
        pausePanel.SetActive (false);
        // questionPanel.SetActive(false);
        _progressBar.gameObject.SetActive (false);
        // _radialProgress.gameObject.SetActive(false);
        _questionSlider.gameObject.SetActive (false);

        // _questionSlider.gameObject.SetActive(true);
        RoomManager.instance.SaveManagerSceneNum ();
        RoomManager.instance.LoadBreakRoom ();
        yield return new WaitUntil (() => !(RoomManager.instance.actionInProgress));

        calPointA = GameObject.Find ("CalPointA");
        calPointB = GameObject.Find ("CalPointB");
        calPointF = GameObject.Find ("CalPointF");

        if (calPointA != null) {
            calPointA.SetActive (false);
            calPointB.SetActive (false);
            calPointF.SetActive (false);
        }

        trainSpawner = GameObject.FindObjectOfType<TrainSpawner> ();
        if (trainSpawner != null) {
            // trainSpawner.SpawnTrain();
        }

        LoadCamRigCal ();

        Debug.Log ("Loaded Camera Rig Position");

        _instructBehaviour.toggleControllerInstruction (true);
        _instructBehaviour.setInstruction ("Press any button on this controller (trigger, side button, or trackpad).");
        yield return new WaitUntil (() => _instructBehaviour.deactivatedOtherController);
        _instructBehaviour.setInstruction ("The other controller has been disabled!");
        if (userClickedPad)
            yield return new WaitUntil (() => !userClickedPad);
        if (userClickedTrigger)
            yield return new WaitUntil (() => !userTouchedTrigger);
        if (userGrippedControl)
            yield return new WaitUntil (() => !userGrippedControl);
        _instructBehaviour.toggleControllerInstruction (false);

        // Wait for user ID --- Setup() happens here!
        yield return new WaitUntil (() => !setupPanel.activeSelf);
        _instructBehaviour.toggleControllerInstruction (false);

        // Settings from the setup panel have been submitted: start of the trial

        // TO DO: Integrate this with generated / read conditions of the playlist
        // HfG: Request to stand or lean depending on user ID
        // SGL: Request to stand or sit depending on user ID (tobiiTracking means SGL)
        if (m_userId % 2 != 0) {
            Debug.Log ("Odd user ID (" + m_userId + ").");
            if (tobiiTracking)
                toggleMessage (true, "pleaseStand");
            else
                toggleMessage (true, "pleaseLean");
            // _instructBehaviour.toggleWorldInstruction(true, "Please lean on the bench.");
        } else {
            Debug.Log ("Even user ID (" + m_userId + ").");
            toggleMessage (true, "pleaseSit");
        }
        // _instructBehaviour.toggleWorldInstruction(true, "This is a test instruction.\n\nPress trigger.");
        _instructBehaviour.RequestConfirmation (durationToContinue);
        yield return new WaitUntil (() => !_instructBehaviour.requested);
        yield return new WaitForSecondsRealtime (1.0f);
        _instructBehaviour.toggleWorldInstruction (false);
        yield return new WaitForSecondsRealtime (1.0f);

        // First questions: demographics

        ToggleQuestion (true, "How many hours have you spent in VR so far in your life?");
        _questionSlider.UpdateSliderRange (0, 4, true, false, "0", "1--5h", ">20h", "",
            "<1h", "5--20h");
        yield return new WaitUntil (() => _questionSlider.confirmed);
        yield return new WaitForSecondsRealtime (1.0f);
        ToggleQuestion (false);
        yield return new WaitForSecondsRealtime (1.0f);

        ToggleQuestion (true, "How often do you normally use public transport?");
        _questionSlider.UpdateSliderRange (0, 4, true, false, "never", "monthly", "daily", "",
            "yearly", "weekly");
        yield return new WaitUntil (() => _questionSlider.confirmed);
        yield return new WaitForSecondsRealtime (1.0f);
        ToggleQuestion (false);
        yield return new WaitForSecondsRealtime (1.0f);

        ToggleQuestion (true, "How patient would you consider yourself?");
        _questionSlider.UpdateSliderRange (0, 99, true, false, "not at all", " ", "very patient");
        yield return new WaitUntil (() => _questionSlider.confirmed);
        yield return new WaitForSecondsRealtime (1.0f);
        ToggleQuestion (false);
        yield return new WaitForSecondsRealtime (1.0f);

        toggleMessage (true, "pleaseStand");
        _instructBehaviour.RequestConfirmation (durationToContinue);
        yield return new WaitUntil (() => !_instructBehaviour.requested);
        yield return new WaitForSecondsRealtime (1.0f);
        _instructBehaviour.toggleWorldInstruction (false);
        yield return new WaitForSecondsRealtime (1.0f);

        if (!eyeTracking) {
            shaderBehavior.gameObject.SetActive (false);

            // Eye Tracking Setup
            //
            // TO DO: separate calibration from validation; add validation to VivePro Eye routine.
            //
        } else {
            if (!tobiiTracking) {
                toggleMessage (true, "pleaseCalibrateVive");
                yield return new WaitForSeconds (5);
                _instructBehaviour.RequestConfirmation (durationToContinue);
                yield return new WaitUntil (() => !_instructBehaviour.requested);
                yield return new WaitForSecondsRealtime (1.0f);
                _instructBehaviour.toggleWorldInstruction (false);
                yield return new WaitForSecondsRealtime (1.0f);

                // Tobii eye tracker
            } else {
                eyeValidated = false;
                eyeCalibrated = false;
                StartCoroutine (TobiiCalibration ());
                yield return new WaitUntil (() => eyeValidated);
            }
        }

        // Introduce Interaction
        if (controllerTutorial) {

            // toggleMessage(false);
            yield return new WaitForSecondsRealtime (0.5f);
            toggleMessage (true, "Let's learn the VR controls.\n\nTake a look at your controller, and pull its trigger.");
            yield return new WaitForSecondsRealtime (1.0f);

            // _instructBehaviour.setInstruction("Click the controller's trigger, then release it.");
            yield return new WaitUntil (() => userClickedTrigger);
            Debug.Log ("Successfully triggered.");
            _instructBehaviour.setInstruction ("Good!\n\nNow fully release the trigger.");
            yield return new WaitUntil (() => !userTouchedTrigger);
            _instructBehaviour.setInstruction ("You did it!");
            yield return new WaitForSecondsRealtime (1f);
            // _instructBehaviour.toggleControllerInstruction(false);
            _instructBehaviour.toggleWorldInstruction (false);
            yield return new WaitForSecondsRealtime (0.25f);

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
            toggleMessage (true, "Click the controller's touch pad, then release it.");
            yield return new WaitUntil (() => userClickedPad);
            Debug.Log ("Successfully clicked touchpad.");
            _instructBehaviour.setInstruction ("Good!\n\nNow let go of the touchpad.");
            yield return new WaitUntil (() => !userTouchedPad);
            _instructBehaviour.setInstruction ("Well done!");
            yield return new WaitForSecondsRealtime (1f);
            _instructBehaviour.toggleWorldInstruction (false);
            // _instructBehaviour.toggleControllerInstruction(false);

            yield return new WaitForSecondsRealtime (0.25f);

            // _instructBehaviour.toggleControllerInstruction(true);
            toggleMessage (true, "Press and hold the touch pad until the bar fills, then release it.");
            while (padPressedTime < durationToContinue) {
                if (userClickedPad) {
                    padPressedTime += Time.deltaTime;
                    _progressBar.gameObject.SetActive (true);
                    _progressBar.SetProgress (padPressedTime / durationToContinue);
                } else {
                    _progressBar.gameObject.SetActive (false);
                    padPressedTime = 0;
                }
                yield return null;
            }
            yield return new WaitUntil (() => !userTouchedPad);
            _progressBar.gameObject.SetActive (false);
            // _instructBehaviour.toggleControllerInstruction(false);
            toggleMessage (false);

            toggleMessage (false);
            yield return new WaitForSecondsRealtime (0.5f);
            toggleMessage (true, "Now let's practice the questionnaires.\n\nPress the touch pad again to continue.");
            yield return new WaitForSecondsRealtime (0.25f);
            yield return new WaitUntil (() => userClickedPad);
            toggleMessage (false);
            yield return new WaitForSecondsRealtime (0.5f);
        }

        // Introduce slider interaction
        if (questionnaireTutorial) {
            // Time Format
            ToggleQuestion (true, "How long have you been here, in VR, so far?");
            _questionSlider.UpdateSliderRange (10, 300, false, true);
            yield return new WaitUntil (() => _questionSlider.confirmed);
            yield return new WaitForSecondsRealtime (1.0f);
            ToggleQuestion (false);
            yield return new WaitForSecondsRealtime (1.0f);

            // Visual Analog Scale
            ToggleQuestion (true, "What is your feeling toward this environment?");
            _questionSlider.UpdateSliderRange (1, 100, true, false, "bad", "indifferent", "good");
            yield return new WaitUntil (() => _questionSlider.confirmed);
            yield return new WaitForSecondsRealtime (1.0f);
            ToggleQuestion (false);
            yield return new WaitForSecondsRealtime (1.0f);

            // SAM Scale: valence
            ToggleQuestion (true, "What is your valence toward this environment?");
            _questionSlider.UpdateSliderRange (1, 100, true, false, "does", "not", "matter", "v");
            yield return new WaitUntil (() => _questionSlider.confirmed);
            yield return new WaitForSecondsRealtime (1.0f);
            ToggleQuestion (false);
            yield return new WaitForSecondsRealtime (1.0f);

            // SAM Scale: arousal
            ToggleQuestion (true, "What is your arousal toward this environment?");
            _questionSlider.UpdateSliderRange (1, 100, true, false, "does", "not", "matter", "a");
            yield return new WaitUntil (() => _questionSlider.confirmed);
            yield return new WaitForSecondsRealtime (1.0f);
            ToggleQuestion (false);
            yield return new WaitForSecondsRealtime (1.0f);

            // Discrete Scale
            // ToggleQuestion(true, "How would you rate this experience on a 1-to-5 scale?");
            // _questionSlider.UpdateSliderRange(1, 5);
            // yield return new WaitUntil(() => _questionSlider.confirmed);
            // yield return new WaitForSecondsRealtime(1.0f);
            // ToggleQuestion(false);

            yield return new WaitForSecondsRealtime (0.5f);
            toggleMessage (true, "You did great!\n\nPress the side button again to start.");
        }

        Debug.Log ("Currently playing trial " + (m_currentTrialIdx + 1) + " out of " + emotPlaylist.Count);

        while (m_currentTrialIdx < emotPlaylist.Count) {
            toggleMessage (true, "unloading");
            Debug.Log ("Starting room unload...");

            // RoomManager.instance.UnloadScene();
            RoomManager.instance.UnloadRoom ();
            yield return new WaitUntil (() => !(RoomManager.instance.actionInProgress));
            toggleMessage (false);
            _instructBehaviour.toggleControllerInstruction (false);
            Debug.Log ("Room unload finished.");

            int trialidx = currentEmotTrial.trial_idx;

            // condObjects.Clear();

            // Skip trial if the station scene is not finished
            // if (!RoomManager.instance.isRoomAvailable(currentTrial.room_idx)) { m_currentTrialIdx++; continue; }

            long timeSpentLoading = getTimeStamp ();
            toggleMessage (true, "loading");
            // RoomManager.instance.LoadScene(currentTrial.room_idx);
            RoomManager.instance.LoadRoom (currentEmotTrial.roomName);

            WriteInfo (RoomManager.instance.currSceneName);
            yield return new WaitUntil (() => !RoomManager.instance.actionInProgress &&
                RoomManager.instance.currentScene.isLoaded);
            yield return null;
            toggleMessage (false);

            trainSpawner = GameObject.FindObjectOfType<TrainSpawner> ();

            taskTime = 0;
            padPressedTime = 0;

            if (RoomManager.instance.currSceneName == RoomManager.instance.breakRoomName && eyeTracking) {
                // BREAK ROOM: do calibration and continue to next level

                // toggleMessage(true, "calibrate");
                toggleMessage (true, "takeBreak");
                // yield return new WaitUntil(() => userGrippedControl || Input.GetKeyUp(KeyCode.Space));

                // Wait until the user presses a special combination of inputs to stop the trial
                while (padPressedTime < durationToContinue) {
                    if (userClickedPad) {
                        padPressedTime += Time.deltaTime;
                        _progressBar.gameObject.SetActive (true);
                        _progressBar.SetProgress (padPressedTime / durationToContinue);
                    } else {
                        _progressBar.gameObject.SetActive (false);
                        padPressedTime = 0;
                    }
                    yield return null;
                }
                _progressBar.gameObject.SetActive (false);
                toggleMessage (false);

            } else {
                // REGULAR TRIAL ROOM

                // _instructBehaviour.toggleWorldInstruction(false);

                // Update all info panels with the new trial question (there can be more than one question for a same scene)
                // _instructBehaviour.setInstruction(currentTaskString);

                toggleMessage (true, "beginWaitingSit");

                // Wait till user presses a special combination of inputs to stop the trial
                while (padPressedTime < durationToContinue) {
                    if (userClickedPad) {
                        padPressedTime += Time.deltaTime;
                        _progressBar.gameObject.SetActive (true);
                        _progressBar.SetProgress (padPressedTime / durationToContinue);
                    } else {
                        _progressBar.gameObject.SetActive (false);
                        padPressedTime = 0;
                    }
                    yield return null;
                }
                _progressBar.gameObject.SetActive (false);

                toggleMessage (true, "three");
                yield return new WaitForSeconds (1);
                toggleMessage (true, "two");
                yield return new WaitForSeconds (1);
                toggleMessage (true, "one");
                yield return new WaitForSeconds (1);
                toggleMessage (false);

                // Start new gaze record (record name = stimulus name)
                if (eyeTracking) {
                    if (tobiiTracking)
                        startNewRecord ();
                    else
                        _eyeTrack.startNewRecord ();
                    Debug.Log ("Started eye tracking.");
                }
                // Start trial
                m_isPresenting = true;
                long start_time = getTimeStamp ();

                if (debugging) {
                    foreach (var lightCond in LightConditions) {
                        // yield return new WaitForSecondsRealtime(1);
                        yield return new WaitUntil (() => userGrippedControl || Input.GetKeyUp (KeyCode.Space));
                        yield return null; // Leave time for key up event to disappear
                        // setLights (lightCond);
                    }
                    yield return new WaitUntil (() => userGrippedControl || Input.GetKeyUp (KeyCode.Space));
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
                    trainSpawner.Invoke ("SpawnTrain", currentEmotTrial.duration - trainSpawner.DelayToOpenDoors ());

                // Wait until trial time runs out or touchpad pressed
                padPressedTime = 0;
                while (taskTime < currentEmotTrial.duration && padPressedTime < durationToContinue) {
                    taskTime += Time.deltaTime;

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
                _progressBar.gameObject.SetActive (false);

                if (padPressedTime >= durationToContinue)
                    Debug.Log ("Finished from pad press.");
                else
                    Debug.Log ("Finished from expired waiting duration.");

                // Stop recording gaze
                if (eyeTracking)
                    if (tobiiTracking)
                        stopRecord (getTimeStamp () - start_time);
                    else
                        _eyeTrack.stopRecord (getTimeStamp () - start_time);

                m_isPresenting = false;

                _instructBehaviour.setInstruction ("Please wait");

                Debug.Log ($"Finished: {currentEmotTrial.expName} - {trialidx}");

                toggleMessage (true, "beginQuestions");

                yield return new WaitForSeconds (1);
                yield return new WaitUntil (() => userGrippedControl);

                toggleMessage (false);

                // THIS IS THE QUESTION BLOCK

                yield return new WaitForSecondsRealtime (0.5f);
                ToggleQuestion (true, "How long do you think you have been waiting?");
                _questionSlider.UpdateSliderRange (30, 300, false, true);
                yield return new WaitUntil (() => _questionSlider.confirmed);
                yield return new WaitForSecondsRealtime (1.0f);
                ToggleQuestion (false);

                // SAM Scale: valence
                ToggleQuestion (true, "Which of these pictures represents your emotional state best?");
                _questionSlider.UpdateSliderRange (1, 100, true, false, "does", "not", "matter", "v");
                yield return new WaitUntil (() => _questionSlider.confirmed);
                yield return new WaitForSecondsRealtime (1.0f);
                ToggleQuestion (false);
                yield return new WaitForSecondsRealtime (1.0f);

                // SAM Scale: arousal
                ToggleQuestion (true, "Which of these pictures represents your excitement best?");
                _questionSlider.UpdateSliderRange (1, 100, true, false, "does", "not", "matter", "a");
                yield return new WaitUntil (() => _questionSlider.confirmed);
                yield return new WaitForSecondsRealtime (1.0f);
                ToggleQuestion (false);
                yield return new WaitForSecondsRealtime (1.0f);

                yield return new WaitForSecondsRealtime (0.5f);
                ToggleQuestion (true, "How much did you think about\nyour PAST while waiting?");
                _questionSlider.UpdateSliderRange (1, 100, true, false, "not at all", " ", "all the time");
                yield return new WaitUntil (() => _questionSlider.confirmed);
                yield return new WaitForSecondsRealtime (1.0f);
                ToggleQuestion (false);

                yield return new WaitForSecondsRealtime (0.5f);
                ToggleQuestion (true, "How much did you think about\nyour PRESENT while waiting?");
                _questionSlider.UpdateSliderRange (1, 100, true, false, "not at all", " ", "all the time");
                yield return new WaitUntil (() => _questionSlider.confirmed);
                yield return new WaitForSecondsRealtime (1.0f);
                ToggleQuestion (false);

                yield return new WaitForSecondsRealtime (0.5f);
                ToggleQuestion (true, "How much did you think about\nyour FUTURE while waiting?");
                _questionSlider.UpdateSliderRange (1, 100, true, false, "not at all", " ", "all the time");
                yield return new WaitUntil (() => _questionSlider.confirmed);
                yield return new WaitForSecondsRealtime (1.0f);
                ToggleQuestion (false);

                yield return new WaitForSecondsRealtime (0.5f);
                ToggleQuestion (true, "How intensively did you experience\nYOUR BODY most of the time?");
                _questionSlider.UpdateSliderRange (1, 100, true, false, "not at all", " ", "very intensively");
                yield return new WaitUntil (() => _questionSlider.confirmed);
                yield return new WaitForSecondsRealtime (1.0f);
                ToggleQuestion (false);
                yield return new WaitForSecondsRealtime (1.0f);

                yield return new WaitForSecondsRealtime (0.5f);
                ToggleQuestion (true, "How intensively did you experience\nTHE SURROUNDING SPACE most of the time?");
                _questionSlider.UpdateSliderRange (1, 100, true, false, "not at all", " ", "very intensively");
                yield return new WaitUntil (() => _questionSlider.confirmed);
                yield return new WaitForSecondsRealtime (1.0f);
                ToggleQuestion (false);

                yield return new WaitForSecondsRealtime (0.5f);
                ToggleQuestion (true, "How often did you think about time?");
                _questionSlider.UpdateSliderRange (1, 100, true, false, "not at all", " ", "extremely often");
                yield return new WaitUntil (() => _questionSlider.confirmed);
                yield return new WaitForSecondsRealtime (1.0f);
                ToggleQuestion (false);

                yield return new WaitForSecondsRealtime (0.5f);
                ToggleQuestion (true, "How fast did time pass for you?");
                _questionSlider.UpdateSliderRange (1, 100, true, false, "extremely slowly", " ", "extremely fast");
                yield return new WaitUntil (() => _questionSlider.confirmed);
                yield return new WaitForSecondsRealtime (1.0f);
                ToggleQuestion (false);

                // THIS IS THE END OF THE QUESTION BLOCK

                trainSpawner.DepartTrain ();
                yield return new WaitForSecondsRealtime (2.0f);

                toggleMessage (true, "endQuestions");
                yield return new WaitForSecondsRealtime (3.0f);

            }

            m_currentTrialIdx++;
            FlushInfo ();
        }

        Debug.Log ("Experiment concluded. Quitting...");

        toggleMessage (true, "end");
        yield return new WaitUntil (() => userGrippedControl || Input.GetKeyUp (KeyCode.Space));
        toggleMessage (false);

        FlushInfo ();
        Quit ();
    }

    bool paused;
    private void toggleMessage (bool state, string message = "") {

        paused = state;
        pausePanel.SetActive (paused);
        Text msgHolder = pausePanel.transform.Find ("ContentTxt").GetComponent<Text> ();

        if (!messages.ContainsKey (message)) {
            // message = "pause";
            msgHolder.text = message;
        } else {
            msgHolder.text = messages[message];
        }
        // paused = state;
        // pausePanel.SetActive(paused);
        // Text msgHolder = pausePanel.transform.Find("ContentTxt").GetComponent<Text>();
        // string messageText = messages[message];
        // msgHolder.text = messageText;
        Debug.Log (msgHolder.text);
    }

    private void ToggleQuestion (bool state, string question = "") {
        _questionSlider.gameObject.SetActive (state);
        _questionSlider.UpdateQuestionText (question);
        _questionSlider.RequestConfirmation ();
        _questionSlider.confirmed = false;
    }

    private void setLights (LightStruct cond) {
        // condObjects.ToggleLight (cond.orientation, selfregister.objectType.Orientation);
        // condObjects.ToggleLight (cond.landmark, selfregister.objectType.Landmark);
    }

    private void OnGUI () {
        string strInfo = string.Format ("FPS: {0:0.00}", 1 / Time.deltaTime);

        GUI.TextArea (new Rect (0, 200, 100, 35), strInfo);
    }

    public static long getTimeStamp () {
        // USE solution I used in Olivier Z. 's project
        return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
    }

    private void OnApplicationQuit () {
        if (eyeTracking)
            if (tobiiTracking)
                stopRecord (-1);
            else
                _eyeTrack.stopRecord (-1);

        if (m_recorder_info.BaseStream.CanWrite)
            m_recorder_info.Close ();

        if (m_recorder_question.BaseStream.CanWrite)
            m_recorder_question.Close ();

        if (m_recorder_HMD.BaseStream.CanWrite)
            m_recorder_HMD.Close ();

        if (m_recorder_ET.BaseStream.CanWrite)
            m_recorder_ET.Close ();
    }

    public static void Quit () {
        print ("Quitting gracefully");
        if (instance.tobiiTracking)
            EyeTrackingOperations.Terminate ();
        Application.Quit ();

#if UNITY_EDITOR
        //Stop playing the scene
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public InputField participantIDField;
    public InputField trialIDField;
    private readonly Enum _localEnum;

    public void StartButtonClick () {
        string txt = participantIDField.text;

        if (!string.IsNullOrEmpty (txt) && !conCal && !trackCal) {
            setupPanel.SetActive (false);

            m_userId = Int16.Parse (txt);

            // Debug.Log ("User Input: " + txt);

            SetUp ();
        }
    }

    // SGL Tobii Eyetracker Additions

    private bool tobiiTracking = false;

    public class GazePoint {
        public GazePoint () // Empty ctor
        {
            LeftGaze = new VRGazeDataEye ();
            RightGaze = new VRGazeDataEye ();
            data = new VRGazeData ();
        }

        public GazePoint (IVRGazeData gaze) {
            LeftGaze = gaze.Left;
            RightGaze = gaze.Right;
            data = gaze;

            LeftCollide = null;
            RightCollide = null;

            LeftWorldRay = getGazeRay (lateralisation.left);
            RightWorldRay = getGazeRay (lateralisation.right);

            LeftLocalRay = new Ray (LeftGaze.GazeOrigin, LeftGaze.GazeDirection);
            RightLocalRay = new Ray (RightGaze.GazeOrigin, RightGaze.GazeDirection);

            LeftViewportPos = getViewportPos (lateralisation.left);
            RightViewportPos = getViewportPos (lateralisation.right);
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

        public bool valid (lateralisation later) {
            return later == lateralisation.left ? LeftGaze != null && LeftGaze.GazeRayWorldValid : RightGaze != null && RightGaze.GazeRayWorldValid;
        }

        public Vector2 getPor (lateralisation later) {
            return lateralisation.left == later ? LeftViewportPos : RightViewportPos;
        }

        public Ray getGazeRay (lateralisation later) {
            IVRGazeDataEye gp = later == lateralisation.left ? LeftGaze : RightGaze;

            return new Ray (data.Pose.Position + gp.GazeOrigin, data.Pose.Rotation * gp.GazeDirection);
        }

        public Vector2 getViewportPos (lateralisation later) {
            IVRGazeDataEye gaze = later == lateralisation.left ? LeftGaze : RightGaze;

            if (!gaze.GazeDirectionValid) return new Vector2 (Single.NaN, Single.NaN);

            Vector3 worldPosition = (later == lateralisation.left ? LeftWorldRay : RightWorldRay).GetPoint (FillCamFoV.m_distance * 20);

            Vector2 screenPos;
            if (Thread.CurrentThread != mainThread) {
                screenPos = WorldToVP (worldPosition,
                    later == lateralisation.left ? Camera.StereoscopicEye.Left : Camera.StereoscopicEye.Right);
            } else {
                screenPos = ExpeControl.instance.mainCam.WorldToViewportPoint (worldPosition,
                    later == lateralisation.left ? Camera.MonoOrStereoscopicEye.Left : Camera.MonoOrStereoscopicEye.Right);
            }

            return new Vector2 (screenPos.x, screenPos.y);
        }

        private static Vector2 WorldToVP (Vector3 worldpos, Camera.StereoscopicEye eye) {
            Matrix4x4 proj = eye == Camera.StereoscopicEye.Left ? ExpeControl.instance.camStereoProjLeft : ExpeControl.instance.camStereoProjRight;

            Vector4 worldPos = new Vector4 (worldpos.x, worldpos.y, worldpos.z, 1.0f);
            Vector4 viewPos = ExpeControl.instance.camViewMat * worldPos;
            Vector4 projPos = proj * viewPos; // ExpeControl.instance.mainCam.projectionMatrix * viewPos;
            Vector3 ndcPos = new Vector3 (projPos.x / projPos.w, projPos.y / projPos.w, projPos.z / projPos.w);
            Vector3 viewportPos = new Vector3 (ndcPos.x * 0.5f + 0.5f, ndcPos.y * 0.5f + 0.5f, -viewPos.z);

            return viewportPos;
        }
    }

    public GameObject SRAnipal;
    public VREyeTracker trackr;
    public GazePoint gazePoint = new GazePoint ();
    public ShaderBehaviour shaderBehavior;
    public delegate void samplingCallback (GazePoint gaze);
    public Dictionary<string, samplingCallback> SamplingCallbacks = new Dictionary<string, samplingCallback> ();

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
    private void startCalibration () {
        _calibration.StartCalibration (null, CalibrationCallback);
    }

    private void CalibrationCallback (bool calibrationResult) {
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
    private void Update () {
        // return;
        // To be used in this component - Coroutines are called back between "Update" and "LateUpdate"
        if (tobiiTracking) {
            RetrieveCameraData ();
            if (isSampling) {
                m_recorder_HMD.WriteLine (
                    $"{gazePoint.data.TimeStamp},{UnityTimeStamp}," +
                    $"{(gazePoint.LeftCollide != null ? gazePoint.LeftCollide.name : "None")}," +
                    $"{(gazePoint.RightCollide != null ? gazePoint.RightCollide.name : "None")}");
                // $"{(gazePoint.CombinedCollide != null ? gazePoint.CombinedCollide.name : "None")}");
                // m_recorder_HMD.Flush();
            }
        }
    }
    public Vector2[] validationHit = new Vector2[2];

    public void RetrieveCameraData () {
        Transform camTrans = mainCam.transform;

        cameraRotation = camTrans.eulerAngles;
        cameraQuaternion = camTrans.rotation;
        cameraPosition = camTrans.position;
        cameraLocalScale = camTrans.localScale;

        camStereoProjLeft = mainCam.GetStereoProjectionMatrix (Camera.StereoscopicEye.Left);
        camStereoProjRight = mainCam.GetStereoProjectionMatrix (Camera.StereoscopicEye.Right);

        gazePoint.LeftCollide = Physics.Raycast (gazePoint.LeftWorldRay, out RaycastHit hitL) ? hitL.transform : null;
        gazePoint.RightCollide = Physics.Raycast (gazePoint.RightWorldRay, out RaycastHit hitR) ? hitR.transform : null;

        if (gazePoint.LeftCollide != null && gazePoint.LeftCollide.name.Contains ("mesh"))
            gazePoint.LeftCollide = gazePoint.LeftCollide.parent;
        if (gazePoint.RightCollide != null && gazePoint.RightCollide.name.Contains ("mesh"))
            gazePoint.RightCollide = gazePoint.RightCollide.parent;

        if (Physics.Raycast (gazePoint.LeftWorldRay, out RaycastHit vL)) {
            validationHit[0] = shaderBehavior.transform.InverseTransformPoint (vL.point);
            validationHit[0].x = (validationHit[0].x + .5f) * Utils.Cam_FOV_hori;
            validationHit[0].y = (validationHit[0].y + .5f) * Utils.Cam_FOV_vert;

            //            Vector3 aa = shB.transform.InverseTransformPoint(vL.point);
            //            print($"{aa.x},{aa.y},{aa.z} -- {validationHit[0].x},{validationHit[0].y}");
        } else {
            validationHit[0] = new Vector2 (float.NaN, float.NaN);
        }
        if (Physics.Raycast (gazePoint.RightWorldRay, out RaycastHit vR)) {
            validationHit[1] = shaderBehavior.transform.InverseTransformPoint (vR.point);
            validationHit[1].x = (validationHit[1].x + .5f) * Utils.Cam_FOV_hori;
            validationHit[1].y = (validationHit[1].y + .5f) * Utils.Cam_FOV_vert;
        } else {
            validationHit[1] = new Vector2 (float.NaN, float.NaN);
        }

        camViewMat = mainCam.worldToCameraMatrix;

        UnityTimeStamp = getTimeStamp ();
    }

    // Record data
    // private StreamWriter m_recorder_ET = StreamWriter.Null;
    // public StreamWriter m_recorder_HMD = StreamWriter.Null;
    // private StreamWriter m_recorder_info = StreamWriter.Null;

    public bool isSampling;
    public long lastOcuTS;

    private void HMDGazeDataReceivedCallback (object sender, HMDGazeDataEventArgs rawGazeData) {
        long OcutimeStamp = EyeTrackingOperations.GetSystemTimeStamp ();
        // print("in");

        lastOcuTS = OcutimeStamp;

        EyeTrackerOriginPose bestMatchingPose = new EyeTrackerOriginPose (OcutimeStamp, cameraPosition, cameraQuaternion);

        VRGazeData gazeData = new VRGazeData (rawGazeData, bestMatchingPose);
        gazePoint = new GazePoint (gazeData);

        // Viewport positions
        Vector2 leftPor = gazePoint.getPor (lateralisation.left);
        Vector2 rightPor = gazePoint.getPor (lateralisation.right);
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

        // TODO: add back func startNewRecord - unblock below
        if (false && isSampling) {
            m_recorder_ET.WriteLine (
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
                $"{valL},{valR}"
            );
        }

        foreach (samplingCallback func in SamplingCallbacks.Values) {
            func (gazePoint);
        }
    }

    private bool fullTobiiCalibrationComplete = false;
    private bool eyeValidationComplete = false;

    bool eyeCalibrated = false;
    bool eyeValidated = false;
    IEnumerator FullTobiiCalibration () {

        eyeCalibrated = false;
        eyeValidated = false;

        print ("Waiting for the eyetracker to start");
        // Wait for ET server to start
        yield return new WaitUntil (() => _eyeTrackerTobii != null && _eyeTrackerTobii._eyeTracker != null);
        print ("_eyeTrackerTobii != null");

        yield return new WaitForEndOfFrame ();
        _eyeTrackerTobii._eyeTracker.HMDGazeDataReceived += HMDGazeDataReceivedCallback;

        m_ETsubscribed = true;
        print ("Eyetracker started and subscribed to");

        // Tobii calibration routine

        // shaderBehavior.phase = ShaderBehaviour.shaderPhase.none;

        m_calibrationSuccess = false;

        int calCount = 0;
        while (!m_calibrationSuccess) {

            print ("BEFORE CALIBRATION");
            // print("Press space to begin calibration routine!");
            // yield return new WaitUntil(() => Input.GetKeyUp(KeyCode.Space));

            m_calibrationDone = false;
            yield return null;
            startCalibration ();
            yield return new WaitUntil (() => m_calibrationDone);
            print ("AFTER CALIBRATION");

            if (b_validate && m_calibrationSuccess) {
                // calCount = 0;
                // Validation procedure - only if calibration was successful
                m_validationSuccess = false;
                // If fails: new calibration
                m_validationDone = false;
                yield return null;
                shaderBehavior.phase = ShaderBehaviour.shaderPhase.validation;
                yield return new WaitUntil (() => m_validationDone);
                shaderBehavior.phase = ShaderBehaviour.shaderPhase.none;

                m_calibrationSuccess = m_validationSuccess;

                if (!m_validationSuccess) {
                    print ("failedVal");
                    yield return new WaitForSecondsRealtime (3f);
                } else {
                    print ("succeededVal");
                }
            }
            // TODO: log calibration and validation success and precision

            if (++calCount >= 3) {
                print ("failedCal");
                print ("Press space to abort calibration and continue...");
                yield return new WaitUntil (() => Input.GetKeyUp (KeyCode.Space));
                m_calibrationSuccess = true;
                calCount = 0;
            }
        }
        // End of Tobii Calibration

        eyeCalibrated = true;
        eyeValidated = true;
    }

    private void startNewAnswerRecord () {

        m_recorder_question = new StreamWriter (m_userdataPath + "/Answers_" + currentEmotTrial.expName + ".txt");
    }

    private void startNewRecord () {
        // m_recorder_ET = new StreamWriter(m_userdataPath + "/TESTname_ET.csv");
        // m_recorder_ET = new StreamWriter(m_userdataPath + "/" + currentTrial.expName + "_ET.csv");
        m_recorder_ET = new StreamWriter (m_userdataPath + "/" +
            currentEmotTrial.expName + "_ET.csv");
        m_recorder_ET.WriteLine (
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
            "valL,valR");

        // m_recorder_HMD = new StreamWriter(m_userdataPath + "/TESTname_HMD.csv");
        // m_recorder_HMD = new StreamWriter(m_userdataPath + "/" + currentTrial.expName + "_HMD.csv");
        m_recorder_HMD = new StreamWriter (m_userdataPath + "/" +
            currentEmotTrial.expName + "_HMD.csv");
        m_recorder_HMD.WriteLine (
            "OcutimeStamp,UnityTimeStamp," +
            "LeftCollide,RightCollide");

        isSampling = true;

        // writeInfo($"Started: [{currentTrial.exp_idx + 1}] {currentTrial.expName}");
    }

    private void stopRecord (long elapsedtime) {
        isSampling = false;
        if (m_recorder_ET != null && m_recorder_ET.BaseStream.CanWrite)
            m_recorder_ET.Close ();
        if (m_recorder_HMD != null && m_recorder_HMD.BaseStream.CanWrite)
            m_recorder_HMD.Close ();
        if (m_recorder_question != null && m_recorder_question.BaseStream.CanWrite)
            m_recorder_question.Close ();

        // writeInfo($"Elapsed time: {elapsedtime}");
        // writeInfo($"Trial ended: {(userPressed ? "Pressed trigger" : "Ran out of time")}");
    }

    IEnumerator TobiiCalibration () {
        toggleMessage (true, "pleaseCalibrateTobii");
        _instructBehaviour.RequestConfirmation (durationToContinue);
        yield return new WaitUntil (() => !_instructBehaviour.requested);
        yield return new WaitForSecondsRealtime (1.0f);
        _instructBehaviour.toggleWorldInstruction (false);
        yield return new WaitForSecondsRealtime (1.0f);

        // shaderBehavior.gameObject.SetActive(true);

        // Debug.Log("Adding Tobii callback");
        // shaderBehavior.validationCallback = (success) => {
        //     this.m_validationSuccess = success;
        //     this.m_validationDone = true;
        // };

        // shaderBehavior.phase = ShaderBehaviour.shaderPhase.none;

        print ("Waiting for the eyetracker to start");
        // Wait for ET server to start
        yield return new WaitUntil (() => _eyeTrackerTobii != null && _eyeTrackerTobii._eyeTracker != null);
        print ("_eyeTrackerTobii != null");

        yield return new WaitForEndOfFrame ();
        _eyeTrackerTobii._eyeTracker.HMDGazeDataReceived += HMDGazeDataReceivedCallback;

        m_ETsubscribed = true;
        print ("Eyetracker started and subscribed to");

        // Tobii calibration routine

        // shaderBehavior.phase = ShaderBehaviour.shaderPhase.none;

        m_calibrationSuccess = false;

        int calCount = 0;
        while (!m_calibrationSuccess) {

            print ("BEFORE CALIBRATION");
            // print("Press space to begin calibration routine!");
            // yield return new WaitUntil(() => Input.GetKeyUp(KeyCode.Space));

            m_calibrationDone = false;
            yield return null;
            startCalibration ();
            yield return new WaitUntil (() => m_calibrationDone);
            print ("AFTER CALIBRATION");

            if (b_validate && m_calibrationSuccess) {
                // calCount = 0;
                // Validation procedure - only if calibration was successful
                m_validationSuccess = false;
                // If fails: new calibration
                m_validationDone = false;
                yield return null;
                shaderBehavior.phase = ShaderBehaviour.shaderPhase.validation;
                yield return new WaitUntil (() => m_validationDone);
                shaderBehavior.phase = ShaderBehaviour.shaderPhase.none;

                m_calibrationSuccess = m_validationSuccess;

                if (!m_validationSuccess) {
                    print ("failedVal");
                    yield return new WaitForSecondsRealtime (3f);
                } else {
                    print ("succeededVal");
                }
            }
            // TODO: log calibration and validation success and precision

            if (++calCount >= 3) {
                print ("failedCal");
                print ("Press space to abort calibration and continue...");
                yield return new WaitUntil (() => Input.GetKeyUp (KeyCode.Space));
                m_calibrationSuccess = true;
                calCount = 0;
            }
        }
        // End of Tobii Calibration
    }

    // GAZE TRACKING SAMPLING

    // bool paused;

}