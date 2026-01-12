#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

public class TileGridVisualizer : MonoBehaviour
{
    [Header("Visualization Settings")]
    public bool showGrid = true;
    public bool highlightLoadedTiles = true;
    public bool highlightPlayerTile = true;
    public bool showTileCoordinates = false;

    [Header("Manual Alignment")]
    public Vector3 visualOffset = Vector3.zero;
    public Vector3 textOffset = new Vector3(3000f, 5f, 0);

    [Header("Colors")]
    public Color gridColor = Color.white;
    public Color loadedTileColor = new Color(0, 0, 1, 0.3f);
    public Color playerTileColor = new Color(0, 1, 0, 0.5f);
    public Color neighborTileColor = new Color(1, 0.5f, 0, 0.3f);

    private void OnDrawGizmos()
    {
        TileManager tileManager = TileManager.Instance;
        WorldRecenterManager worldRecenter = WorldRecenterManager.Instance;

        if (!showGrid || tileManager == null || tileManager.player == null || worldRecenter == null)
            return;

        Vector3 playerPos = tileManager.player.position;
        Vector3 worldOffset = worldRecenter.GetRecenterOffset();

        int range = 11;
        Vector2Int playerTile = tileManager.GetTileOfPosition(playerPos);

        for (int x = -range; x <= range; x++)
        {
            for (int y = -range; y <= range; y++)
            {
                Vector2Int tile = new Vector2Int(playerTile.x + x, playerTile.y + y);
                Vector3 tileCenter = CalculateTileCenter(tile, playerPos, worldOffset);

                if (tile.x < 0 || tile.x > tileManager.gridSize.x ||
                    tile.y < 0 || tile.y > tileManager.gridSize.y)
                    continue;

                // --- PRIORITY LOGIC ---
                Color tileColor = gridColor;
                float priorityHeightOffset = 0f;

                // 1. Player Tile (Highest Priority)
                if (highlightPlayerTile && tile == playerTile)
                {
                    tileColor = playerTileColor;
                    priorityHeightOffset = 0.03f; // Slightly higher to stay on top
                }
                // 2. Loaded Tiles
                else if (highlightLoadedTiles && tileManager.loadedTiles.ContainsKey(tile) && tileManager.loadedTiles[tile])
                {
                    tileColor = loadedTileColor;
                    priorityHeightOffset = 0.02f;
                }
                // 3. Neighbors
                else if (highlightPlayerTile && IsNeighborTile(tile, playerTile))
                {
                    tileColor = neighborTileColor;
                    priorityHeightOffset = 0.01f;
                }
                // 4. Default Grid (Lowest Priority)
                else
                {
                    tileColor = gridColor;
                    priorityHeightOffset = 0f;
                }

                DrawTile(tile, tileCenter + (Vector3.up * priorityHeightOffset), tileColor);
            }
        }
    }

    private Vector3 CalculateTileCenter(Vector2Int tile, Vector3 playerPos, Vector3 worldOffset)
    {
        return TileManager.StartPos - worldOffset + visualOffset - new Vector3(
            tile.x * TileManager.TileWidth,
            playerPos.y,
            tile.y * TileManager.TileHeight
        );
    }

    private void DrawTile(Vector2Int tile, Vector3 center, Color color)
    {
        float halfWidth = TileManager.TileWidth / 2f;
        float halfHeight = TileManager.TileHeight / 2f;

        Vector3[] corners = {
            center + new Vector3(-halfWidth, 0, -halfHeight),
            center + new Vector3(halfWidth, 0, -halfHeight),
            center + new Vector3(halfWidth, 0, halfHeight),
            center + new Vector3(-halfWidth, 0, halfHeight)
        };

        Gizmos.color = color;
        for (int i = 0; i < 4; i++)
        {
            Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
        }

        if (showTileCoordinates)
        {
            UnityEditor.Handles.Label(center + textOffset, $"tile_{tile.y}_{tile.x}");
        }
    }

    private bool IsNeighborTile(Vector2Int tile, Vector2Int playerTile)
    {
        return Mathf.Abs(tile.x - playerTile.x) <= 1 && Mathf.Abs(tile.y - playerTile.y) <= 1;
    }
}
#endif