using UnityEngine;

namespace HarmonyDialogueSystem.Demo
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 50;
        [SerializeField] float smoothMoveTime = .1f;
        [SerializeField] float turnSpeed = 8;

        private Rigidbody playerRb;

        float angle;
        Vector3 velocity;
        float smoothInputMagnitude;
        float smoothMoveVelocity;

        public bool stopMovement { get; private set; }

        void Start()
        {
            playerRb = GetComponent<Rigidbody>();
            stopMovement = false;
        }

        void Update()
        {
            float horizontalInput = Input.GetAxisRaw("Horizontal");
            float verticalInput = Input.GetAxisRaw("Vertical");
            Vector3 moveDir = Vector3.zero;

            if (!stopMovement && !DialogueManager.instance.dialogueIsPlaying)
            {

                moveDir = new Vector3(horizontalInput, 0, verticalInput);

                float inputMagnitude = moveDir.magnitude;
                smoothInputMagnitude = Mathf.SmoothDamp(smoothInputMagnitude, inputMagnitude, ref smoothMoveVelocity, smoothMoveTime);

                float inputAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;

                angle = Mathf.LerpAngle(angle, inputAngle, turnSpeed * Time.deltaTime * inputMagnitude);
                velocity = moveSpeed * smoothInputMagnitude * transform.forward;
            }
        }

        private void FixedUpdate()
        {
            if (!stopMovement && !DialogueManager.instance.dialogueIsPlaying)
            {
                playerRb.MoveRotation(Quaternion.Euler(Vector3.up * angle));
                playerRb.MovePosition(playerRb.position + velocity * Time.deltaTime);
            }
        }

        private void LockMovement()
        {
            stopMovement = true;
        }

        private void UnlockMovement()
        {
            stopMovement = false;
        }
    }
}
