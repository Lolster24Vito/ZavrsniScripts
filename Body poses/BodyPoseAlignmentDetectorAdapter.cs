using Meta.XR.Movement.BodyTrackingForFitness;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BodyPoseAlignmentDetectorAdapter : BodyPoseAlignmentDetector
{
    public event UnityAction OnComplianceEvent;
    public event UnityAction OnDeficiencyEvent;
    public event UnityAction<int> OnCompliantBoneCountEvent;

    private void Start()
    {
        SubscribeToEvents();
    }
    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    public void SubscribeToEvents()
    {
        _poseEvents.OnCompliance.AddListener(InvokeOnCompliance);
        _poseEvents.OnDeficiency.AddListener(InvokeOnDeficiency);
        _poseEvents.OnCompliantBoneCount.AddListener(InvokeOnCompliantBoneCount);
    }

    public void UnsubscribeFromEvents()
    {
        _poseEvents.OnCompliance.RemoveListener(InvokeOnCompliance);
        _poseEvents.OnDeficiency.RemoveListener(InvokeOnDeficiency);
        _poseEvents.OnCompliantBoneCount.RemoveListener(InvokeOnCompliantBoneCount);
    }

    private void InvokeOnCompliance() => OnComplianceEvent?.Invoke();
    private void InvokeOnDeficiency() => OnDeficiencyEvent?.Invoke();
    private void InvokeOnCompliantBoneCount(int count) => OnCompliantBoneCountEvent?.Invoke(count);
}
