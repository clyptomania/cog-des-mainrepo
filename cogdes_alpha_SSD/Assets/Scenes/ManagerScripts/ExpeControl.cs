using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tilia.Locomotors.Teleporter;
using Valve.VR;

public class ExpeControl : MonoBehaviour
{
    public static ExpeControl instance { get; private set; }
    [SerializeField] private bool debugging;
    
    // Playlist data
    private readonly List<playlistElement> playlist = new List<playlistElement>(90);
    public int m_currentTrialIdx = 0;
    public int currentTrialIdx => m_currentTrialIdx;
    public playlistElement currentTrial => playlist[currentTrialIdx];
    private string currentRoom => playlist[currentTrialIdx].room_name;
    private int currentRoomIdx => playlist[currentTrialIdx].room_idx;

    private Transform cameraRig; 
    public Camera mainCam;
    
    public enum lateralisation
    {
        left, 
        right,
        comb
    }
    
    public struct LightStruct
    {
        public bool orientation;
        public bool landmark;
    }
    static public List<LightStruct> LightConditions = new List<LightStruct>(4)
    {
        // 0: No lightning (ctrl)
        new LightStruct{orientation = false, landmark = false},
        // 1: set light on orientation markers
        new LightStruct{orientation = true, landmark = false},
        // 2: set light on landmarks
        new LightStruct{orientation = false, landmark = true},
        // 3: set light on landmarks and orientation markers
        new LightStruct{orientation = true, landmark = true},
    };
    
    // UI go
    public GameObject instructionPanel;

    // User data
    private int m_userId = -1;
    private string m_basePath;
    public string m_userdataPath { get; private set; }

    // Record data
    private StreamWriter m_recorder_ET = StreamWriter.Null;
    public StreamWriter m_recorder_HMD = StreamWriter.Null;
    private StreamWriter m_recorder_info = StreamWriter.Null;

    public GameObject pauseCanvas;

    private readonly Dictionary<string, string> messages = new Dictionary<string, string>
    {
        {"start", 
            "The training phase has ended.\n\n" +
            "Press the trigger to start the experiment."},
        {"pause", 
            "Take off the headset if you wish.\n\n" +
            "Take a moment to rest before continuing with the experiment.\n" +
            "Press the trigger to start the experiment."},
        {"calibstart", 
            "Re-calibrate the eye tracker.\nCall the experimentalist for assistance."},
        {"loading",
            "Loading next room."},
        {"unloading", 
            "Unloading current room."},
        {"end", 
            "This is the end of the experiment.\nThank you very much for your participation.\nYou can take off the headset."},
    };
    
    // Show training stimuli?
    [SerializeField] private bool trainPhase;
    
    private EyeTrackingSampler _eyeTrack => (EyeTrackingSampler.instance);
    private bool isTracking => (_eyeTrack.ready);
    private InstructBehaviour _instructBehaviour;
    public ObjectManager condObjects { get; private set; }
    private TeleporterFacade _teleporter;

