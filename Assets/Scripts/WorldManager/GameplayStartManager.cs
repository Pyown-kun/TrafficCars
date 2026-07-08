using UnityEngine;

public class GameplayStartManager : MonoBehaviour
{
    public static GameplayStartManager Instance { get; private set; }

    [Header("Components Enabled When Gameplay Starts")]
    [SerializeField] private Behaviour[] gameplayComponents;

    [Header("Auto Start Gameplay")]
    [SerializeField] private bool autoStartGameplay = false;

    private bool gameplayStarted;

    public bool GameplayStarted => gameplayStarted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (autoStartGameplay)
        {
            StartGameplay();
        }
    }

    public void StartGameplay()
    {
        if (gameplayStarted)
            return;

        gameplayStarted = true;

        foreach (Behaviour component in gameplayComponents)
        {
            if (component != null)
                component.enabled = true;
        }
    }

    public void StopGameplay()
    {
        gameplayStarted = false;

        foreach (Behaviour component in gameplayComponents)
        {
            if (component != null)
                component.enabled = false;
        }
    }

    public void ToggleGameplay()
    {
        if (gameplayStarted)
            StopGameplay();
        else
            StartGameplay();
    }
}