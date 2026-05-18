using System.Collections.Generic;
using UnityEngine;

public class PerspectiveCameraFollow : MonoBehaviour
{
    public Transform player;

    [Header("Perspective Position")]
    [Tooltip("Height above the game plane (Y-Achse)")]
    public float height = 8f;
    [Tooltip("Z-Abstand hinter der Spielebene")]
    public float depth = 10f;
    [Tooltip("Wie weit die Kamera hinter dem Schiff (in Flugrichtung) zurückbleibt")]
    public float behindDistance = 3f;

    [Header("Zoom (Field of View)")]
    public float minFOV = 40f;
    public float maxFOV = 70f;
    public float maxPlayerSpeed = 20f;
    public float zoomSmoothTime = 0.3f;

    [Header("Follow Settings")]
    public float followSmoothTime = 0.2f;
    public float rotationSmoothTime = 0.15f;

    [Header("Roll Settings")]
    [Tooltip("Maximale Roll-Neigung der Kamera in Grad")]
    public float maxRollAngle = 15f;
    [Tooltip("Wie stark die Drehrate des Schiffes die Kamera-Roll beeinflusst")]
    public float rollSensitivity = 0.05f;
    [Tooltip("Wie schnell die Roll-Neigung wieder auf 0 zurückgeht")]
    public float rollSmoothTime = 0.3f;

    [Header("Velocity Look-Ahead")]
    [Tooltip("Wie weit die Kamera der Flugrichtung vorausschaut")]
    public float lookAheadDistance = 4f;
    [Tooltip("Wie träge die Look-Ahead-Richtung folgt")]
    public float lookAheadSmoothTime = 0.4f;

    [Header("Planet Focus")]
    [Tooltip("Extra Abstandsfaktor beim Einrahmen von Schiff + Planet")]
    public float focusPadding = 1.3f;

    [Header("Checkpoint Planet Look-At")]
    public CheckpointManager checkpointManager;
    [Tooltip("Smooth-Zeit für die Checkpoint-Positionsverfolgung")]
    public float checkpointFocusSmoothTime = 0.6f;
    [Tooltip("Maximaler Blend-Anteil Richtung Checkpoint-Planet (0 = kein Einfluss, 1 = vollständig)")]
    [Range(0f, 1f)]
    public float checkpointFocusMaxBlend = 0.5f;
    [Tooltip("Zusätzlicher FOV-Anstieg wenn man vom Checkpoint wegfliegt (Winkel = 180°)")]
    public float checkpointFOVBoost = 15f;

    [Header("Gravity Alignment")]
    [Tooltip("Auslösedistanz als Prozent des Planeten-Radius über die Oberfläche hinaus (z.B. 10 → Auslösung bei 110% des Radius)")]
    [Range(0f, 200f)]
    public float gravityProximityPercent = 10f;
    [Tooltip("Smooth-Zeit für das Ein- und Ausblenden der Schwerkraft-Ausrichtung")]
    public float gravityAlignSmoothTime = 0.5f;
    [Tooltip("Gravity-Zonen im Editor anzeigen")]
    public bool showGravityGizmos = true;

    [SerializeField]
    private Camera cam;
    private Rigidbody playerRb;

    private float currentFOVVelocity;
    private Vector3 followVelocity;
    private Vector3 smoothedLookAhead;
    private Vector3 lookAheadVelocity;
    private Vector3 lastVelocityDir = Vector3.right;

    private float checkpointAngleFactor;
    private Vector3 smoothedCheckpointPos;
    private Vector3 checkpointPosVelocity;

    private float previousVelocityZ;
    private float currentRoll;
    private float rollDampVelocity;

    private readonly HashSet<Transform> activePlanets = new HashSet<Transform>();

    // Gravity alignment state
    private struct PlanetData { public Transform transform; public float radius; }
    private PlanetData[] allPlanets;
    private float gravityBlend;
    private float gravityBlendVelocity;
    private Vector3 smoothedPlanetUp = Vector3.up;

