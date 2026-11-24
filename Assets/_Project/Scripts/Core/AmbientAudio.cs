using System;
using UnityEngine;

public class AmbientAudio : MonoBehaviour
{
    private const int Rate = 22050;

    [SerializeField] private float musicGain = 0.35f;
    [SerializeField] private float cricketGain = 0.5f;

    private AudioSource dayMusic;
    private AudioSource nightMusic;
    private AudioSource crickets;
    private AudioSource dangerMusic;
    private float danger;
    private float world = 1f;

    void Start()
    {
        dayMusic = CreateSource("DayMusic", BuildDayMusic());
        nightMusic = CreateSource("NightMusic", BuildNightMusic());
        crickets = CreateSource("Crickets", BuildCrickets());
        dangerMusic = CreateSource("DangerMusic", BuildDangerMusic());
        Apply(true);
    }

    void Update()
    {
        Apply(false);
    }

    private void Apply(bool immediate)
    {
        float dark = DayCycle.Darkness;
        float music = PauseMenu.MusicVolume * musicGain;
        float sfx = PauseMenu.SfxVolume * cricketGain;
        float speed = immediate ? 1f : Time.unscaledDeltaTime * 2f;

        danger = Mathf.MoveTowards(danger, DangerAlert.MusicActive ? 1f : 0f, Time.unscaledDeltaTime * 3f);
        float calm = 1f - danger;

        world = Mathf.MoveTowards(world, ARCameraToggle.Active ? 0f : 1f, Time.unscaledDeltaTime * 4f);
        music *= world;
        sfx *= world;
        SetPaused(dayMusic, world <= 0f);
        SetPaused(nightMusic, world <= 0f);
        SetPaused(crickets, world <= 0f);

        dayMusic.volume = Mathf.Lerp(dayMusic.volume, music * (1f - dark) * calm, speed);
        nightMusic.volume = Mathf.Lerp(nightMusic.volume, music * dark * calm, speed);
        dangerMusic.volume = music * 1.4f * danger;
        crickets.volume = Mathf.Lerp(crickets.volume, sfx * Mathf.Clamp01((dark - 0.3f) / 0.7f), speed);
    }

    private static void SetPaused(AudioSource source, bool paused)
    {
        if (source == null) return;
        if (paused) source.Pause();
        else source.UnPause();
    }

    private AudioSource CreateSource(string sourceName, AudioClip clip)
    {
        var child = new GameObject(sourceName);
        child.transform.SetParent(transform, false);
        var source = child.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.volume = 0f;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;
        source.Play();
        return source;
    }

    private static AudioClip Build(string clipName, float seconds, Action<float[]> fill)
    {
        var data = new float[(int)(Rate * seconds)];
        fill(data);

        float peak = 0f;
        foreach (float sample in data) peak = Mathf.Max(peak, Mathf.Abs(sample));
        if (peak > 0.9f)
            for (int i = 0; i < data.Length; i++) data[i] *= 0.9f / peak;

        var clip = AudioClip.Create(clipName, data.Length, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static void Tone(float[] data, float start, float length, float frequency, float amplitude, float attack, float decay)
    {
        int first = (int)(start * Rate);
        int count = (int)(length * Rate);

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)Rate;
            float envelope = Mathf.Min(t / attack, 1f) * Mathf.Exp(-t / decay);
            float release = Mathf.Clamp01((length - t) / 0.08f);
            float phase = 2f * Mathf.PI * frequency * t;
            float sample = Mathf.Sin(phase) + 0.25f * Mathf.Sin(2f * phase) + 0.08f * Mathf.Sin(3f * phase);
            data[(first + i) % data.Length] += sample * envelope * release * amplitude;
        }
    }

    private static float Note(int semitonesFromA4)
    {
        return 440f * Mathf.Pow(2f, semitonesFromA4 / 12f);
    }

