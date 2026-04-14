using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RidgidNoiseFilter : INoiseFilter {

    NoiseSettings.RidgidNoiseSettings settings;
    Noise noise = new Noise();
    Vector3 seedOffset;

    public RidgidNoiseFilter(NoiseSettings.RidgidNoiseSettings settings)
    {
        this.settings = settings;
        System.Random prng = new System.Random(settings.seed);
        seedOffset = new Vector3(
            (float)(prng.NextDouble() * 2 - 1),
            (float)(prng.NextDouble() * 2 - 1),
            (float)(prng.NextDouble() * 2 - 1)
        ) * 1000f;
    }

    public float Evaluate(Vector3 point)
    {
        float noiseValue = 0;
        float frequency = settings.baseRoughness;
        float amplitude = 1;
        float weight = 1;

        for (int i = 0; i < settings.numLayers; i++)
        {
            float v = 1-Mathf.Abs(noise.Evaluate(point * frequency + settings.centre + seedOffset));
            v *= v;
            v *= weight;
            weight = Mathf.Clamp01(v * settings.weightMultiplier);

            noiseValue += v * amplitude;
            frequency *= settings.roughness;
            amplitude *= settings.persistence;
        }

        noiseValue = noiseValue - settings.minValue; 
        return noiseValue * settings.strength;
    }
}
