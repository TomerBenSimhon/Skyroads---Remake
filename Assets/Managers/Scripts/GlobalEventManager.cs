using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalEventManager : MonoBehaviour
{
    public static GlobalEventManager I;

    private Dictionary<string, GameObject> _triggerEvents = new();
    private Dictionary<string, GameObject> _cancelEvents = new();
    public Dictionary<string, GameObject> TriggerEvents => _triggerEvents;
    public Dictionary<string, GameObject> CancelEvents => _cancelEvents;

    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public void TriggerEvent(string eventName, GameObject eventObject)
    {
        _triggerEvents.Add(eventName, eventObject);
        StartCoroutine(ClearEvents(_triggerEvents));
    }

    public void TriggerCancelEvent(string eventName, GameObject eventObject)
    {
        _cancelEvents.Add(eventName, eventObject);
        StartCoroutine(ClearEvents(_cancelEvents));
    }

    IEnumerator ClearEvents(Dictionary<string,GameObject> events)
    {
        yield return null;
        events.Clear();
    }
}
