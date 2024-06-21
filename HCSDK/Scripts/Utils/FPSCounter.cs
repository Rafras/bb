
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    private int fps = 0;
    private float _expSmoothingFactor = 0.9f;
    private float _refreshFrequency = 0.4f;
    private float _timeSinceUpdate = 0f;
    private float _averageFps = 1f;
    private float x;
    private float y;

    public void SetPosition(float x, float y)
    {
        this.x = x;
        this.y = y;
    }
    private void Update()
    {
        _averageFps = _expSmoothingFactor * _averageFps + (1f - _expSmoothingFactor) * 1f / Time.unscaledDeltaTime;
        if (_timeSinceUpdate < _refreshFrequency)
        {
            _timeSinceUpdate += Time.deltaTime;
            return;
        }
        fps = Mathf.RoundToInt(_averageFps);
        _timeSinceUpdate = 0f;
    }
    void OnGUI()
    {
        if (GUI.Button(new Rect(Screen.width * x, Screen.height * y, Screen.width * 0.2f, Screen.height * 0.1f), fps.ToString()))
        {
        }
    }
}
