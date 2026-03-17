using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;
    // Set this to keep the camera pulled back (e.g., Z = -10)
    public Vector3 offset = new Vector3(0, 0, -10f); 

    void LateUpdate()
    {
        if (player != null)
        {
            transform.position = player.position + offset;
        }
    }
}