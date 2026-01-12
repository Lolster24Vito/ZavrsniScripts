using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class AdaptiveOpenScenesManager : MonoBehaviour
{

    public TileManager tileManager;

    public float checkInterval = 12f; // Time between FPS checks
    public int targetFPS = 72;
    public int targetFPSRange = 8;
    public int minOpenScenes = 4;
    public int criticalMinScenes = 2; // The absolute floor (Player only)
    public int maxOpenScenesLimit = 9;
    public bool autoStablizeOpenScenes = false;
    private float deltaTime = 0.0f;
    float currentFPS;
    // FPS Calculation
    private float fpsAccumulator = 0f;
    private int fpsSamples = 0;

    private int consecutiveLowFPSFrames = 0;
    private int consecutiveHighFPSFrames = 0;
    void Start()
    {
        SetRefreshRateTo72();
        //SetRefreshRateTo90();
        //set fps to 120
        // SetRefreshRateTo120();
        if (autoStablizeOpenScenes)
        {
            StartCoroutine(AdjustTileLoading());
        }
    }

    private void SetRefreshRateTo120()
    {
        // Query all available display frequencies
        float[] availableFreqs = OVRPlugin.systemDisplayFrequenciesAvailable;
        foreach (float freq in availableFreqs)
        {
            Debug.Log($"Quest 2 supports: {freq} Hz");
        }

        // If 120 Hz is available, switch to it
        const float desiredHz = 120.0f;
        if (System.Array.Exists(availableFreqs, f => Mathf.Approximately(f, desiredHz)))
        {
            OVRPlugin.systemDisplayFrequency = desiredHz;
            Debug.Log($"Requested {desiredHz} Hz refresh rate");
        }
        else
        {
            Debug.LogWarning($"{desiredHz} Hz not available—running at default refresh");
        }
    }
    private void SetRefreshRateTo72()
    {
        // Query all available display frequencies
        float[] availableFreqs = OVRPlugin.systemDisplayFrequenciesAvailable;
        foreach (float freq in availableFreqs)
        {
            Debug.Log("Quest 2 supports: " + freq + " Hz");
        }

        // If 72 Hz is available, switch to it
        if (System.Array.Exists(availableFreqs, f => Mathf.Approximately(f, 72.0f)))
        {
            OVRPlugin.systemDisplayFrequency = 72.0f;
            Debug.Log("Requested 72 Hz refresh rate");
        }
        else
        {
            Debug.LogWarning("72 Hz not available—running at default refresh");
        }
    }
    private void SetRefreshRateTo90()
    {
        // Query all available display frequencies
        float[] availableFreqs = OVRPlugin.systemDisplayFrequenciesAvailable;
        foreach (float freq in availableFreqs)
        {
            Debug.Log("Quest 2 supports: " + freq + " Hz");
        }

        // If 90 Hz is among them, switch to it
        if (System.Array.Exists(availableFreqs, f => Mathf.Approximately(f, 90.0f)))
        {
            OVRPlugin.systemDisplayFrequency = 90.0f;
            Debug.Log("Requested 90 Hz refresh rate");
        }
        else
        {
            Debug.LogWarning("90 Hz not available—running at default refresh");
        }
    }

    void Update()
    {
        currentFPS = 1.0f / Time.unscaledDeltaTime;
        fpsAccumulator += currentFPS;
        fpsSamples++;
    }
    IEnumerator AdjustTileLoading()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);

            float averageFPS = 0;
            if (fpsSamples > 0) averageFPS = fpsAccumulator / fpsSamples;
            // Reset accumulator for next interval
            fpsAccumulator = 0;
            fpsSamples = 0;

            int currentMaxScenes = tileManager.GetMaxOpenScenes();
            // Track consecutive frames of low/high FPS to avoid overreacting to temporary spikes
            if (averageFPS < targetFPS)
            {
                consecutiveLowFPSFrames++;
                consecutiveHighFPSFrames = 0;
            }
            else if (averageFPS > targetFPS + targetFPSRange)
            {
                consecutiveHighFPSFrames++;
                consecutiveLowFPSFrames = 0;
            }
            else
            {
                // FPS is in acceptable range
                consecutiveLowFPSFrames = 0;
                consecutiveHighFPSFrames = 0;
            }

            // Only adjust after multiple consecutive checks to avoid oscillation
            if (consecutiveLowFPSFrames >= 1) // React faster using average
            {
                // If FPS is consistently low, reduce max open scenes
                if (tileManager.GetMaxOpenScenes() > minOpenScenes)
                {
                    tileManager.DecreaseMaxOpenScenes();
                    Debug.Log($"Performance issue detected. Reducing max open scenes to {tileManager.GetMaxOpenScenes()}");
                }
                // If at minimum and still having issues, go into critical mode
                else if (tileManager.GetMaxOpenScenes() <= minOpenScenes && consecutiveLowFPSFrames >= 3)
                {
                    Debug.Log("Critical performance mode: Only player tile will be kept loaded");
                    // This would require additional implementation in TileManager
                }
            }
            else if (consecutiveHighFPSFrames >= 2)
            {
                // If FPS is consistently high, increase max open scenes
                if (tileManager.GetMaxOpenScenes() < maxOpenScenesLimit)
                {
                    tileManager.IncreaseMaxOpenScenes();
                    Debug.Log($"Performance is good. Increasing max open scenes to {tileManager.GetMaxOpenScenes()}");
                }
            }
        }
        /*
        IEnumerator AdjustTileLoading()
        {
            while (true)
            {
                yield return new WaitForSeconds(checkInterval);

                if (currentFPS < targetFPS && tileManager.GetMaxOpenScenes() > minOpenScenes)
                {
                    tileManager.DecreaseMaxOpenScenes();
                }
                else if (currentFPS > targetFPS+ targetFPSRange && tileManager.GetMaxOpenScenes() < maxOpenScenesLimit)
                {
                    tileManager.IncreaseMaxOpenScenes();
                }
            }
        }*/
    }
}