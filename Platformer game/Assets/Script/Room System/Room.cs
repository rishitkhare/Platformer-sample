using UnityEngine;

[System.Serializable]
public class Room {
    public Vector2 Position;
    public Vector2 Size;

    public bool Contains(Vector2 point) {
        return
            point.x > Position.x - Size.x &&
            point.x < Position.x + Size.x &&
            point.y > Position.y - Size.y &&
            point.y < Position.y + Size.y;
    }

    public Vector3 GetBottomRightCorner() {
        return Position + new Vector2(Size.x, -Size.y);
    }

    public Vector3 GetBottomLeftCorner() {
        return Position - Size;
    }

    public Vector3 GetTopRightCorner() {
        return Position + Size;
    }

    public Vector3 GetTopLeftCorner() {
        return Position + new Vector2(-Size.x, Size.y);
    }
}