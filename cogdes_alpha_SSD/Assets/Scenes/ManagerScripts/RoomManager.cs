using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Valve.VR.InteractionSystem.Sample;

public class RoomManager : MonoBehaviour
{
    // Start is called before the first frame update

    static public string[] RoomNames =
    {
        "Version 0", "Version 1", "Version 2", "Version 3", "Version 4", "Version 5", "Version 6", "Version 7", "Version 8", "Version 9"
    };

    public static RoomManager instance { get; private set; }

    public bool actionInProgress => isLoading || isUnloading;
    private int nLoadedScenes => SceneManager.sceneCount;
    private int nScenes => SceneManager.sceneCountInBuildSettings;

    public string currSceneName
    {
        get
        {
            if (isValidSceneIdx)
            {
                return RoomNames[currentSceneIdx];
            }
            else
            {
                return null;
            }
        }
    }

    public bool isRoomLoaded
    {
        get
        {
            if (isValidSceneIdx)
            {
                return currentScene.isLoaded;
            }
            else
            {
                return false;
            }
        }
    }

    private bool isValidSceneIdx => (currentSceneIdx >= 0 && currentSceneIdx < RoomNames.Length);
    [SerializeField]
    public int currentSceneIdx = -1;
    [SerializeField]
    private bool isLoading = false;
    [SerializeField]
    private bool isUnloading = false;
    [SerializeField]
    public Scene currentScene => (SceneManager.GetSceneByName(currSceneName)) ;

    public bool debug = false;

    private void Awake()
    {
        instance = this;
    }

    void Enable()
    {
//        print(SceneManager.sceneCountInBuildSettings);
//        currentSceneIdx = 0;

        if (debug)
        {
            LoadScene(1);
        }
    }
    
    // Example that will load/Unload all rooms one after another and output the loading time in msec.
//    void Update()
//    {
//        if (isLoading || isUnloading)
//        {
//            return;
//        }
//        
//        if (isRoomLoaded)
//        {
//            UnloadScene(currentSceneIdx);
//        }
//        else if (isValidSceneIdx)
//        {
//            LoadScene(currentSceneIdx);
//        }
//        else
//        {
//            print("Done");
//            Quit();
//        }
//    }

    public void LoadScene(int roomIDx, bool force = false)
    {
        if (actionInProgress && !force)
        {
            // Write to debug file
            print($"Couldn't load scene: action in progress [l:{isLoading}, u:{isUnloading}]");
            return;
        }
        if (nLoadedScenes >= 2 && !force)
        {
            // Write to debug file
            print($"You cannot load a new scene until you unload the secondary one currently loaded [nScene: {nLoadedScenes}]");
            return;
        }
        
        currentSceneIdx = roomIDx;
        if (isValidSceneIdx)
        {
            StartCoroutine(AsyncSceneLoadMonitor(roomIDx));
        }
        else
        {
            currentSceneIdx = -1;
            // Write to debug file
            print($"Wrong scene build index: {roomIDx}");
        }
        
    }

    public void UnloadScene(bool force = false)
    {
        UnloadScene(currentSceneIdx, force);
    }
    public void UnloadScene(int roomIDx, bool force = false)
    {
        if (actionInProgress && !force)
        {
            // Write to debug file
            print($"Couldn't unload scene: action in progress [l:{isLoading}, u:{isUnloading}]");
            return;
        }
        if (nScenes < 2 && !force)
        {
            // Write to debug file
            print($"No scene to unload [nScene: {nScenes}]");
            return;
        }
        
        if (isRoomLoaded)
        {
            StartCoroutine(AsyncSceneUnloadMonitor(roomIDx));
            currentSceneIdx = -1;
        }
        else
        {
            // Write to debug file
            print($"Scene {currSceneName} is not loaded");
        }
    }

    public string getCurrentSceneInfo()
    {
        string output = "";
        output += currentScene.name;
        output += currentScene.isLoaded ? " (Loaded, " : " (Not Loaded, ";
        output += currentScene.isDirty ? "Dirty, " : "Clean, ";
        output += currentScene.buildIndex >= 0 ? " in build)\n" : " NOT in build)\n";
        return output;
    }

    IEnumerator AsyncSceneLoadMonitor(int sceneBuildIndex)
    {
        long t1 = getTimeStamp();
//        ExpeControl.instance.writeInfo($"Loading {RoomNames[sceneBuildIndex]}");
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(RoomNames[sceneBuildIndex], LoadSceneMode.Additive);

        isLoading = true;

        // Wait until the asynchronous scene fully loads
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

//        print($"[{currentScene.name}] Loading time: {getTimeStamp() - t1}");

        yield return new WaitForEndOfFrame();
        isLoading = false;
    }

    IEnumerator AsyncSceneUnloadMonitor(int sceneBuildIndex)
    {
        long t1 = getTimeStamp();
//        ExpeControl.instance.writeInfo($"Unloading {sceneBuildIndex} {RoomNames[sceneBuildIndex]}");
        print($"Unloading {sceneBuildIndex} {RoomNames[sceneBuildIndex]}");
        AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(RoomNames[sceneBuildIndex]);

        isUnloading = true;

        // Wait until the asynchronous scene fully loads
        while (!asyncUnload.isDone)
        {
            yield return null;
        }
        
        isUnloading = false;
//        print($"Unloading time: {getTimeStamp() - t1}");
    }
    
    public static long getTimeStamp()
    {
        return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
    }

    public void Quit()
    {
#if UNITY_EDITOR
        //Stop playing the scene
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
    
}
