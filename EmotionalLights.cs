using UnityEngine;

public class EmotionLightController_FearDramatic : MonoBehaviour
{
    [Header("Sources")]
    public MonoBehaviour emotionClientBehaviour;   // Drag EmotionClient here
    public Light mainLight;                        // Main scene light
    public Light spotLight;                        // Spotlight

    [Header("Main Light Settings")]
    public float normalIntensity = 1.0f;
    public float fearMainMin = 0.2f;     // lower than before for blackout feel
    public float fearMainMax = 2.2f;     // sudden bright flickers
    public float flickerSpeed = 30f;     // how chaotic the flicker is

    [Header("Spotlight Settings")]
    public float spotNormalIntensity = 0.0f;
    public float spotFearIntensity = 5.0f;     // brighter & scarier
    public Color spotFearColor = Color.red;
    public float swingSpeed = 40f;             // how fast spotlight swings
    public float swingAngle = 25f;             // swing range in degrees

    [Header("Timing")]
    public float fearHoldTime = 8.0f;  // hold longer
    public float smoothTime = 2.0f;

    private float _mainTarget;
    private float _spotTarget;
    private float _velMain;
    private float _velSpot;
    private float fearTimer = 0f;

    System.Reflection.FieldInfo _emotionField;

    void Awake()
    {
        if (emotionClientBehaviour != null)
        {
            _emotionField = emotionClientBehaviour.GetType().GetField(
                "emotion",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic
            );
        }
    }

    void Update()
    {
        string raw = ReadRawEmotion();
        string emo = ExtractDominantEmotion(raw);

        if (emo == "fear")
        {
            fearTimer = fearHoldTime;
        }
        else if (fearTimer > 0f)
        {
            fearTimer -= Time.deltaTime;
        }

        bool inFearMode = fearTimer > 0f;

        // MAIN LIGHT flicker effect
        if (mainLight)
        {
            if (inFearMode)
            {
                // chaotic flicker with Perlin noise
                float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
                _mainTarget = Mathf.Lerp(fearMainMin, fearMainMax, noise);
            }
            else
            {
                _mainTarget = normalIntensity;
            }
            mainLight.intensity = Mathf.SmoothDamp(mainLight.intensity, _mainTarget, ref _velMain, smoothTime * Time.deltaTime);
        }

        // SPOTLIGHT dramatic red + swinging
        if (spotLight)
        {
            if (inFearMode)
            {
                _spotTarget = spotFearIntensity;
                spotLight.color = spotFearColor;

                // rotate spotlight side-to-side
                float angle = Mathf.Sin(Time.time * swingSpeed * Mathf.Deg2Rad) * swingAngle;
                spotLight.transform.localRotation = Quaternion.Euler(spotLight.transform.localRotation.eulerAngles.x, angle, 0);
            }
            else
            {
                _spotTarget = spotNormalIntensity;
            }

            spotLight.intensity = Mathf.SmoothDamp(spotLight.intensity, _spotTarget, ref _velSpot, smoothTime * Time.deltaTime);
        }
    }

    string ReadRawEmotion()
    {
        if (emotionClientBehaviour == null || _emotionField == null) return null;
        object v = _emotionField.GetValue(emotionClientBehaviour);
        return v != null ? v.ToString() : null;
    }

    string ExtractDominantEmotion(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "neutral";
        string key = "\"dominant_emotion\"";
        int i = raw.IndexOf(key, System.StringComparison.OrdinalIgnoreCase);
        if (i < 0) return "neutral";
        int colon = raw.IndexOf(':', i);
        int q1 = raw.IndexOf('"', colon + 1);
        int q2 = raw.IndexOf('"', q1 + 1);
        if (colon < 0 || q1 < 0 || q2 < 0) return "neutral";
        return raw.Substring(q1 + 1, q2 - q1 - 1).ToLowerInvariant();
    }
}
