#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

public class SpawnerDebugVisualizer : MonoBehaviour
{
    [System.Serializable]
    public class SpawnerDebugInfo
    {
        public PedestrianSpawner spawner;
        public Color color = Color.white;
        public bool enabled = true;
    }

    [System.Serializable]
    public class DebugSettings
    {
        public bool showBubbles = true;
        public bool showAgentLines = false;
        public bool showAgentInfo = false;
        public bool showSpawnPoints = false;
    }

    [Header("Spawner Assignments")]
    [SerializeField] private List<SpawnerDebugInfo> spawnerDebugInfos = new List<SpawnerDebugInfo>();

    [Header("Visual Settings")]
    public DebugSettings gizmoSettings = new DebugSettings();

    private void Awake()
    {
        // Auto-populate if empty
        if (spawnerDebugInfos.Count == 0)
        {
            var spawners = GetComponents<PedestrianSpawner>();
            foreach (var spawner in spawners)
            {
                var info = new SpawnerDebugInfo
                {
                    spawner = spawner,
                    enabled = true
                };

                // Auto-assign colors based on entity type
                if (spawner.entityType == EntityType.Pedestrian)
                {
                    info.color = Color.cyan;
                }
                else if (spawner.entityType == EntityType.Car)
                {
                    info.color = Color.red;
                }

                spawnerDebugInfos.Add(info);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!gizmoSettings.showBubbles) return;

        foreach (var info in spawnerDebugInfos)
        {
            if (!info.enabled || info.spawner == null || info.spawner.PlayerTransform == null)
                continue;

            DrawSpawnerBubbles(info.spawner, info.color);

            if (gizmoSettings.showAgentLines)
            {
                DrawAgentConnections(info.spawner, info.color);
            }
        }
    }

    private void DrawSpawnerBubbles(PedestrianSpawner spawner, Color color)
    {
        Vector3 playerPos = spawner.PlayerTransform.position;

        // Draw spawn radius
        Gizmos.color = new Color(color.r, color.g, color.b, 0.3f);
        Gizmos.DrawWireSphere(playerPos, spawner.SpawnRadius);

        // Draw despawn radius
        Gizmos.color = new Color(color.r, color.g, color.b, 0.1f);
        Gizmos.DrawWireSphere(playerPos, spawner.DespawnRadius);

        // Draw min spawn distance
        Gizmos.color = new Color(color.r, color.g, color.b, 0.5f);
        Gizmos.DrawWireSphere(playerPos, spawner.MinSpawnDistance);

        // Draw spawner info label
        Vector3 labelPos = playerPos + Vector3.up * (spawner.SpawnRadius + 50f);
        UnityEditor.Handles.Label(labelPos, $"{spawner.entityType}: {spawner.activeAgents.Count} agents");
    }

    private void DrawAgentConnections(PedestrianSpawner spawner, Color color)
    {
        if (spawner.PlayerTransform == null) return;

        Vector3 playerPos = spawner.PlayerTransform.position;
        Gizmos.color = new Color(color.r, color.g, color.b, 0.5f);

        foreach (var agent in spawner.activeAgents)
        {
            if (agent != null)
            {
                Gizmos.DrawLine(playerPos, agent.transform.position);

                // Draw agent sphere
                Gizmos.DrawWireSphere(agent.transform.position, 2f);
            }
        }
    }
}
#endif