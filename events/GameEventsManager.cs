using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameEventsManager : MonoBehaviour
{
    public static GameEventsManager Instance { get; private set; }

    public event Action<FlapEventDTO> OnFlapEvent;
    //todo experienceEvents and stuff??
    //redo experience to seperate class for now it's just a number here
    public int experience;
    public QuestEvents questEvents;
    public BreadEvents breadEvents;
    //playerMovementEvents ->EnableMovement and DisableMovement.
    //InputEvents -> onSubmitButton, OnQuestLogButton
    public PlayerMovementEvents playerMovementEvents;
    public InputEvents inputEvents;
    public DialogueEvents dialogueEvents;
   // public event Action OnSubmitButtonPressed;
  //  public event Action OnQuestLogTogglePressed;


    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("Found more than one Game Events Manager in the scene.");
        }
        Instance = this;

        breadEvents = new BreadEvents();
        questEvents = new QuestEvents();
        playerMovementEvents = new PlayerMovementEvents();
        inputEvents = new InputEvents();
        dialogueEvents = new DialogueEvents();
    }

    private void Update()
    {
        //the time has cum.
        //todo this is debug remove and replace with oculus controlls
        /*if (Input.GetKeyDown(KeyCode.Space))
        {
            if (OnSubmitButtonPressed != null)
            {
                OnSubmitButtonPressed();
            }
        }
        if (Input.GetKeyDown(KeyCode.J))
        {
            if (onQuestLogTogglePressed != null)
            {

            }
        }*/

        //TODO FINISH THIS
        //  bool leftHandTriggerDown = GetDown(Button.PrimaryHandTrigger);
        // bool rightHandTriggerDown = GetDown(Button.SecondaryHandTrigger);
        //bool leftHandTriggerUp = GetUp(Button.PrimaryHandTrigger);
        //bool rightHandTriggerUp = GetUp(Button.SecondaryHandTrigger);

        bool leftHandTriggerDown = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger);
        bool rightHandTriggerDown = OVRInput.Get(OVRInput.Button.SecondaryHandTrigger);
        bool leftHandTriggerUp = OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger);
        bool rightHandTriggerUp = OVRInput.GetUp(OVRInput.Button.SecondaryHandTrigger);
        //shoot trigger buttons
        if (leftHandTriggerDown && rightHandTriggerDown)
        {
            inputEvents.BothShootButtonPressedDown();
        }
        else if (leftHandTriggerDown)
        {
            inputEvents.LeftShootButtonPressedDown();
        }
        else if (rightHandTriggerDown)
        {
            inputEvents.RightShootButtonPressedDown();
        }

        if (leftHandTriggerUp && rightHandTriggerUp)
        {
            inputEvents.BothShootButtonPressedUp();
        }
        else if (leftHandTriggerUp)
        {
            inputEvents.LeftShootButtonPressedUp();
        }
        else if (rightHandTriggerUp)
        {
            inputEvents.RightShootButtonPressedUp();
        }

        //UI buttons
        if (Input.GetKeyDown(KeyCode.J))
        {
            Debug.Log("J was pressed");
            inputEvents.QuestLogTogglePressed();
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Space was pressed");
            inputEvents.SubmitButtonPressed();
        }
        if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger))
        {
            Debug.Log("Space was pressed");
            inputEvents.SubmitButtonPressed();
        }
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            Debug.Log("Space was pressed");
            inputEvents.SubmitButtonPressed();
        }
        if (OVRInput.GetDown(OVRInput.Button.Three))
        {
            Debug.Log("Space was pressed");
            inputEvents.SubmitButtonPressed();
        }
        if (OVRInput.GetDown(OVRInput.Button.Four)) //y button left controller upper button
        {
            //y button
            Debug.Log("J was pressed");
            inputEvents.QuestLogTogglePressed();
        }
        if (OVRInput.GetDown(OVRInput.Button.Two)) //b button right controller upper button
        {
            //b button
            Debug.Log("J was pressed");
            inputEvents.QuestLogTogglePressed();
        }
    }
    public void TriggerFlapEvent(FlapEventDTO eventData)
    {
        if (OnFlapEvent != null)
        {
            OnFlapEvent(eventData);
        }
    }

   

}