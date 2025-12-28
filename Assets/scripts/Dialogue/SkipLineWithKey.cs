using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;
using Yarn.Unity.Editor;

public class SkipLineWithKey : MonoBehaviour
{
    public LineView line;
    // Start is called before the first frame update
    void Start()
    {
                
    }

    // Update is called once per frame
    void Update()
    {
        if (line != null)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                line.OnContinueClicked();
            }
        }
    }
}
