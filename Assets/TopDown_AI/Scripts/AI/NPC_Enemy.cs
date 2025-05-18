using UnityEngine;
using System.Collections;
public enum NPC_EnemyState{IDLE_STATIC,IDLE_ROAMER,IDLE_PATROL,INSPECT,ATTACK,FIND_WEAPON,KNOCKED_OUT,DEAD,NONE}
public enum NPC_WeaponType{KNIFE,RIFLE,SHOTGUN}
public class NPC_Enemy : MonoBehaviour {
	[Header("Debug / Gameplay Settings")]
	public bool disableShooting = false;
	public float inspectTimeout; //Once the npc reaches the destination, how much time unitl in goes back.
	public UnityEngine.AI.NavMeshAgent navMeshAgent;
	public Animator npcAnimator;
	public Animator characterAnimator;

    public GameObject proyectilePrefab;
	delegate void InitState();
	delegate void UpdateState();
	delegate void EndState();
	InitState _initState;
	InitState _updateState;
	InitState _endState;
	public NPC_WeaponType weaponType=NPC_WeaponType.KNIFE;
	public NPC_EnemyState idleState=NPC_EnemyState.IDLE_ROAMER;
	NPC_EnemyState currentState=NPC_EnemyState.NONE;
	Vector3 targetPos,startingPos;
	public LayerMask hitTestLayer;
	float weaponRange;
	public Transform weaponPivot;
	float weaponActionTime,weaponTime;
	int hashSpeed;
	public NPC_PatrolNode patrolNode;
	// Use this for initialization
	Misc_Timer patrolWaitTimer = new Misc_Timer();
	bool patrolWaiting = false;
	float defaultWeaponActionTime;
	float defaultWeaponTime;

	[Header("Patrol Settings")]
	public float patrolWaitTimeMin = 1.5f;
	public float patrolWaitTimeMax = 3.0f;

	[Header("Sound Settings")]
	public AudioSource audioSource;
	public AudioClip rifleSound;
	public AudioClip shotgunSound;
	public AudioClip knifeSound;

	private bool previousIsWalking = false;

	private Quaternion originalRotation;

	private Transform playerTransform;

	public float defaultSpeed = 1.0f;

	private bool isPausedBySensor = false;

	private float checkPlayerTimer = 0f;
	private float checkPlayerInterval = 0.5f;

	public float minAttackDistance = 3.0f;
	
	private bool isPlayerAlive = true;

	void Start()
	{
		startingPos = transform.position;
		hashSpeed = Animator.StringToHash ("Speed");
		SetWeapon (weaponType);
		SetState (idleState);

		playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
		
		if (audioSource == null)
		{
			audioSource = gameObject.AddComponent<AudioSource>();
		}
	}
	void SetWeapon(NPC_WeaponType newWeapon){
        npcAnimator.SetTrigger("WeaponChange");
		npcAnimator.SetInteger ("WeaponType", (int)weaponType);
		switch (weaponType) {
			case NPC_WeaponType.KNIFE:
				weaponRange=1.0f;
				weaponActionTime=0.2f;
				weaponTime=0.4f;
			break;
			case NPC_WeaponType.RIFLE:
				weaponRange=20.0f;
				weaponActionTime=0.025f;
				weaponTime=0.05f;
			break;
		case NPC_WeaponType.SHOTGUN:
			weaponRange=20.0f;
			weaponActionTime=0.15f;
			weaponTime=0.35f;
			break;
		}
		// defaultWeaponActionTime = weaponActionTime;
		// defaultWeaponTime = weaponTime;
	}
	// Update is called once per frame
	void Update()
	{
		_updateState();
		CheckForPlayerInRange();

		npcAnimator.SetFloat(hashSpeed, navMeshAgent.velocity.magnitude);

		float speed = navMeshAgent.velocity.magnitude;
		bool isWalking = speed > 0.1f;

		if (isWalking != previousIsWalking)
		{
			previousIsWalking = isWalking;
		}

		characterAnimator.SetFloat(hashSpeed, speed);
		characterAnimator.SetBool("IsWalking", isWalking);
	}
	public void SetState(NPC_EnemyState newState){
		if (currentState != newState) {
			if(_endState!=null)
				_endState();
			switch(newState){
				case NPC_EnemyState.IDLE_STATIC:  _initState=StateInit_IdleStatic; 	_updateState=StateUpdate_IdleStatic; 	_endState=StateEnd_IdleStatic; 	break;				
				case NPC_EnemyState.IDLE_ROAMER:  _initState=StateInit_IdleRoamer; 	_updateState=StateUpdate_IdleRoamer; 	_endState=StateEnd_IdleRoamer; 	break;			
				case NPC_EnemyState.IDLE_PATROL:  _initState=StateInit_IdlePatrol; 	_updateState=StateUpdate_IdlePatrol; 	_endState=StateEnd_IdlePatrol; 	break;			
				case NPC_EnemyState.INSPECT:  _initState=StateInit_Inspect; 	_updateState=StateUpdate_Inspect; 	_endState=StateEnd_Inspect; 	break;			
				case NPC_EnemyState.ATTACK:  _initState=StateInit_Attack; 	_updateState=StateUpdate_Attack; 	_endState=StateEnd_Attack; 	break;			
			}
			_initState();			
			currentState=newState;					
		}
	}

