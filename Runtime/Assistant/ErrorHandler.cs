using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OpenAI
{
    public class ErrorHandler
    {
        private const string OnRunFailedPhrase = "��, ����������, ����� ��� ������� ��������.";

        //private const AudioClip OnRunFailedAudioClip;
        private const string OnRequestFailedPhrase = "��������, �� � ������ ������ � �� ���� �������� �� ��� ������. ���������� �������.";

        //private const AudioClip OnRequestFailedAudioClip;

        public static Action<string> OnRunFailed { get; set; }
        public static Action<ErrorType, string> OnRequestFailed { get; set; }

        public static void SendRunFailed()
        {
            Debug.LogError("Run failed");

            OnRunFailed?.Invoke(OnRunFailedPhrase);
        }

        public static void SendRequestFailed(ErrorType errorType)
        {
            Debug.LogError("Request failed");
            OnRequestFailed?.Invoke(errorType, OnRequestFailedPhrase);
        }
    }

    public enum ErrorType
    {
        CreateThreadFailed,
        RetrieveThreadFailed,
        DeleteThreadFailed,
        CreateRunFailed,
        RetrieveRunFailed,
        CancelRunFailed,
        CreateMessageFailed,
        RetrieveMessageFailed,
        WhisperRequestFailed,
        ElevenLabsRequestFailed,
        CharGPTRequestFailed,
        Default
    }
}