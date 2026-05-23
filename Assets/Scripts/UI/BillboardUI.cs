using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillboardUI : MonoBehaviour
{
    private Camera cam;

    void Awake()
    {
        cam = Camera.main;
    }

    void LateUpdate()
    {
        transform.forward = cam.transform.forward;
    }
}
