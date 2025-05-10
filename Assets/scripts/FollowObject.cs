using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowObject : MonoBehaviour
{
    public GameObject gameObject;
    public GameObject target;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        target.transform.position = new Vector3(gameObject.transform.position.x, target.transform.position.y, gameObject.transform.position.z);    
    }
}
