using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class FlapEventDTO
{
    public FlapTypeEnum FlapType { get; set; }

    public Vector3 aimingDirection { get; set; }
    public Vector3 aimingDirectionUp { get; set; }
    public Vector3 aimingDirectionDown { get; set; }

    public float TimeDown { get; set; }
    public float TimeUp { get; set; }

    public FlapBodyPosesEnum FlapUpType;
    public FlapBodyPosesEnum FlapDownType;

    public float FlapEndTime { get; set; }

    public Vector3 GetAverageAimingDirection()
    {
        return Vector3.Lerp(aimingDirectionUp, aimingDirectionDown, 0.5f);
    }
    public void SetAimingDirection()
    {
        aimingDirection = Vector3.Lerp(aimingDirectionUp, aimingDirectionDown, 0.5f);
    }
    // Method to return detailed string for logging or display
    public string GetDetails()
    {
        return
            $"Flap Event Details: \n" +
            $"Flap Type: {FlapType}, \n" +
            $"Aiming Direction: {aimingDirection}, \n" +
            $"Aiming Direction Up: {aimingDirectionUp}, \n" +
            $"Aiming Direction Down: {aimingDirectionDown}, \n" +
            $"Time Down: {TimeDown}, \n" +
            $"Time Up: {TimeUp}, \n" +
            $"Flap End Time: {FlapEndTime}";
    }
}
