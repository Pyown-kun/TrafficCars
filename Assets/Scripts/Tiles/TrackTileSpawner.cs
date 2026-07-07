using System.Collections.Generic;
using UnityEngine;

public class TrackTileSpawner : MonoBehaviour
{
    [Header("Tile Prefabs")]
    [SerializeField]
    private List<TrackTile> tilePrefabs = new();

    [Header("References")]
    [SerializeField]
    private Transform spawnPoint;

    [SerializeField]
    private Transform destroyPoint;

    [Header("Spawn Settings")]
    [SerializeField]
    private int initialTiles = 5;

    [Tooltip("Semakin besar nilainya maka tile berikutnya akan muncul lebih cepat.")]
    [SerializeField]
    private float spawnOffset = -4f;

    [Tooltip("Tambahan jarak sebelum tile dihancurkan.")]
    [SerializeField]
    private float destroyOffset = 0f;

    [SerializeField]
    private bool drawSpawnGizmo = true;

    private readonly List<TrackTile> activeTiles = new();

    private int nextPrefabIndex;

    public Transform SpawnPoint => spawnPoint;
    public Transform DestroyPoint => destroyPoint;
    private int spawnedCount;

    private void Start()
    {
        spawnedCount = 0;

        SpawnTile();
    }

    private void Update()
    {
        SpawnLogic();

        DestroyLogic();
    }

    //==================================================
    // SPAWN
    //==================================================

    private void SpawnInitialTiles()
    {
        for (int i = 0; i < initialTiles; i++)
        {
            SpawnTile();
        }
    }

    private void SpawnLogic()
    {
        while (activeTiles.Count < initialTiles)
        {
            if (!CanSpawn())
                return;

            SpawnTile();
        }
    }

    private bool CanSpawn()
    {
        foreach (TrackTile tile in activeTiles)
        {
            if (tile == null)
                continue;

            if (tile.BackAnchor.position.z >
                spawnPoint.position.z + spawnOffset)
            {
                return false;
            }
        }

        return true;
    }

    private void SpawnTile()
    {
        if (tilePrefabs.Count == 0)
            return;

        TrackTile prefab = GetNextPrefab();

        TrackTile tile = Instantiate(
            prefab,
            spawnPoint.position,
            Quaternion.identity,
            transform);

        tile.Initialize(this);

        activeTiles.Add(tile);
    }

    //==================================================
    // DESTROY
    //==================================================

    private void DestroyLogic()
    {
        for (int i = activeTiles.Count - 1; i >= 0; i--)
        {
            TrackTile tile = activeTiles[i];

            if (tile == null)
            {
                activeTiles.RemoveAt(i);
                continue;
            }

            if (tile.BackAnchor.position.z <=
                destroyPoint.position.z - destroyOffset)
            {
                Destroy(tile.gameObject);

                activeTiles.RemoveAt(i);
            }
        }
    }

    //==================================================
    // PREFAB
    //==================================================

    private TrackTile GetNextPrefab()
    {
        TrackTile prefab = tilePrefabs[nextPrefabIndex];

        nextPrefabIndex++;

        if (nextPrefabIndex >= tilePrefabs.Count)
            nextPrefabIndex = 0;

        return prefab;
    }

    //==================================================
    // GIZMOS
    //==================================================

    private void OnDrawGizmos()
    {
        if (!drawSpawnGizmo || spawnPoint == null)
            return;

        Gizmos.color = Color.green;

        Vector3 pos = spawnPoint.position;
        pos.z += spawnOffset;

        Gizmos.DrawWireCube(
            pos,
            new Vector3(8f, 0.2f, 0.5f));

        if (destroyPoint != null)
        {
            Gizmos.color = Color.red;

            Vector3 destroyPos = destroyPoint.position;
            destroyPos.z -= destroyOffset;

            Gizmos.DrawWireCube(
                destroyPos,
                new Vector3(8f, 0.2f, 0.5f));
        }
    }
}