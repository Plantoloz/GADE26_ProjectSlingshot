using UnityEngine;

[RequireComponent(typeof(TrajectoryPredictor))]
[RequireComponent(typeof(AudioSource))]
public class CollisionWarningSystem : MonoBehaviour
{
    [Header("Beep Settings")]
    public AudioClip beepClip;
    public float minBeepInterval = 0.05f;
    public float maxBeepInterval = 0.5f;
    public float maxWarningTime = 5f; // Warning starts only at 5 seconds

    [Header("Impact Warning Settings")]
    public AudioClip impactWarningClip;
    public float impactWarningThreshold = 5f;
    public float impactWarningCooldown = 10f;

    private TrajectoryPredictor trajectory;
    private AudioSource audioSource;
    private float lastBeepTime;
    private float lastImpactWarningTime;

    void Awake()
    {
        trajectory = GetComponent<TrajectoryPredictor>();
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (trajectory.pathCollisionDetected)
        {
            float timeToImpact = trajectory.timeToImpact;

            // 1. Regular Beep (Interval based on time to impact)
            if (timeToImpact <= maxWarningTime)
            {
                float interval = Mathf.Lerp(minBeepInterval, maxBeepInterval, Mathf.Clamp01(timeToImpact / maxWarningTime));
                
                if (Time.time - lastBeepTime >= interval)
                {
                    PlayBeep();
                    lastBeepTime = Time.time;
                }
            }

            // 2. Impact Imminent Warning (5 seconds away)
            if (timeToImpact <= impactWarningThreshold)
            {
                if (Time.time - lastImpactWarningTime >= impactWarningCooldown)
                {
                    PlayImpactWarning();
                    lastImpactWarningTime = Time.time;
                }
            }
        }
    }

    void PlayBeep()
    {
        if (beepClip != null)
        {
            audioSource.PlayOneShot(beepClip);
        }
        else
        {
            // Fallback: simple beep sound if no clip is assigned
            // We can use a Debug.Log for now or try to generate a beep
            // Debug.Log("BEEP! Time to impact: " + trajectory.timeToImpact);
        }
    }

    void PlayImpactWarning()
    {
        if (impactWarningClip != null)
        {
            audioSource.PlayOneShot(impactWarningClip);
        }
        else
        {
            Debug.Log("<color=red>IMPACT IMMINENT! 5 SECONDS TO IMPACT!</color>");
        }
    }

    void OnGUI()
    {
        if (trajectory.pathCollisionDetected)
        {
            float timeToImpact = trajectory.timeToImpact;
            string message = $"COLLISION COURSE! Impact in: {timeToImpact:F1}s";
            
            GUIStyle style = new GUIStyle();
            style.fontSize = 24;
            style.normal.textColor = timeToImpact <= impactWarningThreshold ? Color.red : Color.yellow;
            style.alignment = TextAnchor.MiddleCenter;

            Rect rect = new Rect(0, 50, Screen.width, 50);
            GUI.Label(rect, message, style);

            if (timeToImpact <= impactWarningThreshold)
            {
                Rect warningRect = new Rect(0, 100, Screen.width, 50);
                style.fontSize = 32;
                GUI.Label(warningRect, "!!! WARNING: IMPACT IMMINENT !!!", style);
            }
        }
    }
}
