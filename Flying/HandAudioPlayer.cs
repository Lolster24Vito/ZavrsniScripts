using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandAudioPlayer : MonoBehaviour
{
    [SerializeField] AudioClip flapAudioClip;
    private AudioSource flapAudioSource;
    // Start is called before the first frame update
    void Start()
    {
        flapAudioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {

    }
    void OnEnable()
    {
        GameEventsManager.Instance.OnFlapEvent+= OnFlapPlaySound;
    }

    private void OnFlapPlaySound(FlapEventDTO obj)
    {
        //pitch po jacini mahanja
        flapAudioSource.PlayOneShot(flapAudioClip);

    }

    void OnDisable()
    {
        GameEventsManager.Instance.OnFlapEvent -= OnFlapPlaySound;
    }
}
