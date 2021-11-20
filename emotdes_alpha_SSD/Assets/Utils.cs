using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

public static class Utils
{
//    public const int ET_width = 2160;
//    public const int ET_height = 1200;

//    public const int HMD_width = 1080;
//    public const int HMD_height = 1200;

//    public static float HMD_FOV_hori = 111.9f;
//    public static float HMD_FOV_vert = 105.6f;

    // public const float HMD_FOV_ratio = HMD_width / HMD_FOV_hori;

    public static float Cam_FOV_hori;
    public static float Cam_FOV_vert;

    public static float Cam_DimX;
    public static float Cam_DimY;
    
    public static readonly System.Random rng = new System.Random();

    public struct FOVSubjectiveS
    {
        public float x1;
        public float x2;
        public float y1;
        public float y2;
        
        public float width { get { return x2-x1; } }
        public float height { get { return y2-y1; } }
        public Vector2 dim { get { return new Vector2(width, height); } }
        
        public float normedX1 { get { return (Cam_FOV_hori / 2 + x1) / Cam_FOV_hori; } }  
        public float normedX2 { get { return (Cam_FOV_hori / 2 + x2) / Cam_FOV_hori; } }  
        
        public float normedY1 { get { return (Cam_FOV_vert / 2 + y1) / Cam_FOV_vert; } }  
        public float normedY2 { get { return (Cam_FOV_vert / 2 + y2) / Cam_FOV_vert; } }        
        
        public Vector2 normedX { get  { return new Vector2(normedX1, normedX2); } }
        public Vector2 normedY { get  { return new Vector2(normedY1, normedY2); } }

        public override string ToString()
        {
            return string.Format("rect(X: {0}, {1}, Y: {2}, {3})\nWidth: {4}, Height: {5}\nNormed: {6}, {7}",
                                x1, x2, y1, y2,
                                width, height,
                                normedX, normedY);
        }

        public string ToFile()
        {
            return this.ToString().Replace("\n", ". ");
        }
    }
    public static FOVSubjectiveS SubjFoV;
    
    public static List<T> ShuffleList<T>(List<T> list)
    {   // Fisher-Yates shuffle: http://stackoverflow.com/questions/273313/randomize-a-listt#1262619

        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
        return list;
    }

    // Same but bot a grid
    public static Vector2 getTargetPosFromIdx(int Idx, int nRows, int nCols, Vector2 rangeX, Vector2 rangeY, float aspectRatio)
    {
        int irow, icol;
        irow = Idx % nCols;
        icol = Idx / nRows;

        Vector2 target_pos = new Vector2(
            irow/(float)nRows + 1f/nRows/2f, // X&Y pos
            icol/(float)nCols + 1f/nCols/2f  // add X&Yspacing 
        );
        
        target_pos.y /= aspectRatio;

        target_pos.x = (rangeX.y - rangeX.x) * target_pos.x + rangeX.x;
        target_pos.y = (rangeY.y - rangeY.x) * target_pos.y + rangeY.x;

//        target_pos.y = target_pos.y;

        return target_pos;
    }

    public static float[] getFixationAccuracy(List<Vector2> samples, Vector2 targetPos)
    {
        float sum = samples.Sum(gaze => Mathf.Sqrt((gaze.x - targetPos.x) * (gaze.x - targetPos.x) +
                                                   (gaze.y - targetPos.y) * (gaze.y - targetPos.y)));
        
//        Vector2 sumXY = Vector2.zero;
//        sumXY.x = samples.Sum(gaze => gaze.x);
//        sumXY.y = samples.Sum(gaze => gaze.y);
//        sumXY /= samples.Count;
//        UnityEngine.Debug.Log(string.Format("{0}, {1} - {2}", sumXY.x, sumXY.y, samples.Count));
        
        float mean = sum / samples.Count;
        
        float std = samples.Sum(
            gaze => Mathf.Pow(
                Mathf.Sqrt((gaze.x - targetPos.x) * (gaze.x - targetPos.x) +
                           (gaze.y - targetPos.y) * (gaze.y - targetPos.y)) -
                        mean, 2));
        std = Mathf.Sqrt(std / samples.Count);

        return new []{mean, std};
    }
}