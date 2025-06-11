using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugTime : MonoBehaviour
{
    bool timeStopped = false;
    [SerializeField] private Transform rightHandController;
    [SerializeField] private Transform leftHandController;
    [SerializeField] private Transform parentTransform;

    [SerializeField] private GameObject leftHandToSpawnDuringPause;
    [SerializeField] private GameObject rightHandToSpawnDuringPause;

    private GameObject pauseLeftHand;
    private GameObject pauseRightHand;

    private void StopTime()
    {
        Time.timeScale = 0f;
    }
    private void ContinueTime()
    {
        Time.timeScale = 1f;
    }
    public void ToggleTime()
    {
        timeStopped = !timeStopped;
        if (timeStopped)
        {
            pauseLeftHand = Instantiate(leftHandToSpawnDuringPause, leftHandController.position, leftHandController.rotation, parentTransform);
            pauseRightHand = Instantiate(rightHandToSpawnDuringPause, rightHandController.position, rightHandController.rotation, parentTransform);

            StopTime();
        }
        else
        {
            ContinueTime();
            if (pauseLeftHand != null)
            {
                //  Destroy(pauseLeftHand);
            }
            if (pauseRightHand != null)
            {
                //    Destroy(pauseRightHand);
            }
        }
    }

}
