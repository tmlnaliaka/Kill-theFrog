using System;
using UnityEngine;

/// <summary>
/// Fully self-contained procedural audio generation using AudioClip.Create.
/// Generates Kill, Miss, Ribbit, Game Over, and Start sounds on demand without external audio assets.
/// </summary>
public static class SoundGenerator
{
    private const int SampleRate = 44100;

    private static AudioClip _killClip;
    private static AudioClip _missClip;
    private static AudioClip _ribbitClip;
    private static AudioClip _gameOverClip;
    private static AudioClip _startClip;

    public static AudioClip KillClip => _killClip ?? (_killClip = CreateKillClip());
    public static AudioClip MissClip => _missClip ?? (_missClip = CreateMissClip());
    public static AudioClip RibbitClip => _ribbitClip ?? (_ribbitClip = CreateRibbitClip());
    public static AudioClip GameOverClip => _gameOverClip ?? (_gameOverClip = CreateGameOverClip());
    public static AudioClip StartClip => _startClip ?? (_startClip = CreateStartClip());

    /// <summary>
    /// Upward chirping pop/splat sound for frog kill.
    /// </summary>
    public static AudioClip CreateKillClip()
    {
        float duration = 0.18f;
        int sampleCount = Mathf.CeilToInt(duration * SampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            float progress = t / duration;

            // Pitch rises sharply from 300Hz to 1200Hz
            float freq = Mathf.Lerp(320f, 1250f, Mathf.Pow(progress, 0.7f));
            float phase = 2f * Mathf.PI * freq * t;

            // Splat pop with subtle harmonic
            float wave = Mathf.Sin(phase) * 0.75f + Mathf.Sin(phase * 2f) * 0.25f;

            // Rapid exponential decay envelope with punchy attack
            float attack = Mathf.Clamp01(t / 0.015f);
            float decay = Mathf.Exp(-progress * 6.5f);
            samples[i] = wave * attack * decay * 0.85f;
        }

        AudioClip clip = AudioClip.Create("SFX_Kill", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// Low pitch damp thud for missed clicks on water/ground.
    /// </summary>
    public static AudioClip CreateMissClip()
    {
        float duration = 0.24f;
        int sampleCount = Mathf.CeilToInt(duration * SampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            float progress = t / duration;

            // Pitch drops from 120Hz down to 40Hz
            float freq = Mathf.Lerp(120f, 40f, progress);
            float phase = 2f * Mathf.PI * freq * t;

            // Low thump plus tiny soft noise for water splash feel
            float sine = Mathf.Sin(phase);
            float noise = (UnityEngine.Random.value * 2f - 1f) * 0.15f * Mathf.Exp(-progress * 12f);

            float attack = Mathf.Clamp01(t / 0.008f);
            float decay = Mathf.Exp(-progress * 9f);
            samples[i] = (sine * 0.85f + noise) * attack * decay * 0.75f;
        }

        AudioClip clip = AudioClip.Create("SFX_Miss", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// Realistic frequency-modulated frog croak / ribbit on spawn.
    /// </summary>
    public static AudioClip CreateRibbitClip()
    {
        float duration = 0.42f;
        int sampleCount = Mathf.CeilToInt(duration * SampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;

            // Two distinct pulse syllables ("Rib-bit")
            float pulseEnvelope = 0f;
            float pulseTime = t;
            if (t < 0.18f)
            {
                float p = t / 0.18f;
                pulseEnvelope = Mathf.Sin(p * Mathf.PI) * 0.8f;
            }
            else if (t > 0.22f && t < 0.40f)
            {
                float p = (t - 0.22f) / 0.18f;
                pulseEnvelope = Mathf.Sin(p * Mathf.PI) * 1.0f;
                pulseTime = t - 0.04f;
            }

            // Guttural modulation (harsh larynx flutter ~35Hz)
            float mod = Mathf.Sin(2f * Mathf.PI * 38f * pulseTime);
            float carrierFreq = 195f + (mod * 65f);
            float carrier = Mathf.Sin(2f * Mathf.PI * carrierFreq * t);

            // Add second formant resonance
            float formant = Mathf.Sin(2f * Mathf.PI * (carrierFreq * 2.3f) * t) * 0.35f;

            samples[i] = (carrier + formant) * pulseEnvelope * 0.7f;
        }

        AudioClip clip = AudioClip.Create("SFX_Ribbit", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// Descending 4-note minor melody on Game Over.
    /// </summary>
    public static AudioClip CreateGameOverClip()
    {
        float noteDuration = 0.32f;
        float[] notes = { 440.00f, 392.00f, 349.23f, 293.66f }; // A4, G4, F4, D4
        float totalDuration = noteDuration * notes.Length + 0.3f;
        int sampleCount = Mathf.CeilToInt(totalDuration * SampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            int noteIndex = Mathf.Clamp(Mathf.FloorToInt(t / noteDuration), 0, notes.Length - 1);
            float noteTime = t - (noteIndex * noteDuration);

            float freq = notes[noteIndex];
            float phase = 2f * Mathf.PI * freq * t;
            float tone = Mathf.Sin(phase) * 0.7f + Mathf.Sin(phase * 2f) * 0.2f + Mathf.Sin(phase * 3f) * 0.1f;

            float env = Mathf.Exp(-noteTime * 4.5f) * Mathf.Clamp01(noteTime / 0.02f);
            samples[i] = tone * env * 0.65f;
        }

        AudioClip clip = AudioClip.Create("SFX_GameOver", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// Ascending celebratory chime on Start Game.
    /// </summary>
    public static AudioClip CreateStartClip()
    {
        float noteDuration = 0.14f;
        float[] notes = { 261.63f, 329.63f, 392.00f, 523.25f, 659.25f }; // C4, E4, G4, C5, E5
        float totalDuration = noteDuration * notes.Length + 0.45f;
        int sampleCount = Mathf.CeilToInt(totalDuration * SampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            int noteIndex = Mathf.Clamp(Mathf.FloorToInt(t / noteDuration), 0, notes.Length - 1);
            float noteTime = t - (noteIndex * noteDuration);

            float freq = notes[noteIndex];
            float phase = 2f * Mathf.PI * freq * t;
            float chime = Mathf.Sin(phase) * 0.75f + Mathf.Sin(phase * 2.02f) * 0.2f + Mathf.Sin(phase * 4f) * 0.05f;

            float env = Mathf.Exp(-noteTime * 3.8f) * Mathf.Clamp01(noteTime / 0.01f);
            samples[i] = chime * env * 0.7f;
        }

        AudioClip clip = AudioClip.Create("SFX_Start", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    public static void Play(AudioSource source, AudioClip clip, float volume = 1f)
    {
        if (source != null && clip != null)
        {
            source.PlayOneShot(clip, volume);
        }
    }
}
