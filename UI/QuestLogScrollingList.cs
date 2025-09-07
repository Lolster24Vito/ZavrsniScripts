using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class QuestLogScrollingList : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private GameObject contentParent;
    //VITO TODO rect transforms for scroll and content maybe?

    [Header("Quest Log Button")]
    [SerializeField] private GameObject questLogButtonPrefab;

    private Dictionary<string, QuestLogButton> idToButtonMap = new Dictionary<string, QuestLogButton>();

    /*
    private void Start()
     {
         for (int i = 0; i < 4; i++)
        {
            QuestInfoSO questInfoTest = ScriptableObject.CreateInstance<QuestInfoSO>();
            questInfoTest.id = "test_" + i;
            questInfoTest.displayName = "Test " + i;
            questInfoTest.questStepPrefabs = new GameObject[0];
            Quest quest = new Quest(questInfoTest);

            QuestLogButton questLogButton = CreateButtonIfNotExists(quest,(bool e)=> {
                Debug.Log("Selected func from start:" + questInfoTest.displayName+" "+e);
                });
            // set the debug info on the button via a named method (no lambda)
            if (i == 0)
            {
                questLogButton.toggle.isOn = false;
                // NOTE: depending on the Toggle implementation you might want to call
                // a method instead of directly setting isOn — this is for testing only.
                //check if this triggers event
                //VITO TODO Uncomment? questLogButton.toggle.State = true;
                //VITO TODO toggle.state=true;
                Debug.Log("VITO Setting first toggle to on");
               questLogButton.toggle.isOn = false;
                //   questLogButton.button.Select();
            }
        }
    }*/



    public QuestLogButton CreateButtonIfNotExists(Quest quest,UnityAction selectAction)
    {
        Debug.Log("INside CreateButtonIfNotExists");
        QuestLogButton questLogButton = null;
        // only create the button if we haven't seen this quest id before
        if (!idToButtonMap.ContainsKey(quest.info.id))
        {
            Debug.Log("Vito creating quest button:" + quest.info.id);
            questLogButton = InstantiateQuestLogButton(quest, selectAction);
        }
        else
        {
            questLogButton = idToButtonMap[quest.info.id];
        }
        return questLogButton;
    }

    private QuestLogButton InstantiateQuestLogButton(Quest quest,UnityAction selectAction)
    {
        QuestLogButton questLogButton = Instantiate(
            questLogButtonPrefab,
            contentParent.transform).GetComponent<QuestLogButton>();
        questLogButton.gameObject.name = quest.info.id + "_button";

        questLogButton.Initialize(quest.info.displayName, selectAction);

        idToButtonMap[quest.info.id] = questLogButton;
        return questLogButton;
    }
}
