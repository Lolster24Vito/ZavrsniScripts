using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovementEvents 
{
    public event Action onDisablePlayerMovement;
    public event Action onEnablePlayerMovement;

    public void DisablePlayerMovement()
    {
        onDisablePlayerMovement?.Invoke();
    }

    public void EnablePlayerMovement()
    {
        onEnablePlayerMovement?.Invoke();
    }
}
