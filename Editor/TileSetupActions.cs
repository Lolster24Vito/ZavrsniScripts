using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


/// <summary>
/// This class provides a right-click context menu in the Hierarchy to set up tile GameObjects.
/// This approach avoids overriding the default GameObject inspector, preserving standard
/// functionality like the enable/disable checkbox.
/// </summary>
public static class TileSetupActions
{
    // --- Paths to Prefabs ---
    private const string PEDESTRIAN_PREFAB_PATH = "Assets/prefabs/npc/casual_Male_G.prefab";
    private const string CAR_PREFAB_PATH = "Assets/prefabs/npc/Vehicle_Police Car.prefab";

    /// <summary>
    /// Adds a "Setup Tile Components" option to the right-click menu for any GameObject.
    /// The 'priority' parameter (10) helps position it in the menu.
    /// </summary>
    [MenuItem("GameObject/City Tools/Setup Tile Components", false, 10)]
    private static void SetupTileFromMenu(MenuCommand menuCommand)
    {
        // The context is the GameObject that was right-clicked.
        GameObject tileObject = menuCommand.context as GameObject;
        if (tileObject == null)
        {
            Debug.LogError("Could not get GameObject from context menu.");
            return;
        }

        // Check if the object is a potential tile before proceeding.
        if (!IsPotentialTile(tileObject))
        {
            Debug.LogWarning($"'{tileObject.name}' is not a valid tile. It must start with 'tile_'.");
            return;
        }

        Debug.Log($"[TileSetupActions] Setting up tile: {tileObject.name}");

        // Record the entire object for a single "Undo" action.
        Undo.RecordObject(tileObject, $"Setup Tile {tileObject.name}");

        // Execute the setup logic.
        AddTileComponents(tileObject);
        ConfigureTilePedestrianPoints(tileObject);

        // Mark the object and its children as "dirty" to ensure changes are saved.
        EditorUtility.SetDirty(tileObject);
        foreach (Transform child in tileObject.transform)
        {
            EditorUtility.SetDirty(child.gameObject);
        }
    }

    /// <summary>
    /// Validates the menu item. This method is called by Unity to determine if the
    /// menu item should be enabled or grayed out.
    /// </summary>
    [MenuItem("GameObject/City Tools/Setup Tile Components", true)]
    private static bool ValidateSetupTile()
    {
        // Only enable the menu option if a single GameObject is selected and it's a potential tile.
        return Selection.activeGameObject != null && IsPotentialTile(Selection.activeGameObject);
    }

    // ===================================================================
    // All your original logic has been moved below as static helper methods.
    // ===================================================================

    private static bool IsPotentialTile(GameObject go)
    {
        return go != null && go.name.StartsWith("tile_");
    }

    private static void AddTileComponents(GameObject tileObject)
    {
        // Add required components if missing
        AddComponentIfMissing<TilePedestrianPoints>(tileObject);
        AddComponentIfMissing<PedestrianSpawner>(tileObject);
        AddComponentIfMissing<ObjectOffsetter>(tileObject);

        // Automatically configure children
        AutoConfigureChildren(tileObject);
    }

    private static void AddComponentIfMissing<T>(GameObject go) where T : Component
    {
        if (go.GetComponent<T>() == null)
        {
            // We use Undo.AddComponent to make the addition undoable.
            Undo.AddComponent<T>(go);
        }
    }

    private static void AutoConfigureChildren(GameObject parent)
    {
        Vector2Int parentTile = ParseTileCoordinates(parent.name);

        foreach (Transform child in parent.transform)
        {
            string childName = child.name;
            bool isPath = childName.Contains("osm_paths");

            if (childName.Contains("osm_paths") || childName.Contains("osm_roads"))
            {
                ConfigureMeshToWorldPoints(child.gameObject, parentTile, isPath);
            }
        }
    }

    private static Vector2Int ParseTileCoordinates(string tileName)
    {
        string[] parts = tileName.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length >= 3 && parts[0] == "tile")
        {
            if (int.TryParse(parts[1], out int y) && int.TryParse(parts[2], out int x))
            {
                return new Vector2Int(x, y);
            }
        }

