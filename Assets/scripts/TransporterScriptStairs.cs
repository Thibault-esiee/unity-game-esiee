using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TransporterScript : MonoBehaviour
{
    public GameObject player;
    public string sceneName;
    public string playerTag = "Player";
    public string playerSpawnPointTag = "PlayerSpawnPoint";
    public string playerSpawnPointName = "PlayerSpawnPoint";
    public bool isTransporting = false;
    public int floorIndex;

    private BoxCollider boxCollider;

    void Start()
    {
        boxCollider = GetComponent<BoxCollider>();
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag(playerTag) && !isTransporting)
        {
            Debug.Log("Trigger");
            

            StartCoroutine(ActivateTransportBarrier());
            Debug.Log("Transporting player to " + sceneName);
            // SceneManager.LoadScene(sceneName);
        }
    }

    private IEnumerator ActivateTransportBarrier()
    {
        isTransporting = true;


        if (floorIndex == 1)
        {
            yield return new WaitForSeconds(0f);
            if (boxCollider != null)
            {
                Debug.Log("Staits Locked! at floor " + floorIndex.ToString());
                boxCollider.isTrigger = false;
            }
        }

        yield return new WaitForSeconds(0.5f);

        if (boxCollider != null)
        {
            boxCollider.isTrigger = false;
        }
    }
}
