using System;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerFollow : MonoBehaviour
{
    [SerializeField] private Transform target;

    void Update()
    {
        if (!target) return;
        transform.position = target.position;
    }
}
