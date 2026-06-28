using UnityEngine;

public class WorldEventManager : MonoBehaviour
{
    public static WorldEventManager Instance;

    public enum WorldEventType
    {
        None,
        Ambulance,
        Crosswalk
    }

    [Header("References")]
    public PedestrianSpawner pedestrianSpawner;
    public AmbulanceTrigger[] ambulanceSpawners;

    [Header("Event Timing")]
    public float minEventInterval = 12f;
    public float maxEventInterval = 18f;

    [Header("Chance")]
    [Range(0,100)]
    public int ambulanceChance = 50;

    [Range(0,100)]
    public int crosswalkChance = 50;

    private float timer;
    private float nextEventTime;

    private bool waitingEventFinished;
    private WorldEventType currentEvent = WorldEventType.None;

    public bool IsEventRunning => waitingEventFinished;
    public WorldEventType CurrentEvent => currentEvent;

    public bool IsCrosswalkRunning()
    {
        return currentEvent == WorldEventType.Crosswalk;
    }

    public bool IsAmbulanceRunning()
    {
        return currentEvent == WorldEventType.Ambulance;
    }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        ScheduleNextEvent();
    }

    private void Update()
    {
        if (Time.timeScale == 0f)
            return;

        if (waitingEventFinished)
            return;

        timer += Time.deltaTime;

        if (timer >= nextEventTime)
        {
            timer = 0f;
            SpawnRandomEvent();
        }
    }

    void SpawnRandomEvent()
    {
        int totalChance = ambulanceChance + crosswalkChance;

        if (totalChance <= 0)
        {
            ScheduleNextEvent();
            return;
        }

        int roll = Random.Range(0, totalChance);

        if (roll < ambulanceChance)
        {
            SpawnAmbulance();
        }
        else
        {
            SpawnCrosswalk();
        }
    }

    void SpawnAmbulance()
    {
        if (ambulanceSpawners == null)
        {
            ScheduleNextEvent();
            return;
        }

        if (ambulanceSpawners.Length == 0)
        {
            ScheduleNextEvent();
            return;
        }

        int index = Random.Range(0, ambulanceSpawners.Length);

        AmbulanceTrigger trigger = ambulanceSpawners[index];

        if (trigger == null)
        {
            ScheduleNextEvent();
            return;
        }

        bool success = trigger.RequestSpawn();

        if (success)
        {
            waitingEventFinished = true;
            currentEvent = WorldEventType.Ambulance;
        }
        else
        {
            ScheduleNextEvent();
        }
    }

    void SpawnCrosswalk()
    {
        if (pedestrianSpawner == null)
        {
            ScheduleNextEvent();
            return;
        }

        bool success = pedestrianSpawner.RequestSpawn();

        if (success)
        {
            waitingEventFinished = true;
            currentEvent = WorldEventType.Crosswalk;
        }
        else
        {
            ScheduleNextEvent();
        }
    }

    public void NotifyEventFinished()
    {
        waitingEventFinished = false;
        currentEvent = WorldEventType.None;
        ScheduleNextEvent();
    }

    // ======================================================
    // Compatibility API
    // ======================================================

    public void NotifyCrosswalkSpawned()
    {
        waitingEventFinished = true;
        currentEvent = WorldEventType.Crosswalk;
    }

    public void NotifyAmbulanceSpawned()
    {
        waitingEventFinished = true;
        currentEvent = WorldEventType.Ambulance;
    }

    public void FinishCurrentEvent()
    {
        currentEvent = WorldEventType.None;
        NotifyEventFinished();
    }

    void ScheduleNextEvent()
    {
        nextEventTime = Random.Range(
            minEventInterval,
            maxEventInterval);

        timer = 0f;
    }
}