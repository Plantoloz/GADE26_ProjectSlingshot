using UnityEngine;

public class ThirdPersonCameraFollow : MonoBehaviour
{
    public Transform target;

    [Header("Position")]
    public float radius = 5f;
    public float height = 8f;
    public float smoothTime = 0.2f;

    private Rigidbody targetRb;
    private Vector3 lastVelocityDir = Vector3.up;
    private Vector3 smoothVelocity;

    void Start()
    {
        if (target != null)
            targetRb = target.GetComponent<Rigidbody>();
    }

    void LateUpdate()
    {
        if (target == null) return;

        if (targetRb != null)
        {
            Vector2 vel2D = new Vector2(targetRb.linearVelocity.x, targetRb.linearVelocity.y);
            if (vel2D.sqrMagnitude > 0.01f)
                lastVelocityDir = new Vector3(vel2D.normalized.x, vel2D.normalized.y, 0f);
        }

        // Hinter dem Schiff auf der XY-Ebene, versetzt auf der Z-Achse
        Vector3 targetPos = target.position - lastVelocityDir * radius + new Vector3(0f, 0f, -height);
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref smoothVelocity, smoothTime);

        Vector3 worldUp = new Vector3(-lastVelocityDir.y, lastVelocityDir.x, -90f);
        transform.rotation = Quaternion.LookRotation(target.position - transform.position, worldUp);
    }
}
