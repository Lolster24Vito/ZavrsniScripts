using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameEventsManager : MonoBehaviour
{
    public static GameEventsManager Instance { get; private set; }

    public event Action<FlapEventDTO> OnFlapEvent;

    public int experience;
    public QuestEvents questEvents;
    public BreadEvents breadEvents;

    public PlayerMovementEvents playerMovementEvents;
    public InputEvents inputEvents;
    public DialogueEvents dialogueEvents;



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
        HandleShootingInput();
        HandleGrabbingInput();
        HandleUIInput();

    }
    private void HandleShootingInput()
    {
        // SHOOTING = INDEX TRIGGERS
        bool leftDown = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger);
        bool rightDown = OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger);

        if (leftDown && rightDown) inputEvents.BothShootButtonPressedDown();
        else if (leftDown) inputEvents.LeftShootButtonPressedDown();
        else if (rightDown) inputEvents.RightShootButtonPressedDown();

        bool leftUp = OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger);
        bool rightUp = OVRInput.GetUp(OVRInput.Button.SecondaryIndexTrigger);

        if (leftUp && rightUp) inputEvents.BothShootButtonPressedUp();
        else if (leftUp) inputEvents.LeftShootButtonPressedUp();
        else if (rightUp) inputEvents.RightShootButtonPressedUp();
    }

    private void HandleGrabbingInput()
    {
        // GRABBING = HAND TRIGGERS (GRIPS)
        if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger) || OVRInput.GetDown(OVRInput.Button.SecondaryHandTrigger))
        {
            inputEvents.GrabButtonPressedDown();
        }

        if (OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger) || OVRInput.GetUp(OVRInput.Button.SecondaryHandTrigger))
        {
            inputEvents.GrabButtonPressedUp();
        }
    }

    private void HandleUIInput()
    {
        // PAUSE = Menu Button (Left Controller)
        if (OVRInput.GetDown(OVRInput.Button.Start))
            inputEvents.PauseMenuTogglePressed();

        // QUEST LOG = Y or B
        if (OVRInput.GetDown(OVRInput.Button.Four) || OVRInput.GetDown(OVRInput.Button.Two))
            inputEvents.QuestLogTogglePressed();

        // SUBMIT = A or X
        if (OVRInput.GetDown(OVRInput.Button.One) || OVRInput.GetDown(OVRInput.Button.Three))
            inputEvents.SubmitButtonPressed();
    }


    public void TriggerFlapEvent(FlapEventDTO eventData)
    {
        if (OnFlapEvent != null)
        {
            OnFlapEvent(eventData);
        }
    }

   

}