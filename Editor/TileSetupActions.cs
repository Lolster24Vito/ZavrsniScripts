using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class TileSetupActions
{
    private const string PEDESTRIAN_PREFAB_PATH = "Assets/prefabs/npc/casual_Male_G.prefab";
    private const string CAR_PREFAB_PATH = "Assets/prefabs/npc/Vehicle_Police Car.prefab";

    [MenuItem("GameObject/City Tools/Setup Tile Components", false, 10)]
    private static void SetupTileFromMenu(MenuCommand menuCommand)
    {
        GameObject tileObject = menuCommand.context as GameObject;
        if (tileObject == null) return;

        if (!IsPotentialTile(tileObject))
        {
            Debug.LogWarning($"'{tileObject.name}' is not a valid setup target.");
            return;
        }

        Undo.RecordObject(tileObject, $"Setup Tile {tileObject.name}");

        // 1. Setup the main components
        AddTileComponents(tileObject);

        // 2. Link all paths/roads to the master list
        ConfigureTilePedestrianPoints(tileObject);

        EditorUtility.SetDirty(tileObject);
    }

    [MenuItem("GameObject/City Tools/Setup Tile Components", true)]
    private static bool ValidateSetupTile()
    {
        return Selection.activeGameObject != null && IsPotentialTile(Selection.activeGameObject);
    }

    private static bool IsPotentialTile(GameObject go)
    {
        // Now valid if it's the main tile OR one of the path/road containers
        return go != null && (go.name.StartsWith("tile_") || IsPathName(go.name) || IsRoadName(go.name));
    }

    private static bool IsPathName(string name)
    {
        string n = name.ToLower();
        return n.Contains("paths") || n.Contains("footway") || n.Contains("steps") || n.Contains("cycleway");
    }

    private static bool IsRoadName(string name)
    {
        string n = name.ToLower();
        return n.Contains("roads") || n.Contains("unclassified") || n.Contains("residential") || n.Contains("service");
    }

    private static void AddTileComponents(GameObject tileObject)
    {
        AddComponentIfMissing<TilePedestrianPoints>(tileObject);
        AddComponentIfMissing<PedestrianSpawner>(tileObject);
        AddComponentIfMissing<ObjectOffsetter>(tileObject);

        AutoConfigureChildren(tileObject);
    }

    private static void AddComponentIfMissing<T>(GameObject go) where T : Component
    {
        if (go.GetComponent<T>() == null) Undo.AddComponent<T>(go);
    }

    private static void AutoConfigureChildren(GameObject parent)
    {
        Vector2Int parentTile = ParseTileCoordinates(parent.name);

        // We use GetComponentsInChildren to make sure we find "None_paths" even if they are nested
        MeshFilter[] allMeshFilters = parent.GetComponentsInChildren<MeshFilter>(true);

        foreach (MeshFilter mf in allMeshFilters)
        {
            GameObject child = mf.gameObject;
            bool isPath = IsPathName(child.name);
            bool isRoad = IsRoadName(child.name);

            if (isPath || isRoad)
            {
                ConfigureMeshToWorldPoints(child, parentTile, isPath);
            }
        }
    }

    private static Vector2Int ParseTileCoordinates(string tileName)
    {
        // If we clicked a "None_" object, it won't have coordinates. 
        // We should try to find a parent or child that HAS the tile name.
        if (!tileName.StartsWith("tile_"))
        {
            // Placeholder: find coordinates from context or default to 0,0
            return Vector2Int.zero;
        }

        string[] parts = tileName.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 3 && parts[0] == "tile")
        {
            if (int.TryParse(parts[1], out int y) && int.TryParse(parts[2], out int x))
                return new Vector2Int(x, y);
        }
        return Vector2Int.zero;
    }

    private static void ConfigureMeshToWorldPoints(GameObject child, Vector2Int parentTile, bool isPath)
    {
        MeshToWorldPoints points = child.GetComponent<MeshToWorldPoints>();
        if (points == null) points = Undo.AddComponent<MeshToWorldPoints>(child);

        Undo.RecordObject(points, "Set MeshToWorldPoints properties");
        points.Tile = parentTile;
        points.Type = isPath ? NodeType.Sidewalk : NodeType.Road;
    }

    private static void ConfigureTilePedestrianPoints(GameObject tileObject)
    {
        Vector2Int parentTile = ParseTileCoordinates(tileObject.name);
        TilePedestrianPoints tilePoints = tileObject.GetComponent<TilePedestrianPoints>();
        if (tilePoints == null) return;

        SerializedObject tilePointsSO = new SerializedObject(tilePoints);
        tilePointsSO.FindProperty("tile").vector2IntValue = parentTile;

        SerializedProperty meshListProp = tilePointsSO.FindProperty("meshToWorldPoints");

        // Find ALL objects with MeshToWorldPoints in children (including the "None_" ones)
        MeshToWorldPoints[] allPoints = tileObject.GetComponentsInChildren<MeshToWorldPoints>(true);

        foreach (MeshToWorldPoints m in allPoints)
        {
            if (!ListContains(meshListProp, m))
            {
                meshListProp.arraySize++;
                meshListProp.GetArrayElementAtIndex(meshListProp.arraySize - 1).objectReferenceValue = m;
            }
        }
        tilePointsSO.ApplyModifiedProperties();

        // --- Setup Spawners ---
        ConfigureSpawners(tileObject, parentTile, tilePointsSO);
    }

    private static void ConfigureSpawners(GameObject tileObject, Vector2Int parentTile, SerializedObject tilePointsSO)
    {
        PedestrianSpawner[] spawners = tileObject.GetComponents<PedestrianSpawner>();
        while (spawners.Length < 2)
        {
            Undo.AddComponent<PedestrianSpawner>(tileObject);
            spawners = tileObject.GetComponents<PedestrianSpawner>();
        }

        GameObject pedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PEDESTRIAN_PREFAB_PATH);
        GameObject carPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CAR_PREFAB_PATH);

        spawners[0].SetGameObjectToSpawn(pedPrefab);
        spawners[0].SetEntityType(EntityType.Pedestrian);
        spawners[1].SetGameObjectToSpawn(carPrefab);
        spawners[1].SetEntityType(EntityType.Car);

        foreach (var spawner in spawners)
        {
            SerializedObject spawnerSO = new SerializedObject(spawner);
            spawnerSO.FindProperty("currentTile").vector2IntValue = parentTile;
            spawnerSO.ApplyModifiedProperties();

            SerializedProperty spawnerListProp = tilePointsSO.FindProperty("spawners");
            if (!ListContains(spawnerListProp, spawner))
            {
                spawnerListProp.arraySize++;
                spawnerListProp.GetArrayElementAtIndex(spawnerListProp.arraySize - 1).objectReferenceValue = spawner;
            }
        }
        tilePointsSO.ApplyModifiedProperties();
    }

    private static bool ListContains(SerializedProperty listProp, UnityEngine.Object obj)
    {
        for (int i = 0; i < listProp.arraySize; i++)
        {
            if (listProp.GetArrayElementAtIndex(i).objectReferenceValue == obj) return true;
        }
        return false;
    }
}