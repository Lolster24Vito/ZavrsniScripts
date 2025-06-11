using UnityEngine;

[System.Serializable]
public class WingsMap
{
    public Transform handBone;
    public Transform wingBone;
    public Vector3 trackingPositionOffsetSlow;
    public Vector3 trackingRotationOffsetSlow;

    public Vector3 trackingPositionOffsetGlide;
    public Vector3 trackingRotationOffsetGlide;

    public void Map(FlyingStatesEnum currentPose)
    {
        if (isValid())
        {
            if (currentPose.Equals(FlyingStatesEnum.FLYING_FAST))
            {
                wingBone.position = handBone.TransformPoint(trackingPositionOffsetSlow);
                wingBone.rotation = handBone.rotation * Quaternion.Euler(trackingRotationOffsetSlow);
            }
            else
            {
                wingBone.position = handBone.TransformPoint(trackingPositionOffsetGlide);
                wingBone.rotation = handBone.rotation * Quaternion.Euler(trackingRotationOffsetGlide);
            }
        }
    }

    public void Map()
    {
        if (isValid())
        {

            wingBone.position = handBone.TransformPoint(trackingPositionOffsetGlide);
            wingBone.rotation = handBone.rotation * Quaternion.Euler(trackingRotationOffsetGlide);
        }
    }

    public bool isValid()
    {
        return handBone != null && wingBone != null;
    }
}


public class WingsFollowArms : MonoBehaviour
{
    public GlideStateMachineBodyPoses glidePoseAccessor;

    [SerializeField] WingsMap leftShoulder;
    [SerializeField] WingsMap leftMiddle;
    [SerializeField] WingsMap leftUpper;

    [SerializeField] WingsMap rightShoulder;
    [SerializeField] WingsMap rightMiddle;
    [SerializeField] WingsMap rightUpper;





    // Update is called once per frame
    void Update()
    {
        leftShoulder.Map();
        leftMiddle.Map();
        leftUpper.Map();
        rightShoulder.Map();
        rightMiddle.Map();
        rightUpper.Map();
    }
}
