using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestLogUI : MonoBehaviour
{
    public static QuestLogUI Instance { get; private set; }

    [Header("Components")]
    [SerializeField] private Canvas contentParent;
    [SerializeField] private QuestLogScrollingList scrollingList;

    [SerializeField] private TextMeshProUGUI questDisplayNameText;
    [SerializeField] private TextMeshProUGUI questStatusText;
    [SerializeField] private TextMeshProUGUI breadRewardsText;

    [SerializeField] private TextMeshProUGUI currentBreadAmountText;


    private Button firstSelectedButton;

    [SerializeField] Transform playerCamera;
    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("Found more than one Game Events Manager in the scene.");
        }
        Instance = this;
    }
    private void OnEnable()
    {
        GameEventsManager.Instance.inputEvents.onQuestLogTogglePressed += QuestLogTogglePressed;
        GameEventsManager.Instance.questEvents.onQuestStateChange += QuestStateChange;
    }

    private void OnDisable()
    {
        GameEventsManager.Instance.inputEvents.onQuestLogTogglePressed -= QuestLogTogglePressed;
        GameEventsManager.Instance.questEvents.onQuestStateChange -= QuestStateChange;
    }
    public bool IsQuestLogShown()
    {
        return contentParent.enabled;
    }
    private void QuestLogTogglePressed()
    {
        if (contentParent.enabled)
        {
            HideUI();
        }
        else
        {
            ShowUI();
        }
    }

    private void UpdateBreadAmount()
    {
        currentBreadAmountText.text = BreadManager.Instance.currentBreadAmount.ToString();
    }

    private void ShowUI()
    {
        contentParent.enabled=true;
        UpdateBreadAmount();
        contentParent.transform.rotation = Quaternion.LookRotation(contentParent.transform.position - playerCamera.position);
        GameEventsManager.Instance.playerMovementEvents.DisablePlayerMovement();
        // note - this needs to happen after the content parent is set active,
        // or else the onSelectAction won't work as expected
        if (firstSelectedButton != null)
        {
            firstSelectedButton.Select();
        }
    }

   
    private void HideUI()
    {
        contentParent.enabled=false;
        GameEventsManager.Instance.playerMovementEvents.EnablePlayerMovement();
     //   if (firstSelectedButton != null)
      //      firstSelectedButton.Select();
    }

    private void QuestStateChange(Quest quest)
    {
        // add the button to the scrolling list if not already added
        QuestLogButton questLogButton = scrollingList.CreateButtonIfNotExists(quest,()=>
        {
            SetQuestLogInfo(quest);
        });

        if (firstSelectedButton == null)
        {
            firstSelectedButton = questLogButton.button;
        }
        // initialize the first selected button if not already so that it's
        // always the top button



        // set the button color based on quest state
        questLogButton.SetState(quest.state);
    }
    public void SetLatestClickedButton(Button button)
    {
        firstSelectedButton = button;
    }

 
    private void SetQuestLogInfo(Quest quest)
    {
        // quest name
        questDisplayNameText.text = quest.info.displayName;

        // status
        questStatusText.text = quest.GetFullStatusText();


        // rewards
        breadRewardsText.text = quest.info.breadReward + " Bread";
      //  experienceRewardsText.text = quest.info.experienceReward + " XP";
    }
     
}
