using System.Collections.Generic;
using Newtonsoft.Json;
using OpenAI;

namespace OpenAI
{
    #region Common Data Types

    public struct Choice
    {
        public string Text { get; set; }
        public int? Index { get; set; }
        public int? Logprobs { get; set; }
        public string FinishReason { get; set; }
    }

    public struct Usage
    {
        public string PromptTokens { get; set; }
        public string CompletionTokens { get; set; }
        public string TotalTokens { get; set; }
    }

    public class OpenAIFile
    {
        public string Id { get; set; }
        public string Object { get; set; }
        public long Bytes { get; set; }
        public long CreatedAt { get; set; }
        public string Filename { get; set; }
        public string Purpose { get; set; }
        public object StatusDetails { get; set; }
        public string Status { get; set; }
    }

    public class OpenAIFileResponse : OpenAIFile, IResponse
    {
        public ApiError Error { get; set; }
        public string Warning { get; set; }
    }

    public class ApiError
    {
        public string Message = "NetworkConnection";
        public string Type;
        public object Param;
        public object Code;
    }

    public struct Auth
    {
        [JsonRequired]
        public string ApiKey { get; set; }

        public string Organization { get; set; }
    }

    #endregion Common Data Types

    #region Models API Data Types

    public struct ListModelsResponse : IResponse
    {
        public ApiError Error { get; set; }
        public string Warning { get; set; }
        public string Object { get; set; }
        public List<OpenAIModel> Data { get; set; }
    }

    public class OpenAIModel
    {
        public string Id { get; set; }
        public string Object { get; set; }
        public string OwnedBy { get; set; }
        public long Created { get; set; }
        public string Root { get; set; }
        public string Parent { get; set; }
        public List<Dictionary<string, object>> Permission { get; set; }
    }

    public class OpenAIModelResponse : OpenAIModel, IResponse
    {
        public ApiError Error { get; set; }
        public string Warning { get; set; }
    }

    #endregion Models API Data Types

    #region Chat API Data Types

    public sealed class CreateChatCompletionRequest
    {
        public string Model { get; set; }
        public List<ChatMessage> Messages { get; set; }
        public float? Temperature { get; set; } = 1;
        public int N { get; set; } = 1;
        public bool Stream { get; set; } = false;
        public string Stop { get; set; }
        public int? MaxTokens { get; set; }
        public float? PresencePenalty { get; set; } = 0;
        public float? FrequencyPenalty { get; set; } = 0;
        public Dictionary<string, string> LogitBias { get; set; }
        public string User { get; set; }
    }

    public class CreateChatCompletionResponse : IResponse
    {
        public ApiError Error { get; set; }
        public string Warning { get; set; }
        public string Model { get; set; }
        public string Id { get; set; }
        public string Object { get; set; }
        public long Created { get; set; }
        public List<ChatChoice> Choices { get; set; }
        public Usage Usage { get; set; }
        public string SystemFingerprint { get; set; }

        public CreateChatCompletionResponse()
        {
            ChatChoice chatChoice = new ChatChoice();
            this.Choices = new List<ChatChoice>() { chatChoice };
        }
    }

    public class ChatChoice
    {
        public ChatMessage Message { get; set; }
        public ChatMessage Delta { get; set; }
        public int? Index { get; set; }
        public string FinishReason { get; set; }
        public bool Logprobs { get; set; }

        public ChatChoice()
        {
            ChatMessage message = new ChatMessage();
            message.Content = "Ошибка интернет-соединения";
            this.Message = message;
        }
    }

