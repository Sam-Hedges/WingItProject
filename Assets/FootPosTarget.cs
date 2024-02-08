using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FootPosTarget : MonoBehaviour
{
    [HideInInspector]
    public Vector3 position { get { return transform.position; } set { transform.position = value; } }
    public Quaternion rotation { get { return transform.rotation; } set { transform.rotation = value; } }
    public Transform mTransform { get { return transform; } }
    public Vector3 stablePosition { get; set; }
}
