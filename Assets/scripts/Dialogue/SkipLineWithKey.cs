using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;
using Yarn.Unity.Editor;

public class SkipLineWithKey : MonoBehaviour
{
    public LineView line;
    void Start()
    {
                
    }

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
