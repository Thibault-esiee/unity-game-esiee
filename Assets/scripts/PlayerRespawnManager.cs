
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerRespawnManager : MonoBehaviour
{
    [Header("Respawn Settings")]
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject deathBody;
    
    [Header("Respawn UI")]
    [SerializeField] private GameObject respawnUI;

    private PlayerController currentPlayerController;
    private static PlayerRespawnManager instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (respawnUI != null)
            respawnUI.SetActive(false);
    }
    public void OnRespawn(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            TryRespawn();
        }
    }

    public void TryRespawn()
    {
        GameObject currentPlayer = GameObject.FindGameObjectWithTag("Player");
        
        if (currentPlayer != null)
        {
            PlayerController playerController = currentPlayer.GetComponent<PlayerController>();
            

            if (playerController != null && !playerController.IsPlayerAlive())
            {
                RespawnPlayer();
            }
        }
    }

    public void RespawnPlayer()
    {
        GameObject currentPlayer = GameObject.FindGameObjectWithTag("Player");
        
        if (currentPlayer != null)
        {
            PlayerController playerController = currentPlayer.GetComponent<PlayerController>();
            

            if (playerController.DeathBody != null)
            {
                Destroy(playerController.DeathBody);
            }
            

            if (respawnPoint != null)
            {
                currentPlayer.transform.position = respawnPoint.position;
                currentPlayer.transform.rotation = respawnPoint.rotation;
            }
            

            playerController.Respawn();
            

            if (respawnUI != null)
                respawnUI.SetActive(false);
            

            NotifyEnemiesOfRespawn();
        }
    }

    private void NotifyEnemiesOfRespawn()
    {
        NPC_Enemy[] enemies = FindObjectsOfType<NPC_Enemy>();
        foreach (NPC_Enemy enemy in enemies)
        {
            enemy.OnPlayerRespawn();
        }
    }
}