using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class DialogueChoiceButton : MonoBehaviour, ISelectHandler
{
    //todo make prefab button
    [Header("Components")]
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI choiceText;

    private int choiceIndex = -1;

    private UnityAction onClickAction;

    public void SetChoiceText(string choiceTextString)
    {
        choiceText.text = choiceTextString;
    }

    public void SetChoiceIndex(int choiceIndex)
    {
        this.choiceIndex = choiceIndex;
        UpdateClickListener();
    }
    public void SelectButton()
    {
        Debug.LogWarning("Does this fire");
        button.Select();
    }

    public void OnSelect(BaseEventData eventData)
    {
        Debug.LogWarning("Does this fire2");

        GameEventsManager.Instance.dialogueEvents.UpdateChoiceIndex(choiceIndex);
    }
    // Public method to be called by the Button's onClick event
    public void OnClickDialogueChoice()
    {
        Debug.Log("Dialogue choice button was clicked with index: " + choiceIndex);
        GameEventsManager.Instance.dialogueEvents.UpdateChoiceIndex(choiceIndex);
        // We'll also call the SubmitPressed event here so the DialogueManager continues the story
       GameEventsManager.Instance.inputEvents.ChangeInputEventContext(InputEventContext.DIALOGUE);
        GameEventsManager.Instance.inputEvents.SubmitButtonPressed();
    }
    // Private method to handle the listener logic
    private void UpdateClickListener()
    {
        if (button != null)
        {
            // Remove any old listeners before adding a new one
            button.onClick.RemoveAllListeners();
            // Add our public method as the listener
            button.onClick.AddListener(OnClickDialogueChoice);
        }
    }
}
