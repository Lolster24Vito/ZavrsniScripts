using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EventManager
{
    private static Dictionary<string, Action<FlapEventDTO>> flapEventDictionary = new Dictionary<string, Action<FlapEventDTO>>();

    public static void StartListening(string eventName, Action<FlapEventDTO> listener)
    {
        if (!flapEventDictionary.ContainsKey(eventName))
        {
            flapEventDictionary[eventName] = listener;
        }
        else
        {
            flapEventDictionary[eventName] += listener;
        }
    }

    public static void StopListening(string eventName, Action<FlapEventDTO> listener)
    {
        if (flapEventDictionary.ContainsKey(eventName))
        {
            flapEventDictionary[eventName] -= listener;
        }
    }

    public static void TriggerEvent(string eventName, FlapEventDTO eventData)
    {
        if (flapEventDictionary.ContainsKey(eventName))
        {
            flapEventDictionary[eventName]?.Invoke(eventData);
        }
    }
}