    public class ChatMessage
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }

    #endregion Chat API Data Types

    #region Audio Transcriptions Data Types

    public class FileData
    {
        public byte[] Data;
        public string Name;

        public FileData(byte[] data, string name)
        {
            Data = data;
            Name = name;
        }
    }

    public class CreateAudioRequestBase
    {
        public string File { get; set; }
        public FileData FileData { get; set; }
        public string Model { get; set; }
        public string Prompt { get; set; }
        public string ResponseFormat { get; set; } = AudioResponseFormat.Json;
        public float? Temperature { get; set; } = 0;
    }

    public class CreateAudioTranscriptionsRequest : CreateAudioRequestBase
    {
        public string Language { get; set; }
    }

    public class CreateAudioTranslationRequest : CreateAudioRequestBase
    { }

    public class CreateAudioResponse : IResponse
    {
        public ApiError Error { get; set; }
        public string Warning { get; set; }
        public string Text { get; set; }

        public CreateAudioResponse()
        { this.Text = "Ошибка интернет-соединения"; }
    }

    #endregion Audio Transcriptions Data Types

    #region Completions API Data Types

    public sealed class CreateCompletionRequest
    {
        public string Model { get; set; }
        public string Prompt { get; set; } = "<|endoftext|>";
        public string Suffix { get; set; }
        public int? MaxTokens { get; set; } = 16;
        public float? Temperature { get; set; } = 1;
        public float? TopP { get; set; } = 1;
        public int N { get; set; } = 1;
        public bool Stream { get; set; } = false;
        public int? Logpropbs { get; set; }
        public bool? Echo { get; set; } = false;
        public string Stop { get; set; }
        public float? PresencePenalty { get; set; } = 0;
        public float? FrequencyPenalty { get; set; } = 0;
        public int? BestOf { get; set; } = 1;
        public Dictionary<string, string> LogitBias { get; set; }
        public string User { get; set; }
    }

    public class CreateCompletionResponse : IResponse
    {
        public ApiError Error { get; set; }
        public string Warning { get; set; }
        public string Id { get; set; }
        public string Object { get; set; }
        public long Created { get; set; }
        public string Model { get; set; }
        public List<Choice> Choices { get; set; }
        public Usage Usage { get; set; }

        public CreateCompletionResponse()
        {
            Choice choice = new Choice();
            choice.Text = "Ошибка интернет-соединения";
            this.Choices = new List<Choice> { };
        }
    }

    #endregion Completions API Data Types

    #region Edits API Data Types

    public sealed class CreateEditRequest
    {
        public string Model { get; set; }
        public string Input { get; set; } = "";
        public string Instruction { get; set; }
        public float? Temperature { get; set; } = 1;
        public float? TopP { get; set; } = 1;
        public int? N { get; set; } = 1;
    }

    public struct CreateEditResponse : IResponse
    {
        public ApiError Error { get; set; }
        public string Warning { get; set; }
        public string Object { get; set; }
        public long Created { get; set; }
        public List<Choice> Choices { get; set; }
        public Usage Usage { get; set; }
    }

    #endregion Edits API Data Types

    #region Images API Data Types

    public class CreateImageRequestBase
    {
        public int? N { get; set; } = 1;
        public string Size { get; set; } = ImageSize.Size1024;
        public string ResponseFormat { get; set; } = ImageResponseFormat.Url;
        public string User { get; set; }
        public string Model { get; set; } = DALLEVersion.v2;
    }

    public sealed class CreateImageRequest : CreateImageRequestBase
    {
        public string Prompt { get; set; }
        public string Style { get; set; }
    }

    public sealed class CreateImageEditRequest : CreateImageRequestBase
    {
        public string Image { get; set; }
        public string Mask { get; set; }
        public string Prompt { get; set; }
    }

    public sealed class CreateImageVariationRequest : CreateImageRequestBase
    {
        public string Image { get; set; }
    }

    public struct CreateImageResponse : IResponse
    {
        public ApiError Error { get; set; }
        public string Warning { get; set; }
        public long Created { get; set; }
        public List<ImageData> Data { get; set; }
    }

    public struct ImageData
    {
        public string Url { get; set; }
        public string B64Json { get; set; }

        public string RevisedPrompt { get; set; }
    }

    #endregion Images API Data Types

    #region Embeddins API Data Types

    public struct CreateEmbeddingsRequest
    {
        public string Model { get; set; }
        public string Input { get; set; }
        public string User { get; set; }
    }

    public struct CreateEmbeddingsResponse : IResponse
    {
        public ApiError Error { get; set; }
        public string Warning { get; set; }
        public string Object { get; set; }
        public List<EmbeddingData> Data;
        public string Model { get; set; }
        public Usage Usage { get; set; }
    }

    public struct EmbeddingData
    {
        public string Object { get; set; }
        public List<float> Embedding { get; set; }
        public int Index { get; set; }
    }

    #endregion Embeddins API Data Types

    #region Files API Data Types

    public struct ListFilesResponse : IResponse
    {
        public ApiError Error { get; set; }
        public string Warning { get; set; }
        public string Object { get; set; }
        public List<OpenAIFile> Data { get; set; }
    }

    public class DeleteResponse : IResponse
    {
        public ApiError Error { get; set; }
        public string Warning { get; set; }
        public string Id { get; set; }
        public string Object { get; set; }
        public bool Deleted { get; set; }
    }

    public struct CreateFileRequest
    {
        public string File { get; set; }
        public string Purpose { get; set; }
    }

    #endregion Files API Data Types

    #region FineTunes API Data Types

    public class CreateFineTuneRequest
    {
        public string TrainingFile { get; set; }
        public string ValidationFile { get; set; }
        public string Model { get; set; }
        public int NEpochs { get; set; } = 4;
        public int? BatchSize { get; set; } = null;
        public float? LearningRateMultiplier { get; set; } = null;
        public float PromptLossWeight { get; set; } = 0.01f;
        public bool ComputeClassificationMetrics { get; set; } = false;
        public int? ClassificationNClasses { get; set; } = null;
        public string ClassificationPositiveClass { get; set; }
        public List<float> ClassificationBetas { get; set; }
        public string Suffix { get; set; }
    }

    public struct ListFineTunesResponse : IResponse
    {
        public ApiError Error { get; set; }
        public string Warning { get; set; }
        public string Object { get; set; }
        public List<FineTune> Data { get; set; }
    }

    public struct ListFineTuneEventsResponse : IResponse
    {
        public ApiError Error { get; set; }
        public string Warning { get; set; }
        public string Object { get; set; }
        public List<FineTuneEvent> Data { get; set; }
    }

    public class FineTune
    {
        public string Id { get; set; }
        public string Object { get; set; }
        public long CreatedAt { get; set; }
        public long UpdatedAt { get; set; }
        public string Model { get; set; }
        public string FineTunedModel { get; set; }
        public string OrganizationId { get; set; }
        public string Status { get; set; }
        public Dictionary<string, object> Hyperparams { get; set; }
        public List<OpenAIFile> TrainingFiles { get; set; }
        public List<OpenAIFile> ValidationFiles { get; set; }
        public List<OpenAIFile> ResultFiles { get; set; }
        public List<FineTuneEvent> Events { get; set; }
    }

    public class FineTuneResponse : FineTune, IResponse
    {
        public ApiError Error { get; set; }
        public string Warning { get; set; }
    }

    public struct FineTuneEvent
    {
        public string Object { get; set; }
        public long CreatedAt { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
    }

    #endregion FineTunes API Data Types

    #region Moderations API Data Types

    public class CreateModerationRequest
    {
        public string Input { get; set; }
        public string Model { get; set; } = ModerationModel.Latest;
    }

    public struct CreateModerationResponse : IResponse
    {
        public ApiError Error { get; set; }
        public string Warning { get; set; }
        public string Id { get; set; }
        public string Model { get; set; }
        public List<ModerationResult> Results { get; set; }
    }

    public struct ModerationResult
    {
        public bool Flagged { get; set; }
        public Dictionary<string, bool> Categories { get; set; }
        public Dictionary<string, float> CategoryScores { get; set; }
    }

    #endregion Moderations API Data Types

    #region Static String Types

    public static class ContentType
    {
        public const string MultipartFormData = "multipart/form-data";
        public const string ApplicationJson = "application/json";
    }

    public static class ImageSize
    {
        public const string Size256 = "256x256";
        public const string Size512 = "512x512";
        public const string Size1024 = "1024x1024";
        public const string Size1792V = "1024x1792";
        public const string Size1792H = "1792x1024";
    }

    public static class ImageStyle
    {
        public const string Vivid = "Vivid";
        public const string Natural = "Natural";
    }

    public static class DALLEVersion
    {
        public const string v2 = "dall-e-2";
        public const string v3 = "dall-e-3";
    }
}

