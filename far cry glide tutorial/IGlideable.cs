using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGlideable
{
    Vector3 getCurrentVelocity();
    float getCurrentSpeed();
    float getTargetSpeedForDebugText();
}