	void UpdateSensors(){
		
	}

	///////////////////////////////////////////////////////// STATE: IDLE STATIC


	void StateInit_IdleStatic(){	
		navMeshAgent.SetDestination (startingPos);
		navMeshAgent.Resume ();
	}
	void StateUpdate_IdleStatic(){	

		
	}
	void StateEnd_IdleStatic(){	
	}
	///////////////////////////////////////////////////////// STATE: IDLE PATROL
	
	
	void StateInit_IdlePatrol(){	
		// navMeshAgent.speed = 6.0f;
		// patrolWaiting = false;
		navMeshAgent.SetDestination(patrolNode.GetPosition());
	}
	void StateUpdate_IdlePatrol()
	{
		if (!patrolWaiting && HasReachedMyDestination())
		{
			patrolWaiting = true;
			patrolWaitTimer.StartTimer(2.0f);
			navMeshAgent.velocity = Vector3.zero;
			navMeshAgent.isStopped = true;
		}
		else if (patrolWaiting)
		{
			patrolWaitTimer.UpdateTimer();
			if (patrolWaitTimer.IsFinished())
			{
				patrolNode = patrolNode.nextNode;
				navMeshAgent.SetDestination(patrolNode.GetPosition());
				navMeshAgent.isStopped = false;
				patrolWaiting = false;
			}
		}
	}
	void StateEnd_IdlePatrol(){	
	}

	///////////////////////////////////////////////////////// STATE: IDLE ROAMER


	Misc_Timer idleTimer=new Misc_Timer();
	Misc_Timer idleRotateTimer=new Misc_Timer();
	bool idleWaiting,idleMoving;
	void StateInit_IdleRoamer(){	
		navMeshAgent.speed = 7.0f;

		idleTimer.StartTimer (Random.Range (2.0f, 4.0f));
		RandomRotate ();
		AdvanceIdle ();
		idleWaiting = false;
		idleMoving = true;

	}
	void StateUpdate_IdleRoamer(){	
	
		idleTimer.UpdateTimer ();
	
		if (idleMoving) {
			if (HasReachedMyDestination ()) {
				AdvanceIdle();

			}
		} else if(idleWaiting){
			idleRotateTimer.UpdateTimer ();
			if(	idleRotateTimer.IsFinished()){
				RandomRotate();
				idleRotateTimer.StartTimer(Random.Range(1.5f,3.25f));
			}
		
		}
		if (idleTimer.IsFinished ()) {
			if(idleMoving){
				navMeshAgent.Stop();
				float waitTime=Random.Range (2.5f,6.5f);
				float randomTurnTime=waitTime/2.0f;
				idleRotateTimer.StartTimer (randomTurnTime);
				idleTimer.StartTimer (waitTime);

			
			}
			else if(idleWaiting){
				idleTimer.StartTimer (Random.Range (2.0f, 4.0f));

				AdvanceIdle();
			}

			idleMoving=!idleMoving;
			idleWaiting=!idleMoving;

		}

	}
	void StateEnd_IdleRoamer(){	
	}


	void RayDebug(){
		RaycastHit hit = new RaycastHit ();
		Physics.Raycast (transform.position, transform.forward*5.0f, out hit,50.0f,hitTestLayer);

		Debug.DrawLine(transform.position, hit.point, Color.red);
		Vector3 dir =  hit.point-transform.position;
		Vector3 reflectedVector = Vector3.Reflect (dir,hit.normal);	
		Debug.DrawRay (hit.point, reflectedVector*5.0f, Color.green);				
	}

