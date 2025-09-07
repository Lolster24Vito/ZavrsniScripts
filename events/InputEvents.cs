using System;
using UnityEngine;

public class InputEvents 
{
    public InputEventContext inputEventContext { get; private set; } = InputEventContext.DEFAULT;
    public void ChangeInputEventContext(InputEventContext newContext)
    {
        this.inputEventContext = newContext;
    }
    public event Action onQuestLogTogglePressed;
    public event Action<InputEventContext> onSubmitButtonPressed;

    public void QuestLogTogglePressed()
    {
        onQuestLogTogglePressed?.Invoke();
    }

    public void SubmitButtonPressed()
    {
        if (onSubmitButtonPressed != null)
        {
            onSubmitButtonPressed(this.inputEventContext);
        }
    }
}
