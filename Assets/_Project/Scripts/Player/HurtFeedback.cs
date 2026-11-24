using System.Collections.Generic;
using UnityEngine;

public class HurtFeedback : MonoBehaviour
{
    private const int Rate = 22050;
    private static readonly Color Red = new Color(1f, 0.12f, 0.12f, 1f);

    private readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>();
    private readonly List<Color> originals = new List<Color>();
    private AudioSource source;
    private AudioClip instant;
    private AudioClip poison;
    private AudioClip stun;
    private float hurtUntil;
    private float nextSound;
    private bool tinted;

    void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;

        instant = BuildInstant();
        poison = BuildPoison();
        stun = BuildStun();
    }

    void Start()
    {
        foreach (var renderer in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer.name == "Shadow" || renderer.name == "Glow") continue;
            renderers.Add(renderer);
            originals.Add(renderer.color);
        }
    }

    public void Hit(DamageType type)
    {
        hurtUntil = Time.unscaledTime + 0.7f;

        if (Time.unscaledTime < nextSound) return;

        AudioClip clip = type == DamageType.Poison ? poison : type == DamageType.Stun ? stun : instant;
        nextSound = Time.unscaledTime + (type == DamageType.Poison ? 0.7f : 0.25f);
        source.PlayOneShot(clip, Mathf.Clamp01(PauseMenu.SfxVolume * 1.2f));
    }

    void Update()
    {
        bool hurt = Time.unscaledTime < hurtUntil;

        if (!hurt)
        {
            if (tinted) Restore();
            return;
        }

        float blink = Mathf.Sin(Time.unscaledTime * 32f) > 0f ? 1f : 0.1f;
        for (int i = 0; i < renderers.Count; i++)
        {
            if (renderers[i] == null) continue;
            Color original = originals[i];
            Color tint = Color.Lerp(original, Red, blink);
            tint.a = original.a;
            renderers[i].color = tint;
        }

        tinted = true;
    }

    private void Restore()
    {
        for (int i = 0; i < renderers.Count; i++)
            if (renderers[i] != null) renderers[i].color = originals[i];
        tinted = false;
    }

    void OnDisable()
    {
        if (tinted) Restore();
    }

    private static AudioClip BuildInstant()
    {
        int count = (int)(Rate * 0.38f);
        var data = new float[count];
        var random = new System.Random(11);
        float phase = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)Rate;
            float thud = Mathf.Sin(2f * Mathf.PI * 95f * t) * Mathf.Exp(-t * 22f);
            float noise = ((float)random.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 40f) * 0.6f;
            float frequency = Mathf.Lerp(950f, 340f, Mathf.Clamp01(t / 0.3f));
            phase += 2f * Mathf.PI * frequency / Rate;
            float squawk = (Mathf.Sin(phase) + 0.5f * Mathf.Sin(phase * 2f)) * 0.35f * Mathf.Exp(-t * 7f);
            data[i] = Mathf.Clamp(thud + noise + squawk, -1f, 1f) * 0.9f;
        }
        return Make("HurtInstant", data);
    }

    private static AudioClip BuildPoison()
    {
        int count = (int)(Rate * 0.45f);
        var data = new float[count];
        float phase = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)Rate;
            float frequency = 220f + 160f * Mathf.Sin(2f * Mathf.PI * 11f * t);
            phase += 2f * Mathf.PI * frequency / Rate;
            float gate = Mathf.Abs(Mathf.Sin(2f * Mathf.PI * 7f * t));
            data[i] = Mathf.Sin(phase) * gate * Mathf.Exp(-t * 3.2f) * 0.8f;
        }
        return Make("HurtPoison", data);
    }

    private static AudioClip BuildStun()
    {
        int count = (int)(Rate * 0.5f);
        var data = new float[count];
        var random = new System.Random(23);
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)Rate;
            float buzz = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 70f * t)) * 0.45f;
            float zap = Mathf.Sin(2f * Mathf.PI * (1400f - 2200f * t) * t) * 0.35f;
            float noise = ((float)random.NextDouble() * 2f - 1f) * 0.25f;
            data[i] = (buzz + zap + noise) * Mathf.Exp(-t * 5f);
        }
        return Make("HurtStun", data);
    }

    private static AudioClip Make(string name, float[] data)
    {
        var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
