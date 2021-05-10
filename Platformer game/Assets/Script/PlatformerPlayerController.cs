using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SimpleRigidbody))]
public class PlatformerPlayerController : MonoBehaviour {

    public float gravity = 1f;
    public float jumpPower = 15f;
    public int jumpLeewayFrames = 4;
    public float terminalVelocity = -15f;
    public float horizontalAcceleration = 1.2f;
    public float horizontalDecelerationFactor = 0.85f;

    bool isFrozen;


    private float XInput = 0;
    int jumpLeewayFrameCounter;
    bool registerJump;

    bool wasGroundedBefore;

    SimpleRigidbody rb;
    Animator anim;

    // Start is called before the first frame update
    void Start() {
        SpriteRenderer sp = gameObject.GetComponent<SpriteRenderer>();
        sp.sprite = null;
        rb = gameObject.GetComponent<SimpleRigidbody>();
        rb.movesFromExternalScript = true;
        rb.calculateGrounded = true;
        rb.collisionsSetVelocityTo0 = true;
        jumpLeewayFrameCounter = jumpLeewayFrames;

        anim = transform.GetComponentInChildren<Animator>();
        RoomTransitionMovement.RoomSystem.OnRoomTransitionEnter += Freeze;
        RoomTransitionMovement.RoomSystem.OnRoomTransitionExit += UnFreeze;
    }

    void Freeze(object sender, EventArgs e) {
        Debug.Log("Freeze");
        rb.enabled = false;
        isFrozen = true;
    }

    void UnFreeze(object sender, EventArgs e) {
        Debug.Log("Unfreeze");
        rb.enabled = true;
        isFrozen = false;
    }

    // Update is called once per frame
    void Update() {
        XInput = Input.GetAxisRaw("Horizontal");
        CountJumpLeewayFrames();
    }

    // FixedUpdate is called once per physics update
    void FixedUpdate() {
        if(!isFrozen) {
            PlatformerYVelocitySet();
            PlatformerXVelocitySet();

            wasGroundedBefore = rb.GetGrounded();
            rb.Move();
        }
    }

    #region Y Velocity

    private void PlatformerYVelocitySet() {

        if (rb.GetGrounded()) {
            if(!wasGroundedBefore) {
                anim.Play("Squash");
                Debug.Log("Landed");
            }

            rb.SetVelocityY(0);

            if (registerJump) {
                Jump();

                ResetJumpLeewayFrames();
            }
        }
        else {
            rb.AddVelocityY(-gravity);
        }

        //terminal velocity
        if(rb.GetVelocity().y < -15f) {
            rb.SetVelocityY(-15f);
        }
    }

    private void Jump() {
        rb.SetVelocityY(jumpPower);
        anim.Play("Stretch");
    }

    private void CountJumpLeewayFrames() {
        if(jumpLeewayFrameCounter < jumpLeewayFrames)
        jumpLeewayFrameCounter++;

        if(Input.GetButtonDown("Jump")) {
            jumpLeewayFrameCounter = 0;
        }

        registerJump = jumpLeewayFrameCounter < jumpLeewayFrames;
    }

    private void ResetJumpLeewayFrames() {
        jumpLeewayFrameCounter = jumpLeewayFrames;
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
}
