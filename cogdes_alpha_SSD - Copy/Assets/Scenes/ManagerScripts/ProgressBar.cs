using UnityEngine;

public class ProgressBar : MonoBehaviour
{
    public static ProgressBar instance;
    public RectTransform bar;
    // Start is called before the first frame update
    void Awake()
    {
        instance = this;
        
        RectTransform selfRect = GetComponent<RectTransform>();
        
        _maxWidth = selfRect.sizeDelta.x;
        _height = bar.sizeDelta.y;
    }

    private float _maxWidth;
    private float _width;
    private float _height;

    public void setProgress(float ratio)
    {
        if (ratio > 1) { ratio = 1; }
        
        _width = _maxWidth * ratio;
        bar.sizeDelta = new Vector2 (_width, _height);
    }

    public float getCurrentValue()
    {
        return _width / _maxWidth;
    }
}
