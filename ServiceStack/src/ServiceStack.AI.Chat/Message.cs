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
    
    public static AiMessage SystemPrompt(string text) => Text(text, "system");
    
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
    public static string? GetUserPrompt(this ChatCompletion request)
    {
        var textContents = request.Messages
            .Where(x => x.Role is "user" or null)
            .SelectMany(x => x.Content ?? [])
            .Where(x => x is AiTextContent)
            .Cast<AiTextContent>()
            .ToList();
        
        return textContents.LastOrDefault()?.Text;
    }
    
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
}