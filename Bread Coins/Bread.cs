using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(SphereCollider))]
public class Bread : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private float respawnTimeSeconds = 8;
    [SerializeField] private int breadGained = 1;
    
    private MeshRenderer visual;
    private SphereCollider sphereCollider;
    private void Awake()
    {
        visual = GetComponent<MeshRenderer>();
        sphereCollider = GetComponent<SphereCollider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CollectBread();
        }
    }

    private void CollectBread()
    {
        sphereCollider.enabled = false;
        visual.enabled = false;
        GameEventsManager.Instance.breadEvents.BreadGained(breadGained);
        GameEventsManager.Instance.breadEvents.BreadCollected();
        StopAllCoroutines();
        StartCoroutine(RespawnAfterTime());
    }
    private IEnumerator RespawnAfterTime()
    {
        yield return new WaitForSeconds(respawnTimeSeconds);
        sphereCollider.enabled = true;
        visual.enabled=true;
    }
}