public static class ImageResponseFormat
{
    public const string Url = "url";
    public const string Base64Json = "b64_json";
}

public static class AudioResponseFormat
{
    public const string Json = "json";
    public const string Text = "text";
    public const string Srt = "srt";
    public const string VerboseJson = "verbose_json";
    public const string Vtt = "vtt";
}

public static class ModerationModel
{
    public const string Stable = "text-moderation-stable";
    public const string Latest = "text-moderation-latest";
}

#endregion Static String Types

#region Assistan API Data Types

public class CreateMessageRequest : ChatMessage
{
    public List<string> FileIds { get; set; }
}

public class CreateThreadRequest
{
    public List<CreateMessageRequest> Messages { get; set; }
}

public class ThreadResponse : IResponse
{
    public ApiError Error { get; set; }
    public string Warning { get; set; }
    public string Id { get; set; }
    public string Object { get; set; }
    public int CreatedAt { get; set; }
}

public class MessageResponse : IResponse
{
    public ApiError Error { get; set; }
    public string Warning { get; set; }
    public string Id { get; set; }
    public string Object { get; set; }
    public int CreatedAt { get; set; }
    public string ThreadId { get; set; }
    public string Status { get; set; }
    public int? CompletedAt { get; set; }
    public int? IncompleteAt { get; set; }
    public string Role { get; set; }

