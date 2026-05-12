using UnityEngine;

public class ThirdPersonCameraFollow : MonoBehaviour
{
    public Transform target;

    [Header("Position")]
    public float radius = 5f;
    public float height = 8f;
    public float smoothTime = 0.2f;

    private Vector3 smoothVelocity;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 shipFacing = new Vector3(target.up.x, target.up.y, 0f);
        if (shipFacing.sqrMagnitude < 0.001f) shipFacing = Vector3.up;
        else shipFacing.Normalize();

        Vector3 targetPos = target.position - shipFacing * radius + new Vector3(0f, 0f, -height);
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref smoothVelocity, smoothTime);

        transform.rotation = Quaternion.LookRotation(target.position - transform.position, shipFacing);
    }
}