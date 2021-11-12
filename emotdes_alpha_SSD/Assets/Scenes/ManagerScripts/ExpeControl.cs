using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Tilia.Locomotors.Teleporter;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;

public class ExpeControl : MonoBehaviour {
    public static ExpeControl instance { get; private set; }

    [SerializeField] private bool debugging;
    [SerializeField] private bool resetExperiments = false;
    [SerializeField] private bool eyeTracking = true;

    [SerializeField] private Button controllerCalButton, trackerCalButton;

    [SerializeField] private List<int> durations = new List<int>();

    // Playlist data
    private readonly List<playlistElement> playlist = new List<playlistElement>(90);
    private readonly List<EmotPlaylistElement> emotPlaylist = new List<EmotPlaylistElement>(90);
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
    public GameObject setupPanel;

    // User data
    private int m_userId = -1;
    private string m_basePath;
    public string m_userdataPath { get; private set; }

    // Record data
    private StreamWriter m_recorder_ET = StreamWriter.Null;
    public StreamWriter m_recorder_HMD = StreamWriter.Null;
    private StreamWriter m_recorder_info = StreamWriter.Null;
    private StreamWriter m_recorder_question = StreamWriter.Null;

    public GameObject pausePanel;
    public GameObject questionPanel;

    private readonly Dictionary<string, string> messages = new Dictionary<string, string> {
        {
        "calibrate",
        "You can take a quick break if you wish to remove the headset.\n\n"+
        "Then continue please with the calibration;\n"+
        "you may ask the assistant for help with that.\n\n"+
        "Hold the touchpad when you're done to continue!"
        // "Bitte beginne mit der Kalibrierung.\nFrage den Assistenten, sofern du Hilfe benötigst.\n\n"+
        // "Wenn du fertig bist, drücke bitte die Seitentaste um fortzufahren."
        },
        {
        "takeBreak",
        "You can take a quick break if you wish to remove the headset.\n\n"+
        "Hold the touchpad when you're done to continue!"
        // "Bitte beginne mit der Kalibrierung.\nFrage den Assistenten, sofern du Hilfe benötigst.\n\n"+
        // "Wenn du fertig bist, drücke bitte die Seitentaste um fortzufahren."
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
        "The waiting period will begin soon!\n"+
        "You will get notified once it's over, and you will\n"+
        "be asked questions about your experience then.\n\n"+
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
        "The waiting time is now over!\n\n"+
        "Please stand up now and step a way a bit from the chairs.\n\n"+
        "Press the side button to continue with the questionnaires."
        // "Die Wartezeit ist jetzt vorbei!\n\n" +
        // "Betätige die Seitenknöpfe um mit den Fragebögen fortzufahren."
        },
    };

    private EyeTrackingSampler _eyeTrack => EyeTrackingSampler.instance;
    private ProgressBar _progressBar => ProgressBar.instance;
    private QuestionSlider _questionSlider => QuestionSlider.instance;
    private bool isTracking => (_eyeTrack.ready);
    private InstructBehaviour _instructBehaviour;
    public ObjectManager condObjects { get; private set; }
    // private TeleporterFacade _teleporter;

    [Tooltip("Time in seconds needed to press touchpad ending trial")] public float durationPressToLeave;

