using System;
using System.IO;
using UnityEngine;
using System.Text;
using Newtonsoft.Json;
using System.Threading;
using System.Globalization;
using System.Threading.Tasks;
using UnityEngine.Networking;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;
using System.Linq;
using System.Text.RegularExpressions;

namespace OpenAI
{
    public class AssistantRequestHandler : OpenAIApi
    {
        public AssistantRequestHandler(string apiKey = null, string organization = null) : base(apiKey, organization)
        {
        }

        private async void DispatchRequestToAssistant<T>(string path, string method, Action<string> onRunCreate, Action<List<T>> onRunResponse, Action onRunComplete, Action<ErrorType> onRunFailed, byte[] payload = null) where T : IResponse
        {
            const int maxRetries = 3; // Максимальное количество попыток
            string run_id = "";
            int attempt = 0;
            bool shouldRetry = false;

            do
            {
                if (Application.internetReachability != NetworkReachability.NotReachable)
                {
                    using (var request = UnityWebRequest.Put(path, payload))
                    {
                        request.method = method;

                        request.SetHeaders(Configuration, ContentType.ApplicationJson);
                        request.SetRequestHeader("OpenAI-Beta", "assistants=v2");

                        var asyncOperation = request.SendWebRequest();

                        shouldRetry = false; // Сбрасываем флаг перед каждой попыткой

                        do
                        {
                            //if (token.IsCancellationRequested) return;
                            List<T> dataList = new List<T>();
                            List<string> lines = request.downloadHandler.text.Split('\n').Where(line => line != "").ToList();
                            if (lines.Count > 0)
                            {
                                //Debug.Log(request.downloadHandler.text);
                                //Debug.Log(request.error);
                                Debug.Log(request.result);
                                foreach (string line in lines)
                                {
                                    string value = line;
                                    if (line.Contains("event: "))
                                    {
                                        if (line.Contains(".failed"))
                                        {
                                            shouldRetry = true;
                                            onRunFailed?.Invoke(ErrorType.RunFailed);
                                            break;
                                        }
                                        continue;
                                    }
                                    value = line.Replace("data: ", "");

                                    if (value.Contains("[DONE]"))
                                    {
                                        onRunComplete?.Invoke();
                                        break;
                                    }

                                    //Debug.Log(value);
                                    if (value.Contains("run_") && run_id == "")
                                    {
                                        string pattern = "\"id\":\"(run_[^\"]+)\"";

                                        // Поиск соответствия регулярному выражению в строке JSON
                                        Match match = Regex.Match(value, pattern);

                                        // Извлечение id, если соответствие найдено
                                        if (match.Success)
                                        {
                                            run_id = match.Groups[1].Value;
                                            onRunCreate?.Invoke(run_id);
                                        }
                                    }
                                    //Debug.Log(value);
                                    if (!value.Contains("thread.message.delta")) continue;

                                    var data = JsonConvert.DeserializeObject<T>(value, jsonSerializerSettings);
                                    //Debug.Log(value);

                                    if (data?.Error != null)
                                    {
                                        ApiError error = data.Error;
                                        Debug.LogError($"Error Message: {error.Message}\nError Type: {error.Type}\n");
                                        attempt = 10;
                                    }
                                    else
                                    {
                                        dataList.Add(data);
                                    }
                                }
                                if (dataList.Count > 0)
                                    onRunResponse?.Invoke(dataList);

                                await Task.Yield();
                            }

                            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                            {
                                Debug.LogError("Server's connection error.");
                                //onRunFailed?.Invoke(ErrorType.RunFailed);
                                shouldRetry = true;
                                break;
                            }
                        }
                        while (!asyncOperation.isDone);
                        // Проверка состояния завершения операции после выполнения запроса

                        if (!shouldRetry)
                        {
                            onRunComplete?.Invoke();
                        }

                        attempt++;
                    }
                }
                else
                {
                    Debug.LogError("No internet connection.");
                    onRunFailed?.Invoke(ErrorType.RunFailed);
                    break;
                }
            }
            while (shouldRetry && attempt < maxRetries);

            if (shouldRetry && attempt >= maxRetries)
            {
                onRunFailed?.Invoke(ErrorType.RequestFailed);
            }
        }

