using UnityEngine;
using UnityEngine.Events;

public class CharacterController2D : MonoBehaviour
{
	//_m=player
	//_k=??
	[SerializeField] private float m_JumpForce = 400f;
	// �÷��̾ ������ �� �߰��Ǵ� ���� ��
	[Range(0, 1)] [SerializeField] private float m_CrouchSpeed = .36f;
	// ��ũ���� ���ۿ� ����Ǵ� �ִ� �ӵ��� 1 = 100% ex).36f=36%
	[Range(0, .3f)] [SerializeField] private float m_MovementSmoothing = .05f;
	// �������� �ε巯�� ����
	[SerializeField] private bool m_AirControl = false;
	// ���� �߿� �÷��̾ ������ �����ϰ� �� �������� ���� ����
	[SerializeField] private LayerMask m_WhatIsGround;
	// ĳ������ ������ �����ϴ� ����ũ(���� �پ��ִ���)
	[SerializeField] private Transform m_GroundCheck;
	// �÷��̾��� ������ġ ǥ��
	[SerializeField] private Transform m_CeilingCheck;
	// õ�� ��ġ üũ/ǥ��
	[SerializeField] private Collider2D m_CrouchDisableCollider;
	// ��ũ�� �� ��Ȱ��ȭ�Ǵ� �ݶ��̴�

	const float k_GroundedRadius = .2f; // ���� ���θ� �����ϴ� ��ġ�� ���� ������ (�ݶ��̴�)
	private bool m_Grounded;            // �÷��̾�� ���� ���� ���θ� �Ǵ��ϴ� ����
	const float k_CeilingRadius = .2f; // �÷��̾ ���ִ��� �Ǵ��ϴ� ��
	private bool m_FacingRight = true;  // �÷��̾��� ���� ������ ����
	private Rigidbody2D m_Rigidbody2D;
	private Vector3 m_Velocity = Vector3.zero;

	[Header("Events")]//�̺�Ʈ���
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
		//���� Ȯ�� ��ġ�� ���׶�̰� ������ ��ġ�� ������ �÷��̾�� �����˴ϴ�.
		// This can be done using layers instead but Sample Assets will not overwrite your project settings.
		//��� ������ ����Ͽ� �� �۾��� ������ �� ������ ���� �ڻ��� ������Ʈ ������ ����� �ʽ��ϴ�
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
		// ��ũ�� ��� ĳ���Ͱ� �Ͼ �� �ִ��� Ȯ���մϴ�.
		if (!crouch)
		{
			// If the character has a ceiling preventing them from standing up, keep them crouching
			// ĳ���Ϳ� õ���� �־� �Ͼ �� ���� ���, ���� ��ũ�� ���·� �����մϴ�.
			if (Physics2D.OverlapCircle(m_CeilingCheck.position, k_CeilingRadius, m_WhatIsGround))
			{
				crouch = true;
			}
		}

		//only control the player if grounded or airControl is turned on
		//���� �Ǵ� ���� ��Ʈ���� ���� �ִ� ��쿡�� �÷��̾ �����մϴ�.
		if (m_Grounded || m_AirControl)
		{

			// If crouching
			// ��ũ������ ����
			if (crouch)
			{
				if (!m_wasCrouching)
				{
					m_wasCrouching = true;
					OnCrouchEvent.Invoke(true);
				}

				// Reduce the speed by the crouchSpeed multiplier
				// ��ũ�� �����ϰ��, �ӵ��� ���ҽ�Ŵ
				move *= m_CrouchSpeed;

				// Disable one of the colliders when crouching
				// ��ũ�� �� �ݶ��̴� �� �ϳ��� ��Ȱ��ȭ
				if (m_CrouchDisableCollider != null)
					m_CrouchDisableCollider.enabled = false;
			} else
			{
				// Enable the collider when not crouching
				// ��ũ���� ���� �� �ݶ��̴� Ȱ��ȭ
				if (m_CrouchDisableCollider != null)
					m_CrouchDisableCollider.enabled = true;

				if (m_wasCrouching)
				{
					m_wasCrouching = false;
					OnCrouchEvent.Invoke(false);
				}
			}

			// Move the character by finding the target velocity
			// ��ǥ�� ã�Ƽ� ���Ʒ��� �̵���Ŵ
			Vector3 targetVelocity = new Vector2(move * 10f, m_Rigidbody2D.velocity.y);
			// ��Ȱȭ�Ͽ� ĳ���Ϳ� ���� (��Ȱȭ) -->��Ȱȭ( � �ε巯�� ��� �������� ���Ƿ�(Random) ��Ż�Ͽ� �ð迭�ڷ� ������ ����� ���̶�� �����Ͽ��� �� �ε巯�� ��� ������ ã�Ƴ��ڴ� ��� )

			m_Rigidbody2D.velocity = Vector3.SmoothDamp(m_Rigidbody2D.velocity, targetVelocity, ref m_Velocity, m_MovementSmoothing);

			// If the input is moving the player right and the player is facing left
			//�Է��� �÷��̾ ���������� �̵��ϰ� �÷��̾ ������ ����������
			if (move > 0 && !m_FacingRight)
			{
				// ... flip the player.
				// �����ִ� ������ ������
				Flip();
			}
			// Otherwise if the input is moving the player left and the player is facing right...
			// �׷��� ������ �Է� ������ �÷��̾ �������� �̵��ϰ� �÷��̾ �������� �����մϴ�.
			else if (move < 0 && m_FacingRight)
			{
				// ... flip the player.
				// �����ִ� ������ ������
				Flip();
			}
		}
		// If the player should jump...
		// ����Ű�� ��������
		if (m_Grounded && jump)
		{
			// Add a vertical force to the player.
			// vertical force�� player���� ����
			m_Grounded = false; //�߷� ������
			m_Rigidbody2D.AddForce(new Vector2(0f, m_JumpForce));
		}
	}


	private void Flip()
	{
		// Switch the way the player is labelled as facing.
		// �÷��̾ ���ֺ��� �ִ� �󺧷� ǥ�õǴ� ����� ��ȯ�մϴ�.-->���� ���� ��ȯ
		m_FacingRight = !m_FacingRight;

		// Multiply the player's x local scale by -1.
		// �÷��̾��� x ���� ô���� -1�� ���մϴ�.
		Vector3 theScale = transform.localScale;
		theScale.x *= -1;
		transform.localScale = theScale;
	}
}