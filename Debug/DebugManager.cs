using System.Collections.Generic;
using UnityEngine;

public class DebugManager : MonoBehaviour
{
    public static DebugManager Instance { get; private set; }

    [Header("Debug Categories")]
    public bool showTileSystem = true;
    public bool showSpawners = true;
    public bool showNPCs = true;
    public bool showWorldOffsets = true;

    [Header("GUI Settings")]
    public bool enableGUI = true;
    public Vector2 guiPosition = new Vector2(10, 10);
    public int fontSize = 12;
    public float sectionSpacing = 10f;

    [Header("Gizmo Settings")]
    public bool enableGizmos = true;
    public bool showInGameView = false;

    // GUI layout tracking
    private float currentY;
    private float lineHeight;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnGUI()
    {
        if (!enableGUI) return;

        // Reset GUI layout for this frame
        currentY = guiPosition.y;
        lineHeight = fontSize + 5;
        GUI.skin.label.fontSize = fontSize;

        // Draw sections in order
        if (showTileSystem) DrawTileSystemSection();
        if (showSpawners) DrawSpawnersSection();
        if (showWorldOffsets) DrawWorldOffsetsSection();
    }

    private void DrawTileSystemSection()
    {
        TileManager tileManager = TileManager.Instance;
        if (tileManager == null) return;

        DrawSectionHeader("TILE SYSTEM");

        DrawLabel($"Player Tile: {TileManager.PlayerOnTile}");
        DrawLabel($"Loaded Tiles: {CountLoadedTiles(tileManager)}");
        DrawLabel($"Max Scenes: {tileManager.GetMaxOpenScenes()}");

        AddSpacing();
    }

    private void DrawSpawnersSection()
    {
        PedestrianSpawner[] allSpawners = FindObjectsOfType<PedestrianSpawner>(true);
        if (allSpawners.Length == 0) return;

        DrawSectionHeader($"SPAWNERS ({allSpawners.Length})");

        int activeCount = 0;
        foreach (var spawner in allSpawners)
        {
            if (spawner != null && spawner.isActiveAndEnabled)
            {
                activeCount++;
                string entityName = spawner.entityType == EntityType.Pedestrian ? "Ped" : "Car";
                DrawLabel($"  {entityName}: {spawner.activeAgents.Count} agents", indent: 10);

                // Optional: Show more details
                if (showNPCs && spawner.activeAgents.Count > 0)
                {
                    DrawLabel($"    Tile: {spawner.currentTile}", indent: 20);
                    DrawLabel($"    Offset: {spawner.WorldOffset}", indent: 20);
                }
            }
        }

        if (activeCount == 0)
        {
            DrawLabel("No active spawners");
        }

        AddSpacing();
    }

    private void DrawWorldOffsetsSection()
    {
        WorldRecenterManager worldRecenter = WorldRecenterManager.Instance;
        if (worldRecenter == null) return;

        DrawSectionHeader("WORLD OFFSETS");

        DrawLabel($"Offset: {worldRecenter.GetRecenterOffset()}");
        DrawLabel($"Custom Offset: {worldRecenter.GetCustomWorldOffsetWithoutFirst()}");

        // Show offset dictionary if needed
        if (TileManager.EntityWorldRecenterOffsets.Count > 0)
        {
            DrawLabel($"Tile Offsets: {TileManager.EntityWorldRecenterOffsets.Count}");
            foreach (var entry in TileManager.EntityWorldRecenterOffsets)
            {
                if (entry.Value != Vector3.zero)
                {
                    DrawLabel($"  Tile {entry.Key}: {entry.Value}", indent: 10);
                }
            }
        }
    }

    private void DrawSectionHeader(string title)
    {
        GUI.Label(new Rect(guiPosition.x, currentY, 500, lineHeight), $"=== {title} ===");
        currentY += lineHeight;
    }

    private void DrawLabel(string text, int indent = 0)
    {
        GUI.Label(new Rect(guiPosition.x + indent, currentY, 500 - indent, lineHeight), text);
        currentY += lineHeight;
    }

    private void AddSpacing()
    {
        currentY += sectionSpacing;
    }

    private int CountLoadedTiles(TileManager tileManager)
    {
        if (tileManager == null) return 0;
        int count = 0;
        foreach (var tile in tileManager.loadedTiles)
        {
            if (tile.Value) count++;
        }
        return count;
    }
}