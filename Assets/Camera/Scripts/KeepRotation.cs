using UnityEngine;
using Unity.Cinemachine;

public class KeepRotation : CinemachineExtension
{
    
    private Vector3 _startRotation;
    void Start()
    {
        _startRotation = transform.rotation.eulerAngles;
    }

    // Update is called once per frame
    void Update()
    {
        if(transform.rotation.eulerAngles != _startRotation)
            transform.rotation = Quaternion.Euler(_startRotation);
    }
}