    void Awake() {
        instance = this;
        string[] RoomNames = RoomManager.RoomNames;
        cameraRig = transform.GetChild(0);
        Debug.Log("Found camera rig: " + cameraRig.name);
        Debug.Log("CamRig pos: " + cameraRig.position + " CamRig rot: " + cameraRig.rotation);
        _instructBehaviour = GetComponent<InstructBehaviour>();

        if (_instructBehaviour.leftControllerActive)
            controllerBasePoint = GameObject.Find("BasePointL");
        else
            controllerBasePoint = GameObject.Find("BasePointR");

        controllerBasePoint.SetActive(false);

        // condObjects = new ObjectManager();

        // _teleporter = gameObject.GetComponentInChildren<TeleporterFacade>();

        // Disable panels
        setupPanel.SetActive(false);
        pausePanel.SetActive(false);
        // questionPanel.SetActive(false);

        // Get last user number
        m_basePath = Directory.GetParent(Application.dataPath) + "/SubjectData";
        if (!Directory.Exists(m_basePath)) Directory.CreateDirectory(m_basePath);

        if (m_userId < 0) {
            //  Loop through existing subject folder and find last one
            string[] directories = Directory.GetDirectories(m_basePath);
            int lastSubjID = -1;
            foreach (string s in directories) {
                string ss = s.Split('/').Last();

                int subjIdtmp;
                int.TryParse(ss.Split('_')[1], out subjIdtmp);

                if (subjIdtmp > lastSubjID)
                    lastSubjID = subjIdtmp;
            }
            m_userId = lastSubjID + 1;
        }
        idTxt.text = m_userId.ToString();
    }

    private void SetUp() {
        m_userdataPath = m_basePath + "/Subj_" + m_userId;
        // If this user already exists: start after last trial
        if (Directory.Exists(m_userdataPath)) {

            if (resetExperiments) {

                string[] oldFiles = Directory.GetFiles(m_userdataPath, "*.*", SearchOption.AllDirectories);

                foreach (string file in oldFiles) {
                    // Debug.Log(file);
                    File.Delete(file);
                }
                Debug.Log("Deleted " + oldFiles.Length + " old files from " + m_userdataPath);

            } else {

                int count = Directory.GetFiles(m_userdataPath, "*.csv", SearchOption.AllDirectories).Length;

                // Rename userdata file before creating a new one
                if (File.Exists(m_userdataPath + "/UserData.txt")) {
                    File.Move(m_userdataPath + "/UserData.txt", m_userdataPath + $"/UserData_{getTimeStamp()}.txt");
                }

                if (count > 1) {
                    m_currentTrialIdx = count / 2;
                }
            }
        }

        print(m_userdataPath);
        // Create new folder with subject ID
        Directory.CreateDirectory(m_userdataPath);
        // User information: basic data + playlist
        m_recorder_info = new StreamWriter(m_userdataPath + "/UserData.txt");

        // get playlist for user ID
        SetUserPlaylist(m_userId);
        setTaskList();

        // Record some protocol information
        WriteInfo("User_ID: " + m_userId);
        // writeInfo("Stimuli order, room name, target idx, scotoma condition:");
        WriteInfo("Room name, Duration, Order:");
        // foreach (playlistElement elp in playlist)
        //     writeInfo($"{elp.expName} - quest_{elp.task_idx}");
        foreach (EmotPlaylistElement ple in emotPlaylist)
            WriteInfo($"{ple.expName}");
        FlushInfo();

        m_recorder_question = new StreamWriter(m_userdataPath + "/Answers_" + m_userId + ".txt");
    }

    private bool calibrating = false;
    private bool conCal = false;
    private bool trackCal = false;

    public void CalibrateByController() {
        if (!trackCal) {
            conCal = !conCal;
            controllerCalButton.colors = SwapColors(controllerCalButton.colors);
            calibrating = conCal ? true : false;
            if (conCal && calibrating) {
                StartCoroutine(ControllerPositioning());
            }
            // if (conCal) calibrating = true; else calibrating = false;
        }
        // SaveCamRigCal();
        Debug.Log("Calibrating: " + calibrating);
    }
    public void CalibrateByTracker() {
        if (!conCal) {
            trackCal = !trackCal;
            trackerCalButton.colors = SwapColors(trackerCalButton.colors);
            calibrating = trackCal ? true : false;
        }
        // LoadCamRigCal();
        Debug.Log("Calibrating: " + calibrating);
    }

    private GameObject calPointA, calPointB, calPointF;
    private GameObject controllerBasePoint;

