using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] GameObject LeftControllerRayInteractor;
    [SerializeField] GameObject RightControllerRayInteractor;
    [SerializeField] GameObject LeftControllerPokeInteractor;
    [SerializeField] GameObject RightControllerPokeInteractor;

    private bool raysEnabled = false;



    // Start is called before the first frame update
    void Start()
    {
        DisableRayInteractors();
    }
    private void OnEnable()
    {
        GameEventsManager.Instance.inputEvents.onQuestLogTogglePressed += ToggleRayInteractors;
        GameEventsManager.Instance.dialogueEvents.onDialogueStarted += EnableRayInteractors;
        GameEventsManager.Instance.dialogueEvents.onDialogueFinished += DisableRayInteractors;


    }



    private void OnDisable()
    {
        GameEventsManager.Instance.inputEvents.onQuestLogTogglePressed -= ToggleRayInteractors;
        GameEventsManager.Instance.dialogueEvents.onDialogueStarted -= EnableRayInteractors;
        GameEventsManager.Instance.dialogueEvents.onDialogueFinished -= DisableRayInteractors;
    }
    private void DisableRayInteractors()
    {
      

        raysEnabled = false;
        LeftControllerRayInteractor.SetActive(false);
        RightControllerRayInteractor.SetActive(false);
        LeftControllerPokeInteractor.SetActive(false);
        RightControllerPokeInteractor.SetActive(false);
        

    }
    private void EnableRayInteractors()
    {
        raysEnabled = true;
        LeftControllerRayInteractor.SetActive(true);
        RightControllerRayInteractor.SetActive(true);
        LeftControllerPokeInteractor.SetActive(true);
        RightControllerPokeInteractor.SetActive(true);
    }
    private void ToggleRayInteractors()
    {
        if (raysEnabled && GameEventsManager.Instance.inputEvents.inputEventContext != InputEventContext.DIALOGUE)
        {
            DisableRayInteractors();
        }
        else
        {
            EnableRayInteractors();
        }
    }


}
