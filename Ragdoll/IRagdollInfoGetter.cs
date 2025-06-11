using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IRagdollInfoGetter
{
    Vector3 GetAimingDirection();
    float GetFlapVelocity();
}
