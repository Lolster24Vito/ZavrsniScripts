using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class AdaptiveOpenScenesManager : MonoBehaviour
{

    public TileManager tileManager;

    public float checkInterval = 15.0f; // Time between FPS checks
    public int targetFPS = 72;
    public int targetFPSRange = 8;
    public int minOpenScenes = 4;
    public int maxOpenScenesLimit = 20;
    public bool autoStablizeOpenScenes = false;
    private float deltaTime = 0.0f;
    float currentFPS;
    void Start()
    {
        //set fps to 120
        SetRefreshRateTo120();
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
    private  void SetRefreshRateTo90()
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
    }
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
    }
}