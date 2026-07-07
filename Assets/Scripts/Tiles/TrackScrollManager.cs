using UnityEngine;

public class TrackScrollManager : MonoBehaviour
{
    public static TrackScrollManager Instance;

    [SerializeField]
    private bool stopScrolling;

    public bool IsScrollingStopped => stopScrolling;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void StopScrolling()
    {
        stopScrolling = true;
    }

    public void ResumeScrolling()
    {
        stopScrolling = false;
    }

    public void SetScrolling(bool value)
    {
        stopScrolling = !value;
    }
}