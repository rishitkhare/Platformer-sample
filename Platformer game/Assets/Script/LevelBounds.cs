using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelBounds : MonoBehaviour {

    Transform player;
    Collider2D levelBoundsCollider;
    SimpleRigidbody playerRB;

    Vector2 startingPosition;

    // Start is called before the first frame update
    void Start() {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        playerRB = GameObject.FindGameObjectWithTag("Player").GetComponent<SimpleRigidbody>();
        levelBoundsCollider = gameObject.GetComponent<Collider2D>();
        startingPosition = player.position;
    }

    // Update is called once per frame
    void Update() {
        if(!levelBoundsCollider.OverlapPoint(player.transform.position)) {
            RespawnPlayer();
        }
    }

    private void RespawnPlayer() {
        playerRB.SetVelocity(Vector2.zero);
        player.position = startingPosition;
    }
}
