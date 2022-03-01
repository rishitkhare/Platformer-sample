using System;
using System.Collections.Generic;
using UnityEngine;

public class RoomTransitionMovement : MonoBehaviour {
    // Events fired when the room starts changing and when it has
    // finished changing
    public event EventHandler OnRoomTransitionEnter;
    public event EventHandler OnRoomTransitionExit;

    public static RoomTransitionMovement RoomSystem;

    public List<Room> roomList;

    public Vector2 cameraDimensions = new Vector2(13.5f, 7.5f);

    public float smoothMovementSpeed = 8.0f;
    public float smoothMinSpeed = 0.02f;

    public Vector2 followPlayerAccel = new Vector2(0.013625f, 0.0078125f);

    public bool dontFollowPlayerOffScreen;

    private bool isTransitioningRoom;

    private int targetRoomIndex;

    private Vector3 transitionTarget;

    private int currentRoomIndex;

    float smoothMinSpeedSqr;


    GameObject player;

#if UNITY_EDITOR
    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.magenta;

        foreach (Room r in roomList) {
            Gizmos.DrawLine(r.GetTopLeftCorner(), r.GetTopRightCorner());
            Gizmos.DrawLine(r.GetTopRightCorner(), r.GetBottomRightCorner());
            Gizmos.DrawLine(r.GetBottomRightCorner(), r.GetBottomLeftCorner());
            Gizmos.DrawLine(r.GetBottomLeftCorner(), r.GetTopLeftCorner());
        }
    }
#endif

    void Awake() {
        if (RoomSystem = null) {
            throw new System.ArgumentException("doubled instantiation of RoomTransitionMovement (singleton)");
        }
        RoomSystem = this;
    }

    void Start() {
        player = GameObject.FindGameObjectWithTag("Player");
        isTransitioningRoom = false;

        smoothMinSpeedSqr = Mathf.Pow(smoothMinSpeed, 2);

        SetRoomIndex();
    }

    private void LateUpdate() {
        SetRoomIndex();

        // Based on player's position, decide whether to initiate transition to another room
        if (!isTransitioningRoom && RoomNeedsToSwitch()) {
            OnRoomTransitionEnter?.Invoke(this, EventArgs.Empty);
            transitionTarget = SetTargetPosition();
            isTransitioningRoom = true;
        }

        // If transition initiated, quickly shift screen to the next room
        // otherwise just follow the player within the current room
        if (isTransitioningRoom) {
            InterpolateToNextRoom();
            SnapCameraToRoom(smoothMinSpeedSqr);
        }
        else {
            FollowPlayer();
        }
    }

    private void SetRoomIndex() {
        int oldTargetIndex = targetRoomIndex;
        targetRoomIndex = RoomIndex(player.transform.position);

        if(dontFollowPlayerOffScreen && targetRoomIndex == -1) {
            targetRoomIndex = oldTargetIndex;
        }
    }

    public static int RoomIndex(Vector2 position) {
        for (int i = 0; i < RoomSystem.roomList.Count; i++) {
            if (RoomSystem.roomList[i].Contains(position)) {
                return i;
            }
        }

        return -1;
    }

    private void SnapCameraToRoom(float leeway) {
        //snaps camera to correct position if slightly off to prevent overshooting the movement

        if (Mathf.Abs(((Vector2)transform.position - (Vector2)transitionTarget).sqrMagnitude) < leeway) {
            transform.position = transitionTarget + new Vector3(0, 0, -10f);

            OnRoomTransitionExit?.Invoke(this, EventArgs.Empty);
            currentRoomIndex = targetRoomIndex;
            isTransitioningRoom = false;
        }
    }

    private Vector3 SetTargetPosition() {
        Vector3 output = player.transform.position;
        output.z = -10f;

        return CorrectPosition(output);
    }

    private Vector3 CorrectPosition(Vector3 original) {
        if (targetRoomIndex == -1) {
            return original;
        }

        Room targetRoom = roomList[targetRoomIndex];

        float xMinBound = targetRoom.Position.x - targetRoom.Size.x + cameraDimensions.x;
        float xMaxBound = targetRoom.Position.x + targetRoom.Size.x - cameraDimensions.x;
        float yMinBound = targetRoom.Position.y - targetRoom.Size.y + cameraDimensions.y;
        float yMaxBound = targetRoom.Position.y + targetRoom.Size.y - cameraDimensions.y;

        if (original.x < xMinBound) {
            original.x = xMinBound;
        }
        else if (original.x > xMaxBound) {
            original.x = xMaxBound;
        }

        if (original.y < yMinBound) {
            original.y = yMinBound;
        }
        else if (original.y > yMaxBound) {
            original.y = yMaxBound;
        }

        return original;
    }

    private void InterpolateToNextRoom() {
        // move camera if necessary
        Vector3 deltaTransform = new Vector3(0, 0);
        float Xdiff = transitionTarget.x - transform.position.x;
        float Ydiff = transitionTarget.y - transform.position.y;

        // X
        if (transform.position.x != transitionTarget.x) {
            deltaTransform += new Vector3((Xdiff) * Time.deltaTime, 0);
        }

        // Y
        if (transform.position.y != transitionTarget.y) {
            deltaTransform += new Vector3(0, (Ydiff) * Time.deltaTime);
        }

        deltaTransform *= smoothMovementSpeed;

        //prevents from going too slow
        if (deltaTransform.magnitude < smoothMinSpeed) {
            deltaTransform.Normalize();
            deltaTransform *= smoothMinSpeed;
        }

        transform.position += deltaTransform;
    }

    private void FollowPlayer() {
        Vector3 newPosition = new Vector3() {
            x = transform.position.x + (followPlayerAccel.x * (player.transform.position.x - transform.position.x)),
            y = transform.position.y + (followPlayerAccel.y * (player.transform.position.y - transform.position.y)),
            z = -10
        };

        newPosition = CorrectPosition(newPosition);

        transform.position = newPosition;
    }

    private bool RoomNeedsToSwitch() {
        return targetRoomIndex != currentRoomIndex;
    }
}