	void AdvanceIdle(){

		RaycastHit hit = new RaycastHit ();
		Physics.Raycast (transform.position, transform.forward*5.0f, out hit,50.0f,hitTestLayer);
		//Debug.DrawRay (transform.position, transform.forward, Color.red);

		if (hit.distance < 3.0f) {
			Vector3 dir =  hit.point-transform.position;
			Vector3 reflectedVector = Vector3.Reflect (dir,hit.normal);	
			Physics.Raycast (transform.position,reflectedVector, out hit,50.0f,hitTestLayer);
		}

		navMeshAgent.Resume();
		navMeshAgent.SetDestination (hit.point);

	
	}
	///////////////////////////////////////////////////////// STATE: INSPECT
	Misc_Timer inspectTimer = new Misc_Timer ();
	Misc_Timer inspectTurnTimer = new Misc_Timer ();
	bool inspectWait;
	void StateInit_Inspect(){	
		navMeshAgent.speed = 16.0f;
		navMeshAgent.Resume ();
		inspectTimer.StopTimer ();
		inspectWait = false;
	}
	void StateUpdate_Inspect()
	{
		GameObject player = GameObject.FindGameObjectWithTag("Player");
		float distanceToPlayer = player != null ? Vector3.Distance(transform.position, player.transform.position) : float.MaxValue;

		if (player != null && distanceToPlayer <= minAttackDistance)
		{
			SetState(NPC_EnemyState.ATTACK);
			return;
		}

		if (!HasReachedMyDestination() && player != null)
		{
			Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;
			Vector3 targetPosition = player.transform.position - directionToPlayer * minAttackDistance;

			navMeshAgent.SetDestination(targetPosition);
		}
		else if (!inspectWait && HasReachedMyDestination())
		{
			inspectWait = true;
			inspectTimer.StartTimer(2.0f);
			inspectTurnTimer.StartTimer(1.0f);
		}
		else
		{
			navMeshAgent.SetDestination(targetPos);
		}

		RaycastHit hit = new RaycastHit();
		Physics.Raycast(transform.position, transform.forward, out hit, weaponRange, hitTestLayer);

		if (hit.collider != null && hit.collider.tag == "Player")
		{
			SetState(NPC_EnemyState.ATTACK);
		}

		if (inspectWait)
		{
			inspectTimer.UpdateTimer();
			inspectTurnTimer.UpdateTimer();
			if (inspectTurnTimer.IsFinished())
			{
				RandomRotate();
				inspectTurnTimer.StartTimer(Random.Range(0.5f, 1.25f));
			}
			if (inspectTimer.IsFinished())
				SetState(idleState);
		}
	}
	void StateEnd_Inspect(){	
		navMeshAgent.speed = defaultSpeed;
	}

	///////////////////////////////////////////////////////// STATE: ATTACK
	Misc_Timer attackActionTimer=new Misc_Timer();
	bool actionDone;
	void StateInit_Attack()
	{
		// Arrêter complètement le déplacement
		navMeshAgent.isStopped = true;
		navMeshAgent.velocity = Vector3.zero;
		
		// Animations d'attaque
		npcAnimator.SetBool("Attack", true);
		characterAnimator.SetBool("IsShooting", true);
		
		// Préparation de l'attaque
		CancelInvoke("AttackAction");
		Invoke("AttackAction", weaponActionTime);
		attackActionTimer.StartTimer(weaponTime);
		
		actionDone = false;
		
		// S'assurer de regarder le joueur au début de l'attaque
		RotateTowardsPlayer();
	}

	void StateUpdate_Attack()
	{
		// Vérifier si le joueur est toujours vivant
		GameObject player = GameObject.FindGameObjectWithTag("Player");
		if (player == null || !player.GetComponent<PlayerController>().enabled)
		{
			// Joueur mort, revenir à l'état initial
			SetState(idleState);
			return;
		}

		// Rotation vers le joueur pendant l'attaque
		RotateTowardsPlayer();
		
		attackActionTimer.UpdateTimer();
		if (!actionDone && attackActionTimer.IsFinished())
		{
			EndAttack();
			actionDone = true;
		}
	}
	void StateEnd_Attack(){	
		npcAnimator.SetBool ("Attack", false);
		characterAnimator.SetBool("IsShooting", false);
		navMeshAgent.speed = defaultSpeed;

		// weaponActionTime = defaultWeaponActionTime;
		// weaponTime = defaultWeaponTime;
	}
	void EndAttack(){
		SetState (NPC_EnemyState.INSPECT);
	}
	void AttackAction(){
		if (disableShooting) return;
		switch (weaponType) {
			case NPC_WeaponType.KNIFE:
				RaycastHit[] hits=Physics.SphereCastAll (weaponPivot.position,2.0f, weaponPivot.forward);
				foreach(RaycastHit hit in hits){
					if (hit.collider!=null && hit.collider.tag == "Player") {
						hit.collider.GetComponent<PlayerBehavior>().DamagePlayer();
					}
				}
				if (knifeSound != null && audioSource != null)
				{
					audioSource.PlayOneShot(knifeSound);
				}
			break;
			case NPC_WeaponType.RIFLE:
				GameObject bullet=GameObject.Instantiate(proyectilePrefab, weaponPivot.position,weaponPivot.rotation) as GameObject;
				bullet.transform.Rotate(0,Random.Range(-7.5f,7.5f),0);
				if (rifleSound != null && audioSource != null)
				{
					audioSource.PlayOneShot(rifleSound);
				}
			break;
			case NPC_WeaponType.SHOTGUN:
				for(int i=0;i<5;i++){
					GameObject birdshot=GameObject.Instantiate(proyectilePrefab, weaponPivot.position,weaponPivot.rotation) as GameObject;
					birdshot.transform.Rotate(0,Random.Range(-15,15),0);
				}
				if (shotgunSound != null && audioSource != null)
				{
					audioSource.PlayOneShot(shotgunSound);
				}
			break;
		}
	}
	////////////////////////// MISC FUNCTIONS //////////////////////////

