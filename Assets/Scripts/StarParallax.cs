using UnityEngine;

public class StarParallax : MonoBehaviour
{
    [System.Serializable]
    public class Layer
    {
        public Transform transform;
        [Range(0f, 1f)] public float parallaxFactor = 0.1f;
    }

    public Transform ship;
    public Layer[] layers;

    private Vector3 lastShipPos;

    void Start()
    {
        if (ship != null)
            lastShipPos = ship.position;
    }

    void LateUpdate()
    {
        if (ship == null) return;

        Vector3 delta = ship.position - lastShipPos;

        foreach (var layer in layers)
        {
            if (layer.transform == null) continue;
            layer.transform.position += new Vector3(
                delta.x * layer.parallaxFactor,
                delta.y * layer.parallaxFactor,
                0f
            );
        }

        lastShipPos = ship.position;
    }
}
