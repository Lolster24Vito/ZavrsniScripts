using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(SphereCollider))]
public class QuestPoint : MonoBehaviour
{
    [Header("Dialogue (optional)")]
    [SerializeField] private string dialogueKnotName;

    [Header("Quest")]
    [SerializeField] private QuestInfoSO questInfoForPoint;
    [Header("Config")]
    [SerializeField] private bool startPoint = true;
    [SerializeField] private bool finishPoint = true;


    private bool playerIsNear = false;
    private string questId;
    private QuestState currentQuestState;
    private QuestIcon questIcon;
    private void Awake()
    {
        questId = questInfoForPoint.id;
        questIcon = GetComponentInChildren<QuestIcon>();
    }

    private void OnEnable()
    {
        GameEventsManager.Instance.questEvents.onQuestStateChange += QuestStateChanged;
        GameEventsManager.Instance.inputEvents.onSubmitButtonPressed += SubmitPressed;
    }
    private void OnDisable()
    {
        GameEventsManager.Instance.questEvents.onQuestStateChange -= QuestStateChanged;
        GameEventsManager.Instance.inputEvents.onSubmitButtonPressed -= SubmitPressed;
    }
    private void SubmitPressed(InputEventContext inputEventContext)
    {
        if (!playerIsNear|| !inputEventContext.Equals(InputEventContext.DEFAULT))
            return;
        if (!dialogueKnotName.Equals(""))
        {
            GameEventsManager.Instance.dialogueEvents.EnterDialogue(dialogueKnotName);
        }
        else
        {

            // start or finish a quest
            if (currentQuestState.Equals(QuestState.CAN_START) && startPoint)
            {
                GameEventsManager.Instance.questEvents.StartQuest(questId);
            }
            else if (currentQuestState.Equals(QuestState.CAN_FINISH) && finishPoint)
            {
                GameEventsManager.Instance.questEvents.FinishQuest(questId);
            }
        }


    }
    private void QuestStateChanged(Quest quest)
    {
        //only update the quest state if this point has the corresponding quest
        if (quest.info.id.Equals(questId))
        {
            currentQuestState = quest.state;
            questIcon.SetState(currentQuestState, startPoint, finishPoint);
        }
    }
    private void OnTriggerEnter(Collider otherCollider)
    {
        if (otherCollider.CompareTag("Player"))
        {
            playerIsNear = true;
        }
    }
    private void OnTriggerExit(Collider otherCollider)
    {
        if (otherCollider.CompareTag("Player"))
        {
            playerIsNear = false;
        }
    }
}
