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

    public event Action<ShootTypes> onLeftShootButtonPressedDown;
    public event Action<ShootTypes> onRightShootButtonPressedDown;
    public event Action<ShootTypes> onBothShootButtonPressedDown;


    public event Action<ShootTypes> onLeftShootButtonPressedUp;
    public event Action<ShootTypes> onRightShootButtonPressedUp;
    public event Action<ShootTypes> onBothShootButtonPressedUp;
    
    public void LeftShootButtonPressedDown()
    {
        onLeftShootButtonPressedDown?.Invoke(ShootTypes.LEFT);
    }
    public void RightShootButtonPressedDown()
    {
        onRightShootButtonPressedDown?.Invoke(ShootTypes.RIGHT);
    }
    public void BothShootButtonPressedDown()
    {
        onBothShootButtonPressedDown?.Invoke(ShootTypes.BOTH);
    }

    public void LeftShootButtonPressedUp()
    {
        onLeftShootButtonPressedUp?.Invoke(ShootTypes.LEFT);

    }
    public void RightShootButtonPressedUp()
    {
        onRightShootButtonPressedUp?.Invoke(ShootTypes.RIGHT);

    }
    public void BothShootButtonPressedUp()
    {
        onBothShootButtonPressedUp?.Invoke(ShootTypes.BOTH);

    }

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
