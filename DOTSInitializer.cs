// DOTSInitializer.cs
using UnityEngine;
using Unity.Entities;

[DefaultExecutionOrder(-1000)]
public class DOTSInitializer : MonoBehaviour
{
    public static bool IsInitialized { get; private set; }

    private void Awake()
    {
        if (World.DefaultGameObjectInjectionWorld == null)
        {
            DefaultWorldInitialization.DefaultLazyEditModeInitialize();
        }
        StartCoroutine(ConfirmInitialization());
    }

    private System.Collections.IEnumerator ConfirmInitialization()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        // Wait for critical systems to be ready
        while (World.DefaultGameObjectInjectionWorld == null ||
               !World.DefaultGameObjectInjectionWorld.IsCreated)
        {
            yield return null;
        }

        IsInitialized = true;
        Debug.Log("[DOTSInitializer] DOTS World initialized successfully.");
    }
}