using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cinemachine;

public class InteractWithEnvironment : MonoBehaviour
{
    [Header("Choose the way you want to trigger the event")]
    [Tooltip("By using a tag, default: Player")]
    [SerializeField]
    private string otherTag = "Player";
    [Tooltip("By using a GameObject, default: null")]
    [SerializeField]
    private GameObject GameObject;

    [Header("Choose the image that will be used to display the indicator")]
    [SerializeField]
    private Image interactImage;

    [SerializeField]
    [Header("Virtual Camera (cinemachine)")]
    private CinemachineVirtualCamera virtualCamera;
    
    private bool interacting = false;
    private bool insideRange = false;


    private Transform originalLookAt;
    private Transform originalFollow;
    private Quaternion originalRotation;
    private Vector3 originalPosition;
    
    // Start is called before the first frame update
    void Start()
    {
        if (interactImage != null)
        {
            if (interactImage.enabled)
            {
                interactImage.enabled = false;
            }
        }
    }

    // Update is called once per frame

    void Update()
    {
        if (insideRange && !interacting)
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                interacting = true;
                interactImage.enabled = false;

                if (virtualCamera != null)
                {
                    if (GameObject !=  null) { GameObject.SetActive(false); }

                    originalLookAt = virtualCamera.LookAt;
                    originalFollow = virtualCamera.Follow;
                    originalRotation = virtualCamera.transform.rotation;
                    originalPosition = virtualCamera.transform.position;

                    virtualCamera.Follow = null;
                    virtualCamera.LookAt = transform;
                    
                    Vector3 directionToTarget = transform.position - virtualCamera.transform.position;
                    directionToTarget.y = 0;
                    virtualCamera.transform.rotation = Quaternion.LookRotation(directionToTarget, Vector3.up);

                    Vector3 loweredPosition = virtualCamera.transform.position;
                    loweredPosition.y = transform.position.y;
                    virtualCamera.transform.position = loweredPosition;
                    
                    StartCoroutine(ResetCameraAfterDelay(2f));
                }
            }
        }

    }

    private IEnumerator ResetCameraAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        interacting = false;

        if (virtualCamera)
        {
            virtualCamera.LookAt = originalLookAt;
            virtualCamera.Follow = originalFollow;
            virtualCamera.transform.rotation = originalRotation;
            virtualCamera.transform.position = originalPosition;
        }

        if (GameObject)
        {
            GameObject.SetActive(true);
            GameObject = null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other != null)
        {
            bool f = GameObject ? GameObject.Equals(other.gameObject) : false;
            if (other.CompareTag(otherTag) || f)
            {
                GameObject = other.gameObject;
                interactImage.enabled = true;
                insideRange = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        insideRange = false;
        interactImage.enabled = false;
    }
}
