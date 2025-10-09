using ServiceStack.Text;

namespace ServiceStack.AI;

public static class Message
{
    public static class Content
    {
        public static List<AiContent> Text(string text)
        {
            return [new AiTextContent { Type = "text", Text = text }];
        }
        
        public static List<AiContent> Image(string imageUrl, string? text = null)
        {
            var image = new AiImageContent {
                Type = "image_url",
                ImageUrl = new() {
                    Url = imageUrl
                }
            }; 
            return text == null
                ? [image]
                : [image, new AiTextContent { Type = "text", Text = text }];
        }
        
        public static List<AiContent> Audio(string data, string format="mp3", string? text = null)
        {
            var audio = new AiAudioContent {
                Type = "input_audio",
                InputAudio = new() {
                    Data = data,
                    Format = format
                }
            }; 
            return text == null
                ? [audio]
                : [audio, new AiTextContent { Type = "text", Text = text }];
        }
        
        public static List<AiContent> File(string fileData, string? filename=null, string? text = null)
        {
            var file = new AiFileContent {
                Type = "file",
                File = new() {
                    FileData = fileData,
                    Filename = filename,
                }
            }; 
            return text == null
                ? [file]
                : [file, new AiTextContent { Type = "text", Text = text }];
        }
    }
    
    public static AiMessage Text(string text, string role="user")
    {
        return new AiMessage
        {
            Role = role,
            Content = Content.Text(text),
        };
    }
    
    public static AiMessage Image(string imageUrl, string? text = null, string role="user")
    {
        return new AiMessage
        {
            Role = role,
            Content = Content.Image(imageUrl:imageUrl, text:text),
        };
    }
    
    public static AiMessage Audio(string data, string format="mp3", string? text = null, string role="user")
    {
        return new AiMessage
        {
            Role = role,
            Content = Content.Audio(data:data, format:format, text:text),
        };
    }
    
    public static AiMessage File(string fileData, string? filename=null, string? text = null, string role="user")
    {
        return new AiMessage
        {
            Role = role,
            Content = Content.File(fileData:fileData, filename:filename, text:text),
        };
    }
}

public static class MessageUtils
{
    public static string? GetAnswer(this ChatResponse? response)
    {
        var sb = StringBuilderCache.Allocate();
        foreach (var choice in response?.Choices ?? [])
        {
            if (choice.Message?.Content != null)
            {
                sb.AppendLine(choice.Message.Content);
            }
        }
        var ret = StringBuilderCache.ReturnAndFree(sb);
        return string.IsNullOrEmpty(ret) ? null : ret;
    }

    public class CreateChatCompletion : ChatCompletion, IPost, IReturn<ChatResponse>
    {
    }

    /// <summary>
    /// Convert to OpenAI Chat Completion DTO to avoid sending unnecessary fields
    /// </summary>
    public static ChatCompletion ToChatCompletion(this CreateChatCompletion from)
    {
        return new ChatCompletion
        {
            Messages = from.Messages,
            Model = from.Model,
            Audio = from.Audio,
            FrequencyPenalty = from.FrequencyPenalty,
            LogitBias = from.LogitBias,
            Logprobs = from.Logprobs,
            MaxCompletionTokens = from.MaxCompletionTokens,
            Metadata = from.Metadata,
            Modalities = from.Modalities,
            N = from.N,
            ParallelToolCalls = from.ParallelToolCalls,
            PresencePenalty = from.PresencePenalty,
            PromptCacheKey = from.PromptCacheKey,
            ReasoningEffort = from.ReasoningEffort,
            ResponseFormat = from.ResponseFormat,
            SafetyIdentifier = from.SafetyIdentifier,
            Seed = from.Seed,
            ServiceTier = from.ServiceTier,
            Stop = from.Stop,
            Store = from.Store,
            Stream = from.Stream,
            Temperature = from.Temperature,
            Tools = from.Tools,
            TopLogprobs = from.TopLogprobs,
            TopP = from.TopP,
            Verbosity = from.Verbosity,
            EnableThinking = from.EnableThinking,
        };
    }
}