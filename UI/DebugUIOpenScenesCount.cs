using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DebugUIOpenScenesCount : MonoBehaviour
{
    private TextMeshProUGUI textOutput = null;
    [SerializeField] TileManager tileManager;
    public float updateInteval = 0.5f;

    private void Awake()
    {
        textOutput = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Start()
    {

        StartCoroutine(ShowOpenScenesNumbers());
    }

    private void Update()
    {
    }


    private IEnumerator ShowOpenScenesNumbers()
    {
        while (true)
        {
            int openScenes = tileManager.GetMaxOpenScenes();
               int totalOpenSceneCount = UnityEngine.SceneManagement.SceneManager.sceneCount;
            string text = "OpenScenes:" + openScenes + "\n" + "Total open scenes:" + totalOpenSceneCount
               + "\n" + "Npc spawner to spawn:" + PedestrianSpawner.GetPedestrianNumberToSpawn();
            //    + "\n" + "NUMBER OF NPCS:" + Pedestrian.npcCount;


          
            textOutput.text = text;



            yield return new WaitForSeconds(updateInteval);
        }
    }
}
