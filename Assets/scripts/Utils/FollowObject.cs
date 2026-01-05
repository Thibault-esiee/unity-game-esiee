using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowObject : MonoBehaviour
{
    public GameObject objectToFollow;
    public GameObject target;
    public bool followVertical = false;
    public bool followLook = false;
    public float yOffset = 0f;
    public float xOffset = 0f;
    public float zOffset = 0f;

    void Start()
    {
        
    }

    void Update()
    {
        if (target != null && objectToFollow != null)
        {
            target.transform.position = new Vector3(objectToFollow.transform.position.x + xOffset, (followVertical ? objectToFollow.transform.position.y : target.transform.position.y) + yOffset, objectToFollow.transform.position.z + zOffset);    
            if (followLook)
            {
                target.transform.rotation = objectToFollow.transform.rotation;
            }
        }
    }
}