    public List<Content> Content { get; set; }

    public string AssistantId { get; set; }
    public string RunId { get; set; }

    public List<OpenAIFile> OpenAIFiles { get; set; }

    public MessageResponse()
    {
        this.Content = new List<Content>() { new Content() };
    }
}

public class MessageListResponce : IResponse
{
    public ApiError Error { get; set; }
    public string Warning { get; set; }

    [JsonProperty("object")]
    public string Object { get; set; }

    [JsonProperty("data")]
    public List<MessageResponse> Data { get; set; }

    [JsonProperty("first_id")]
    public string FirstId { get; set; }

    [JsonProperty("last_id")]
    public string LastId { get; set; }

    [JsonProperty("has_more")]
    public bool HasMore { get; set; }
}

public class MessageFile
{
    public string Id { get; set; }
    public string Object { get; set; }
    public int CreatedAt { get; set; }

    public int MessageId { get; set; }
}

public class MessageDelta : IResponse
{
    public ApiError Error { get; set; }
    public string Warning { get; set; }
    public string Id { get; set; }
    public string Object { get; set; }

    public DeltaMessageContent Delta { get; set; }
}

public partial class DeltaMessageContent
{
    [JsonProperty("content")]
    public Content[] Content { get; set; }
}

public class CreateRunRequest
{
    public string AssistantId { get; set; }
    public string Model { get; set; }
    public string Instructions { get; set; }

    public string AdditionalInstructions { get; set; }

    public List<object> Tools { get; set; }
    public float? Temperature { get; set; } = 0.7f;
    public float? TopP { get; set; } = 1f;

    public bool Stream { get; set; } = false;
}

public class RunResponse : IResponse
{
    public ApiError Error { get; set; }
    public string Warning { get; set; }

    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("object")]
    public string Object { get; set; }

    [JsonProperty("created_at")]
    public long CreatedAt { get; set; }

    [JsonProperty("assistant_id")]
    public string AssistantId { get; set; }

    [JsonProperty("thread_id")]
    public string ThreadId { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("started_at")]
    public long StartedAt { get; set; }

    [JsonProperty("expires_at")]
    public object ExpiresAt { get; set; }

    [JsonProperty("cancelled_at")]
    public object CancelledAt { get; set; }

    [JsonProperty("failed_at")]
    public object FailedAt { get; set; }

    [JsonProperty("completed_at")]
    public long CompletedAt { get; set; }

    [JsonProperty("last_error")]
    public object LastError { get; set; }

    [JsonProperty("model")]
    public string Model { get; set; }

    [JsonProperty("instructions")]
    public object Instructions { get; set; }

    [JsonProperty("file_ids")]
    public object[] FileIds { get; set; }

    [JsonProperty("usage")]
    public Usage Usage { get; set; }
}

public class CreateThreadRunRequest
{
    public string AssistantId { get; set; }

    public CreateThreadRequest Thread { get; set; }
    public string Model { get; set; }
    public string Instructions { get; set; }
    public float? Temperature { get; set; } = 0.7f;

    public bool Stream { get; set; } = false;
}

public interface IFile
{
    public string Type { get; set; }
}

public partial class Content
{
    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
    public TextAssistant Text { get; set; }

    [JsonProperty("image_file", NullValueHandling = NullValueHandling.Ignore)]
    public ImageFile ImageFile { get; set; }
}

public partial class ImageFile
{
    [JsonProperty("file_id")]
    public string FileId { get; set; }
}

public partial class TextAssistant
{
    [JsonProperty("value")]
    public string Value { get; set; } = "Ошибка интернет - соединения";

    [JsonProperty("annotations")]
    public List<object> Annotations { get; set; }
}

public class CreateStreamRunAsync : IResponse
{
    public ApiError Error { get; set; }
    public string Warning { get; set; }

    [JsonProperty("event")]
    public string Event { get; set; }

    [JsonProperty("data")]
    public RunResponse Data { get; set; }
}

#endregion Assistan API Data Types