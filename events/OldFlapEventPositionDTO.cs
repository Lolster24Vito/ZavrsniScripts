using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static WingFlapDetectorBodyPoses;

public class OldFlapEventPositionDTO 
{
    public HandTypeEnum Hand { get; set; }       // "left" or "right"
    public FlapBodyPosesEnum FlapType { get; set; }   // "fast" or "slow"
    //VELOCITY REMOVE
    public float Velocity { get; set; }    // Velocity of the flap
    public float VelocityUp { get; set; }    // Velocity of the flap
    public float VelocityDown { get; set; }    // Velocity of the flap

    public Vector3 aimingDirection { get; set; }
    public Vector3 aimingDirectionUp { get; set; }
    public Vector3 aimingDirectionDown { get; set; }

    public float TimeDown { get; set; }
    public float TimeUp { get; set; }

    public float FlapEndTime { get; set; }
    //public enum HandTypeEnum { LEFT,RIGHT}
    public float GetVelocity()
    {
        Velocity = Mathf.Max(Mathf.Abs(VelocityUp), Mathf.Abs(VelocityDown));
        return Velocity;
    }
    public void SetVelocity()
    {
        Velocity = Mathf.Max(Mathf.Abs(VelocityUp), Mathf.Abs(VelocityDown));
    }
    public Vector3 GetAimingDirection()
    {
        aimingDirection = Vector3.Lerp(aimingDirectionUp, aimingDirectionDown, 0.5f);
        return aimingDirection;
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
            $"Hand: {Hand}, \n" +
            $"Flap Type: {FlapType}, \n" +
            $"Velocity: {Velocity}, \n" +
            $"Velocity Up: {VelocityUp}, \n" +
            $"Velocity Down: {VelocityDown}, \n" +
            $"Aiming Direction: {aimingDirection}, \n" +
            $"Aiming Direction Up: {aimingDirectionUp}, \n" +
            $"Aiming Direction Down: {aimingDirectionDown}, \n" +
            $"Time Down: {TimeDown}, \n" +
            $"Time Up: {TimeUp}, \n" +
            $"Flap End Time: {FlapEndTime}";
    }
}
