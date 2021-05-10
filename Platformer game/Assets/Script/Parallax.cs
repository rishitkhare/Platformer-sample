using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Parallax : MonoBehaviour {
    public Transform gameCamera;
    public float parallaxFactor = 0.5f;

    private Vector3 originalPosition;

    // Start is called before the first frame update
    void Start() {
        originalPosition = transform.position - gameCamera.position;
    }

    // Update is called once per frame
    void Update() {
        transform.position = originalPosition + (gameCamera.position * parallaxFactor);
    }
}
