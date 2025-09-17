using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreadManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private int startingBreadAmount = 0;
    public static BreadManager Instance { get; private set; }

    public int currentBreadAmount { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("Found more than one Game Events Manager in the scene.");
        }
        Instance = this;

        currentBreadAmount = startingBreadAmount;
    }

    private void OnEnable()
    {
        GameEventsManager.Instance.breadEvents.onBreadGained += BreadGained;

    }

    private void OnDisable()
    {
        GameEventsManager.Instance.breadEvents.onBreadGained -= BreadGained;
    }

    private void Start()
    {
        GameEventsManager.Instance.breadEvents.BreadChange(currentBreadAmount);
    }

    private void BreadGained(int breadAmount)
    {
        currentBreadAmount += breadAmount;
        GameEventsManager.Instance.breadEvents.BreadChange(currentBreadAmount);
        GameEventsManager.Instance.breadEvents.BreadCollected();
    }

}