    IEnumerator ControllerPositioning() {

        if (calPointA != null) calPointA.SetActive(true); else calPointA = GameObject.Find("CalPointA");
        if (calPointB != null) calPointB.SetActive(true); else calPointB = GameObject.Find("CalPointB");
        if (calPointF != null) calPointF.SetActive(true); else calPointF = GameObject.Find("CalPointF");

        controllerBasePoint.SetActive(true);

        Vector3 newPos = new Vector3();

        // Get first point: floor
        while (!userClickedTrigger && calibrating) {
            newPos = cameraRig.position;
            newPos.y = newPos.y - (controllerBasePoint.transform.position.y - calPointF.transform.position.y);
            cameraRig.position = newPos;
            yield return null;
        }
        Debug.Log("Calibrated floor!");
        calPointF.SetActive(false);

        // Wait for trigger release
        yield return new WaitUntil(() => !userClickedTrigger);

        // Get second point: corner A
        while (!userClickedTrigger && calibrating) {
            newPos = cameraRig.position;
            newPos = newPos - (controllerBasePoint.transform.position - calPointA.transform.position);
            newPos.y = cameraRig.position.y;
            cameraRig.position = newPos;
            yield return null;
        }
        Debug.Log("Calibrated first corner!");
        float angleBetween;
        Vector3 firstCornerPos = calPointA.transform.position;
        Vector3 horizontalControllerPos = controllerBasePoint.transform.position;
        calPointA.SetActive(false);

        // Wait for trigger release
        yield return new WaitUntil(() => !userClickedTrigger);

        // Get final point: corner B
        while (!userClickedTrigger && calibrating) {
            newPos = cameraRig.position;

            horizontalControllerPos = controllerBasePoint.transform.position;
            horizontalControllerPos.y = 0;
            firstCornerPos.y = 0;

            angleBetween = Vector3.SignedAngle(Vector3.right, horizontalControllerPos - firstCornerPos, Vector3.up);

            Debug.Log("Signed angle: " + angleBetween);

            cameraRig.transform.RotateAround(firstCornerPos, Vector3.up, -angleBetween);


            // newPos = newPos - (controllerBasePoint.transform.position - calPointA.transform.position);
            // newPos.y = cameraRig.position.y;
            // cameraRig.position = newPos;
            yield return null;
        }
        Debug.Log("Calibrated first corner!");
        calPointA.SetActive(false);

        yield return new WaitUntil(() => !calibrating);
        controllerBasePoint.SetActive(false);
        Debug.Log("Done calibrating!");
        SaveCamRigCal();
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
        print(txt);

        if (m_recorder_info.BaseStream.CanWrite)
            m_recorder_info.WriteLine("{0}: {1}", getTimeStamp(), txt);
    }

