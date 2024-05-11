using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioDataTypes
{
}

[Serializable]
public class AudioRequest
{
    public string ModelId { get; set; }
    public string Text { get; set; }
    public VoiceSettings VoiceSettings { get; set; } = new VoiceSettings();
}

[System.Serializable]
public class VoiceSettings
{
    public float SimilarityBoost { get; set; } = 0.8f;
    public float Stability { get; set; } = 0.3f;
    public float Style { get; set; } = 0;
    public bool UseSpeakerBoost { get; set; } = false;

    public VoiceSettings(float similarityBoost = 0.5f, float stability = 0.5f, float style = 0, bool useSpeakerBoost = true)
    {
        this.SimilarityBoost = similarityBoost;
        this.Stability = stability;
        this.Style = style;

        this.UseSpeakerBoost = useSpeakerBoost;
    }

    public VoiceSettings()
    {
    }
}

public class AudioResponce
{
    public int OptimizeStreamingLatency { get; set; }
    public string OutputFormat { get; set; } = AudioResponceFormat.MP3128;
}

public class AudioResponceFormat
{
    public const string MP364 = "mp3_44100_64";
    public const string MP396 = "mp3_44100_96";
    public const string MP3128 = "mp3_44100_128";
    public const string MP3192 = "mp3_44100_192";
    public const string PCM16000 = "pcm_16000";
    public const string PCM22050 = "pcm_22050";
    public const string PCM44100 = "pcm_44100";
    public const string ULAW8000 = "ulaw_8000";
}

public class ElevenLabModels
{
    public const string English_v1 = "eleven_monolingual_v1";
    public const string Multilingual_v1 = "eleven_multilingual_v1";
    public const string Multilingual_v2 = "eleven_multilingual_v2";
    public const string Turbo_v2 = "eleven_turbo_v2";
}