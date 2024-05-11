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
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace OpenAI
{
    public class OpenAIApi
    {
        /// <summary>
        ///     Reads and sets user credentials from %User%/.openai/auth.json
        ///     Remember that your API key is a secret! Do not share it with others or expose it in any client-side code (browsers, apps).
        ///     Production requests must be routed through your own backend server where your API key can be securely loaded from an environment variable or key management service.
        /// </summary>
        private Configuration configuration;

        private Configuration Configuration
        {
            get
            {
                if (configuration == null)
                {
                    configuration = new Configuration();
                }

                return configuration;
            }
        }

        /// OpenAI API base path for requests.
        private const string BASE_PATH = "https://api.openai.com/v1";

        public OpenAIApi(string apiKey = null, string organization = null)
        {
            if (apiKey != null)
            {
                configuration = new Configuration(apiKey, organization);
            }
        }

        /// Used for serializing and deserializing PascalCase request object fields into snake_case format for JSON. Ignores null fields when creating JSON strings.
        private readonly JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Ignore,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            ContractResolver = new DefaultContractResolver()
            {
                NamingStrategy = new CustomNamingStrategy()
            },
            MissingMemberHandling = MissingMemberHandling.Ignore,
            Culture = CultureInfo.InvariantCulture
        };

        /// <summary>
        ///     Dispatches an HTTP request to the specified path with the specified method and optional payload.
        /// </summary>
        /// <param name="path">The path to send the request to.</param>
        /// <param name="method">The HTTP method to use for the request.</param>
        /// <param name="payload">An optional byte array of json payload to include in the request.</param>
        /// <typeparam name="T">Response type of the request.</typeparam>
        /// <returns>A Task containing the response from the request as the specified type.</returns>
        private async Task<T> DispatchRequest<T>(string path, string method, byte[] payload = null, bool isBeta = false) where T : IResponse, new()
        {
            T data = new T();
            Debug.Log(path);
            using (var request = UnityWebRequest.Put(path, payload))
            {
                request.method = method;
                request.SetHeaders(Configuration, ContentType.ApplicationJson);
                if (isBeta)
                    request.SetRequestHeader("OpenAI-Beta", "assistants=v2");

                var asyncOperation = request.SendWebRequest();

                while (!asyncOperation.isDone) await Task.Yield();

                try
                {
                    Debug.Log(request.downloadHandler.text);

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        data = JsonConvert.DeserializeObject<T>(request.downloadHandler.text, jsonSerializerSettings);
                    }
                    else
                        Debug.Log(request.error);
                }
                catch (JsonReaderException ex)
                {
                    // ��������� ������ ��������������
                    Debug.Log($"������ �������������� JSON: {ex.Message}");
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

            return data;
        }

        /// <summary>
        ///     Dispatches an HTTP request to the specified path with the specified method and optional payload.
        /// </summary>
        /// <param name="path">The path to send the request to.</param>
        /// <param name="method">The HTTP method to use for the request.</param>
        /// <param name="onResponse">A callback function to be called when a response is updated.</param>
        /// <param name="onComplete">A callback function to be called when the request is complete.</param>
        /// <param name="token">A cancellation token to cancel the request.</param>
        /// <param name="payload">An optional byte array of json payload to include in the request.</param>
        private async void DispatchRequest<T>(string path, string method, Action<List<T>> onResponse, Action<string> onComplete, CancellationTokenSource token, byte[] payload = null, bool isBeta = false) where T : IResponse
        {
            using (var request = UnityWebRequest.Put(path, payload))
            {
                request.method = method;
                request.SetHeaders(Configuration, ContentType.ApplicationJson);
                if (isBeta)
                    request.SetRequestHeader("OpenAI-Beta", "assistants=v2");
                var asyncOperation = request.SendWebRequest();
                string run_id = null;

                do
                {
                    //if (token.IsCancellationRequested) return;
                    List<T> dataList = new List<T>();
                    List<string> lines = request.downloadHandler.text.Split('\n').Where(line => line != "").ToList();
                    if (lines.Count > 0)
                    {
                        //Debug.Log(request.downloadHandler.text);

                        foreach (string line in lines)
                        {
                            string value = line;
                            if (line.Contains("event: "))
                            {
                                //Debug.Log(line);
                                continue;
                            }
                            value = line.Replace("data: ", "");

                            if (value.Contains("[DONE]"))
                            {
                                onComplete?.Invoke(("ddd"));
                                break;
                            }

                            if (isBeta)
                            {
                                //Debug.Log(value);
                                if (value.Contains("run_"))
                                {
                                    string pattern = "\"id\":\"(run_[^\"]+)\"";

                                    // ����� ������������ ����������� ��������� � ������ JSON
                                    Match match = Regex.Match(value, pattern);

                                    // ���������� id, ���� ������������ �������
                                    if (match.Success)
                                    {
                                        run_id = match.Groups[1].Value;
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
                                }
                                else
                                {
                                    dataList.Add(data);
                                }
                            }
                            else
                            {
                                var data = JsonConvert.DeserializeObject<T>(value, jsonSerializerSettings);

                                if (data?.Error != null)
                                {
                                    ApiError error = data.Error;
                                    Debug.LogError($"Error Message: {error.Message}\nError Type: {error.Type}\n");
                                }
                                else
                                {
                                    dataList.Add(data);
                                }
                            }
                        }
                        if (dataList.Count > 0)
                            onResponse?.Invoke(dataList);

                        await Task.Yield();
                    }
                }
                while (!asyncOperation.isDone && !token.IsCancellationRequested);

                onComplete?.Invoke(run_id);
            }
        }

        /// <summary>
        ///     Dispatches an HTTP request to the specified path with a multi-part data form.
        /// </summary>
        /// <param name="path">The path to send the request to.</param>
        /// <param name="form">A multi-part data form to upload with the request.</param>
        /// <typeparam name="T">Response type of the request.</typeparam>
        /// <returns>A Task containing the response from the request as the specified type.</returns>
        private async Task<T> DispatchRequest<T>(string path, List<IMultipartFormSection> form, Action onSomeError = null) where T : IResponse, new()
        {
            T data = new T();
            using (var request = new UnityWebRequest(path, "POST"))
            {
                request.SetHeaders(Configuration);
                var boundary = UnityWebRequest.GenerateBoundary();
                var formSections = UnityWebRequest.SerializeFormSections(form, boundary);
                var contentType = $"{ContentType.MultipartFormData}; boundary={Encoding.UTF8.GetString(boundary)}";
                request.uploadHandler = new UploadHandlerRaw(formSections) { contentType = contentType };
                request.downloadHandler = new DownloadHandlerBuffer();
                var asyncOperation = request.SendWebRequest();

                while (!asyncOperation.isDone)
                {
                    //Debug.Log(asyncOperation.progress);
                    await Task.Yield();
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("������ ��� ���������� �������: " + request.result);

                    onSomeError?.Invoke();

                    return data;
                }
                Debug.Log(request.downloadHandler.text);
                try
                {
                    data = JsonConvert.DeserializeObject<T>(request.downloadHandler.text, jsonSerializerSettings);
                }
                catch (JsonSerializationException ex)
                {
                    Debug.LogError("������ ��� �������������� �������: " + ex.Message);

                    onSomeError?.Invoke();
                    return data;
                }
                catch (Exception ex)
                {
                    Debug.LogError("������: " + ex.Message);

                    onSomeError?.Invoke();
                    return data;
                }
            }

            if (data != null && data.Error != null)
            {
                ApiError error = data.Error;
                Debug.LogError($"Error Message: {error.Message}\nError Type: {error.Type}\n");
            }

            return data;
        }

        /// <summary>
        ///     Create byte array payload from the given request object that contains the parameters.
        /// </summary>
        /// <param name="request">The request object that contains the parameters of the payload.</param>
        /// <typeparam name="T">type of the request object.</typeparam>
        /// <returns>Byte array payload.</returns>
        private byte[] CreatePayload<T>(T request)
        {
            var json = JsonConvert.SerializeObject(request, jsonSerializerSettings);
            return Encoding.UTF8.GetBytes(json);
        }

        /// <summary>
        ///     Lists the currently available models, and provides basic information about each one such as the owner and availability.
        /// </summary>
        public async Task<ListModelsResponse> ListModels()
        {
            var path = $"{BASE_PATH}/models";
            return await DispatchRequest<ListModelsResponse>(path, UnityWebRequest.kHttpVerbGET);
        }

        /// <summary>
        ///     Retrieves a model instance, providing basic information about the model such as the owner and permissioning.
        /// </summary>
        /// <param name="id">The ID of the model to use for this request</param>
        /// <returns>See <see cref="Model"/></returns>
        public async Task<OpenAIModel> RetrieveModel(string id)
        {
            var path = $"{BASE_PATH}/models/{id}";
            return await DispatchRequest<OpenAIModelResponse>(path, UnityWebRequest.kHttpVerbGET);
        }

        /// <summary>
        ///     Creates a completion for the provided prompt and parameters.
        /// </summary>
        /// <param name="request">See <see cref="CreateCompletionRequest"/></param>
        /// <returns>See <see cref="CreateCompletionResponse"/></returns>
        public async Task<CreateCompletionResponse> CreateCompletion(CreateCompletionRequest request)
        {
            var path = $"{BASE_PATH}/completions";
            var payload = CreatePayload(request);
            return await DispatchRequest<CreateCompletionResponse>(path, UnityWebRequest.kHttpVerbPOST, payload);
        }

        /// <summary>
        ///     Creates a chat completion request as in ChatGPT.
        /// </summary>
        /// <param name="request">See <see cref="CreateChatCompletionRequest"/></param>
        /// <param name="onResponse">Callback function that will be called when stream response is updated.</param>
        /// <param name="onComplete">Callback function that will be called when stream response is completed.</param>
        /// <param name="token">Cancellation token to cancel the request.</param>
        public void CreateCompletionAsync(CreateCompletionRequest request, Action<List<CreateCompletionResponse>> onResponse, Action<string> onComplete, CancellationTokenSource token)
        {
            request.Stream = true;
            var path = $"{BASE_PATH}/completions";
            var payload = CreatePayload(request);

            DispatchRequest(path, UnityWebRequest.kHttpVerbPOST, onResponse, onComplete, token, payload);
        }

        /// <summary>
        ///     Creates a chat completion request as in ChatGPT.
        /// </summary>
        /// <param name="request">See <see cref="CreateChatCompletionRequest"/></param>
        /// <returns>See <see cref="CreateChatCompletionResponse"/></returns>
        public async Task<CreateChatCompletionResponse> CreateChatCompletion(CreateChatCompletionRequest request)
        {
            var path = $"{BASE_PATH}/chat/completions";
            var payload = CreatePayload(request);

            return await DispatchRequest<CreateChatCompletionResponse>(path, UnityWebRequest.kHttpVerbPOST, payload);
        }

        /// <summary>
        ///     Creates a chat completion request as in ChatGPT.
        /// </summary>
        /// <param name="request">See <see cref="CreateChatCompletionRequest"/></param>
        /// <param name="onResponse">Callback function that will be called when stream response is updated.</param>
        /// <param name="onComplete">Callback function that will be called when stream response is completed.</param>
        /// <param name="token">Cancellation token to cancel the request.</param>
        public void CreateChatCompletionAsync(CreateChatCompletionRequest request, Action<List<CreateChatCompletionResponse>> onResponse, Action<string> onComplete, CancellationTokenSource token)
        {
            request.Stream = true;
            var path = $"{BASE_PATH}/chat/completions";
            var payload = CreatePayload(request);

            DispatchRequest(path, UnityWebRequest.kHttpVerbPOST, onResponse, onComplete, token, payload);
        }

        /// <summary>
        ///     Creates a new edit for the provided input, instruction, and parameters.
        /// </summary>
        /// <param name="request">See <see cref="CreateEditRequest"/></param>
        /// <returns>See <see cref="CreateEditResponse"/></returns>
        public async Task<CreateEditResponse> CreateEdit(CreateEditRequest request)
        {
            var path = $"{BASE_PATH}/edits";
            var payload = CreatePayload(request);
            return await DispatchRequest<CreateEditResponse>(path, UnityWebRequest.kHttpVerbPOST, payload);
        }

        /// <summary>
        ///     Creates an image given a prompt.
        /// </summary>
        /// <param name="request">See <see cref="CreateImageRequest"/></param>
        /// <returns>See <see cref="CreateImageResponse"/></returns>
        public async Task<CreateImageResponse> CreateImage(CreateImageRequest request)
        {
            var path = $"{BASE_PATH}/images/generations";
            var payload = CreatePayload(request);
            Debug.Log("������");
            return await DispatchRequest<CreateImageResponse>(path, UnityWebRequest.kHttpVerbPOST, payload);
        }

        /// <summary>
        ///     Creates an edited or extended image given an original image and a prompt.
        /// </summary>
        /// <param name="request">See <see cref="CreateImageEditRequest"/></param>
        /// <returns>See <see cref="CreateImageResponse"/></returns>
        public async Task<CreateImageResponse> CreateImageEdit(CreateImageEditRequest request)
        {
            var path = $"{BASE_PATH}/images/edits";

            var form = new List<IMultipartFormSection>();
            form.AddFile(request.Image, "image", "image/png");
            form.AddFile(request.Mask, "mask", "image/png");
            form.AddValue(request.Prompt, "prompt");
            form.AddValue(request.N, "n");
            form.AddValue(request.Size, "size");
            form.AddValue(request.ResponseFormat, "response_format");

            return await DispatchRequest<CreateImageResponse>(path, form);
        }

        /// <summary>
        ///     Creates a variation of a given image.
        /// </summary>
        /// <param name="request">See <see cref="CreateImageVariationRequest"/></param>
        /// <returns>See <see cref="CreateImageResponse"/></returns>
        public async Task<CreateImageResponse> CreateImageVariation(CreateImageVariationRequest request)
        {
            var path = $"{BASE_PATH}/images/variations";

            var form = new List<IMultipartFormSection>();
            form.AddFile(request.Image, "image", "image/png");
            form.AddValue(request.N, "n");
            form.AddValue(request.Size, "size");
            form.AddValue(request.ResponseFormat, "response_format");
            form.AddValue(request.User, "user");

            return await DispatchRequest<CreateImageResponse>(path, form);
        }

        /// <summary>
        ///     Creates an embedding vector representing the input text.
        /// </summary>
        /// <param name="request">See <see cref="CreateEmbeddingsRequest"/></param>
        /// <returns>See <see cref="CreateEmbeddingsResponse"/></returns>
        public async Task<CreateEmbeddingsResponse> CreateEmbeddings(CreateEmbeddingsRequest request)
        {
            var path = $"{BASE_PATH}/embeddings";
            var payload = CreatePayload(request);
            return await DispatchRequest<CreateEmbeddingsResponse>(path, UnityWebRequest.kHttpVerbPOST, payload);
        }

        /// <summary>
        ///     Transcribes audio into the input language.
        /// </summary>
        /// <param name="request">See <see cref="CreateAudioTranscriptionsRequest"/></param>
        /// <returns>See <see cref="CreateAudioResponse"/></returns>
        public async Task<CreateAudioResponse> CreateAudioTranscription(CreateAudioTranscriptionsRequest request)
        {
            var path = $"{BASE_PATH}/audio/transcriptions";

            var form = new List<IMultipartFormSection>();

            if (string.IsNullOrEmpty(request.File))
            {
                form.AddData(request.FileData, "file", $"audio/{Path.GetExtension(request.File)}");
            }
            else
            {
                form.AddFile(request.File, "file", $"audio/{Path.GetExtension(request.File)}");
            }
            form.AddValue(request.Model, "model");
            form.AddValue(request.Prompt, "prompt");
            form.AddValue(request.ResponseFormat, "response_format");
            form.AddValue(request.Temperature, "temperature");
            form.AddValue(request.Language, "language");

            var result = await DispatchRequest<CreateAudioResponse>(path, form);

            if (result.Error != null)
            {
                Debug.Log(result.Error);
            }

            return result;
        }

        /// <summary>
        ///     Translates audio into into English.
        /// </summary>
        /// <param name="request">See <see cref="CreateAudioTranslationRequest"/></param>
        /// <returns>See <see cref="CreateAudioResponse"/></returns>
        public async Task<CreateAudioResponse> CreateAudioTranslation(CreateAudioTranslationRequest request)
        {
            var path = $"{BASE_PATH}/audio/translations";

            var form = new List<IMultipartFormSection>();
            if (string.IsNullOrEmpty(request.File))
            {
                form.AddData(request.FileData, "file", $"audio/{Path.GetExtension(request.File)}");
            }
            else
            {
                form.AddFile(request.File, "file", $"audio/{Path.GetExtension(request.File)}");
            }
            form.AddValue(request.Model, "model");
            form.AddValue(request.Prompt, "prompt");
            form.AddValue(request.ResponseFormat, "response_format");
            form.AddValue(request.Temperature, "temperature");

            return await DispatchRequest<CreateAudioResponse>(path, form);
        }

        /// <summary>
        ///     Returns a list of files that belong to the user's organization.
        /// </summary>
        /// <returns>See <see cref="ListFilesResponse"/></returns>
        public async Task<ListFilesResponse> ListFiles()
        {
            var path = $"{BASE_PATH}/files";
            return await DispatchRequest<ListFilesResponse>(path, UnityWebRequest.kHttpVerbGET);
        }

        /// <summary>
        ///     Upload a file that contains document(s) to be used across various endpoints/features.
        ///     Currently, the size of all the files uploaded by one organization can be up to 1 GB.
        ///     Please contact us if you need to increase the storage limit.
        /// </summary>
        /// <param name="request">See <see cref="CreateFileRequest"/></param>
        /// <returns>See <see cref="OpenAIFile"/></returns>
        public async Task<OpenAIFile> CreateFile(CreateFileRequest request)
        {
            var path = $"{BASE_PATH}/files";

            var form = new List<IMultipartFormSection>();
            form.AddFile(request.File, "file", "application/json");
            form.AddValue(request.Purpose, "purpose");

            return await DispatchRequest<OpenAIFileResponse>(path, form);
        }

        /// <summary>
        ///     Delete a file.
        /// </summary>
        /// <param name="id">The ID of the file to use for this request</param>
        /// <returns>See <see cref="DeleteResponse"/></returns>
        public async Task<DeleteResponse> DeleteFile(string id)
        {
            var path = $"{BASE_PATH}/files/{id}";
            return await DispatchRequest<DeleteResponse>(path, UnityWebRequest.kHttpVerbDELETE);
        }

        /// <summary>
        ///     Returns information about a specific file.
        /// </summary>
        /// <param name="id">The ID of the file to use for this request</param>
        /// <returns>See <see cref="OpenAIFile"/></returns>
        public async Task<OpenAIFile> RetrieveFile(string id)
        {
            var path = $"{BASE_PATH}/files/{id}";
            return await DispatchRequest<OpenAIFileResponse>(path, UnityWebRequest.kHttpVerbGET);
        }

        /// <summary>
        ///     Returns the contents of the specified file
        /// </summary>
        /// <param name="id">The ID of the file to use for this request</param>
        /// <returns>See <see cref="OpenAIFile"/></returns>
        public async Task<OpenAIFile> DownloadFile(string id)
        {
            var path = $"{BASE_PATH}/files/{id}/content";
            return await DispatchRequest<OpenAIFileResponse>(path, UnityWebRequest.kHttpVerbGET);
        }

        /// <summary>
        ///     Manage fine-tuning jobs to tailor a model to your specific training data.
        ///     Related guide: <a href="https://beta.openai.com/docs/guides/fine-tuning">Fine-tune models</a>
        /// </summary>
        /// <param name="request">See <see cref="CreateFineTuneRequest"/></param>
        /// <returns>See <see cref="FineTune"/></returns>
        public async Task<FineTune> CreateFineTune(CreateFineTuneRequest request)
        {
            var path = $"{BASE_PATH}/fine-tunes";
            var payload = CreatePayload(request);
            return await DispatchRequest<FineTuneResponse>(path, UnityWebRequest.kHttpVerbPOST, payload);
        }

        /// <summary>
        ///     List your organization's fine-tuning jobs
        /// </summary>
        /// <returns>See <see cref="ListFineTunesResponse"/></returns>
        public async Task<ListFineTunesResponse> ListFineTunes()
        {
            var path = $"{BASE_PATH}/fine-tunes";
            return await DispatchRequest<ListFineTunesResponse>(path, UnityWebRequest.kHttpVerbGET);
        }

        /// <summary>
        ///     Gets info about the fine-tune job.
        /// </summary>
        /// <param name="id">The ID of the fine-tune job</param>
        /// <returns>See <see cref="FineTune"/></returns>
        public async Task<FineTune> RetrieveFineTune(string id)
        {
            var path = $"{BASE_PATH}/fine-tunes/{id}";
            return await DispatchRequest<FineTuneResponse>(path, UnityWebRequest.kHttpVerbGET);
        }

        /// <summary>
        ///     Immediately cancel a fine-tune job.
        /// </summary>
        /// <param name="id">The ID of the fine-tune job to cancel</param>
        /// <returns>See <see cref="FineTune"/></returns>
        public async Task<FineTune> CancelFineTune(string id)
        {
            var path = $"{BASE_PATH}/fine-tunes/{id}/cancel";
            return await DispatchRequest<FineTuneResponse>(path, UnityWebRequest.kHttpVerbPOST);
        }

        /// <summary>
        ///     Get fine-grained status updates for a fine-tune job.
        /// </summary>
        /// <param name="id">The ID of the fine-tune job to get events for.</param>
        /// <param name="stream">Whether to stream events for the fine-tune job.
        /// If set to true, events will be sent as data-only <a href="https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events/Using_server-sent_events#event_stream_format">server-sent events</a> as they become available.
        /// The stream will terminate with a data: [DONE] message when the job is finished (succeeded, cancelled, or failed).
        /// If set to false, only events generated so far will be returned.</param>
        /// <returns>See <see cref="ListFineTuneEventsResponse"/></returns>
        public async Task<ListFineTuneEventsResponse> ListFineTuneEvents(string id, bool stream = false)
        {
            var path = $"{BASE_PATH}/fine-tunes/{id}/events?stream={stream}";
            return await DispatchRequest<ListFineTuneEventsResponse>(path, UnityWebRequest.kHttpVerbGET);
        }

        /// <summary>
        ///     Delete a fine-tuned model. You must have the Owner role in your organization.
        /// </summary>
        /// <param name="model">The model to delete</param>
        /// <returns>See <see cref="DeleteResponse"/></returns>
        public async Task<DeleteResponse> DeleteFineTunedModel(string model)
        {
            var path = $"{BASE_PATH}/models/{model}";
            return await DispatchRequest<DeleteResponse>(path, UnityWebRequest.kHttpVerbDELETE);
        }

        /// <summary>
        ///     Classifies if text violates OpenAI's Content Policy
        /// </summary>
        /// <param name="request">See <see cref="CreateModerationRequest"/></param>
        /// <returns>See <see cref="CreateModerationResponse"/></returns>
        public async Task<CreateModerationResponse> CreateModeration(CreateModerationRequest request)
        {
            var path = $"{BASE_PATH}/moderations";
            var payload = CreatePayload(request);
            return await DispatchRequest<CreateModerationResponse>(path, UnityWebRequest.kHttpVerbPOST, payload);
        }

        public async Task<ThreadResponse> CreateThread(CreateThreadRequest request)
        {
            var path = $"{BASE_PATH}/threads";
            var payload = CreatePayload(request);

            return await DispatchRequest<ThreadResponse>(path, UnityWebRequest.kHttpVerbPOST, payload, true);
        }

        public async Task<ThreadResponse> RetrieveThread(string threadId)
        {
            var path = $"{BASE_PATH}/threads/{threadId}";

            return await DispatchRequest<ThreadResponse>(path, UnityWebRequest.kHttpVerbGET, isBeta: true);
        }

        public async Task<DeleteResponse> DeleteThread(string threadId)
        {
            var path = $"{BASE_PATH}/threads/{threadId}";
            return await DispatchRequest<DeleteResponse>(path, UnityWebRequest.kHttpVerbDELETE, isBeta: true);
        }

        public async Task<MessageResponse> CreateMessage(string threadId, CreateMessageRequest request)
        {
            var path = $"{BASE_PATH}/threads/{threadId}/messages";
            var payload = CreatePayload(request);
            MessageResponse message = await DispatchRequest<MessageResponse>(path, UnityWebRequest.kHttpVerbPOST, payload, true);

            return message;
        }

        public async Task<List<MessageResponse>> RetrieveMessages(string threadId)
        {
            var path = $"{BASE_PATH}/threads/{threadId}/messages";

            var run = await DispatchRequest<MessageListResponce>(path, UnityWebRequest.kHttpVerbGET, isBeta: true);
            Debug.Log(run.Data.Count);
            return run.Data;
        }

        public async Task<RunResponse> CreateRun(string threadId, CreateRunRequest request)
        {
            var path = $"{BASE_PATH}/threads/{threadId}/runs";
            var payload = CreatePayload(request);
            RunResponse run = await DispatchRequest<RunResponse>(path, UnityWebRequest.kHttpVerbPOST, payload, isBeta: true);
            Debug.Log(run.ThreadId);
            return run;
        }

        public void CreateRunAsync(string threadId, CreateRunRequest request, Action<List<MessageDelta>> onResponse, Action<string> onComplete, CancellationTokenSource token)
        {
            var path = $"{BASE_PATH}/threads/{threadId}/runs";
            request.Stream = true;

            var payload = CreatePayload(request);

            DispatchRequest(path, UnityWebRequest.kHttpVerbPOST, onResponse, onComplete, token, payload, isBeta: true);
        }

        public async Task<RunResponse> CreateThreadAndRun(CreateThreadRunRequest request)
        {
            var path = $"{BASE_PATH}/threads/runs";
            var payload = CreatePayload(request);
            RunResponse run = await DispatchRequest<RunResponse>(path, UnityWebRequest.kHttpVerbPOST, payload, isBeta: true); ;
            Debug.Log(run.ThreadId);
            return run;
        }

        public async Task<RunResponse> RetrieveRun(string threadId, string runId)
        {
            var path = $"{BASE_PATH}/threads/{threadId}/runs/{runId}";

            var run = await DispatchRequest<RunResponse>(path, UnityWebRequest.kHttpVerbGET, isBeta: true);

            return run;
        }

        public async Task<RunResponse> CancelRun(string threadId, string runId)
        {
            var path = $"{BASE_PATH}/threads/{threadId}/runs/{runId}/cancel";

            var run = await DispatchRequest<RunResponse>(path, UnityWebRequest.kHttpVerbGET, isBeta: true);

            return run;
        }
    }
}