    private static AudioClip BuildDayMusic()
    {
        const float beat = 0.6f;
        int[][] chords =
        {
            new[] { -9, -5, -2, 3 },
            new[] { 0, 3, 7, 12 },
            new[] { -4, 0, 3, 8 },
            new[] { -2, 2, 5, 10 }
        };
        int[] bass = { -21, -24, -28, -26 };
        int[] pattern = { 0, 1, 2, 3, 2, 1, 2, 1 };

        return Build("DayMusic", beat * 32f, data =>
        {
            for (int c = 0; c < chords.Length; c++)
            {
                float chordStart = c * beat * 8f;
                Tone(data, chordStart, beat * 4f, Note(bass[c]), 0.28f, 0.03f, 1.6f);
                Tone(data, chordStart + beat * 4f, beat * 4f, Note(bass[c]), 0.22f, 0.03f, 1.6f);

                for (int step = 0; step < 16; step++)
                {
                    int tone = chords[c][pattern[step % pattern.Length]];
                    Tone(data, chordStart + step * beat * 0.5f, 0.5f, Note(tone), 0.2f, 0.01f, 0.28f);
                }
            }
        });
    }

    private static AudioClip BuildNightMusic()
    {
        const float chordLength = 4.8f;
        int[][] chords =
        {
            new[] { -24, -17, -12, -9 },
            new[] { -28, -21, -16, -12 },
            new[] { -31, -24, -19, -16 },
            new[] { -26, -19, -14, -10 }
        };
        float[] bellTimes = { 1.2f, 6.0f, 8.4f, 13.2f, 15.6f, 18.0f };
        int[] bellNotes = { 12, 15, 19, 17, 14, 19 };

        return Build("NightMusic", chordLength * 4f, data =>
        {
            for (int c = 0; c < chords.Length; c++)
                foreach (int tone in chords[c])
                    Tone(data, c * chordLength, chordLength + 0.8f, Note(tone), 0.14f, 1.2f, 100f);

            for (int b = 0; b < bellTimes.Length; b++)
                Tone(data, bellTimes[b], 2.4f, Note(bellNotes[b]), 0.1f, 0.01f, 1.1f);
        });
    }

    private static AudioClip BuildDangerMusic()
    {
        const float step = 0.25f;

        return Build("DangerMusic", step * 16f, data =>
        {
            for (int i = 0; i < 16; i++)
            {
                int tone = i % 2 == 0 ? -36 : -30;
                Tone(data, i * step, step, Note(tone), 0.3f, 0.005f, 0.2f);
            }

            for (int beat = 0; beat < 8; beat++)
                Tone(data, beat * step * 2f, 0.25f, 58f, 0.55f, 0.004f, 0.09f);

            Tone(data, 0f, 0.9f, Note(22), 0.12f, 0.01f, 0.5f);
            Tone(data, step * 8f, 0.9f, Note(21), 0.12f, 0.01f, 0.5f);
        });
    }

    private static AudioClip BuildCrickets()
    {
        const float length = 4f;
        var random = new System.Random(11);

        return Build("Crickets", length, data =>
        {
            for (int cricket = 0; cricket < 6; cricket++)
            {
                float frequency = 3500f + cricket * 230f + (float)random.NextDouble() * 80f;
                float period = 0.55f + cricket * 0.08f;
                float offset = (float)random.NextDouble() * period;

                for (float groupStart = offset; groupStart < length + period; groupStart += period)
                {
                    for (int pulse = 0; pulse < 4; pulse++)
                    {
                        float pulseStart = groupStart + pulse * 0.055f;
                        int first = (int)(pulseStart * Rate);
                        int count = (int)(0.03f * Rate);

                        for (int i = 0; i < count; i++)
                        {
                            float t = i / (float)Rate;
                            float envelope = Mathf.Pow(Mathf.Sin(Mathf.PI * i / count), 2f);
                            data[(first + i) % data.Length] += Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.1f;
                        }
                    }
                }
            }
        });
    }
}
