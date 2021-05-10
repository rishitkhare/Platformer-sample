using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimateParameters : MonoBehaviour {

    Animator anim;
    SimpleRigidbody playerRB;
    SpriteRenderer sp;
    readonly int groundedAnimHash = Animator.StringToHash("IsGrounded");
    readonly int XSpeedHash = Animator.StringToHash("XSpeed");

    // Start is called before the first frame update
    void Start() {
        anim = gameObject.GetComponent<Animator>();
        sp = gameObject.GetComponent<SpriteRenderer>();
        playerRB = transform.parent.GetComponent<SimpleRigidbody>();
    }

    // Update is called once per frame
    void Update() {
        anim.SetBool(groundedAnimHash, playerRB.GetGrounded());

        float xInput = Input.GetAxisRaw("Horizontal");
        anim.SetFloat(XSpeedHash, Mathf.Abs(xInput));

        if (xInput > 0.1f) {
            sp.flipX = false;
        }
        else if(xInput < -0.1f) {
            sp.flipX = true;
        }
    }
}
