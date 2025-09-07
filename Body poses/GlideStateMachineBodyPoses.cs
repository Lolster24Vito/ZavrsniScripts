using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GlideStateMachineBodyPoses : MonoBehaviour, IRagdollInfoGetter
{
    //    GLIDING,STATIONARY,FLYING_FAST,FLYING_SLOW
    public int compliantGlidingBones { get; set; }
    public int compliantGlidingLeftBones { get; set; }
    public int compliantGlidingRightBones { get; set; }

    public int compliantStationaryBones { get; set; }
    public int compliantFlyingFastBones { get; set; }
    public int compliantFlyingSlowBones { get; set; }
    public int compliantFlyingSlowLeftBones { get; set; }
    public int compliantFlyingSlowRightBones { get; set; }

    public Vector3 AimingDirection { get; set; }

    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;



    [SerializeField] private Rigidbody rb;
    private Transform playerTr;

    [SerializeField] private BodyPoseAlignmentDetectorAdapter glidingDetector;
    [SerializeField] private BodyPoseAlignmentDetectorAdapter glidingLeftDetector;
    [SerializeField] private BodyPoseAlignmentDetectorAdapter glidingRightDetector;

    [SerializeField] private BodyPoseAlignmentDetectorAdapter stationaryDetector;
    [SerializeField] private BodyPoseAlignmentDetectorAdapter flyingFastDetector;
    [SerializeField] private BodyPoseAlignmentDetectorAdapter flyingSlowDetector;
    [SerializeField] private BodyPoseAlignmentDetectorAdapter flyingSlowLeftDetector;
    [SerializeField] private BodyPoseAlignmentDetectorAdapter flyingSlowRightDetector;



    public float flapVelocity = 0f;
    private bool isOnFloor = false;
    [SerializeField] private float flapStrentgh = 4;
    [SerializeField] private float flapSlowStrentgh = 12f;

    [SerializeField] private float flapSlowDownTimeStrentgh = 2f;

    [SerializeField] private float dragFastAngleValue = 0f;
    [SerializeField] private float dragSlowAngleValue = 9f;

    [SerializeField] private float speedFastAngle = 80f;
    [SerializeField] private float speedSlowAngle = 5f;

    [SerializeField] private float speedStationaryGravity = 10f;


    [SerializeField] private float flapAngleSlowDownStrength = 4f;
    [SerializeField] private float flapAngleSpeedUpStrength = 7.5f;
    public float maxAllowedFlapVelocity = 900f;  // Maximum allowed velocity to avoid unrealistic spikes

    public FlyingStatesEnum CurrentGlidePose { get => _currentGlidePose; set => _currentGlidePose = value; }
    private FlyingStatesEnum _currentGlidePose = FlyingStatesEnum.STATIONARY;

    public Vector3 leftHandAimingDirection;
    public Vector3 rightHandAimingDirection;

    private float currentDrag;
    private float angleTargetSpeed;

    FlapEventDTO lastFlap = null;

    void OnEnable()
    {
        GameEventsManager.Instance.OnFlapEvent += OnFlapDetected;
        GameEventsManager.Instance.playerMovementEvents.onDisablePlayerMovement += DisablePlayerMovement;
        GameEventsManager.Instance.playerMovementEvents.onEnablePlayerMovement += EnablePlayerMovement;
    }




    void OnDisable()
    {
        GameEventsManager.Instance.OnFlapEvent -= OnFlapDetected;
        GameEventsManager.Instance.playerMovementEvents.onDisablePlayerMovement += DisablePlayerMovement;
        GameEventsManager.Instance.playerMovementEvents.onEnablePlayerMovement += EnablePlayerMovement;

    }
    private void DisablePlayerMovement()
    {
        Debug.Log("Disabling player movement debug");
        FreezePlayerRigidbodyPosition();
    }

    private void EnablePlayerMovement()
    {
        Debug.Log("Enabling player movement debug");
        UnfreezePlayerRigidbodyPosition();
    }
    private void FreezePlayerRigidbodyPosition()
    {
        rb.constraints = RigidbodyConstraints.FreezeAll;
    }
    private void UnfreezePlayerRigidbodyPosition()
    {
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }
    // For GLIDING detector
    private void GlidingOnCompliantBoneCount(int count) => compliantGlidingBones = count;
    private void GlidingLeftOnCompliantBoneCount(int count) => compliantGlidingLeftBones = count;
    private void GlidingRightOnCompliantBoneCount(int count) => compliantGlidingRightBones = count;


    // For STATIONARY detector
    private void StationaryOnCompliantBoneCount(int count) => compliantStationaryBones = count;

    // For FLYING_FAST detector
    private void FlyingFastOnCompliantBoneCount(int count) => compliantFlyingFastBones = count;

    // For FLYING_SLOW detector
    private void FlyingSlowOnCompliantBoneCount(int count) => compliantFlyingSlowBones = count;
    private void FlyingSlowLeftOnCompliantBoneCount(int count) => compliantFlyingSlowLeftBones = count;
    private void FlyingSlowRightOnCompliantBoneCount(int count) => compliantFlyingSlowRightBones = count;

    private Vector3 appliedForce = Vector3.zero;


    void Start()
    {

        playerTr = rb.transform;
        // Subscribe to GLIDING detector events
        glidingDetector.OnComplianceEvent += GlidingOnCompliance;
        glidingDetector.OnCompliantBoneCountEvent += GlidingOnCompliantBoneCount;
        glidingLeftDetector.OnCompliantBoneCountEvent += GlidingLeftOnCompliantBoneCount;
        glidingRightDetector.OnCompliantBoneCountEvent += GlidingRightOnCompliantBoneCount;


        // Subscribe to STATIONARY detector events
        stationaryDetector.OnComplianceEvent += StationaryOnCompliance;
        stationaryDetector.OnCompliantBoneCountEvent += StationaryOnCompliantBoneCount;

        // Subscribe to FLYING_FAST detector events
        flyingFastDetector.OnComplianceEvent += FlyingFastOnCompliance;
        flyingFastDetector.OnCompliantBoneCountEvent += FlyingFastOnCompliantBoneCount;

        // Subscribe to FLYING_SLOW detector events
        flyingSlowDetector.OnComplianceEvent += FlyingSlowOnCompliance;
        flyingSlowDetector.OnCompliantBoneCountEvent += FlyingSlowOnCompliantBoneCount;
        flyingSlowLeftDetector.OnCompliantBoneCountEvent += FlyingSlowLeftOnCompliantBoneCount;
        flyingSlowRightDetector.OnCompliantBoneCountEvent += FlyingSlowRightOnCompliantBoneCount;



    }
    private void OnDestroy()
    {
        // Unsubscribe from GLIDING detector events
        glidingDetector.OnComplianceEvent -= GlidingOnCompliance;
        glidingDetector.OnCompliantBoneCountEvent -= GlidingOnCompliantBoneCount;
        glidingLeftDetector.OnCompliantBoneCountEvent -= GlidingLeftOnCompliantBoneCount;
        glidingRightDetector.OnCompliantBoneCountEvent -= GlidingRightOnCompliantBoneCount;

        // Unsubscribe from STATIONARY detector events
        stationaryDetector.OnComplianceEvent -= StationaryOnCompliance;
        stationaryDetector.OnCompliantBoneCountEvent -= StationaryOnCompliantBoneCount;

        // Unsubscribe from FLYING_FAST detector events
        flyingFastDetector.OnComplianceEvent -= FlyingFastOnCompliance;
        flyingFastDetector.OnCompliantBoneCountEvent -= FlyingFastOnCompliantBoneCount;

        // Unsubscribe from FLYING_SLOW detector events
        flyingSlowDetector.OnComplianceEvent -= FlyingSlowOnCompliance;
        flyingSlowDetector.OnCompliantBoneCountEvent -= FlyingSlowOnCompliantBoneCount;
        flyingSlowLeftDetector.OnCompliantBoneCountEvent -= FlyingSlowLeftOnCompliantBoneCount;
        flyingSlowRightDetector.OnCompliantBoneCountEvent -= FlyingSlowRightOnCompliantBoneCount;
    }

    public Vector3 GetAimingDirection()
    {
        return AimingDirection;
    }
    public Vector3 GetHandsForwardDirection()
    {
        return (leftHand.forward + rightHand.forward) / 2f;

    }

    void Update()
    {
        //determine flying state
        FlyingStatesEnum highestPose = DetermineHighestFlapPose();
        if (highestPose != _currentGlidePose)
        {
            switch (highestPose)
            {

                case FlyingStatesEnum.FLYING_FAST:
                    flapVelocity *= 2;
                    break;
                case FlyingStatesEnum.FLYING_SLOW:
                    flapVelocity /= 2;
                    break;
            }
            _currentGlidePose = highestPose;

        }
        //determine aiming direction based on pose
        leftHandAimingDirection = DetermineLeftHandFlyingDirection();
        rightHandAimingDirection = DetermineRightHandFlyingDirection();
        Vector3 leftHandLocalDirection = leftHandAimingDirection.normalized;
        Vector3 rightHandLocalDirection = rightHandAimingDirection.normalized;
        Vector3 averageDirection = Vector3.zero;

        averageDirection = (leftHandLocalDirection + rightHandLocalDirection) / 2f;

        AimingDirection = averageDirection;
        //determine new velocity 
        flapVelocity = CalculateFlapVelocity(flapVelocity);
        //ANGLE BASED VELOCITY START
        currentDrag = CalculateDragBasedOnAngle(averageDirection);
        rb.drag = currentDrag;


        flapVelocity = GetAngleSpeedOfFlapVelocity(averageDirection);
        //ANGLE BASED VELOCITY END
        appliedForce = GetVelocity(averageDirection);
        Vector3 velocityChange = appliedForce - rb.velocity;
        if (flapVelocity != 0f)
        {
            rb.AddForce(velocityChange, ForceMode.VelocityChange);
        }

    }

    void OnFlapDetected(FlapEventDTO eventData)
    {
        isOnFloor = false;
        lastFlap = eventData;
        // Handle different flap types by triggering the appropriate state changes and movement
        if (eventData.FlapType.Equals(FlapTypeEnum.FLAPPING_GLIDE))
        {
            TriggerFlappingGlide(eventData);
        }
        else
        {
            if (eventData.FlapType.Equals(FlapTypeEnum.FLAPPING_SLOW_DODGE))
            {
                TriggerSlowFlap(eventData);
            }
        }


    }



    private void TriggerFlappingGlide(FlapEventDTO eventData)
    {
        // Set the movement behavior when the character is gliding and flapping

        flapVelocity += flapStrentgh;
        flapVelocity = Mathf.Clamp(flapVelocity, -maxAllowedFlapVelocity, maxAllowedFlapVelocity);
        // rb.velocity = GetVelocity(aimingDirectionAverage, flapVelocity); // Apply forward velocity
        rb.AddForce(eventData.aimingDirectionUp * flapStrentgh, ForceMode.Impulse);


    }
    // Update is called once per frame
    private void TriggerSlowFlap(FlapEventDTO eventData)
    {
        flapVelocity -= flapStrentgh;
        flapVelocity = Mathf.Clamp(flapVelocity, -maxAllowedFlapVelocity, maxAllowedFlapVelocity);

        rb.AddForce(-eventData.aimingDirection * flapStrentgh, ForceMode.Impulse);
    }

    private FlyingStatesEnum DetermineHighestFlapPose()
    {
        FlyingStatesEnum highestPose = FlyingStatesEnum.STATIONARY;
        int highestCount = 0;

        // Compare each pose's compliant bone count and select the one with the highest count above threshold

        if (compliantStationaryBones >= highestCount)
        {
            highestCount = compliantStationaryBones;
            highestPose = FlyingStatesEnum.STATIONARY;
        }

        if (compliantFlyingSlowBones >= highestCount)
        {
            highestCount = compliantFlyingSlowBones;
            highestPose = FlyingStatesEnum.FLYING_SLOW;
        }

        if (compliantFlyingSlowLeftBones >= highestCount)
        {
            highestCount = compliantFlyingSlowLeftBones;
            highestPose = FlyingStatesEnum.FLYING_SLOW;
        }

        if (compliantFlyingSlowRightBones >= highestCount)
        {
            highestCount = compliantFlyingSlowRightBones;
            highestPose = FlyingStatesEnum.FLYING_SLOW;
        }
        if (compliantGlidingBones >= highestCount)
        {
            highestCount = compliantGlidingBones;
            highestPose = FlyingStatesEnum.GLIDING;
        }
        if (compliantGlidingLeftBones >= highestCount)
        {
            highestCount = compliantGlidingLeftBones;
            highestPose = FlyingStatesEnum.GLIDING;
        }
        if (compliantGlidingRightBones >= highestCount)
        {
            highestCount = compliantGlidingRightBones;
            highestPose = FlyingStatesEnum.GLIDING;
        }

        if (compliantFlyingFastBones >= highestCount)
        {
            highestCount = compliantFlyingFastBones;
            highestPose = FlyingStatesEnum.FLYING_FAST;
        }


        return highestPose;
    }



    private Vector3 DetermineLeftHandFlyingDirection()
    {
        switch (_currentGlidePose)
        {
            case FlyingStatesEnum.GLIDING:
                return leftHand.up;
            case FlyingStatesEnum.STATIONARY:
                return leftHandAimingDirection;
            case FlyingStatesEnum.FLYING_FAST:
                return leftHand.forward;
            case FlyingStatesEnum.FLYING_SLOW:
                return leftHand.right;

            default:
                return Vector3.zero;

        }
    }
    private Vector3 DetermineRightHandFlyingDirection()
    {
        switch (_currentGlidePose)
        {
            case FlyingStatesEnum.GLIDING:
                return rightHand.up;
            case FlyingStatesEnum.STATIONARY:
                return rightHandAimingDirection;
            case FlyingStatesEnum.FLYING_FAST:
                return rightHand.forward;
            case FlyingStatesEnum.FLYING_SLOW:
                return -rightHand.right;
            default:
                return Vector3.zero;

        }
    }
    //todo add events

    public void OnTheCollisionWithFloor()
    {
        flapVelocity = 0f;
        rb.velocity = Vector3.zero;
        isOnFloor = true;
    }
    public void OnExitCollisionWithFloor()
    {
        isOnFloor = false;
    }
    private float CalculateFlapVelocity(float currentVelocity)
    {
        bool isThereRecentSlowFlap = IsThereRecentSlowFlap();

        if (!isOnFloor)
        {
            //normally I don't want the player going backwards,But when there's a flap Slow I want it to slow down and go backwards
            if (isThereRecentSlowFlap)
            {
                return currentVelocity - (Time.deltaTime * flapSlowDownTimeStrentgh);
            }
            else
            {

                return Mathf.Max(0f, currentVelocity - (Time.deltaTime * flapSlowDownTimeStrentgh));
            }
        }
        else
        {
            if (isOnFloor)
            {
                return 0f;
            }
        }
        return currentVelocity;



    }

    private bool IsThereRecentSlowFlap()
    {
        if (lastFlap != null && Time.time - lastFlap.FlapEndTime < 2f && lastFlap.FlapType.Equals(FlapTypeEnum.FLAPPING_SLOW_DODGE))
        {
            return true;
        }
        return false;
    }

    private float CalculateDragBasedOnAngle(Vector3 direction)
    {
        Vector3 upDirection = Vector3.up;


        float dotValueUp = Vector3.Dot(direction, upDirection); // For flying upwards
        float dotValueDown = Vector3.Dot(direction, -upDirection); // For flying downwards

        float drag = 0f;
        //1 if same
        //0 of 90 degrees diference
        //-1 if 180 degrees of difference
        //going up
        if (dotValueDown < dotValueUp)
        {
            drag = dragFastAngleValue;
        }
        //going down
        else
        {
            drag = dragSlowAngleValue;

        }
        return drag;
    }

    private float GetAngleSpeedOfFlapVelocity(Vector3 direction)
    {

        //movement
        angleTargetSpeed = CalculateSpeedBasedOnAngle(direction);

        float returnAngleVelocity = flapVelocity;

        //if previous value is lower than target speed reduce by multiplier else give responsive fast speed
        float differenceSpeeds = Mathf.Abs(returnAngleVelocity - angleTargetSpeed);

        if (angleTargetSpeed > flapVelocity)
        {
            returnAngleVelocity += Time.deltaTime * flapAngleSpeedUpStrength;
        }
        else
        {
            returnAngleVelocity -= Time.deltaTime * flapAngleSlowDownStrength;
        }
        if (differenceSpeeds < 0.01f)
        {
            returnAngleVelocity = angleTargetSpeed;
        }
        return returnAngleVelocity;
    }
    private float CalculateSpeedBasedOnAngle(Vector3 direction)
    {
        Vector3 upDirection = Vector3.up;
        Vector3 forwardDirection = Vector3.forward;

        // Calculate dot products for 90 degrees (upward) and -90 degrees (downward)
        float dotValueUp = Vector3.Dot(direction, upDirection); // For flying upwards
        float dotValueDown = Vector3.Dot(direction, -upDirection); // For flying downwards
   //     float dotValueForward = Vector3.Dot(direction, forwardDirection); // For flying in a straigth line

        //Dot product
        //1 if same
        //0 of 90 degrees diference
        //-1 if 180 degrees of difference
        //going up

        float targetSpeed;


        //going down
        if (dotValueDown > dotValueUp)
        {
            targetSpeed = speedFastAngle;
        }
        //going up
        else
        {
            targetSpeed = speedSlowAngle;
        }

      /*  if (dotValueForward > 0.95f)
        {
            targetSpeed = flapVelocity;
        }*/


        return targetSpeed;
    }

    private Vector3 GetVelocity(Vector3 averageDirection)
    {


        if (_currentGlidePose.Equals(FlyingStatesEnum.STATIONARY))
        {
            Vector3 normalVelocity = (averageDirection * flapVelocity);
            float yVelocity = rb.velocity.y - (Time.deltaTime * speedStationaryGravity);
            return new Vector3(normalVelocity.x, yVelocity, normalVelocity.z);
        }
        if (_currentGlidePose.Equals(FlyingStatesEnum.FLYING_SLOW))
        {
            //todo
            rb.drag = 9f;
            return averageDirection * (flapVelocity);
        }
        else
        {
            rb.drag = 0f;
        }
        return averageDirection * (flapVelocity);
    }
    //body poses events
    // Compliance Handlers for GLIDING
    public void GlidingOnCompliance()
    {
        CurrentGlidePose = FlyingStatesEnum.GLIDING;
    }



    // Compliance and Deficiency Handlers for STATIONARY
    public void StationaryOnCompliance()
    {
        CurrentGlidePose = FlyingStatesEnum.STATIONARY;
    }



    // Compliance and Deficiency Handlers for FLYING_FAST
    public void FlyingFastOnCompliance()
    {
        CurrentGlidePose = FlyingStatesEnum.FLYING_FAST;

    }



    // Compliance and Deficiency Handlers for FLYING_SLOW
    public void FlyingSlowOnCompliance()
    {
        CurrentGlidePose = FlyingStatesEnum.FLYING_SLOW;
    }

    public float GetFlapVelocity()
    {
        return flapVelocity;
    }

}
