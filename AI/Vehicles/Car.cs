using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Car : Pedestrian
{

    // Start is called before the first frame update
    protected override void Start()
    {
        minDistanceForCompletion = 18f;
        entityType = EntityType.Car;
        randomSpeedMinimumOffset = -5f;
        randomSpeedMaximumOffset = 15f;
        base.Start();
    }


}