    void Awake()
    {
        instance = this;
        string[] RoomNames = RoomManager.RoomNames;
        cameraRig = transform.GetChild(0);
        _instructBehaviour = GetComponent<InstructBehaviour>();
        condObjects = new ObjectManager();
        _teleporter = gameObject.GetComponentInChildren<TeleporterFacade>();
        
        // Disable panels
        instructionPanel.SetActive(false);
        pauseCanvas.SetActive(false);

        // Get last user number
        m_basePath = Directory.GetParent(Application.dataPath) + "/SubjectData";
        if (!Directory.Exists(m_basePath)) Directory.CreateDirectory(m_basePath);
        
        if (m_userId < 0){
            //  Loop through existing subject folder and find last one
            string[] directories = Directory.GetDirectories(m_basePath);
            int lastSubjID = -1;
            foreach (string s in directories)
            {
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

    private void SetUp(){
        m_userdataPath = m_basePath + "/Subj_" + m_userId;
        // If this user already exists: start after last trial
        if (Directory.Exists(m_userdataPath))
        {
            int count = Directory.GetFiles(m_userdataPath, "*.csv", SearchOption.AllDirectories).Length;
    
            // Rename userdata file before creating a new one
            if (File.Exists(m_userdataPath + "/UserData.txt"))
            {
                File.Move(m_userdataPath + "/UserData.txt", m_userdataPath + $"/UserData_{getTimeStamp()}.txt" );
            }

            if (count > 1){
                m_currentTrialIdx = count/2;
            }
        }

        print(m_userdataPath);
        // Create new folder with subject ID
        Directory.CreateDirectory(m_userdataPath);
        // User information: basic data + playlist
        m_recorder_info = new StreamWriter(m_userdataPath + "/UserData.txt");

        // Add training trials if needed
        if (trainPhase) {
            playlist.Add(new playlistElement(0, 0, 0, -1));
        }
        // get playlist for user ID
        setUserPlaylist(m_userId);
        setTaskList();

        // Record some protocol information
        writeInfo("User_ID: " + m_userId);
        writeInfo("Stimuli order, room name, target idx, scotoma condition:");
        foreach (playlistElement elp in playlist)
            writeInfo($"{elp.expName} - quest_{elp.task_idx}");
        flushInfo();
    }

    public void writeInfo(string txt)
    {
        print(txt);
        
        if (m_recorder_info.BaseStream.CanWrite)
                m_recorder_info.WriteLine("{0}: {1}", getTimeStamp(), txt);
    }
    public void flushInfo(){
        if (m_recorder_info.BaseStream.CanWrite)
                m_recorder_info.Flush();
    }
    
    private IDictionary<string, string> tasks;

    public string currentTaskString => tasks[$"{currentTrial.room_idx}.{currentTrial.task_idx}"];

    private void setTaskList()
    {
        tasks = new Dictionary<string, string>();
        var lines = File.ReadLines(
            Directory.GetParent(Application.dataPath) + "/SubjectData/questions.csv");
        foreach (var line in lines)
        {
            string[] linesplit = line.Split(',');
            tasks.Add(linesplit[0], linesplit[1]);
            print(line);
        }
    }
    private void setUserPlaylist(int idx)
    {
        int max_idx = 100;
        if (idx > max_idx){
            Debug.LogError($"User index cannot be over {max_idx}.", this);
            Quit();
        }

        StreamReader file = new StreamReader(Directory.GetParent(Application.dataPath) +
                                             "/SubjectData/playlist.csv", Encoding.UTF8);

        int nrep = 10;
        int linesize = 5 * nrep + (nrep-1) + 2;
        // Number of characters per line plus line return

        // Read line according to the user ID number
        char[] lineChar = new char[linesize-1];
        file.BaseStream.Position = idx*linesize;
        file.Read(lineChar, 0, lineChar.Length);
        file.Close();
        
        // Convert line from char[] to string
        string line = new string(lineChar);
        // Split line by commas
        string[] ell = line.Split(',');
        print(ell);
        // For all element in list
        for (int i=0; i<ell.Length; i++){
            // Split by '-' 
            print(ell[i]);
            string[] els = ell[i].Split('-');
             
            // 0: Scene, 1: light cond, 2: Task
            int.TryParse(els[0], out var room_idx);
            int.TryParse(els[1], out var light_cond);
            int.TryParse(els[2], out var quest_idx);

            // new playlistElement to insert in playlist
            playlist.Add(new playlistElement(room_idx + (trainPhase?1:0), light_cond, quest_idx, i));
        }
        
        print($"playlist.Count: {playlist.Count}");
    }

    private bool m_isPresenting;

    public SteamVR_Input_Sources handType;
    public bool userPressed => TrackPadInput.instance.Pressed();
    public bool userPressedSpecial => TrackPadInput.instance.HeldDisplayDown() &&
                                      TrackPadInput.instance.Pressed();

    IEnumerator Start()
    {
            /*
         * FLOW
         *     Pre
         *         Vision tests: dominant eye, stereo vision, color vision, visual acuity
         *         Data gathering (age, gender, dominant eye, VR usage)
         *         Data input (subj num)
         *     Train
         *         Explore scene to get used to the headset (scene that needs a long time to load)
         *             1-2 tasks in training station
         *     Inter-trial break
         *     Trial
         *         Recenter
         *         Read question
         *         End:
         *             Wait n seconds
         *             Wait till target is found: trigger pressed
         *     Inter-trial break
         *         Disable cameras
         *         Unload/Load scenes async
         *             Load scene if not already loaded
         *             Else: unload then load new scene
         *         Wait the remainder of the rest time
         *         Enable cameras
         */

        if (!debugging)
        {
            writeInfo("Waiting for the eyetracker to start");
            yield return new WaitUntil(() => _eyeTrack.ready);
            writeInfo("Eyetracker started and sampling");
        }

        // Show SubjInfo panel
        instructionPanel.SetActive(true);
        // Wait for user ID
        yield return new WaitUntil(() => !instructionPanel.activeSelf);
        
        while (m_currentTrialIdx < playlist.Count)
        {
            toggleMessage(true, "unloading");
            
            RoomManager.instance.UnloadScene();
            yield return new WaitUntil(() => !(RoomManager.instance.actionInProgress));
            toggleMessage(false);
            
            int trialidx = currentTrial.task_idx;
            
            // TODO: Training scene
            // TODO: Inter-trial break
            // TODO: Calibration (start + between scenes)
            
            condObjects.Clear();
            
            // Skip trial if the station scene is not finished
            if (!RoomManager.instance.isRoomAvailable(currentTrial.room_idx))  { m_currentTrialIdx++; continue;  }

            long timeSpentLoading = getTimeStamp();
            toggleMessage(true, "loading");
            RoomManager.instance.LoadScene(currentTrial.room_idx);
            writeInfo(RoomManager.instance.currSceneName);
            yield return new WaitUntil(() => !RoomManager.instance.actionInProgress &&
                                                      RoomManager.instance.currentScene.isLoaded);
            yield return null;
            toggleMessage(false);
            
            // Start new gaze record (record name = stimulus name)
            _eyeTrack.startNewRecord();
            // Start trial
            m_isPresenting = true;
            long start_time = getTimeStamp();

            if (debugging)
            {
                foreach (var lightCond in LightConditions)
                {
                    // yield return new WaitForSecondsRealtime(1);
                    yield return new WaitUntil(() => userPressed || Input.GetKeyUp(KeyCode.Space));
                    yield return null; // Leave time for key up event to disappear
                    setLights(lightCond);
                }
                yield return new WaitUntil(() => userPressed || Input.GetKeyUp(KeyCode.Space));
            }
            else
            { 
                setLights(LightConditions[currentTrial.light_cond]);
            }
            
            // Teleport user to starting position
            Transform startTr = GameObject.Find("Start").transform;
            _teleporter.Teleport(startTr);
            startTr.gameObject.SetActive(false);
            
            // Give time to teleport before relocating world instruction panel
            yield return new WaitForSecondsRealtime(.5f);
            
            // Position instruction panel relative to new user location
            _instructBehaviour.positionWorldInstruction(startTr);
            // Set instruction panel visible
            _instructBehaviour.toggleWorldInstruction(true);
            // Update all info panels with the new trial question (there can be more than one question for a same scene)
            _instructBehaviour.setInstruction(currentTaskString);
            
            // Wait till user presses a special combination of inputs to stop the trial
            yield return new WaitUntil(() => userPressedSpecial || Input.GetKeyUp(KeyCode.Space));
            m_isPresenting = false;
            
            _instructBehaviour.setInstruction("Please wait");
            
            // Stop recording gaze
            _eyeTrack.stopRecord(getTimeStamp() - start_time);
            
            Debug.Log($"Finished: {currentTrial.expName} - {trialidx + 1}");
            
            m_currentTrialIdx++;
            flushInfo();
        }
        
        toggleMessage(true, "end");
        yield return new WaitUntil(() => userPressed || Input.GetKeyUp(KeyCode.Space));
        toggleMessage(false);

        flushInfo();
        Quit();
    }
    
    bool paused;
    private void toggleMessage(bool state, string message = ""){

        if (!messages.ContainsKey(message))
        {
            message = "pause";
        }
        paused = state;
        pauseCanvas.SetActive(paused);
        Text msgHolder = pauseCanvas.transform.GetChild(0).Find("ContentTxt").GetComponent<Text>();
        msgHolder.text = messages[message];
    }

    private void setLights(LightStruct cond)
    {
        condObjects.ToggleLight(cond.orientation, selfregister.objectType.Orientation);
        condObjects.ToggleLight(cond.landmark, selfregister.objectType.Landmark);
    }

    private void OnGUI()
    {
        string strInfo = string.Format("FPS: {0:0.00}", 1 / Time.deltaTime);

        GUI.TextArea(new Rect(0, 200, 100, 35), strInfo);
    }

    public static long getTimeStamp()
    {
        // USE solution I used in Olivier Z. 's project
        return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
    }

    private void OnApplicationQuit()
    {
        _eyeTrack.stopRecord(-1);
        
        if (m_recorder_info.BaseStream.CanWrite)
            m_recorder_info.Close();

        if (m_recorder_HMD.BaseStream.CanWrite)
            m_recorder_HMD.Close();

        if (m_recorder_ET.BaseStream.CanWrite)
            m_recorder_ET.Close();
    }

    public static void Quit()
    {
        print("Quitting gracefully");
        Application.Quit();

#if UNITY_EDITOR
        //Stop playing the scene
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public InputField idTxt;
    private readonly Enum _localEnum;

    public void validateDataInput(){
        string txt = idTxt.text;
        
        if (!string.IsNullOrEmpty(txt)){
            instructionPanel.SetActive(false);
            
            m_userId = Int16.Parse(txt);
            
            SetUp();
        }
    }
}
