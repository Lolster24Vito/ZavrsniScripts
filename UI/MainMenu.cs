using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{

    [Header("UI References")]
    [Tooltip("Assign your Settings Canvas/Panel here.")]
    [SerializeField] private GameObject settingsPanel;


    [Header("Scene References")]
    [SerializeField] private AssetReference mainSceneReference;

    private AsyncOperationHandle<SceneInstance> sceneLoadHandle;

    // Start is called before the first frame update
    void Start()
    {
        // Ensure the settings panel is hidden when the menu first loads
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }
    public void StartGame()
    {
        if (mainSceneReference == null)
        {
            Debug.LogError("[MainMenu] Main Scene Reference is not assigned!");
            return;
        }

        Debug.Log("[MainMenu] Loading main scene...");

        // Use Addressables to load the scene asynchronously. 
        // LoadSceneMode.Single replaces the MainMenu scene entirely.
        sceneLoadHandle = Addressables.LoadSceneAsync(mainSceneReference, LoadSceneMode.Single);
    }
    /*
    private void LoadMainScene()
    {
        // Load the main scene using Addressables
        sceneLoadHandle = Addressables.LoadSceneAsync(mainSceneReference, LoadSceneMode.Single);
        /*
        // Update loading progress if slider is available
        while (!sceneLoadHandle.IsDone)
        {
            if (loadingSlider != null)
            {
                loadingSlider.value = sceneLoadHandle.PercentComplete;
            }
            yield return null;
        }*/
    /*
    // Check if loading was successful
    if (sceneLoadHandle.Status == AsyncOperationStatus.Succeeded)
    {
        Debug.Log("Main scene loaded successfully");
    }
    else
    {
        Debug.LogError("Failed to load main scene");
        // Hide loading panel if loading failed
      //  if (loadingPanel != null) loadingPanel.SetActive(false);
    }*/
    //}


    public void OpenSettings()
    {
        // Toggles the settings panel on and off when the button is clicked
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
        }
        else
        {
            Debug.LogWarning("[MainMenu] Settings panel is not assigned in the Inspector!");
        }
    }
    public void ExitGame()
    {
        Debug.Log("[MainMenu] Exiting Game...");

        // Quits the application in the compiled Quest 2 build
        Application.Quit();

        // Stops Play Mode if you are testing inside the Unity Editor
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

}
