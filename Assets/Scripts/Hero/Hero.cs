﻿using UnityEngine;
using System.Collections;
using System;

public abstract class Hero : MonoBehaviour {

	[SerializeField] private float maxWalkingSpeed = 5f;
	[SerializeField] private float walkMotorTorque = 40f;
	[SerializeField] private float horizontalFlyingForce = 0.5f;
	[SerializeField] private float jumpHeight = 1f;
	[SerializeField] private LayerMask whatIsGround;
	[SerializeField] private LayerMask heroPlatformMask;
	[SerializeField] private LayerMask mapInteractiveObjectsMask;
	[SerializeField] private Collider2D headCollider = null;
	[SerializeField] private Collider2D heroPlatform = null;
	[SerializeField] private Collider2D bodyCollider = null;
	[SerializeField] private HingeJoint2D walkMotor = null;


	private float motorMaxAngularSpeed = 0f;
	private bool m_FacingRight = true;
	private Rigidbody2D rigidBody2D;
	private double jumpForce;
	private Animator animator;


	protected bool m_isActive = false;
	public bool IsActive{
		get { return m_isActive; }
		protected set { m_isActive = value; }
	}

	private bool m_onAir = false;
	public bool OnAir{
		get { return m_onAir; }
	}


	protected virtual void Awake(){
		rigidBody2D = GetComponent<Rigidbody2D> ();

		//get the animator
		animator = GetComponentInChildren<Animator> ();

		CalculateJumpForce ();
		CalculateWalkingMotorAngularSpeed ();
	}

	void FixedUpdate(){

	}

	public void Move(float horizontalMove, bool crouch, bool jump){

		//TODO better method to check if grounded
		bool grounded = Physics2D.OverlapCircle (transform.position, 0.2f, whatIsGround.value | heroPlatformMask.value | mapInteractiveObjectsMask.value);

		//WALK HORIZONTALY
		if (grounded) {
			ChangeMotorSpeed(motorMaxAngularSpeed * horizontalMove);

		} else {
			if (rigidBody2D.velocity.x * Mathf.Sign(horizontalMove) < maxWalkingSpeed)
				rigidBody2D.AddForce (new Vector2 (horizontalMove * horizontalFlyingForce, 0), ForceMode2D.Impulse);
		}


		//SET WALKING ANIMATION
		animator.SetBool ("walk", Mathf.Abs (horizontalMove) > 0);
		if (horizontalMove != 0)
			animator.speed = Mathf.Abs (horizontalMove);
		else
			animator.speed = 1;

		//CROUCH
		if (crouch) {
			//headCollider.isTrigger = crouch;
			heroPlatform.offset = bodyCollider.offset +  new Vector2(0, bodyCollider.bounds.extents.y - heroPlatform.bounds.extents.y);
			//Crouch Animation
			animator.SetBool ("crouch", true);
		} else if (!crouch && !Physics2D.OverlapArea (headCollider.bounds.min, headCollider.bounds.max, whatIsGround.value)) {
			heroPlatform.offset = bodyCollider.offset +  new Vector2(0, bodyCollider.bounds.extents.y + heroPlatform.bounds.extents.y);
			//Crouch Animatio
			animator.SetBool ("crouch", false);
		}

		//JUMP, IF GROUDED OR ON OTHER HERO PLATFORM
		if (jump && grounded) {
			foreach (Rigidbody2D rg2d in transform.GetComponentsInChildren<Rigidbody2D>())
				rg2d.velocity = new Vector2(rigidBody2D.velocity.x, 0);
			rigidBody2D.AddForce (new Vector2 (0f, (float)jumpForce), ForceMode2D.Impulse);
		}

		if (grounded)
			m_onAir = false;
		else
			m_onAir = true;

		if ((horizontalMove > 0 && !m_FacingRight) || (horizontalMove < 0 && m_FacingRight)) {
			//Flip the animation
			// Switch the way the player is labelled as facing.
			m_FacingRight = !m_FacingRight;
		
			// Multiply the player's x local scale by -1.
			Transform rendererTransform = GetComponentInChildren<SpriteRenderer>().transform;
			Vector3 theScale = rendererTransform.localScale;
			theScale.x *= -1;
			rendererTransform.localScale = theScale;
		}

	}

	private void CalculateJumpForce(){
		//Impulse to Jump that height
		//more info look at http://hyperphysics.phy-astr.gsu.edu/hbase/impulse.html and reverse http://hyperphysics.phy-astr.gsu.edu/hbase/flobj.html#c2
		double totalMass = 0;
		//get the total mass from the hero
		foreach (Rigidbody2D rg2d in transform.GetComponentsInChildren<Rigidbody2D>())
			totalMass += rg2d.mass;

		//Force using Math instead of Mathf, to use double instead of float. (no big result changes)
		jumpForce = ((double)totalMass) * ((double)Math.Sqrt ((double)(2D * ((double)jumpHeight) * ((double)rigidBody2D.gravityScale) * ((double)Math.Abs (Physics2D.gravity.y)))));
		//Add a epsilon to compensate for an unknown error
		jumpForce *= 1.03;
	}

	private void CalculateWalkingMotorAngularSpeed(){
		float footRadius = walkMotor.GetComponent<CircleCollider2D>().radius;

		motorMaxAngularSpeed = Mathf.Rad2Deg * maxWalkingSpeed/footRadius;

	}

	public void ChangeHero(){
		m_isActive = !m_isActive;
	}

	public void StopWalk(){
		animator.SetBool ("walk", false);
		ChangeMotorSpeed (0f);
	}

	private void ChangeMotorSpeed(float speed){
		JointMotor2D tMotor = walkMotor.motor; 
		tMotor.motorSpeed = speed;
		tMotor.maxMotorTorque = walkMotorTorque;
		walkMotor.motor = tMotor;
	}

    private void DoAction()
    {
        Renderer r = GetComponentInChildren<Renderer>();
        Vector2 a = new Vector2(transform.position.x - r.bounds.extents.x, transform.position.y - r.bounds.extents.x);
        Vector2 b = new Vector2(transform.position.x + r.bounds.extents.x, transform.position.y + r.bounds.extents.x);
        Collider2D coll = Physics2D.OverlapArea(a, b, 1 << 11);

        if (coll != null)
        {
            switch (coll.tag)
            {
                case "Lever":
                    {

                        coll.SendMessage("ChangeState");
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }
    }

	public float JumpHeight {
		get {
			return this.jumpHeight;
		}
		set {
			jumpHeight = value;
			CalculateJumpForce ();
		}
	}

	public float WalkMotorTorque {
		get {
			return this.walkMotorTorque;
		}
		set {
			walkMotorTorque = value;
			CalculateWalkingMotorAngularSpeed();
		}
	}

	public float MaxWalkingSpeed {
		get {
			return this.maxWalkingSpeed;
		}
		set {
			maxWalkingSpeed = value;
			CalculateWalkingMotorAngularSpeed();
		}
	}

	public float HorizontalFlyingForce {
		get {
			return this.horizontalFlyingForce;
		}
		set {
			horizontalFlyingForce = value;
		}
	}

	public float GravityScale{
		get {
			return rigidBody2D.gravityScale;
		}

		set {
			foreach (Rigidbody2D rg2d in transform.GetComponentsInChildren<Rigidbody2D>())
				rg2d.gravityScale = value;

			CalculateJumpForce();
		}

	}


}
