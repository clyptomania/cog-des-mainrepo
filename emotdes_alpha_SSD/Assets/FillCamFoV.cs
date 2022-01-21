using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FillCamFoV : MonoBehaviour {

    public Camera mainCam;
    public static readonly float m_distance = 100f;
    // public static readonly float m_distance = 100f;

    // Position and scale quad so that it always fills exactly the camera's FoV
    void PositionQuad() {
        Ray camRay = new Ray(mainCam.transform.position, mainCam.transform.forward);

        transform.position = camRay.GetPoint(m_distance);
        transform.rotation = mainCam.transform.rotation;
    }

    void Start() {
        if (Utils.Cam_FOV_hori < 1e-6f) {
            float frustumHeight = 2.0f * m_distance * Mathf.Tan(mainCam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float frustumWidth = frustumHeight * mainCam.aspect;

            Utils.Cam_DimX = frustumWidth;
            Utils.Cam_DimY = frustumHeight;

            Utils.Cam_FOV_hori = mainCam.fieldOfView * mainCam.aspect;
            Utils.Cam_FOV_vert = mainCam.fieldOfView;

            print(string.Format("FoV: {0} ({1})", new Vector2(Utils.Cam_FOV_hori, Utils.Cam_FOV_vert), mainCam.aspect));
            print(string.Format("Camera frustum dim: {0:.0}x{1:.0}", Utils.Cam_DimX, Utils.Cam_DimY));
        }

        transform.localScale = new Vector3(Utils.Cam_DimX, Utils.Cam_DimY, 0);
    }

    void LateUpdate() {
        PositionQuad();
    }
}