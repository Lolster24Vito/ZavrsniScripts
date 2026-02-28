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

        // Totals for the whole world
        int totalPeds = 0;
        int totalCars = 0;
        int activeSpawners = 0;

        // First pass: Calculate totals
        foreach (var spawner in allSpawners)
        {
            if (spawner != null && spawner.isActiveAndEnabled)
            {
                activeSpawners++;
                // We use GetChildCount() because clones are children of the tileContainer
                // but might not be in the spawner's internal 'activeAgents' list.
                int count = (spawner.tileContainer != null) ? spawner.tileContainer.childCount : 0;

                if (spawner.entityType == EntityType.Pedestrian) totalPeds += count;
                else totalCars += count;
            }
        }

        DrawSectionHeader($"NPC TOTALS (Spawners Active: {activeSpawners})");
        DrawLabel($"Total Pedestrians: {totalPeds}", indent: 10);
        DrawLabel($"Total Cars: {totalCars}", indent: 10);
        DrawLabel($"Combined: {totalPeds + totalCars}", indent: 10);

        AddSpacing();

        if (showNPCs)
        {
            DrawSectionHeader("SPAWNER BREAKDOWN");
            foreach (var spawner in allSpawners)
            {
                if (spawner != null && spawner.isActiveAndEnabled)
                {
                    int childCount = (spawner.tileContainer != null) ? spawner.tileContainer.childCount : 0;
                    string type = spawner.entityType == EntityType.Pedestrian ? "PED" : "CAR";

                    // Format: [PED] Tile (4,1): 32 NPCs
                    DrawLabel($"[{type}] Tile {spawner.currentTile}: {childCount} NPCs", indent: 10);
                }
            }
        }

        if (activeSpawners == 0) DrawLabel("No active spawners");
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