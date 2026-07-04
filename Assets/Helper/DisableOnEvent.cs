using System;
using UnityEngine;
using UnityEngine.Events;

public class DisableOnEvent : MonoBehaviour
{
   [SerializeField] private GlobalEvents.Id triggerMask;
   [SerializeField] private GameObject eventObject;

   void OnEnable()
   {
      GlobalEvents.Raised += OnEvent;
   }

   void OnDisable()
   {
      GlobalEvents.Raised -= OnEvent;
   }

   private void OnEvent(GlobalEvents.Id id, GameObject sender)
   {
      if ((triggerMask & id) == 0) return;
      if (eventObject != null && sender != eventObject && sender != null) return;
      
      gameObject.SetActive(false);
   }
}
