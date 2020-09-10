using System;
using System.Collections;
using System.Globalization;
using System.IO;
using UnityEngine;
using ViveSR.anipal.Eye;
using System.Runtime.InteropServices;
using System.Threading;

public class EyeTrackingSampler: MonoBehaviour
{
    private static EyeData eyeData = new EyeData();
    public static EyeTrackingSampler instance { get; private set; }
    public bool ready { get; private set; }
    
    // Record data
    private StreamWriter m_recorder_ET = StreamWriter.Null;
    public StreamWriter m_recorder_HMD = StreamWriter.Null;
    
    static GazePoint gazePoint = new GazePoint();
    static private ExpeControl _expeControl;

    private IEnumerator Start()
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        
        instance = this;
        _expeControl = ExpeControl.instance;
        ready = false;

        while (SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.WORKING &&
            SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.NOT_SUPPORT)
        {
            yield return null;
        }
        
        SRanipal_Eye.WrapperRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye.CallbackBasic)Callback));

        ready = true;
    }

    public bool isSampling { get; private set; }
    private long lastOcuTS;
    private static void Callback(ref EyeData eye_data)
    {
        gazePoint = new GazePoint(eye_data);
        
        if (instance.isSampling)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            
            Vector3 meanBasePoint = gazePoint.CombGaze.gaze_origin_mm;
            Vector3 leftBasePoint = gazePoint.LeftGaze.gaze_origin_mm;
            Vector3 rightBasePoint = gazePoint.RightGaze.gaze_origin_mm;
            
            Vector3 meanGazeDirection = gazePoint.CombGaze.gaze_direction_normalized;
            Vector3 leftGazeDirection = gazePoint.CombGaze.gaze_direction_normalized;
            Vector3 rightGazeDirection = gazePoint.CombGaze.gaze_direction_normalized;

            bool valC = gazePoint.valid(ExpeControl.lateralisation.comb);
            bool valL = gazePoint.valid(ExpeControl.lateralisation.left);
            bool valR = gazePoint.valid(ExpeControl.lateralisation.right);
            
            instance.m_recorder_ET.WriteLine(
                $"{gazePoint.data.timestamp},{instance.UnityTimeStamp}," +
                $"{instance.cameraPosition.x},{instance.cameraPosition.y},{instance.cameraPosition.z}," +
                $"{instance.cameraQuaternion.x},{instance.cameraQuaternion.y}," +
                $"{instance.cameraQuaternion.z},{instance.cameraQuaternion.w}," +
                $"{meanBasePoint.x},{meanBasePoint.y},{meanBasePoint.z}," +
                $"{meanGazeDirection.x},{meanGazeDirection.y},{meanGazeDirection.z}," +
                $"{leftBasePoint.x},{leftBasePoint.y},{leftBasePoint.z}," +
                $"{rightBasePoint.x},{rightBasePoint.y},{rightBasePoint.z}," +
                $"{leftGazeDirection.x},{leftGazeDirection.y},{leftGazeDirection.z}," +
                $"{rightGazeDirection.x},{rightGazeDirection.y},{rightGazeDirection.z}," +
                $"{valC},{valL},{valR}"
             );
        }
    }
    
    private void Update()
    {
        RetrieveCameraData();
        if (isSampling)
        {
            m_recorder_HMD.WriteLine(
                $"{gazePoint.data.timestamp},{UnityTimeStamp}," +
                $"{(gazePoint.LeftCollide != null ? gazePoint.LeftCollide.name : "None")}," +
                $"{(gazePoint.RightCollide != null ? gazePoint.RightCollide.name : "None")}," +
                $"{(gazePoint.CombinedCollide != null ? gazePoint.CombinedCollide.name : "None")}");
            // m_recorder_HMD.Flush();
        }
    }

    public void startNewRecord()
    {
        m_recorder_ET = new StreamWriter(_expeControl.m_userdataPath + "/" +
                                         _expeControl.currentTrial.expName + "_ET.csv");
        m_recorder_ET.WriteLine(
            "OcutimeStamp,UnityTimeStamp," +
            "cameraPosition.x,cameraPosition.y,cameraPosition.z," +
            "cameraRotation.x,cameraRotation.y,cameraRotation.z,cameraRotation.w," +
            "meanBasePoint.x,meanBasePoint.y,meanBasePoint.z," +
            "meanGazeDirection.x,meanGazeDirection.y,meanGazeDirection.z," +
            "leftBasePoint.x,leftBasePoint.y,leftBasePoint.z," +
            "rightBasePoint.x,rightBasePoint.y,rightBasePoint.z," +
            "leftEyeDirection.x,leftEyeDirection.y,leftEyeDirection.z," +
            "rightEyeDirection.x,rightEyeDirection.y,rightEyeDirection.z," +
            "valB,valL,valR");
        
        m_recorder_HMD = new StreamWriter(_expeControl.m_userdataPath + "/" +
                                          _expeControl.currentTrial.expName + "_HMD.csv");
        m_recorder_HMD.WriteLine(
            "OcutimeStamp,UnityTimeStamp," +
            "LeftCollide,RightCollide,CombCollide");
        
        isSampling = true;
        
        _expeControl.writeInfo($"Started: [{_expeControl.currentTrial.task_idx + 1}] " +
                               $"{_expeControl.currentTrial.expName}");
    }

    public void stopRecord(long elapsedtime)
    {
        isSampling = false;
        if (m_recorder_ET != null && m_recorder_ET.BaseStream.CanWrite)
            m_recorder_ET.Close();
        if (m_recorder_HMD != null && m_recorder_HMD.BaseStream.CanWrite)
            m_recorder_HMD.Close();
        
        _expeControl.writeInfo($"Elapsed time: {elapsedtime}");
        _expeControl.writeInfo($"Trial ended: {(_expeControl.userPressed ? "Pressed trigger" : "Ran out of time")}");
    }
    
    private Vector3 cameraPosition;
    private Quaternion cameraQuaternion;
    
    [NonSerialized]
    public long UnityTimeStamp;
    
    public void RetrieveCameraData()
    {
        Transform camTrans = ExpeControl.instance.mainCam.transform;
        
        cameraQuaternion = camTrans.rotation;
        cameraPosition = camTrans.position;
        
        gazePoint.LeftCollide = Physics.Raycast(gazePoint.LeftWorldRay, out RaycastHit hitL) ? hitL.transform : null;
        gazePoint.RightCollide = Physics.Raycast(gazePoint.RightWorldRay, out RaycastHit hitR) ? hitR.transform : null;
        gazePoint.CombinedCollide = Physics.Raycast(gazePoint.RightWorldRay, out RaycastHit hitC) ? hitC.transform : null;
        
        UnityTimeStamp = ExpeControl.getTimeStamp();
    }

    public class GazePoint
    {
        public GazePoint() // Empty ctor
        {
        }
        
        public GazePoint(EyeData gaze)
        { 
            LeftGaze = gaze.verbose_data.left;
            RightGaze = gaze.verbose_data.left;
            CombGaze = gaze.verbose_data.combined.eye_data;
            data = gaze;

            LeftCollide = null;
            RightCollide = null;

            LeftWorldRay = getGazeRay(ExpeControl.lateralisation.left);
            RightWorldRay = getGazeRay(ExpeControl.lateralisation.right);
            RightWorldRay = getGazeRay(ExpeControl.lateralisation.comb);
        }

        public readonly EyeData data;
        public readonly SingleEyeData LeftGaze;
        public readonly SingleEyeData RightGaze;
        public readonly SingleEyeData CombGaze;

        public readonly Ray LeftWorldRay;
        public readonly Ray RightWorldRay;

        public Transform LeftCollide;
        public Transform RightCollide;
        public Transform CombinedCollide;

        public bool valid(ExpeControl.lateralisation later)
        {
            return later == ExpeControl.lateralisation.left ?
                LeftGaze.GetValidity(SingleEyeDataValidity.SINGLE_EYE_DATA_GAZE_ORIGIN_VALIDITY) :
                RightGaze.GetValidity(SingleEyeDataValidity.SINGLE_EYE_DATA_GAZE_ORIGIN_VALIDITY);
        }

        public Ray getGazeRay(ExpeControl.lateralisation later)
        {
            SingleEyeData gp = later == ExpeControl.lateralisation.left ? LeftGaze : RightGaze;
            
            return new Ray(instance.cameraPosition + gp.gaze_origin_mm, 
                instance.cameraQuaternion * gp.gaze_direction_normalized);
        }
    }
    
}
