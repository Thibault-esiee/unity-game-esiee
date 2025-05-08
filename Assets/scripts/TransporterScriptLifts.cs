using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TransporterScriptLifts : MonoBehaviour
{
    public string sceneName;
    public string playerTag = "Player";
    public bool isTransporting = false;

    private BoxCollider boxCollider;

    void Start()
    {
        boxCollider = GetComponent<BoxCollider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isTransporting && other.CompareTag(playerTag))
        {
            Debug.Log("Player entered lift trigger.");
            StartCoroutine(ActivateTransportBarrier());
        }
    }

    private IEnumerator ActivateTransportBarrier()
    {
        isTransporting = true;

        yield return new WaitForSeconds(0.5f);

        // Lock the lift entrance
        if (boxCollider != null)
        {
            boxCollider.isTrigger = false;
            Debug.Log("Lift entrance locked.");
        }

        // SceneManager.LoadScene(sceneName);
    }
}
