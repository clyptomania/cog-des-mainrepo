using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RadialProgress : MonoBehaviour {

    public static RadialProgress instance;
    public Image fillImage;
    public RectTransform checkMark;


    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// </summary>
    void Awake() {
        instance = this;

        fillImage.fillAmount = 0;

        Debug.Log("Found image: " + fillImage.name);

        if (checkMark != null)
            checkMark.gameObject.SetActive(false);
    }

    public void SetProgress(float fill) {
        if (fill >= 1) {
            fill = 1;
            if (checkMark != null)
                checkMark.gameObject.SetActive(true);
        }
        fillImage.fillAmount = fill;
        Debug.Log("Setting radial progress to " + fill);
    }

    public void ResetFill() {
        fillImage.fillAmount = 0;
        if (checkMark != null)
            checkMark.gameObject.SetActive(false);

    }
}
