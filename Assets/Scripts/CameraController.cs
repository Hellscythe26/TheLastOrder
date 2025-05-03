using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform objective;
    public float camraVelocity = 0.5f;
    public Vector3 scrolling;

    private void LateUpdate()
    {
        Vector3 wishScrolling = objective.position + scrolling;
        Vector3 smoothedScrolling = Vector3.Lerp(transform.position, wishScrolling, camraVelocity);
        transform.position = smoothedScrolling;
    }
}