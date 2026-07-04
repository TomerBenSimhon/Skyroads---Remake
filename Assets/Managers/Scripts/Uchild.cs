using System;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class Unchild : MonoBehaviour
{
    private void Awake()
    {
        transform.parent = null;
    }
}