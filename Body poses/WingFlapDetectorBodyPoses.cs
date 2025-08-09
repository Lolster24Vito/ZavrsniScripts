using Oculus.Interaction.Body.PoseDetection;
using Meta.XR.Movement.BodyTrackingForFitness;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class WingFlapDetectorBodyPoses : MonoBehaviour
{

    private FlapBodyPosesEnum _currentFlapPose = FlapBodyPosesEnum.None;
    public int TPoseCompliantBoneCount { get; set; }
    public int ArmsDownCompliantBoneCount { get; set; }

    public int ArmsUpCompliantBoneCount { get; set; }
    public int ArmsInFrontCompliantBoneCount { get; set; }

    public FlapBodyPosesEnum CurrentFlapPose { get => _currentFlapPose; set => _currentFlapPose = value; }

    [SerializeField] private GlideStateMachineBodyPoses glideStateMachine;

    [SerializeField] private BodyPoseAlignmentDetectorAdapter tPoseDetector;
    [SerializeField] private BodyPoseAlignmentDetectorAdapter handsDownDetector;
    [SerializeField] private BodyPoseAlignmentDetectorAdapter handsUpDetector;
    [SerializeField] private BodyPoseAlignmentDetectorAdapter handsForwardDetector;

    private void TPoseOnCompliantBoneCount(int count) => TPoseCompliantBoneCount = count;
    private void AboveHeadOnCompliantBoneCount(int count) => ArmsUpCompliantBoneCount = count;
    private void ArmsDownOnCompliantBoneCount(int count) => ArmsDownCompliantBoneCount = count;
    private void ArmsInFrontOnCompliantBoneCount(int count) => ArmsInFrontCompliantBoneCount = count;



    public float FlapStrength = 9f;

    public FlapEventDTO currentFlapEvent = new FlapEventDTO();

    private float flapUpDownAllowedTimeDifference = 2f;
    private FlapEventDTO previousFlapEventDTO = new FlapEventDTO();

    private void Start()
    {

        // Subscribe to T-Pose events

        tPoseDetector.OnCompliantBoneCountEvent += TPoseOnCompliantBoneCount;

        // Subscribe to Hands-Up detector events

        handsUpDetector.OnCompliantBoneCountEvent += AboveHeadOnCompliantBoneCount;

        // Subscribe to Hands-Down detector events

        handsDownDetector.OnCompliantBoneCountEvent += ArmsDownOnCompliantBoneCount;
        // Subscribe to Hands-Forward detector events

        handsForwardDetector.OnCompliantBoneCountEvent += ArmsInFrontOnCompliantBoneCount;
    }
    private void OnDestroy()
    {
        // Unsubscribe from T-Pose detector events

        tPoseDetector.OnCompliantBoneCountEvent -= TPoseOnCompliantBoneCount;

        // Unsubscribe from Hands-Up detector events

        handsUpDetector.OnCompliantBoneCountEvent -= AboveHeadOnCompliantBoneCount;

        // Unsubscribe from Hands-Down detector events
        handsDownDetector.OnCompliantBoneCountEvent -= ArmsDownOnCompliantBoneCount;

        handsForwardDetector.OnCompliantBoneCountEvent -= ArmsInFrontOnCompliantBoneCount;

    }

    public void Update()
    {
        FlapBodyPosesEnum highestPose = DetermineHighestFlapPose();
        // Update current flap pose to the one with the highest compliant count
        if (highestPose != _currentFlapPose)
        {
            CurrentFlapPose = highestPose;

        }

        if (CurrentFlapPose.Equals(FlapBodyPosesEnum.TPose) || CurrentFlapPose.Equals(FlapBodyPosesEnum.AboveHead))
        {
            currentFlapEvent.TimeUp = Time.time;
            currentFlapEvent.aimingDirectionUp = glideStateMachine.GetAimingDirection();
            currentFlapEvent.FlapUpType = CurrentFlapPose;

        }
        if (CurrentFlapPose.Equals(FlapBodyPosesEnum.ArmsDown))
        {
            currentFlapEvent.TimeDown = Time.time;
            currentFlapEvent.aimingDirectionDown = glideStateMachine.GetAimingDirection();
            currentFlapEvent.FlapDownType = CurrentFlapPose;
        }
        if (CurrentFlapPose.Equals(FlapBodyPosesEnum.ArmsInFront))
        {
            currentFlapEvent.TimeDown = Time.time;
            currentFlapEvent.aimingDirectionDown = glideStateMachine.GetHandsForwardDirection();
            currentFlapEvent.FlapDownType = CurrentFlapPose;
        }

        bool isFlap = IsFlapEventAFlap(currentFlapEvent);
        if (isFlap)
        {

            currentFlapEvent.FlapType = DetermineFlapType(currentFlapEvent);
            currentFlapEvent.SetAimingDirection();
            currentFlapEvent.FlapEndTime = Time.time;

            previousFlapEventDTO = currentFlapEvent;
            GameEventsManager.Instance.TriggerFlapEvent(currentFlapEvent);
            currentFlapEvent = new FlapEventDTO();
        }
    }

    private FlapTypeEnum DetermineFlapType(FlapEventDTO currentFlapEvent)
    {
        if (currentFlapEvent.FlapDownType.Equals(FlapBodyPosesEnum.ArmsInFront))
        {
            return FlapTypeEnum.FLAPPING_SLOW_DODGE;
        }
        else
        {
            return FlapTypeEnum.FLAPPING_GLIDE;
        }
    }


    // Sets _currentFlapPose to the pose with the highest compliant bone count
    private FlapBodyPosesEnum DetermineHighestFlapPose()
    {
        FlapBodyPosesEnum highestPose = FlapBodyPosesEnum.None;
        int highestCount = 0;

        // Compare each pose's compliant bone count and select the one with the highest count
        if (TPoseCompliantBoneCount > highestCount)
        {
            highestCount = TPoseCompliantBoneCount;
            highestPose = FlapBodyPosesEnum.TPose;
        }

        if (ArmsUpCompliantBoneCount > highestCount)
        {
            highestCount = ArmsUpCompliantBoneCount;
            highestPose = FlapBodyPosesEnum.AboveHead;
        }

        if (ArmsDownCompliantBoneCount > highestCount)
        {
            highestCount = ArmsDownCompliantBoneCount;
            highestPose = FlapBodyPosesEnum.ArmsDown;
        }
        if (ArmsInFrontCompliantBoneCount > highestCount)
        {
            highestCount = ArmsInFrontCompliantBoneCount;
            highestPose = FlapBodyPosesEnum.ArmsInFront;
        }


        return highestPose;
    }

    private bool IsFlapEventAFlap(FlapEventDTO flapEvent)
    {
        bool notInFirstSecond = Time.time > 0.2f;
        float timeDifferenceUpDown = Mathf.Abs(flapEvent.TimeUp - flapEvent.TimeDown);
        bool isDuplicate = previousFlapEventDTO.TimeDown.Equals(flapEvent.TimeDown)
            || previousFlapEventDTO.TimeUp.Equals(flapEvent.TimeUp);
        bool timeDifferenceIsFlap = timeDifferenceUpDown < flapUpDownAllowedTimeDifference;
        bool isDefined = !(flapEvent.TimeDown.Equals(0f)) && !(flapEvent.TimeUp.Equals(0f));
        return isDefined && !isDuplicate && notInFirstSecond && timeDifferenceIsFlap;

    }

}