    void Start()
    {
        if (player != null)
        {
            playerRb = player.GetComponent<Rigidbody>();
            smoothedCheckpointPos = player.position;
            previousVelocityZ = Mathf.Atan2(-lastVelocityDir.x, lastVelocityDir.y) * Mathf.Rad2Deg;
        }

        var planetObjects = GameObject.FindGameObjectsWithTag("Planet");
        allPlanets = new PlanetData[planetObjects.Length];
        for (int i = 0; i < planetObjects.Length; i++)
        {
            var go = planetObjects[i];
            // Use ShapeSettings.planetRadius (the actual mesh radius) * world scale.
            // Fallback to localScale if no Planet component is found.
            float r = go.transform.localScale.x * 0.5f;
            var planetComp = go.GetComponent<Planet>();
            if (planetComp != null && planetComp.shapeSettings != null)
                r = planetComp.shapeSettings.planetRadius * go.transform.localScale.x;
            allPlanets[i] = new PlanetData { transform = go.transform, radius = r };
            Debug.Log($"[GravityAlign] Cached planet '{go.name}' radius={r:F1}");
        }
    }

    public void RegisterTrigger(Transform planet)   => activePlanets.Add(planet);
    public void UnregisterTrigger(Transform planet) => activePlanets.Remove(planet);

    Transform ClosestPlanet()
    {
        Transform closest = null;
        float minDist = float.MaxValue;
        foreach (Transform planet in activePlanets)
        {
            float dist = Vector3.Distance(player.position, planet.position);
            if (dist < minDist) { minDist = dist; closest = planet; }
        }
        return closest;
    }

    void LateUpdate()
    {
        if (player == null) return;

        // Velocity direction in XY plane
        if (playerRb != null)
        {
            Vector2 vel2D = new Vector2(playerRb.linearVelocity.x, playerRb.linearVelocity.y);
            if (vel2D.sqrMagnitude > 0.01f)
                lastVelocityDir = new Vector3(vel2D.normalized.x, vel2D.normalized.y, 0f);
        }

        // Smooth look-ahead (used for checkpoint FOV blend)
        Vector3 targetLookAhead = lastVelocityDir * lookAheadDistance;
        smoothedLookAhead = Vector3.SmoothDamp(smoothedLookAhead, targetLookAhead, ref lookAheadVelocity, lookAheadSmoothTime);

        // Checkpoint planet — influences FOV
        checkpointAngleFactor = 0f;
        if (checkpointManager != null && !checkpointManager.IsComplete)
        {
            int idx = checkpointManager.CurrentIndex;
            var cps = checkpointManager.checkpoints;
            if (idx >= 0 && idx < cps.Count && cps[idx] != null)
            {
                Vector3 cpPos = cps[idx].transform.position;
                smoothedCheckpointPos = Vector3.SmoothDamp(
                    smoothedCheckpointPos, cpPos, ref checkpointPosVelocity, checkpointFocusSmoothTime);

                Vector3 smoothedVelDir = smoothedLookAhead.sqrMagnitude > 0.001f
                    ? smoothedLookAhead.normalized
                    : lastVelocityDir;
                float dot = Vector3.Dot(smoothedVelDir, (cpPos - player.position).normalized);
                checkpointAngleFactor = (1f - dot) * 0.5f;
            }
        }

        // --- Gravity Alignment ---
        Transform nearestGravityPlanet = null;
        float closestFrac = float.MaxValue;
        if (allPlanets != null)
        {
            foreach (var pd in allPlanets)
            {
                float triggerDist = pd.radius * (1f + gravityProximityPercent / 100f);
                float dist = Vector3.Distance(player.position, pd.transform.position);
                float frac = dist / triggerDist;
                if (frac < 1f && frac < closestFrac)
                {
                    closestFrac = frac;
                    nearestGravityPlanet = pd.transform;
                }
            }
        }

        float targetGravityBlend = nearestGravityPlanet != null ? 1f : 0f;
        gravityBlend = Mathf.SmoothDamp(gravityBlend, targetGravityBlend, ref gravityBlendVelocity, gravityAlignSmoothTime);

        // Update smoothed planet-up only while a planet is in range
        if (nearestGravityPlanet != null)
        {
            Vector3 raw = player.position - nearestGravityPlanet.position;
            Vector3 planetUpXY = new Vector3(raw.x, raw.y, 0f).normalized;
            if (planetUpXY.sqrMagnitude > 0.001f)
                smoothedPlanetUp = Vector3.Slerp(smoothedPlanetUp, planetUpXY, Time.deltaTime / Mathf.Max(0.001f, gravityAlignSmoothTime));
        }

        // Effective up: blend world-up → planet-up
        Vector3 effectiveUp = Vector3.Slerp(Vector3.up, smoothedPlanetUp, gravityBlend);

        // --- Focus planet (existing trigger system) ---
        Transform focusPlanet = activePlanets.Count > 0 ? ClosestPlanet() : null;

        // --- Position ---
        Vector3 targetPosition;
        float targetFOV;

        if (focusPlanet != null)
        {
            Vector3 midpoint = (player.position + focusPlanet.position) * 0.5f;
            targetPosition = midpoint + effectiveUp * height + new Vector3(0f, 0f, -depth);

            float halfDist = Vector3.Distance(player.position, focusPlanet.position) * 0.5f;
            float planetRadius = focusPlanet.localScale.x * 0.5f;
            float requiredSize = (halfDist + planetRadius) * focusPadding;
            targetFOV = Mathf.Clamp(requiredSize * 4f, minFOV, maxFOV);
        }
        else
        {
            Vector3 behindOffset = -lastVelocityDir * behindDistance;
            targetPosition = player.position + behindOffset + effectiveUp * height + new Vector3(0f, 0f, -depth);

            float speed = playerRb != null ? playerRb.linearVelocity.magnitude : 0f;
            targetFOV = Mathf.Lerp(minFOV, maxFOV, speed / maxPlayerSpeed);
        }

        targetFOV = Mathf.Clamp(targetFOV + checkpointAngleFactor * checkpointFOVBoost, minFOV, maxFOV);
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref followVelocity, followSmoothTime);

