using UnityEngine;
using UnityEngine.InputSystem;
public class PerformanceTracker : MonoBehaviour
{
    public bool track = false;
    private float _totalTime = 0f;
    private int _frameCount = 0;

    void Update()
    {
        if (track)
        {
            float currentMs = Time.unscaledDeltaTime * 1000f;

            _totalTime += currentMs;
            _frameCount++;
        }

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            Debug.Log($"Average MS = {GetAverageMs()}");
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            Track();
        }
    }

    public float GetAverageMs() => _totalTime / _frameCount;

    public void Track(bool track = true)
    {
        string status = track? "Start" : "Stop";
        Debug.Log($"Performance Tracker : {status}");
        this.track = track;
    }
}