	void RandomRotate(){
		float randomAngle =Random.Range (45, 180);
		float randomSign = Random.Range (0, 2);
		if (randomSign == 0)
			randomAngle *= -1;

		transform.Rotate (0, randomAngle, 0);
	}
	/*float randomMoveInnerRadius=0.5f, randomMoveOuterRadius=10.0f;
	private Vector3 GetRandomPoint(){	
		Vector3 newPos;
		//do{
			newPos=Random.insideUnitSphere * randomMoveOuterRadius;
		//}while(newPos.x <randomMoveInnerRadius && newPos.y<randomMoveInnerRadius);
		Vector3 finalPos = transform.position + newPos;

		return finalPos;
	}*/
	public bool HasReachedMyDestination(){
		float dist = Vector3.Distance (transform.position, navMeshAgent.destination);
		if ( dist<= 1.5f) {
			return 	true;
		}
		
		return false;
	}
	////////////////////////// PUBLIC FUNCTIONS //////////////////////////
	public void SetAlertPos(Vector3 newPos){
		if (idleState != NPC_EnemyState.IDLE_STATIC) {
			SetTargetPos(newPos);
		}
	}
	public void SetTargetPos(Vector3 newPos)
	{
		GameObject player = GameObject.FindGameObjectWithTag("Player");

		if (player != null && player.transform.position == newPos)
		{
			float distanceToPlayer = Vector3.Distance(transform.position, newPos);

			if (distanceToPlayer <= minAttackDistance)
			{
				targetPos = transform.position;
				if (currentState != NPC_EnemyState.ATTACK)
				{
					SetState(NPC_EnemyState.ATTACK);
				}
				return;
			}
			else
			{
				Vector3 directionToPlayer = (newPos - transform.position).normalized;
				targetPos = newPos - directionToPlayer * minAttackDistance;
			}
		}
		else
		{
			targetPos = newPos;
		}

		if (currentState != NPC_EnemyState.ATTACK)
		{
			SetState(NPC_EnemyState.INSPECT);
		}
	}
	public void Damage(){
		navMeshAgent.velocity = Vector3.zero;
		//navMeshAgent.Stop ();
		npcAnimator.SetBool ("Dead", true);
		GameManager.AddScore (100);
		npcAnimator.transform.parent = null;
		Vector3 pos = npcAnimator.transform.position;
		pos.y = 0.2f;
		npcAnimator.transform.position = pos;
		GameManager.RemoveEnemy ();		
		Destroy (gameObject);
	}

	void RotateTowardsPlayer()
	{
		GameObject player = GameObject.FindGameObjectWithTag("Player");
		if (player != null)
		{
			Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;
			
			directionToPlayer.y = 0;
			
			if (directionToPlayer != Vector3.zero)
			{
				Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
				
				transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
			}
		}
	}

	public void OnPlayerDeath()
	{
		isPlayerAlive = false;
		
		if (currentState == NPC_EnemyState.ATTACK || currentState == NPC_EnemyState.INSPECT)
		{
			npcAnimator.SetBool("Attack", false);
			characterAnimator.SetBool("IsShooting", false);
			
			navMeshAgent.isStopped = false;
			navMeshAgent.velocity = Vector3.zero;
			
			SetState(idleState);
		}
	}

	public void OnPlayerRespawn()
	{
		isPlayerAlive = true;
	}

	void CheckForPlayerInRange()
	{
		if (!isPlayerAlive)
		{
			if (currentState == NPC_EnemyState.ATTACK)
			{
				SetState(idleState);
			}
			return;
		}

		if (currentState == NPC_EnemyState.INSPECT || currentState == NPC_EnemyState.ATTACK) 
		{
			checkPlayerTimer += Time.deltaTime;
			
			if (checkPlayerTimer >= checkPlayerInterval) 
			{
				checkPlayerTimer = 0f;
				
				GameObject player = GameObject.FindGameObjectWithTag("Player");
				if (player != null) 
				{
					float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
					
					PlayerController playerController = player.GetComponent<PlayerController>();
					if (playerController != null && playerController.enabled)
					{
					}
					else 
					{
						isPlayerAlive = false;
						SetState(idleState);
					}
				}
			}
		}
	}
}
