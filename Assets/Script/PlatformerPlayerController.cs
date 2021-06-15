using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerState {
    platforming,
    dashing,
    frozen
}

[RequireComponent(typeof(SimpleRigidbody))]
public class PlatformerPlayerController : MonoBehaviour {

    public ParticleSystem DustParticles;
    public ParticleSystem DashParticles;

    public float jumpGravity = 0.6f;
    public float gravity = 1f;
    public float jumpPower = 15f;

    public int jumpHoldFrames = 12;
    public int coyoteTimeFrames = 15;
    public int jumpLeewayFrames = 4;


    public float terminalVelocity = -15f;
    public float horizontalAcceleration = 1.2f;
    public float horizontalDecelerationFactor = 0.85f;

    public float dashSpeed = 15f;
    public float dashTime = 0.2f;
    public float postDashDecel = 0.3f;

    private PlayerState playerState;

    bool isFrozen;

    private float XInput = 0;
    int jumpLeewayFrameCounter;
    int jumpHoldFrameCounter;
    int coyoteTimeCounter;

    float dashTimer;

    bool wasGroundedBefore;

    bool jumpWasPressed;
    bool recentlyTouchedPlatform;
    bool isHoldingJump;

    SimpleRigidbody rb;
    Animator anim;

    // Start is called before the first frame update
    void Start() {
        playerState = PlayerState.platforming;
        SpriteRenderer sp = gameObject.GetComponent<SpriteRenderer>();
        sp.sprite = null;
        rb = gameObject.GetComponent<SimpleRigidbody>();
        rb.movesFromExternalScript = true;
        rb.calculateGrounded = true;
        rb.collisionsSetVelocityTo0 = true;
        jumpLeewayFrameCounter = jumpLeewayFrames;
        jumpHoldFrameCounter = jumpHoldFrames;
        coyoteTimeCounter = coyoteTimeFrames;

        anim = transform.GetComponentInChildren<Animator>();
        RoomTransitionMovement.RoomSystem.OnRoomTransitionEnter += Freeze;
        RoomTransitionMovement.RoomSystem.OnRoomTransitionExit += UnFreeze;
    }

    void Freeze(object sender, EventArgs e) {
        rb.enabled = false;
        isFrozen = true;
    }

    void UnFreeze(object sender, EventArgs e) {
        rb.enabled = true;
        isFrozen = false;
    }

    // Update is called once per frame
    void Update() {
        if(!isFrozen) {
            switch(playerState) {
                case (PlayerState.platforming):
                    XInput = Input.GetAxisRaw("Horizontal");
                    CountJumpLeewayFrames();
                    CheckForDash();

                    break;

                case (PlayerState.dashing):
                    dashTimer -= Time.deltaTime;
                    if (dashTimer < 0f) {
                        playerState = PlayerState.platforming;
                        rb.SetVelocity(rb.GetVelocity() * postDashDecel);
                    }

                    break;

            }
        }
    }

    // FixedUpdate is called once per physics update
    void FixedUpdate() {
        if(!isFrozen) {
            switch(playerState) {
                case (PlayerState.platforming):
                    CountJumpHoldFrames();
                    DoCoyoteTime();

                    PlatformerYVelocitySet();
                    PlatformerXVelocitySet();

                    wasGroundedBefore = rb.GetGrounded();
                    break;
            }

            rb.Move();
        }
    }

    #region Y Velocity

    private void PlatformerYVelocitySet() {

        if (rb.GetGrounded()) {
            if(!wasGroundedBefore) {
                anim.Play("Squash");

                if(!DustParticles.isPlaying) {
                    DustParticles.Play();
                }
            }

            rb.SetVelocityY(0);
        }
        else {
            if(isHoldingJump) {
                rb.AddVelocityY(-jumpGravity);
            }
            else {
                rb.AddVelocityY(-gravity);
            }
        }

        if (jumpWasPressed && recentlyTouchedPlatform) {
            Jump();
            ResetJumpHoldFrames();
            ResetJumpLeewayFrames();
        }

        //terminal velocity
        if (rb.GetVelocity().y < terminalVelocity) {
            rb.SetVelocityY(terminalVelocity);
        }
    }

    private void Jump() {
        rb.SetVelocityY(jumpPower);
        anim.Play("Stretch");

        if(!DustParticles.isPlaying) {
            DustParticles.Play();
        }
    }

    private void CountJumpLeewayFrames() {
        if(jumpLeewayFrameCounter < jumpLeewayFrames) {
            jumpLeewayFrameCounter++;
        }

        if(Input.GetButtonDown("Jump")) {
            jumpLeewayFrameCounter = 0;
        }

        jumpWasPressed = jumpLeewayFrameCounter < jumpLeewayFrames;
    }

    private void ResetJumpLeewayFrames() {
        jumpLeewayFrameCounter = jumpLeewayFrames;
    }


    private void CountJumpHoldFrames() {
        if(Input.GetButton("Jump") && jumpHoldFrameCounter < jumpHoldFrames) {
            jumpHoldFrameCounter++;
        }
        else if(!Input.GetButton("Jump")) {
            jumpHoldFrameCounter = jumpHoldFrames;
        }

        isHoldingJump = jumpHoldFrameCounter < jumpHoldFrames;
    }

    private void ResetJumpHoldFrames() {
        jumpHoldFrameCounter = 0;
    }

    private void DoCoyoteTime() {
        if(rb.GetGrounded()) {
            coyoteTimeCounter = 0;
        }
        else if(!rb.GetGrounded() && coyoteTimeCounter < coyoteTimeFrames) {
            coyoteTimeCounter++;
        }

        recentlyTouchedPlatform = coyoteTimeCounter < coyoteTimeFrames;
    }

    #endregion Y Velocity


    #region X Velocity

    private void PlatformerXVelocitySet() {
        rb.AddVelocityX(XInput * horizontalAcceleration);

        DecelerateX();
    }

    private void DecelerateX() {
        float initialXvel = rb.GetVelocity().x;

        if (Mathf.Abs(0 - initialXvel) < 0.08f) {
            rb.SetVelocityX(0);
        }
        else {
            rb.SetVelocityX(horizontalDecelerationFactor * initialXvel);
        }
    }

    #endregion X Velocity

    #region input checking

    private void CheckForDash() {
        if(Input.GetButtonDown("Dash")) {
            Vector2 input = Vector2.zero;
            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");


            playerState = PlayerState.dashing;
            rb.SetVelocity(input.normalized * dashSpeed);
            dashTimer = dashTime;

            anim.Play("Dash");
            DashParticles.Play();
        }
    }

    #endregion input checking
}
