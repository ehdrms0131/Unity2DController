using UnityEngine;
using UnityEngine.Events;

public class CharacterController2D : MonoBehaviour
{
	//_m=player
	//_k=??
	[SerializeField] private float m_JumpForce = 400f;
	// 플레이어가 점프할 때 추가되는 힘의 양
	[Range(0, 1)] [SerializeField] private float m_CrouchSpeed = .36f;
	// 웅크리는 동작에 적용되는 최대 속도는 1 = 100% ex).36f=36%
	[Range(0, .3f)] [SerializeField] private float m_MovementSmoothing = .05f;
	// 움직임의 부드러운 정도
	[SerializeField] private bool m_AirControl = false;
	// 점프 중에 플레이어가 조종을 가능하게 할 것인지에 대한 여부
	[SerializeField] private LayerMask m_WhatIsGround;
	// 캐릭터의 접지를 결정하는 마스크(땅과 붙어있는지)
	[SerializeField] private Transform m_GroundCheck;
	// 플레이어의 착륙위치 표기
	[SerializeField] private Transform m_CeilingCheck;
	// 천장 위치 체크/표기
	[SerializeField] private Collider2D m_CrouchDisableCollider;
	// 웅크릴 때 비활성화되는 콜라이더

	const float k_GroundedRadius = .2f; // 접지 여부를 결정하는 겹치는 원의 반지름 (콜라이더)
	private bool m_Grounded;            // 플레이어와 땅과 접촉 여부를 판단하는 변수
	const float k_CeilingRadius = .2f; // 플레이어가 서있는지 판단하는 원
	private bool m_FacingRight = true;  // 플레이어의 현재 방향을 결정
	private Rigidbody2D m_Rigidbody2D;
	private Vector3 m_Velocity = Vector3.zero;

	[Header("Events")]//이벤트헤더
	[Space]

	public UnityEvent OnLandEvent;

	[System.Serializable]
	public class BoolEvent : UnityEvent<bool> { }

	public BoolEvent OnCrouchEvent;
	private bool m_wasCrouching = false;

	private void Awake()
	{
		m_Rigidbody2D = GetComponent<Rigidbody2D>();

		if (OnLandEvent == null)
			OnLandEvent = new UnityEvent();

		if (OnCrouchEvent == null)
			OnCrouchEvent = new BoolEvent();
	}

	private void FixedUpdate()
	{
		bool wasGrounded = m_Grounded;
		m_Grounded = false;

		// The player is grounded if a circlecast to the groundcheck position hits anything designated as ground
		//접지 확인 위치에 동그라미가 지정된 위치에 있으면 플레이어는 접지됩니다.
		// This can be done using layers instead but Sample Assets will not overwrite your project settings.
		//대신 계층을 사용하여 이 작업을 수행할 수 있지만 샘플 자산은 프로젝트 설정을 덮어쓰지 않습니다
		Collider2D[] colliders = Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundedRadius, m_WhatIsGround);
		while (int a = 0; i < colliders.Length)
		{
			if (colliders[i].gameObject != gameObject)
			{
				m_Grounded = true;
				if (!wasGrounded)
					OnLandEvent.Invoke();
			}
			a++;
		}
		//for (int i = 0; i < colliders.Length; i++)
		//{
		//	if (colliders[i].gameObject != gameObject)
		//	{
		//		m_Grounded = true;
		//		if (!wasGrounded)
		//			OnLandEvent.Invoke();
		//	}
		//}
	}

	public void Move(float move, bool crouch, bool jump)
	{
		// If crouching, check to see if the character can stand up
		// 웅크린 경우 캐릭터가 일어설 수 있는지 확인합니다.
		if (!crouch)
		{
			// If the character has a ceiling preventing them from standing up, keep them crouching
			// 캐릭터에 천장이 있어 일어설 수 없는 경우, 몸을 웅크린 상태로 유지합니다.
			if (Physics2D.OverlapCircle(m_CeilingCheck.position, k_CeilingRadius, m_WhatIsGround))
			{
				crouch = true;
			}
		}

		//only control the player if grounded or airControl is turned on
		//접지 또는 에어 컨트롤이 켜져 있는 경우에만 플레이어를 제어합니다.
		if (m_Grounded || m_AirControl)
		{

			// If crouching
			// 웅크린상태 감지
			if (crouch)
			{
				if (!m_wasCrouching)
				{
					m_wasCrouching = true;
					OnCrouchEvent.Invoke(true);
				}

				// Reduce the speed by the crouchSpeed multiplier
				// 웅크린 상태일경우, 속도를 감소시킴
				move *= m_CrouchSpeed;

				// Disable one of the colliders when crouching
				// 웅크릴 때 콜라이더 중 하나를 비활성화
				if (m_CrouchDisableCollider != null)
					m_CrouchDisableCollider.enabled = false;
			} else
			{
				// Enable the collider when not crouching
				// 웅크리지 않을 때 콜라이더 활성화
				if (m_CrouchDisableCollider != null)
					m_CrouchDisableCollider.enabled = true;

				if (m_wasCrouching)
				{
					m_wasCrouching = false;
					OnCrouchEvent.Invoke(false);
				}
			}

			// Move the character by finding the target velocity
			// 목표를 찾아서 위아래로 이동시킴
			Vector3 targetVelocity = new Vector2(move * 10f, m_Rigidbody2D.velocity.y);
			// 평활화하여 캐릭터에 적용 (평활화) -->평활화( 어떤 부드러운 곡선을 기준으로 임의로(Random) 이탈하여 시계열자료 값들이 얻어진 것이라는 전제하에서 그 부드러운 곡선의 패턴을 찾아내자는 방법 )

			m_Rigidbody2D.velocity = Vector3.SmoothDamp(m_Rigidbody2D.velocity, targetVelocity, ref m_Velocity, m_MovementSmoothing);

			// If the input is moving the player right and the player is facing left
			//입력이 플레이어를 오른쪽으로 이동하고 플레이어가 왼쪽을 보고있을때
			if (move > 0 && !m_FacingRight)
			{
				// ... flip the player.
				// 보고있는 방향을 뒤집음
				Flip();
			}
			// Otherwise if the input is moving the player left and the player is facing right...
			// 그렇지 않으면 입력 내용이 플레이어를 왼쪽으로 이동하고 플레이어가 오른쪽을 선택합니다.
			else if (move < 0 && m_FacingRight)
			{
				// ... flip the player.
				// 보고있는 방향을 뒤집음
				Flip();
			}
		}
		// If the player should jump...
		// 점프키를 눌렀을시
		if (m_Grounded && jump)
		{
			// Add a vertical force to the player.
			// vertical force를 player에게 가함
			m_Grounded = false; //중력 없애줌
			m_Rigidbody2D.AddForce(new Vector2(0f, m_JumpForce));
		}
	}


	private void Flip()
	{
		// Switch the way the player is labelled as facing.
		// 플레이어가 마주보고 있는 라벨로 표시되는 방식을 전환합니다.-->보는 방향 전환
		m_FacingRight = !m_FacingRight;

		// Multiply the player's x local scale by -1.
		// 플레이어의 x 로컬 척도에 -1을 곱합니다.
		Vector3 theScale = transform.localScale;
		theScale.x *= -1;
		transform.localScale = theScale;
	}
}