        Debug.LogError($"Invalid tile name format: {tileName}");
        return Vector2Int.zero;
    }

    private static void ConfigureMeshToWorldPoints(GameObject child, Vector2Int parentTile, bool isPath)
    {
        MeshToWorldPoints points = child.GetComponent<MeshToWorldPoints>();
        if (points == null)
        {
            points = Undo.AddComponent<MeshToWorldPoints>(child);
        }

        // Set properties with undo support
        Undo.RecordObject(points, "Set MeshToWorldPoints properties");
        points.Tile = parentTile;
        points.Type = isPath ? NodeType.Sidewalk : NodeType.Road;
    }

    private static void ConfigureTilePedestrianPoints(GameObject tileObject)
    {
        Vector2Int parentTile = ParseTileCoordinates(tileObject.name);
        TilePedestrianPoints tilePoints = tileObject.GetComponent<TilePedestrianPoints>();
        if (tilePoints == null)
        {
            Debug.LogWarning("TilePedestrianPoints component not found on tile object.");
            return;
        }

        // Configure TilePedestrianPoints
        SerializedObject tilePointsSO = new SerializedObject(tilePoints);
        tilePointsSO.FindProperty("tile").vector2IntValue = parentTile;

        SerializedProperty meshListProp = tilePointsSO.FindProperty("meshToWorldPoints");
        foreach (Transform child in tileObject.transform)
        {
            if (child.name.Contains("osm_paths") || child.name.Contains("osm_roads"))
            {
                MeshToWorldPoints m = child.GetComponent<MeshToWorldPoints>();
                if (m != null && !ListContains(meshListProp, m))
                {
                    meshListProp.arraySize++;
                    meshListProp.GetArrayElementAtIndex(meshListProp.arraySize - 1).objectReferenceValue = m;
                }
            }
        }
        tilePointsSO.ApplyModifiedProperties();

        // --- Handle PedestrianSpawner components ---
        PedestrianSpawner[] spawners = tileObject.GetComponents<PedestrianSpawner>();
        PedestrianSpawner pedestrianSpawner = null;
        PedestrianSpawner carSpawner = null;

        if (spawners.Length == 0)
        {
            pedestrianSpawner = Undo.AddComponent<PedestrianSpawner>(tileObject);
            carSpawner = Undo.AddComponent<PedestrianSpawner>(tileObject);
        }
        else if (spawners.Length == 1)
        {
            pedestrianSpawner = spawners[0];
            carSpawner = Undo.AddComponent<PedestrianSpawner>(tileObject);
        }
        else
        {
            pedestrianSpawner = spawners[0];
            carSpawner = spawners[1];
        }

        // Load prefabs
        GameObject pedestrianPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PEDESTRIAN_PREFAB_PATH);
        GameObject carPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CAR_PREFAB_PATH);

        if (pedestrianPrefab == null) Debug.LogError($"Failed to load pedestrian prefab at: {PEDESTRIAN_PREFAB_PATH}");
        if (carPrefab == null) Debug.LogError($"Failed to load car prefab at: {CAR_PREFAB_PATH}");

        // Configure spawners
        pedestrianSpawner.SetGameObjectToSpawn(pedestrianPrefab);
        pedestrianSpawner.SetEntityType(EntityType.Pedestrian);
        carSpawner.SetGameObjectToSpawn(carPrefab);
        carSpawner.SetEntityType(EntityType.Car);

        // Set tile for both spawners
        foreach (var spawner in new PedestrianSpawner[] { pedestrianSpawner, carSpawner })
        {
            SerializedObject spawnerSO = new SerializedObject(spawner);
            spawnerSO.FindProperty("currentTile").vector2IntValue = parentTile;
            spawnerSO.ApplyModifiedProperties();
        }

        // Add spawners to TilePedestrianPoints list
        SerializedProperty spawnerListProp = tilePointsSO.FindProperty("spawners");
        AddToSpawnerListIfMissing(spawnerListProp, pedestrianSpawner);
        AddToSpawnerListIfMissing(spawnerListProp, carSpawner);
        tilePointsSO.ApplyModifiedProperties();
    }

    private static void AddToSpawnerListIfMissing(SerializedProperty spawnerListProp, PedestrianSpawner spawner)
    {
        if (!ListContains(spawnerListProp, spawner))
        {
            spawnerListProp.arraySize++;
            spawnerListProp.GetArrayElementAtIndex(spawnerListProp.arraySize - 1).objectReferenceValue = spawner;
        }
    }

    private static bool ListContains(SerializedProperty listProp, UnityEngine.Object obj)
    {
        for (int i = 0; i < listProp.arraySize; i++)
        {
            if (listProp.GetArrayElementAtIndex(i).objectReferenceValue == obj)
                return true;
        }
        return false;
    }
}