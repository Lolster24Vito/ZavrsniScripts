using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestLogUI : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private GameObject contentParent;
    [SerializeField] private QuestLogScrollingList scrollingList;

    [SerializeField] private TextMeshProUGUI questDisplayNameText;
    [SerializeField] private TextMeshProUGUI questStatusText;
    [SerializeField] private TextMeshProUGUI breadRewardsText;

    private Toggle firstSelectedButton;
    // Start is called before the first frame update
    void Start()
    {
       // firstSelectedButton.State = true;
    }

    private void OnEnable()
    {
        //VITO TODO
        GameEventsManager.Instance.onQuestLogTogglePressed += QuestLogTogglePressed;
       // GameEventsManager.Instance.questEvents.onQuestStateChange += QuestStateChange;
    }

    private void OnDisable()
    {
       // GameEventsManager.Instance.inputEvents.onQuestLogTogglePressed -= QuestLogTogglePressed;
       // GameEventsManager.Instance.questEvents.onQuestStateChange -= QuestStateChange;
    }
    private void QuestLogTogglePressed()
    {
        if (contentParent.activeInHierarchy)
        {
            HideUI();
        }
        else
        {
            ShowUI();
        }
    }
    private void ShowUI()
    {
        contentParent.SetActive(true);
        //VITO TODO
       // GameEventsManager.instance.playerEvents.DisablePlayerMovement();
        // note - this needs to happen after the content parent is set active,
        // or else the onSelectAction won't work as expected
        if (firstSelectedButton != null)
        {
            //VITO TODO CHECK IF THIS TRIGGERS EVENTS?
            firstSelectedButton.isOn = true;
           // firstSelectedButton.Select();
        }
    }
    private void HideUI()
    {
        contentParent.SetActive(false);
        //VITO TODO
        //GameEventsManager.Instance.playerEvents.EnablePlayerMovement();
       //this is unneded probably vito todo remove line? EventSystem.current.SetSelectedGameObject(null);
    }
   
    private void QuestStateChange(Quest quest)
    {
        // add the button to the scrolling list if not already added
        QuestLogButton questLogButton = scrollingList.CreateButtonIfNotExists(quest);
            /*, () => {
            SetQuestLogInfo(quest);
        });*/

        // initialize the first selected button if not already so that it's
        // always the top button
        if (firstSelectedButton == null)
        {
            firstSelectedButton = questLogButton.toggle;
        }

        // set the button color based on quest state
        questLogButton.SetState(quest.state);
    }
    
    private void SetQuestLogInfo(Quest quest)
    {
        // quest name
        questDisplayNameText.text = quest.info.displayName;

        // status
        questStatusText.text = quest.GetFullStatusText();

        // requirements
    //    levelRequirementsText.text = "Level " + quest.info.levelRequirement;
     //   questRequirementsText.text = "";
       /*
        foreach (QuestInfoSO prerequisiteQuestInfo in quest.info.questPrerequisites)
        {
            questRequirementsText.text += prerequisiteQuestInfo.displayName + "\n";
        }*/


        // rewards
        breadRewardsText.text = quest.info.breadReward + " Bread";
      //  experienceRewardsText.text = quest.info.experienceReward + " XP";
    }
}
