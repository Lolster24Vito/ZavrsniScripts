using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class QuestLogButton : MonoBehaviour
{
    public Toggle toggle;
    private TextMeshProUGUI buttonText;
    //VITO TODO remove secondary from prefab
    //private TextMeshProUGUI buttonSecondaryText;
    //VITO TODO icon settings or enum?
    // new field for debug target
    //public QuestInfoSO debugQuestInfo;
    private void Awake()
    {
        if(this.toggle==null)
        this.toggle = this.GetComponent<Toggle>();
        if(this.buttonText==null)
        this.buttonText = this.GetComponentInChildren<TextMeshProUGUI>();

    }
    public void Initialize(string displayName)
    {
        this.toggle = this.GetComponent<Toggle>();
        this.buttonText = this.GetComponentInChildren<TextMeshProUGUI>();

        this.buttonText.text = displayName;
        toggle.onValueChanged.AddListener(onValueChangedListener);
        //onSelectAction = OnDebugSelected;
        //this.onSelectAction = selectAction;
        //VTIO TODO this is sus but it's safe according to gpt

       //VITO TODO uncomment toggle.StateChanged += OnToggleStateChanged;
    }

    private void onValueChangedListener(bool currentValue)
    {
        Debug.Log("VITO Value "+gameObject.name+" changed:"+currentValue);
    }



    private void OnToggleStateChanged(bool state)
    {
        if (state) { }
      //      onSelectAction.Invoke();
    }

    public void OnSelect()
    {
       // toggle.ToggleState();
        
        //onSelectAction();
    }

    public void SetState(QuestState state)
    {
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
}
