using UnityEngine;

public class TrackTile : MonoBehaviour
{
    public enum TrackType
    {
        Normal,
        Crosswalk,
        Finish
    }

    [SerializeField] private TrackType trackType;

    [Header("Anchors")]
    [SerializeField] private Transform backAnchor;
    [SerializeField] private Transform frontAnchor;

    [Header("Crosswalk")]
    [SerializeField] private Transform crosswalkSpawnPoint;

    [Header("Finish")]
    [SerializeField] private Transform finishTriggerPoint;

    private TrackTileSpawner spawner;

    private bool spawnedNext;

    public TrackType Type => trackType;

    public Transform BackAnchor => backAnchor;
    public Transform FrontAnchor => frontAnchor;

    public Transform CrosswalkSpawnPoint => crosswalkSpawnPoint;
    public Transform FinishTriggerPoint => finishTriggerPoint;

    public float PivotToBackOffset => backAnchor.localPosition.z;
    public float PivotToFrontOffset => frontAnchor.localPosition.z;

    public void Initialize(TrackTileSpawner owner)
    {
        spawner = owner;
    }

    public void SnapBehind(TrackTile previousTile)
    {
        Vector3 delta =
            previousTile.FrontAnchor.position -
            BackAnchor.position;

        transform.position += delta;
    }

    private void Update()
    {
        if (TrackScrollManager.Instance != null &&
            TrackScrollManager.Instance.IsScrollingStopped)
            return;

        if (WorldSpeedManager.Instance == null)
            return;

        transform.position +=
            Vector3.back *
            WorldSpeedManager.Instance.GetCurrentWorldSpeed() *
            Time.deltaTime;
    }

}