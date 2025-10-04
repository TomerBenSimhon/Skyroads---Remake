using System;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerFollow : MonoBehaviour
{
    [SerializeField] private Transform target;

    void Update()
    {
        transform.position = target.position;
    }
}
