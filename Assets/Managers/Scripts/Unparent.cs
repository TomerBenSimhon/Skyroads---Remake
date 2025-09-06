using System;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class Unparent : MonoBehaviour
{
    private void Awake()
    {
        foreach (Transform child in transform.GetComponentsInChildren<Transform>())
        {
            child.parent = null;
        }
    }
}
