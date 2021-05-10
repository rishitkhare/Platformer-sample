using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class SimpleRigidbody : MonoBehaviour {
    private Vector2 velocity;
    private Vector2 direction = Vector2.down;
    private bool grounded = false;

    [HideInInspector]
    public bool movesFromExternalScript = false;
    public bool calculateGrounded = false;
    public bool collisionsSetVelocityTo0 = false;

    public LayerMask collidableLayer;

    BoxCollider2D myCollider;

    // Start is called before the first frame update
    void Start() {
        myCollider = gameObject.GetComponent<BoxCollider2D>();
    }

    void FixedUpdate() {
        if(!movesFromExternalScript) {
            Move();
        }
    }


    #region MUTATORS AND ACCESSORS

    public void SetDirection(Vector2 newDirection) {
        direction = newDirection;
    }

    public void SetDirectionX(float x) {
        direction.x = x;
    }
    public void SetDirectionY(float y) {
        direction.y = y;
    }

    public Vector2 GetDirection() { return direction; }

    public Vector2 GetTrueDirection() {
        if (velocity.magnitude != 0) {
            return velocity.normalized;
        }
        else {
            return direction;
        }
    }

    //Velocity mutator

    public void SetVelocity(Vector2 newVelocity) {
        velocity = newVelocity;
    }

    public void SetVelocityX(float x) {
        velocity = new Vector2(x, velocity.y);
    }

    public void SetVelocityY(float y) {
        velocity = new Vector2(velocity.x, y);
    }

    public void AddVelocityY(float deltaY) {
        velocity = new Vector2(velocity.x, velocity.y + deltaY);
    }

    public void AddVelocityX(float deltaX) {
        velocity = new Vector2(velocity.x + deltaX, velocity.y) ;
    }

    public Vector2 GetVelocity() {
        return velocity;
    }

    #endregion MUTATORS AND ACCESSORS

    public void Move() {
        Vector2 deltaPosition = Vector2.zero;
        if (velocity.x != 0) {
            deltaPosition.x = RaycastXCollision(velocity.x * Time.fixedDeltaTime);
        }

        //Move X
        if(Mathf.Abs(deltaPosition.x) > 0.02f) {
            transform.position += new Vector3(deltaPosition.x, 0, 0);
        }

        if (velocity.y != 0) {
            deltaPosition.y = RaycastYCollision(velocity.y * Time.fixedDeltaTime);
        }
        else if(calculateGrounded) {
            //to make sure grounded is set properly even when rb is not falling.
            RaycastYCollision(-0.05f);
        }

        //Move Y
        if (Mathf.Abs(deltaPosition.y) > 0.02f) {
            transform.position += new Vector3(0, deltaPosition.y);
        }
    }

    public bool GetGrounded() {
        return grounded;
    }

    private float RaycastXCollision(float deltaX) {
        //finds center of collider
        Vector2 colliderCenterPos = transform.position;
        colliderCenterPos.x += myCollider.offset.x;
        colliderCenterPos.y += myCollider.offset.y;

        //start ray on edge of collider (depending on right or left)
        Vector2 origin = colliderCenterPos;
        origin.x += FindEdgeMidpoint(deltaX, myCollider.size.x);

        //cast 3 rays for detection
        RaycastHit2D hit1 = Physics2D.Raycast(origin, Vector2.right, deltaX, collidableLayer);
        DebugRayHits(hit1, origin, Vector2.right, deltaX);

        origin.y += myCollider.size.y * 0.45f;
        RaycastHit2D hit2 = Physics2D.Raycast(origin, Vector2.right, deltaX, collidableLayer);
        DebugRayHits(hit2, origin, Vector2.right, deltaX);

        origin.y -= myCollider.size.y * 0.9f;
        RaycastHit2D hit3 = Physics2D.Raycast(origin, Vector2.right, deltaX, collidableLayer);
        DebugRayHits(hit3, origin, Vector2.right, deltaX);

        if (hit1 || hit2 || hit3) {
            if(collisionsSetVelocityTo0) {
                SetVelocityX(0);
            }
            return Mathf.Sign(deltaX) * FindMinExcluding0(hit1.distance, hit2.distance, hit3.distance);
        }
        else {
            return deltaX;
        }
    }

    private float FindEdgeMidpoint(float velocity, float value) {
        return Mathf.Sign(velocity) * 0.5f * value;
    }

    private float RaycastYCollision(float deltaY) {
        //finds center of collider
        Vector2 colliderCenterPos = transform.position;
        colliderCenterPos.x += myCollider.offset.x;
        colliderCenterPos.y += myCollider.offset.y;

        //start ray on edge of collider (depending on up or down)
        Vector2 origin = colliderCenterPos;
        origin.y += FindEdgeMidpoint(deltaY, myCollider.size.y);

        //cast 3 rays for detection
        RaycastHit2D hit1 = Physics2D.Raycast(origin, Vector2.up, deltaY, collidableLayer);
        DebugRayHits(hit1, origin, Vector2.up, deltaY);

        origin.x += myCollider.size.x * 0.45f;
        RaycastHit2D hit2 = Physics2D.Raycast(origin, Vector2.up, deltaY, collidableLayer);
        DebugRayHits(hit2, origin, Vector2.up, deltaY);

        origin.x -= myCollider.size.x * 0.9f;
        RaycastHit2D hit3 = Physics2D.Raycast(origin, Vector2.up, deltaY, collidableLayer);
        DebugRayHits(hit3, origin, Vector2.up, deltaY);

        if (hit1 || hit2 || hit3) {
            if(calculateGrounded) {
                if(Mathf.Sign(deltaY) == -1) {
                    grounded = true;
                }
                else {
                    grounded = false;
                }
            }

            if(collisionsSetVelocityTo0) {
                SetVelocityY(0f);
            }

            float trueDeltaY = Mathf.Sign(deltaY) * FindMinExcluding0(hit1.distance, hit2.distance, hit3.distance);
            return trueDeltaY;
        }
        else {
            grounded = false;
            return deltaY;
        }
    }

    private void DebugRayHits(RaycastHit2D hit, Vector2 origin, Vector2 direction, float magnitude) {
        if (hit) {
            Debug.DrawRay(origin, Mathf.Sign(magnitude) * hit.distance * direction, Color.green);
        }
        else {
            Debug.DrawRay(origin, magnitude * direction, Color.red);
        }
    }

    private float FindMinExcluding0(float f1, float f2, float f3) {
        float[] list = new float[3] {f1, f2, f3 };
        float answer = Mathf.Infinity;

        for(int i = 0; i < 3; i++) {
            if(list[i] != 0 && list[i] < answer) {
                answer = list[i];
            }
        }

        if(answer == Mathf.Infinity) {
            return 0;
        }
        else {
            return answer;
        }
    }
}
