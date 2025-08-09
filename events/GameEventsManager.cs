using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public  class GameEventsManager: MonoBehaviour
{
    public static GameEventsManager Instance { get; private set; }

    public event Action<FlapEventDTO> OnFlapEvent;
    public BreadEvents breadEvents;
    //todo experienceEvents and stuff??
    //redo experience to seperate class for now it's just a number here
    public int experience;
    public QuestEvents questEvents;
    public event Action OnSubmitButtonPressed;
    public event Action onQuestLogTogglePressed;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("Found more than one Game Events Manager in the scene.");
        }
        Instance = this;

        breadEvents = new BreadEvents();
        questEvents = new QuestEvents();
    }

    private void Update()
    {
        //the time has cum.
        //todo this is debug remove and replace with oculus controlls
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (OnSubmitButtonPressed != null)
            {
                Debug.Log("Space was pressed");
                OnSubmitButtonPressed();
            }
        }
        if (Input.GetKeyDown(KeyCode.J))
        {
            if (onQuestLogTogglePressed != null)
            {
                Debug.Log("J was pressed");
                onQuestLogTogglePressed();
            }
        }
    }
    public  void TriggerFlapEvent(FlapEventDTO eventData)
    {
        if (OnFlapEvent != null)
        {
            OnFlapEvent(eventData);
        }
    }
    
}