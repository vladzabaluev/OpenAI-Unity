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
using UnityEditor.PackageManager;

namespace OpenAI
{
    public class AssistantRequestHandler : OpenAIApi
    {
        public AssistantRequestHandler(string apiKey = null, string organization = null) : base(apiKey, organization)
        {
        }

        private async void DispatchRequestToAssistant<T>(string path, string method, Action<string> onRunCreate, Action<List<T>> onRunResponse, Action onRunComplete, Action<ErrorType> onRunFailed, CancellationTokenSource token, byte[] payload = null) where T : IResponse
        {
            const int maxRetries = 3; // Максимальное количество попыток
            string run_id = "";
            int attempt = 0;
            bool shouldRetry = false;

            do
            {
                //if (Application.internetReachability != NetworkReachability.NotReachable)
                //{
                using (var request = UnityWebRequest.Put(path, payload))
                {
                    Debug.Log("op 1");
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

                            foreach (string line in lines)
                            {
                                string value = line;
                                if (line.Contains("event: "))
                                {
                                    //value += ".failed";
                                    if (line.Contains(".failed") || lines.Count > 60)
                                    {
                                        if (attempt == 0)
                                        {
                                            //Debug.LogError(request.downloadHandler.text);
                                            shouldRetry = true;
                                            ErrorHandler.SendRunFailed();
                                            await Task.Delay(2000);
                                            //await Task.Delay(100);
                                            break;
                                        }
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
                                    //attempt = 10;
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
                    while (!asyncOperation.isDone && !shouldRetry && !token.IsCancellationRequested);
                    // Проверка состояния завершения операции после выполнения запроса
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    if (!shouldRetry)
                    {
                        onRunComplete?.Invoke();
                    }
                    else
                    {
                        await Task.Delay(200);
                    }

                    attempt++;
                }
                Debug.Log(attempt);
                //Debug.Log(shouldRetry && attempt < maxRetries);
            }
            while (shouldRetry && attempt < maxRetries);

            if (shouldRetry && attempt >= maxRetries)
            {
                Debug.LogError("ИСПРАВЬ БЛЯ ЭТО ПЕРЕДАВАЙ В МЕТОД ТИП ОШИБКИ");
                ErrorHandler.SendRequestFailed(ErrorType.CharGPTRequestFailed);
            }
        }

        protected async Task<T> DispatchRequestToAssistant<T>(string path, string method, ErrorType errorType, byte[] payload = null, Action<ErrorType> onRequestFailed = null) where T : IResponse, new()
        {
            const int maxRetries = 3; // Максимальное количество попыток
            int attempt = 0;
            bool shouldRetry = false;
            T data = new T();
            do
            {
                //Debug.Log(path);
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
                            await Task.Delay(100);
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
                if (data?.Error == null)
                {
                    ApiError error = new ApiError();
                    error.Message = "RequestFailed";
                    data.Error = error;
                }

                ErrorHandler.SendRequestFailed(errorType);
            }
            return data;
        }

        public async Task<ThreadResponse> CreateThread(CreateThreadRequest request, Action<ErrorType> onRequestFailed = null)
        {
            var path = $"{BASE_PATH}/threads";
            var payload = CreatePayload(request);

            return await DispatchRequestToAssistant<ThreadResponse>(path, UnityWebRequest.kHttpVerbPOST, ErrorType.CreateThreadFailed, payload, onRequestFailed);
        }

        public async Task<ThreadResponse> RetrieveThread(string threadId, Action<ErrorType> onRequestFailed = null)
        {
            var path = $"{BASE_PATH}/threads/{threadId}";

            return await DispatchRequestToAssistant<ThreadResponse>(path, UnityWebRequest.kHttpVerbGET, ErrorType.RetrieveThreadFailed);
        }

        public async Task<DeleteResponse> DeleteThread(string threadId, Action<ErrorType> onRequestFailed = null)
        {
            var path = $"{BASE_PATH}/threads/{threadId}";
            return await DispatchRequestToAssistant<DeleteResponse>(path, UnityWebRequest.kHttpVerbDELETE, ErrorType.DeleteThreadFailed);
        }

        public async Task<MessageResponse> CreateMessage(string threadId, CreateMessageRequest request, Action<ErrorType> onRequestFailed = null)
        {
            var path = $"{BASE_PATH}/threads/{threadId}/messages";
            var payload = CreatePayload(request);
            MessageResponse message = await DispatchRequestToAssistant<MessageResponse>(path, UnityWebRequest.kHttpVerbPOST, ErrorType.CreateMessageFailed, payload);

            return message;
        }

        public async Task<List<MessageResponse>> RetrieveMessages(string threadId, Action<ErrorType> onRequestFailed = null)
        {
            var path = $"{BASE_PATH}/threads/{threadId}/messages";

            var run = await DispatchRequestToAssistant<MessageListResponce>(path, UnityWebRequest.kHttpVerbGET, ErrorType.RetrieveMessageFailed);
            Debug.Log(run.Data.Count);
            return run.Data;
        }

        public async Task<RunResponse> CreateRun(string threadId, CreateRunRequest request, Action<ErrorType> onRequestFailed = null)
        {
            var path = $"{BASE_PATH}/threads/{threadId}/runs";
            var payload = CreatePayload(request);
            RunResponse run = await DispatchRequestToAssistant<RunResponse>(path, UnityWebRequest.kHttpVerbPOST, ErrorType.CreateRunFailed);
            Debug.Log(run.ThreadId);
            return run;
        }

        public void CreateRunAsync(string threadId, CreateRunRequest request, Action<string> onCreate, Action<List<MessageDelta>> onResponse, Action onComplete, Action<ErrorType> onFailed, CancellationTokenSource token)
        {
            var path = $"{BASE_PATH}/threads/{threadId}/runs";
            request.Stream = true;

            var payload = CreatePayload(request);

            DispatchRequestToAssistant(path, UnityWebRequest.kHttpVerbPOST, onCreate, onResponse, onComplete, onFailed, token, payload);
        }

        public async Task<RunResponse> CreateThreadAndRun(CreateThreadRunRequest request, Action<ErrorType> onRequestFailed = null)
        {
            var path = $"{BASE_PATH}/threads/runs";
            var payload = CreatePayload(request);
            RunResponse run = await DispatchRequestToAssistant<RunResponse>(path, UnityWebRequest.kHttpVerbPOST, ErrorType.CreateThreadFailed);
            Debug.Log(run.ThreadId);
            return run;
        }

        public async Task<RunResponse> RetrieveRun(string threadId, string runId, Action<ErrorType> onRequestFailed = null)
        {
            var path = $"{BASE_PATH}/threads/{threadId}/runs/{runId}";

            var run = await DispatchRequestToAssistant<RunResponse>(path, UnityWebRequest.kHttpVerbGET, ErrorType.RetrieveRunFailed);

            return run;
        }

        public async Task<RunResponse> CancelRun(string threadId, string runId, Action<ErrorType> onRequestFailed = null)
        {
            var path = $"{BASE_PATH}/threads/{threadId}/runs/{runId}/cancel";
            Debug.Log(path);
            var run = await DispatchRequestToAssistant<RunResponse>(path, UnityWebRequest.kHttpVerbPOST, ErrorType.CancelRunFailed);

            return run;
        }
    }
}