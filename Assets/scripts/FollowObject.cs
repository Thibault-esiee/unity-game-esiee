using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowObject : MonoBehaviour
{
    public GameObject gameObject;
    public GameObject target;
    public bool followVertical = false;
    public bool followLook = false;
    public float yOffset = 0f;
    public float xOffset = 0f;
    public float zOffset = 0f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        target.transform.position = new Vector3(gameObject.transform.position.x + xOffset, (followVertical ? gameObject.transform.position.y : target.transform.position.y) + yOffset, gameObject.transform.position.z + zOffset);    
        if (followLook)
        {
            target.transform.rotation = gameObject.transform.rotation;
        }
    }
}
