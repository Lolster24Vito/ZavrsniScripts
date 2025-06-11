using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingStateDTO
{
    public FlyingStatesEnum flyingState;

    public Vector3 aimingDirection;

    public FlyingStateDTO(FlyingStatesEnum flyingState, Vector3 aimingDirection)
    {
        this.flyingState = flyingState;
        this.aimingDirection = aimingDirection;
    }
}