    public void WriteAnswer(string txt) {
        if (m_recorder_question.BaseStream.CanWrite) {
            m_recorder_question.WriteLine("{0};{1};{2};{3};{4}", getTimeStamp(),
                RoomManager.instance.currentRoomName, currentEmotTrial.duration, m_currentTrialIdx, txt);
            m_recorder_question.Flush();
            Debug.Log("Wrote answer: " + txt);
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

    private void SetUserPlaylist(int idx) {
        int max_idx = 100;
        if (idx > max_idx) {
            Debug.LogError($"User index cannot be over {max_idx}.", this);
            Quit();
        }

        List<string> roomNames = RoomManager.instance.ListRooms();

        // TEMPORARY, EASY PLAYLIST CREATION (NO RANDOMIZATION)
        for (int i = 0; i < durations.Count; i++) {
            for (int j = 0; j < roomNames.Count; j++) {
                // Debug.Log(roomName + ", available: " + RoomManager.instance.isRoomAvailable(roomName));
                int trial = i * roomNames.Count + j + 1;
                emotPlaylist.Add(new EmotPlaylistElement(roomNames[j], durations[i], trial, trial));
            }
        }



        // print($"playlist.Count: {emotPlaylist.Count}");

        // StreamReader file = new StreamReader(Directory.GetParent(Application.dataPath) +
        //     "/SubjectData/playlist.csv", Encoding.UTF8);

        // int nrep = 14;
        // int linesize = 104 + 1;
        // // Number of characters per line plus line return

        // // Read line according to the user ID number
        // char[] lineChar = new char[linesize - 1];
        // file.BaseStream.Position = idx * linesize;
        // file.Read(lineChar, 0, lineChar.Length);
        // file.Close();

        // // Convert line from char[] to string
        // string line = new string(lineChar);
        // // Split line by commas
        // string[] ell = line.Split(',');
        // For all element in list
        // for (int i = 0; i < ell.Length; i++) {
        //     // Split by '-' 
        //     string[] els = ell[i].Split('-');

        //     // Debug.Log (ell[i]);

        //     // 0: Scene, 1: light cond, 2: Task
        //     int.TryParse(els[0], out var room_idx);
        //     int.TryParse(els[1], out var light_cond);
        //     int.TryParse(els[2], out var quest_idx);

        //     // new playlistElement to insert in playlist
        //     playlist.Add(new playlistElement(room_idx, light_cond, quest_idx, i));
        // }
        // print($"playlist.Count: {playlist.Count}");
    }
    private void setUserPlaylistErwan(int idx) {
        int max_idx = 100;
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
    public bool userClickedTrigger => TrackPadInput.instance.TriggerClicked();


    float taskTime = 0;
    float padPressedTime = 0;


    // THE ACTUAL GAME LOOP!

    IEnumerator Start() {

        taskTime = 0;
        padPressedTime = 0;

        if (eyeTracking)
            yield return new WaitUntil(() => _eyeTrack.ready);
        else
            yield return new WaitForSeconds(1);

        // Show SubjInfo panel
        setupPanel.SetActive(true);
        pausePanel.SetActive(false);
        // questionPanel.SetActive(false);
        _progressBar.gameObject.SetActive(false);
        _questionSlider.gameObject.SetActive(false);

        // _questionSlider.gameObject.SetActive(true);
        RoomManager.instance.SaveManagerSceneNum();
        RoomManager.instance.LoadBreakRoom();
        yield return new WaitUntil(() => !(RoomManager.instance.actionInProgress));


        LoadCamRigCal();




        // Wait for user ID --- Setup() happens here!
        yield return new WaitUntil(() => !setupPanel.activeSelf);

        // toggleMessage(false);
        yield return new WaitForSecondsRealtime(0.5f);
        toggleMessage(true, "Let's learn the VR controls.\n\nTake a look at your controller.");
        yield return new WaitForSecondsRealtime(5.0f);



        // Introduce Interaction
        // _instructBehaviour.toggleWorldInstruction(false);


        _instructBehaviour.setInstruction("Clickt the controller's trigger.");
        yield return new WaitUntil(() => userClickedTrigger);
        Debug.Log("Successfully triggered.");
        _instructBehaviour.setInstruction("You did it!");
        yield return new WaitForSecondsRealtime(1f);
        _instructBehaviour.toggleControllerInstruction(false);
        yield return new WaitForSecondsRealtime(0.25f);

        _instructBehaviour.setInstruction("Press the controller's side buttons.");
        yield return new WaitUntil(() => userGrippedControl);
        Debug.Log("Successfully gripped.");
        _instructBehaviour.setInstruction("You did it!");
        yield return new WaitForSecondsRealtime(1f);
        _instructBehaviour.toggleControllerInstruction(false);
        yield return new WaitForSecondsRealtime(0.25f);

        _instructBehaviour.toggleControllerInstruction(true);
        _instructBehaviour.setInstruction("Touch the controller's touch pad.");
        yield return new WaitUntil(() => userTouchedPad);
        Debug.Log("Successfully touched touchpad.");
        _instructBehaviour.setInstruction("Well done!");
        yield return new WaitForSecondsRealtime(1f);
        _instructBehaviour.toggleControllerInstruction(false);
        yield return new WaitForSecondsRealtime(0.25f);

        _instructBehaviour.toggleControllerInstruction(true);
        _instructBehaviour.setInstruction("Click the controller's touch pad.");
        yield return new WaitUntil(() => userClickedPad);
        Debug.Log("Successfully clicked touchpad.");
        _instructBehaviour.setInstruction("Good job!");
        yield return new WaitForSecondsRealtime(1f);
        _instructBehaviour.toggleControllerInstruction(false);

        yield return new WaitForSecondsRealtime(0.25f);

        _instructBehaviour.toggleControllerInstruction(true);
        _instructBehaviour.setInstruction("Press and hold the touch pad until the bar fills.");
        while (padPressedTime < durationPressToLeave) {
            if (userClickedPad) {
                padPressedTime += Time.deltaTime;
                _progressBar.gameObject.SetActive(true);
                _progressBar.SetProgress(padPressedTime / durationPressToLeave);
            } else {
                _progressBar.gameObject.SetActive(false);
                padPressedTime = 0;
            }
            yield return null;
        }
        _progressBar.gameObject.SetActive(false);
        _instructBehaviour.toggleControllerInstruction(false);
        toggleMessage(false);




        toggleMessage(false);
        yield return new WaitForSecondsRealtime(0.5f);
        toggleMessage(true, "Now let's practice the questionnaires.\n\nPress the side button again to continue.");
        yield return new WaitForSecondsRealtime(0.25f);
        yield return new WaitUntil(() => userGrippedControl);
        toggleMessage(false);
        yield return new WaitForSecondsRealtime(0.5f);

        // Introduce slider interaction

        // Time Format
        ToggleQuestion(true, "How long have you been here, in VR, so far?");
        _questionSlider.UpdateSliderRange(10, 300, false, true);
        yield return new WaitUntil(() => _questionSlider.confirmed);
        yield return new WaitForSecondsRealtime(1.0f);
        ToggleQuestion(false);
        yield return new WaitForSecondsRealtime(1.0f);

        // Visual Analog Scale
        ToggleQuestion(true, "What is your feeling toward this environment?");
        _questionSlider.UpdateSliderRange(1, 100, true, false, "bad", "indifferent", "good");
        yield return new WaitUntil(() => _questionSlider.confirmed);
        yield return new WaitForSecondsRealtime(1.0f);
        ToggleQuestion(false);
        yield return new WaitForSecondsRealtime(1.0f);

        // SAM Scale: valence
        ToggleQuestion(true, "What is your valence toward this environment?");
        _questionSlider.UpdateSliderRange(1, 100, true, false, "does", "not", "matter", "v");
        yield return new WaitUntil(() => _questionSlider.confirmed);
        yield return new WaitForSecondsRealtime(1.0f);
        ToggleQuestion(false);
        yield return new WaitForSecondsRealtime(1.0f);

        // SAM Scale: arousal
        ToggleQuestion(true, "What is your arousal toward this environment?");
        _questionSlider.UpdateSliderRange(1, 100, true, false, "does", "not", "matter", "a");
        yield return new WaitUntil(() => _questionSlider.confirmed);
        yield return new WaitForSecondsRealtime(1.0f);
        ToggleQuestion(false);
        yield return new WaitForSecondsRealtime(1.0f);

        // Discrete Scale
        // ToggleQuestion(true, "How would you rate this experience on a 1-to-5 scale?");
        // _questionSlider.UpdateSliderRange(1, 5);
        // yield return new WaitUntil(() => _questionSlider.confirmed);
        // yield return new WaitForSecondsRealtime(1.0f);
        // ToggleQuestion(false);

        yield return new WaitForSecondsRealtime(0.5f);
        toggleMessage(true, "You did great!\n\nPress the side button again to start.");




        Debug.Log("Currently playing trial " + (m_currentTrialIdx + 1) + " out of " + emotPlaylist.Count);

        while (m_currentTrialIdx < emotPlaylist.Count) {
            toggleMessage(true, "unloading");
            Debug.Log("Starting room unload...");

            // RoomManager.instance.UnloadScene();
            RoomManager.instance.UnloadRoom();
            yield return new WaitUntil(() => !(RoomManager.instance.actionInProgress));
            toggleMessage(false);
            _instructBehaviour.toggleControllerInstruction(false);
            Debug.Log("Room unload finished.");

            int trialidx = currentEmotTrial.task_idx;

            // condObjects.Clear();

            // Skip trial if the station scene is not finished
            // if (!RoomManager.instance.isRoomAvailable(currentTrial.room_idx)) { m_currentTrialIdx++; continue; }

            long timeSpentLoading = getTimeStamp();
            toggleMessage(true, "loading");
            // RoomManager.instance.LoadScene(currentTrial.room_idx);
            RoomManager.instance.LoadRoom(currentEmotTrial.roomName);

            WriteInfo(RoomManager.instance.currSceneName);
            yield return new WaitUntil(() => !RoomManager.instance.actionInProgress &&
               RoomManager.instance.currentScene.isLoaded);
            yield return null;
            toggleMessage(false);




            taskTime = 0;
            padPressedTime = 0;


            if (RoomManager.instance.currSceneName == RoomManager.instance.breakRoomName && eyeTracking) {
                // BREAK ROOM: do calibration and continue to next level

                // toggleMessage(true, "calibrate");
                toggleMessage(true, "takeBreak");
                // yield return new WaitUntil(() => userGrippedControl || Input.GetKeyUp(KeyCode.Space));

                // Wait till user presses a special combination of inputs to stop the trial
                while (padPressedTime < durationPressToLeave) {
                    if (userClickedPad) {
                        padPressedTime += Time.deltaTime;
                        _progressBar.gameObject.SetActive(true);
                        _progressBar.SetProgress(padPressedTime / durationPressToLeave);
                    } else {
                        _progressBar.gameObject.SetActive(false);
                        padPressedTime = 0;
                    }
                    yield return null;
                }
                _progressBar.gameObject.SetActive(false);
                toggleMessage(false);



            } else {
                // REGULAR TRIAL ROOM


                // _instructBehaviour.toggleWorldInstruction(false);

                // Update all info panels with the new trial question (there can be more than one question for a same scene)
                // _instructBehaviour.setInstruction(currentTaskString);

                toggleMessage(true, "beginWaitingSit");

                // Wait till user presses a special combination of inputs to stop the trial
                while (padPressedTime < durationPressToLeave) {
                    if (userClickedPad) {
                        padPressedTime += Time.deltaTime;
                        _progressBar.gameObject.SetActive(true);
                        _progressBar.SetProgress(padPressedTime / durationPressToLeave);
                    } else {
                        _progressBar.gameObject.SetActive(false);
                        padPressedTime = 0;
                    }
                    yield return null;
                }
                _progressBar.gameObject.SetActive(false);

                toggleMessage(true, "three");
                yield return new WaitForSeconds(1);
                toggleMessage(true, "two");
                yield return new WaitForSeconds(1);
                toggleMessage(true, "one");
                yield return new WaitForSeconds(1);
                toggleMessage(false);

                // Start new gaze record (record name = stimulus name)
                if (eyeTracking) {
                    _eyeTrack.startNewRecord();
                    Debug.Log("Started eye tracking.");
                }
                // Start trial
                m_isPresenting = true;
                long start_time = getTimeStamp();

                if (debugging) {
                    foreach (var lightCond in LightConditions) {
                        // yield return new WaitForSecondsRealtime(1);
                        yield return new WaitUntil(() => userGrippedControl || Input.GetKeyUp(KeyCode.Space));
                        yield return null; // Leave time for key up event to disappear
                                           // setLights (lightCond);
                    }
                    yield return new WaitUntil(() => userGrippedControl || Input.GetKeyUp(KeyCode.Space));
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


                // Wait until trial time runs out or touchpad pressed
                padPressedTime = 0;
                while (taskTime < currentEmotTrial.duration && padPressedTime < durationPressToLeave) {
                    taskTime += Time.deltaTime;

                    // if (userClickedPad) {
                    //     padPressedTime += Time.deltaTime;

                    //     _progressBar.gameObject.SetActive(true);
                    //     _progressBar.SetProgress(padPressedTime / durationPressToLeave);
                    // } else {
                    //     _progressBar.gameObject.SetActive(false);
                    //     padPressedTime = 0;
                    // }
                    yield return null;
                }
                _progressBar.gameObject.SetActive(false);

                if (padPressedTime >= durationPressToLeave)
                    Debug.Log("Finished from pad press.");
                else
                    Debug.Log("Finished from expired waiting duration.");

                // Stop recording gaze
                if (eyeTracking)
                    _eyeTrack.stopRecord(getTimeStamp() - start_time);

                m_isPresenting = false;

                _instructBehaviour.setInstruction("Please wait");


                Debug.Log($"Finished: {currentEmotTrial.expName} - {trialidx}");


                toggleMessage(true, "beginQuestions");

                yield return new WaitForSeconds(1);
                yield return new WaitUntil(() => userGrippedControl);

                toggleMessage(false);


                // THIS IS THE QUESTION BLOCK


                yield return new WaitForSecondsRealtime(0.5f);
                ToggleQuestion(true, "How long do you think you have been waiting?");
                _questionSlider.UpdateSliderRange(30, 300, false, true);
                yield return new WaitUntil(() => _questionSlider.confirmed);
                yield return new WaitForSecondsRealtime(1.0f);
                ToggleQuestion(false);



                // SAM Scale: valence
                ToggleQuestion(true, "Which of these pictures represents your emotional state best?");
                _questionSlider.UpdateSliderRange(1, 100, true, false, "does", "not", "matter", "v");
                yield return new WaitUntil(() => _questionSlider.confirmed);
                yield return new WaitForSecondsRealtime(1.0f);
                ToggleQuestion(false);
                yield return new WaitForSecondsRealtime(1.0f);

                // SAM Scale: arousal
                ToggleQuestion(true, "Which of these pictures represents your excitement best?");
                _questionSlider.UpdateSliderRange(1, 100, true, false, "does", "not", "matter", "a");
                yield return new WaitUntil(() => _questionSlider.confirmed);
                yield return new WaitForSecondsRealtime(1.0f);
                ToggleQuestion(false);
                yield return new WaitForSecondsRealtime(1.0f);



                yield return new WaitForSecondsRealtime(0.5f);
                ToggleQuestion(true, "How much did you think about\nyour PAST while waiting?");
                _questionSlider.UpdateSliderRange(1, 100, true, false, "not at all", " ", "all the time");
                yield return new WaitUntil(() => _questionSlider.confirmed);
                yield return new WaitForSecondsRealtime(1.0f);
                ToggleQuestion(false);

                yield return new WaitForSecondsRealtime(0.5f);
                ToggleQuestion(true, "How much did you think about\nyour PRESENT while waiting?");
                _questionSlider.UpdateSliderRange(1, 100, true, false, "not at all", " ", "all the time");
                yield return new WaitUntil(() => _questionSlider.confirmed);
                yield return new WaitForSecondsRealtime(1.0f);
                ToggleQuestion(false);

                yield return new WaitForSecondsRealtime(0.5f);
                ToggleQuestion(true, "How much did you think about\nyour FUTURE while waiting?");
                _questionSlider.UpdateSliderRange(1, 100, true, false, "not at all", " ", "all the time");
                yield return new WaitUntil(() => _questionSlider.confirmed);
                yield return new WaitForSecondsRealtime(1.0f);
                ToggleQuestion(false);



                yield return new WaitForSecondsRealtime(0.5f);
                ToggleQuestion(true, "How intensively did you experience\nYOUR BODY most of the time?");
                _questionSlider.UpdateSliderRange(1, 100, true, false, "not at all", " ", "very intensively");
                yield return new WaitUntil(() => _questionSlider.confirmed);
                yield return new WaitForSecondsRealtime(1.0f);
                ToggleQuestion(false);
                yield return new WaitForSecondsRealtime(1.0f);

                yield return new WaitForSecondsRealtime(0.5f);
                ToggleQuestion(true, "How intensively did you experience\nTHE SURROUNDING SPACE most of the time?");
                _questionSlider.UpdateSliderRange(1, 100, true, false, "not at all", " ", "very intensively");
                yield return new WaitUntil(() => _questionSlider.confirmed);
                yield return new WaitForSecondsRealtime(1.0f);
                ToggleQuestion(false);



                yield return new WaitForSecondsRealtime(0.5f);
                ToggleQuestion(true, "How often did you think about time?");
                _questionSlider.UpdateSliderRange(1, 100, true, false, "not at all", " ", "extremely often");
                yield return new WaitUntil(() => _questionSlider.confirmed);
                yield return new WaitForSecondsRealtime(1.0f);
                ToggleQuestion(false);

                yield return new WaitForSecondsRealtime(0.5f);
                ToggleQuestion(true, "How fast did time pass for you?");
                _questionSlider.UpdateSliderRange(1, 100, true, false, "extremely slowly", " ", "extremely fast");
                yield return new WaitUntil(() => _questionSlider.confirmed);
                yield return new WaitForSecondsRealtime(1.0f);
                ToggleQuestion(false);
                yield return new WaitForSecondsRealtime(1.0f);



                // THIS IS THE END OF THE QUESTION BLOCK

            }

            m_currentTrialIdx++;
            FlushInfo();
        }

        Debug.Log("Experiment concluded. Quitting...");

        toggleMessage(true, "end");
        yield return new WaitUntil(() => userGrippedControl || Input.GetKeyUp(KeyCode.Space));
        toggleMessage(false);

        FlushInfo();
        Quit();
    }

    bool paused;
    private void toggleMessage(bool state, string message = "") {

        paused = state;
        pausePanel.SetActive(paused);
        Text msgHolder = pausePanel.transform.Find("ContentTxt").GetComponent<Text>();

        if (!messages.ContainsKey(message)) {
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
        Debug.Log(msgHolder.text);
    }

    private void ToggleQuestion(bool state, string question = "") {
        _questionSlider.gameObject.SetActive(state);
        _questionSlider.UpdateQuestionText(question);
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
        if (eyeTracking)
            _eyeTrack.stopRecord(-1);

        if (m_recorder_info.BaseStream.CanWrite)
            m_recorder_info.Close();

        if (m_recorder_question.BaseStream.CanWrite)
            m_recorder_question.Close();

        if (m_recorder_HMD.BaseStream.CanWrite)
            m_recorder_HMD.Close();

        if (m_recorder_ET.BaseStream.CanWrite)
            m_recorder_ET.Close();
    }

    public static void Quit() {
        print("Quitting gracefully");
        Application.Quit();

#if UNITY_EDITOR
        //Stop playing the scene
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public InputField idTxt;
    private readonly Enum _localEnum;

    public void validateDataInput() {
        string txt = idTxt.text;

        if (!string.IsNullOrEmpty(txt) && !calibrating) {
            setupPanel.SetActive(false);

            m_userId = Int16.Parse(txt);

            // Debug.Log ("User Input: " + txt);

            SetUp();
        }
    }
}