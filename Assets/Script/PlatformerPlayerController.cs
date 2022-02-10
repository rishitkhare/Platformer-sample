using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;


// Stores the player's state
public enum PlayerState {
    platforming,
    dashing,
    frozen
}

[RequireComponent(typeof(SimpleRigidbody))]
public class PlatformerPlayerController : MonoBehaviour {

    // particle systems are serialized in the editor
    public ParticleSystem DustParticles;
    public ParticleSystem DashParticles;

    // when player holds jump, the gravity is decreased for a
    // short while
    public float jumpGravity = 0.6f; // scale of low-grav
    public float gravity = 1f; // regular grav
    public float jumpPower = 15f; // initial velocity of jump

    public int jumpHoldFrames = 12; // how long will low-grav be in effect (max)
    public int coyoteTimeFrames = 15; // how many frames after leaving ground can player jump
    public int jumpLeewayFrames = 4; // how many frames before leaving ground can player jump


    public float terminalVelocity = -15f; // maximum falling speed (must be negative)
    public float horizontalAcceleration = 1.2f; // is added to Xvel when moving
    public float horizontalDecelerationFactor = 0.85f; // multiplied to Xvel to slow down

    public float dashSpeed = 15f; // how fast the dash is
    public float dashTime = 0.2f; // how long
    public float postDashDecel = 0.3f; // once the dash is over,
                                       // what fraction of the dash
                                       // speed is retained

    // stores state
    private PlayerState playerState;

    // freezing (for when screen transitions)
    bool isFrozen;

    // stores data from key presses
    private float XInput = 0;

    // counters for frame data
    int jumpLeewayFrameCounter;
    int jumpHoldFrameCounter;
    int coyoteTimeCounter;

    // counts how long you can dash
    float dashTimer;

    // stores the state of grounded from the previous frame
    // (used to play particles that emit when the player touches down)
    bool wasGroundedBefore;

    // stores leeway jump status
    bool jumpWasPressed;
    // stores coyote time status
    bool recentlyTouchedPlatform;
    // key input status
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

    // Freezes the player
    void Freeze(object sender, EventArgs e) {
        rb.enabled = false;
        isFrozen = true;
    }

    // unfreezes the player
    void UnFreeze(object sender, EventArgs e) {
        rb.enabled = true;
        isFrozen = false;
    }

    // Update method only checks for input (for maximum input accuracy)
    // and also counts timing
    void Update() {
        if(!isFrozen) {
            switch(playerState) {
                case (PlayerState.platforming):
                    XInput = Input.GetAxisRaw("Horizontal");

                    CountJumpLeewayFrames();
                    CountJumpHoldFrames();

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
                    // various frame counting methods
                    DoCoyoteTimeFrames();

                    // physics
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

    private void DoCoyoteTimeFrames() {
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
