using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class QuestLogButton : MonoBehaviour,ISelectHandler
{
    public Button button;
    private TextMeshProUGUI buttonText;
    //VITO TODO remove secondary from prefab
    //private TextMeshProUGUI buttonSecondaryText;
    //VITO TODO icon settings or enum?
    // new field for debug target
    //public QuestInfoSO debugQuestInfo;
    //    private UnityAction<bool> onSelectAction;
    //by default toggle is true
    private UnityAction onSelectAction;
    /*
    private void Awake()
    {
        if(this.button==null)
        this.button = this.GetComponent<Button>();
        if(this.buttonText==null)
        this.buttonText = this.GetComponentInChildren<TextMeshProUGUI>();

    }*/
    public void Initialize(string displayName,UnityAction selectAction)
    {
        Debug.Log("In initialize for:" + displayName);
        this.button = this.GetComponent<Button>();
        this.buttonText = this.GetComponentInChildren<TextMeshProUGUI>();

        this.buttonText.text = displayName;
        this.onSelectAction = selectAction;
        this.button.onClick.AddListener(this.onSelectAction);
        //onSelectAction = OnDebugSelected;
        //VTIO TODO this is sus but it's safe according to gpt

       //VITO TODO uncomment toggle.StateChanged += OnToggleStateChanged;
    }
/*
    private void onValueChangedListener(bool currentValue)
    {
        Debug.Log("VITO Value "+gameObject.name+" changed:"+currentValue);
        QuestLogUI.Instance.SetLatestClickedButton(button);
    }
*/



    public void SetState(QuestState state)
    {
        Debug.Log("In set state");
        switch (state)
        {
            case QuestState.REQUIREMENTS_NOT_MET:
            case QuestState.CAN_START:
                buttonText.color = Color.red;
                break;
            case QuestState.IN_PROGRESS:
            case QuestState.CAN_FINISH:
                buttonText.color = Color.yellow;
                break;
            case QuestState.FINISHED:
                buttonText.color = Color.green;
                break;
            default:
                Debug.LogWarning("Quest State not recognized by switch statement: " + state);
                break;
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        Debug.Log("ONselect called for:" + gameObject.name);
        onSelectAction();
    }
}
