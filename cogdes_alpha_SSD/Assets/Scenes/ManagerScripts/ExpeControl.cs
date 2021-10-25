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

    [SerializeField] private List<int> durations = new List<int>();

    // Playlist data
    private readonly List<playlistElement> playlist = new List<playlistElement>(90);
    private readonly List<EmotPlaylistElement> emotPlaylist = new List<EmotPlaylistElement>(90);
    public int m_currentTrialIdx = 0;
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

    public GameObject pausePanel;

    private readonly Dictionary<string, string> messages = new Dictionary<string, string> {
        {
        "calibrate",
        "Bitte beginne mit der Kalibrierung.\nFrage den Assistenten, sofern du Hilfe benötigst.\n\n"+
        "Wenn du fertig bist, drücke bitte die Seitentaste um fortzufahren."
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
        "Nächster Raum wird geladen."
        },
        {
        "unloading",
        "Unloading current room."
        },
        {
        "end",
        "This is the end of the experiment.\nThank you very much for your participation.\nYou can take off the headset."
        },
        {
        "beginWaiting",
        "Die Wartezeit beginnt gleich.\n\n" +
        "Halte das Touchpad gedrückt um damit loszulegen!"
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
        "Die Wartezeit ist jetzt vorbei!\n\n" +
        "Betätige die Seitenknöpfe um mit den Fragebögen fortzufahren."
        },
    };

    private EyeTrackingSampler _eyeTrack => EyeTrackingSampler.instance;
    private ProgressBar _progressBar => ProgressBar.instance;
    private bool isTracking => (_eyeTrack.ready);
    private InstructBehaviour _instructBehaviour;
    public ObjectManager condObjects { get; private set; }
    // private TeleporterFacade _teleporter;

    [Tooltip("Time in seconds needed to press touchpad ending trial")] public float durationPressToLeave;

    void Awake() {
        instance = this;
        string[] RoomNames = RoomManager.RoomNames;
        cameraRig = transform.GetChild(0);
        _instructBehaviour = GetComponent<InstructBehaviour>();

        // condObjects = new ObjectManager();

        // _teleporter = gameObject.GetComponentInChildren<TeleporterFacade>();

        // Disable panels
        setupPanel.SetActive(false);
        pausePanel.SetActive(false);

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
        writeInfo("User_ID: " + m_userId);
        // writeInfo("Stimuli order, room name, target idx, scotoma condition:");
        writeInfo("Room name, Duration, Order:");
        // foreach (playlistElement elp in playlist)
        //     writeInfo($"{elp.expName} - quest_{elp.task_idx}");
        foreach (EmotPlaylistElement ple in emotPlaylist)
            writeInfo($"{ple.expName}");
        flushInfo();
    }

    public void writeInfo(string txt) {
        print(txt);

        if (m_recorder_info.BaseStream.CanWrite)
            m_recorder_info.WriteLine("{0}: {1}", getTimeStamp(), txt);
    }
    public void flushInfo() {
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

    public bool userGrippedControl => TrackPadInput.instance.Pressed();
    public bool userTouchedPad => TrackPadInput.instance.DisplayTouched();
    public bool userClickedPad => TrackPadInput.instance.DisplayPressed();


    IEnumerator Start() {
        if (eyeTracking)
            yield return new WaitUntil(() => _eyeTrack.ready);
        else
            yield return new WaitForSeconds(1);

        // Show SubjInfo panel
        setupPanel.SetActive(true);
        pausePanel.SetActive(false);
        _progressBar.gameObject.SetActive(false);

        // Wait for user ID --- Setup() happens here!
        yield return new WaitUntil(() => !setupPanel.activeSelf);

        // Test touchpad interaction

        _instructBehaviour.toggleWorldInstruction(false);
        _instructBehaviour.setInstruction("Press the controller's grip buttons.");
        yield return new WaitUntil(() => userGrippedControl);
        Debug.Log("Successfully gripped.");

        _instructBehaviour.setInstruction("Touch the controller's touch pad.");
        yield return new WaitUntil(() => userTouchedPad);
        Debug.Log("Successfully touched touchpad.");

        _instructBehaviour.setInstruction("Well done!");
        yield return new WaitForSecondsRealtime(.5f);

        _instructBehaviour.setInstruction("Click the controller's touch pad.");
        yield return new WaitUntil(() => userClickedPad);
        Debug.Log("Successfully clicked touchpad.");

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
            writeInfo(RoomManager.instance.currSceneName);
            yield return new WaitUntil(() => !RoomManager.instance.actionInProgress &&
               RoomManager.instance.currentScene.isLoaded);
            yield return null;
            toggleMessage(false);


            _instructBehaviour.toggleWorldInstruction(false);

            // Update all info panels with the new trial question (there can be more than one question for a same scene)
            _instructBehaviour.setInstruction(currentTaskString);

            // if break room = do calibration
            if (RoomManager.instance.currSceneName == "BreakRoom" && eyeTracking) {
                toggleMessage(true, "calibrate");
                yield return new WaitUntil(() => userGrippedControl || Input.GetKeyUp(KeyCode.Space));
                toggleMessage(false);
            }

            toggleMessage(true, "beginWaiting");

            float taskTime = 0;
            float padPressedTime = 0;

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

            m_currentTrialIdx++;
            flushInfo();
        }

        Debug.Log("Experiment concluded. Quitting...");

        toggleMessage(true, "end");
        yield return new WaitUntil(() => userGrippedControl || Input.GetKeyUp(KeyCode.Space));
        toggleMessage(false);

        flushInfo();
        Quit();
    }

    bool paused;
    private void toggleMessage(bool state, string message = "") {

        if (!messages.ContainsKey(message)) {
            message = "pause";
        }
        paused = state;
        pausePanel.SetActive(paused);
        Text msgHolder = pausePanel.transform.Find("ContentTxt").GetComponent<Text>();
        string messageText = messages[message];
        msgHolder.text = messageText;
        Debug.Log(messageText);
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

        if (!string.IsNullOrEmpty(txt)) {
            setupPanel.SetActive(false);

            m_userId = Int16.Parse(txt);

            // Debug.Log ("User Input: " + txt);

            SetUp();
        }
    }
}