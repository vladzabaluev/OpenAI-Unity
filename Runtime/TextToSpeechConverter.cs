using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Serialization;
using System.Globalization;
using System;
using System.Net;
using System.Threading;

public class TextToSpeechConverter
{
    private const string SiteName = "https://api.elevenlabs.io/";
    private const string TextToSpeech = "/text-to-speech/";
    private const string API_KEY = "0f42dffebf0d1fbf04c2aaa4b5ec5fdb";

    private static readonly JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings()
    {
        NullValueHandling = NullValueHandling.Ignore,
        ContractResolver = new DefaultContractResolver()
        {
            NamingStrategy = new OpenAI.CustomNamingStrategy()
        },
        MissingMemberHandling = MissingMemberHandling.Ignore,
        Culture = CultureInfo.InvariantCulture
    };

    public static async Task<AudioClip> GetTextVoiceover(string voiceId, string textToVoice, VoiceSettings voiceSettings, string model = ElevenLabModels.English_v1, string version = "v1")
    {
        string voiceURL = SiteName + version + TextToSpeech + voiceId;

        var postData = new AudioRequest
        {
            ModelId = model,
            Text = textToVoice,
            VoiceSettings = voiceSettings
        };
        string jsonPayload = JsonConvert.SerializeObject(postData, jsonSerializerSettings);
        byte[] payloadBytes = System.Text.Encoding.UTF8.GetBytes(jsonPayload);

        UploadHandlerRaw uploadHandler = new UploadHandlerRaw(payloadBytes);
        DownloadHandlerAudioClip downloadHandler = new DownloadHandlerAudioClip(voiceURL, AudioType.MPEG);
        AudioClip audioClip;
        Debug.Log(jsonPayload);
        using (var request = new UnityWebRequest(voiceURL, "POST"))
        {
            request.uploadHandler = uploadHandler;
            request.downloadHandler = downloadHandler;

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("xi-api-key", API_KEY);
            request.SetRequestHeader("Accept", "audio/mpeg");
            var asyncOperation = request.SendWebRequest();

            while (!asyncOperation.isDone) await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(downloadHandler.text);
                Debug.Log(downloadHandler.data);

                Debug.LogError("Error downloading audio: " + request.error);
            }

            audioClip = downloadHandler.audioClip;
        }

        return audioClip;
    }
}