using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldRecenterManager : MonoBehaviour
{
    [SerializeField] Transform player;
    private Rigidbody playerRb;

    [SerializeField] Transform wholeTerrain;

    public static event Action<Vector3> OnWorldRecentered; // Event to notify listeners about the offset

    public static WorldRecenterManager Instance { get; private set; }
    private Vector3 worldOffset = Vector3.zero;
   [SerializeField] private float recenterDistance = 1000f;
    private Vector3 lastOffset=Vector3.zero;
    private  Vector3 customWorldOffsetWithoutFirst = Vector3.zero;


    bool firstTime = true;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    private void OnEnable()
    {
        GameEventsManager.Instance.inputEvents.onQuestLogTogglePressed += RecenterWorld;
    }
    private void OnDisable()
    {
        GameEventsManager.Instance.inputEvents.onQuestLogTogglePressed -= RecenterWorld;
    }
    private void Start()
    {
        playerRb = player.GetComponent<Rigidbody>();
    }
    private void RecenterWorld()
    {
        if (!player)
        {
            Debug.LogError("Player Transform is not assigned.");
            return;
        }

        Vector3 offset = new Vector3(player.position.x, 0f, player.position.z);
        worldOffset += offset;
        float playerY = player.position.y;
        Debug.Log("Vito offset orignal is: " + offset);
        Vector3 wholeTerrainOffset = new Vector3(offset.x / 5f, 0f, offset.z / 5f);
        Vector3 terrainPosition = wholeTerrain.localPosition - wholeTerrainOffset;
        wholeTerrain.localPosition = terrainPosition;
        // Trigger the event and pass the offset
        // Reset player position to origin
        Vector3 playerPosition = new Vector3(0f, playerY, 0f);
        player.GetComponent<Rigidbody>().Move(playerPosition, player.rotation);
        player.transform.position = playerPosition;
        OnWorldRecentered?.Invoke(offset);
        lastOffset = offset;
        if (!firstTime)
        {
            customWorldOffsetWithoutFirst += offset;
        }
            firstTime = false;
        Debug.Log($"World recentered. Offset applied: {offset}");
    }
    public Vector3 GetRecenterOffset()
    {
        return worldOffset;
    }
    public Vector3 GetCustomWorldOffsetWithoutFirst()
    {
        return customWorldOffsetWithoutFirst;
    }

    private void Update()
    {
        // Calculate the player's position ignoring the Y axis
        Vector3 horizontalPosition = new Vector3(player.position.x, 0f, player.position.z);
        // Automatically recenter if the player moves too far horizontally
        if (horizontalPosition.magnitude > recenterDistance) // Threshold distance
        {
            RecenterWorld();
        }
    }
}
