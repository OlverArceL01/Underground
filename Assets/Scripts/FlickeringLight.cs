
using UnityEngine;

public class FlickeringLight : MonoBehaviour
{
    public Light pointLight;

    public float minIntensity = 0.5f;
    public float maxIntensity = 2.0f;
    public float speed = 8f;

    private float targetIntensity;

    void Start()
    {
        if (pointLight == null)
            pointLight = GetComponent<Light>();

        targetIntensity = maxIntensity;
    }

    void Update()
    {
        float noise = Mathf.PerlinNoise(Time.time * speed, 0f);

        pointLight.intensity = Mathf.Lerp(
            minIntensity,
            maxIntensity,
            noise
        );
    }
}