        protected async Task<T> DispatchRequestToAssistant<T>(string path, string method, byte[] payload = null, Action<ErrorType> onRequestFailed = null) where T : IResponse, new()
        {
            const int maxRetries = 3; // Максимальное количество попыток
            int attempt = 0;
            bool shouldRetry = false;
            T data = new T();
            do
            {
                Debug.Log(path);
                using (var request = UnityWebRequest.Put(path, payload))
                {
                    request.method = method;
                    request.SetHeaders(Configuration, ContentType.ApplicationJson);

                    request.SetRequestHeader("OpenAI-Beta", "assistants=v2");

                    var asyncOperation = request.SendWebRequest();
                    shouldRetry = false;
                    while (!asyncOperation.isDone)
                    {
                        await Task.Yield();
                    }

                    try
                    {
                        Debug.Log(request.downloadHandler.text);

                        if (request.result == UnityWebRequest.Result.Success)
                        {
                            data = JsonConvert.DeserializeObject<T>(request.downloadHandler.text, jsonSerializerSettings);
                        }
                        else
                        {
                            shouldRetry = true;

                            Debug.Log(request.error);
                            Debug.Log(request.result);
                        }
                    }
                    catch (JsonReaderException ex)
                    {
                        // Обработка ошибки десериализации
                        Debug.Log($"Ошибка десериализации JSON: {ex.Message}");
                    }
                }

                if (data?.Error != null)
                {
                    ApiError error = data.Error;
                    Debug.LogError($"Error Message: {error.Message}\nError Type: {error.Type}\n");
                }

                if (data?.Warning != null)
                {
                    Debug.LogWarning(data.Warning);
                }
                attempt++;
            }
            while (shouldRetry && attempt < maxRetries);

            if (shouldRetry && attempt >= maxRetries)
            {
                Debug.Log("asdadas");
                onRequestFailed?.Invoke(ErrorType.RequestFailed);
            }
            return data;
        }

        public async Task<ThreadResponse> CreateThread(CreateThreadRequest request, Action<ErrorType> onRequestFailed = null)
        {
            var path = $"{BASE_PATH}/threads";
            var payload = CreatePayload(request);

            return await DispatchRequestToAssistant<ThreadResponse>(path, UnityWebRequest.kHttpVerbPOST, payload, onRequestFailed);
        }

        public async Task<ThreadResponse> RetrieveThread(string threadId, Action<ErrorType> onRequestFailed = null)
        {
            var path = $"{BASE_PATH}/threads/{threadId}";

            return await DispatchRequestToAssistant<ThreadResponse>(path, UnityWebRequest.kHttpVerbGET);
        }

        public async Task<DeleteResponse> DeleteThread(string threadId, Action<ErrorType> onRequestFailed = null)
        {
            var path = $"{BASE_PATH}/threads/{threadId}";
            return await DispatchRequestToAssistant<DeleteResponse>(path, UnityWebRequest.kHttpVerbDELETE);
        }

        public async Task<MessageResponse> CreateMessage(string threadId, CreateMessageRequest request, Action<ErrorType> onRequestFailed = null)
        {
            var path = $"{BASE_PATH}/threads/{threadId}/messages";
            var payload = CreatePayload(request);
            MessageResponse message = await DispatchRequestToAssistant<MessageResponse>(path, UnityWebRequest.kHttpVerbPOST, payload);

            return message;
        }

        public async Task<List<MessageResponse>> RetrieveMessages(string threadId, Action<ErrorType> onRequestFailed = null)
        {
            var path = $"{BASE_PATH}/threads/{threadId}/messages";

            var run = await DispatchRequestToAssistant<MessageListResponce>(path, UnityWebRequest.kHttpVerbGET);
            Debug.Log(run.Data.Count);
            return run.Data;
        }

        public async Task<RunResponse> CreateRun(string threadId, CreateRunRequest request, Action<ErrorType> onRequestFailed = null)
        {
            var path = $"{BASE_PATH}/threads/{threadId}/runs";
            var payload = CreatePayload(request);
            RunResponse run = await DispatchRequestToAssistant<RunResponse>(path, UnityWebRequest.kHttpVerbPOST);
            Debug.Log(run.ThreadId);
            return run;
        }

        public void CreateRunAsync(string threadId, CreateRunRequest request, Action<string> onCreate, Action<List<MessageDelta>> onResponse, Action onComplete, Action<ErrorType> onFailed)
        {
            var path = $"{BASE_PATH}/threads/{threadId}/runs";
            request.Stream = true;

            var payload = CreatePayload(request);

            DispatchRequestToAssistant(path, UnityWebRequest.kHttpVerbPOST, onCreate, onResponse, onComplete, onFailed, payload);
        }

        public async Task<RunResponse> CreateThreadAndRun(CreateThreadRunRequest request, Action<ErrorType> onRequestFailed = null)
        {
            var path = $"{BASE_PATH}/threads/runs";
            var payload = CreatePayload(request);
            RunResponse run = await DispatchRequestToAssistant<RunResponse>(path, UnityWebRequest.kHttpVerbPOST);
            Debug.Log(run.ThreadId);
            return run;
        }

        public async Task<RunResponse> RetrieveRun(string threadId, string runId, Action<ErrorType> onRequestFailed = null)
        {
            var path = $"{BASE_PATH}/threads/{threadId}/runs/{runId}";

            var run = await DispatchRequestToAssistant<RunResponse>(path, UnityWebRequest.kHttpVerbGET);

            return run;
        }

        public async Task<RunResponse> CancelRun(string threadId, string runId, Action<ErrorType> onRequestFailed = null)
        {
            var path = $"{BASE_PATH}/threads/{threadId}/runs/{runId}/cancel";

            var run = await DispatchRequestToAssistant<RunResponse>(path, UnityWebRequest.kHttpVerbGET);

            return run;
        }
    }
}