        // --- Rotation ---
        Vector3 velDir = smoothedLookAhead.sqrMagnitude > 0.01f ? smoothedLookAhead.normalized : lastVelocityDir;
        float velocityZAngle = Mathf.Atan2(-velDir.x, velDir.y) * Mathf.Rad2Deg;

        float deltaZ = Mathf.DeltaAngle(previousVelocityZ, velocityZAngle);
        previousVelocityZ = velocityZAngle;

        float targetRoll = Mathf.Clamp(-deltaZ / Time.deltaTime * rollSensitivity, -maxRollAngle, maxRollAngle);
        currentRoll = Mathf.SmoothDamp(currentRoll, targetRoll, ref rollDampVelocity, rollSmoothTime);

        Vector3 toShip = player.position - transform.position;

        // Up-Vektor: blend von Geschwindigkeitsrichtung → Planeten-up.
        // LookRotation stellt sicher, dass die lokale Y-Achse (= Kamera-up) exakt
        // senkrecht zur ZX-Ebene steht und auf planet-up zeigt.
        Vector3 velocityUp = new Vector3(velDir.x, velDir.y, 0f);
        if (velocityUp.sqrMagnitude < 0.001f) velocityUp = Vector3.up;
        Vector3 blendedUp = Vector3.Slerp(velocityUp.normalized, smoothedPlanetUp, gravityBlend);

        Quaternion lookRot = Quaternion.LookRotation(toShip, blendedUp);

        // Roll (Kurvenneigung) in lokalem Kameraraum, near planet ausblenden
        float rollAmount = currentRoll * (1f - gravityBlend);
        Quaternion targetRotation = lookRot * Quaternion.AngleAxis(rollAmount, Vector3.forward);

        if (Quaternion.Dot(transform.rotation, targetRotation) < 0f)
            targetRotation = new Quaternion(-targetRotation.x, -targetRotation.y, -targetRotation.z, -targetRotation.w);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime / rotationSmoothTime);

        if (cam != null)
            cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, targetFOV, ref currentFOVVelocity, zoomSmoothTime);
    }

    public void AlignToPlayer()
    {
        if (player == null) return;
        transform.position = player.position + new Vector3(0f, height, -depth);
        previousVelocityZ = Mathf.Atan2(-lastVelocityDir.x, lastVelocityDir.y) * Mathf.Rad2Deg;
        float initPitch = Mathf.Atan2(height, depth) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(initPitch, 0f, previousVelocityZ + 90f);
    }

    void OnDrawGizmos()
    {
        if (!showGravityGizmos || allPlanets == null) return;
        foreach (var pd in allPlanets)
        {
            float triggerDist = pd.radius * (1f + gravityProximityPercent / 100f);
            Gizmos.color = new Color(0f, 1f, 0.4f, 0.15f);
            Gizmos.DrawSphere(pd.transform.position, triggerDist);
            Gizmos.color = new Color(0f, 1f, 0.4f, 0.6f);
            Gizmos.DrawWireSphere(pd.transform.position, triggerDist);
        }
    }
}
