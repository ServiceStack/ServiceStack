using ServiceStack;
using ServiceStack.DataAnnotations;

namespace MyApp.ServiceModel;

[Tag(Tag.Info)]
[Description("Active Media Worker Models available in AI Server")]
public class ActiveMediaModels : IGet, IReturn<StringsResponse> {}


[ValidateApiKey]
[Tag("AI")]
[Api("Convert speech to text")]
[Description("Transcribe audio content to text")]
[SystemJson(UseSystemJson.Response)]
public class SpeechToText : IGeneration, IReturn<GenerationResponse>
{
    [ApiMember(Description = "The audio stream containing the speech to be transcribed")]
    [Description("The audio stream containing the speech to be transcribed")]
    [Required]
    [Input(Type = "file")]
    public Stream Audio { get; set; }
    
    [ApiMember(Description = "Optional client-provided identifier for the request")]
    [Description("Optional client-provided identifier for the request")]
    public string? RefId { get; set; }

    [ApiMember(Description = "Tag to identify the request")]
    [Description("Tag to identify the request")]
    public string? Tag { get; set; }
}

[ValidateApiKey]
[Tag("AI")]
[Api("Convert text to speech")]
[Description("Generate speech audio from text input")]
[SystemJson(UseSystemJson.Response)]
public class TextToSpeech : IGeneration, IReturn<GenerationResponse>
{
    [ApiMember(Description = "The text to be converted to speech")]
    [Description("The text to be converted to speech")]
    [ValidateNotEmpty]
    public string Input { get; set; }

    [ApiMember(Description = "Optional specific model and voice to use for speech generation")]
    [Description("Optional specific model and voice to use for speech generation")]
    public string? Model { get; set; }
    
    [ApiMember(Description = "Optional seed for reproducible results in speech generation")]
    [Description("Optional seed for reproducible results in speech generation")]
    [Range(0, int.MaxValue)]
    public int? Seed { get; set; }
    
    [ApiMember(Description = "Optional client-provided identifier for the request")]
    [Description("Optional client-provided identifier for the request")]
    public string? RefId { get; set; }

    [ApiMember(Description = "Tag to identify the request")]
    [Description("Tag to identify the request")]
    public string? Tag { get; set; }
}

[ValidateApiKey]
[Tag("AI")]
[Api("Generate image from text description")]
[Description("Create an image based on a text prompt")]
[SystemJson(UseSystemJson.Response)]
public class TextToImage : IGeneration, IReturn<GenerationResponse>
{
    [ApiMember(Description = "The main prompt describing the desired image")]
    [Description("The main prompt describing the desired image")]
    [ValidateNotEmpty]
    [Input(Type = "textarea")]
    public string PositivePrompt { get; set; }

    [ApiMember(Description = "Optional prompt specifying what should not be in the image")]
    [Description("Optional prompt specifying what should not be in the image")]
    [Input(Type = "textarea")]
    public string? NegativePrompt { get; set; }

    [ApiMember(Description = "Desired width of the generated image")]
    [Description("Desired width of the generated image")]
    [Range(64, 2048)]
    public int? Width { get; set; }

    [ApiMember(Description = "Desired height of the generated image")]
    [Description("Desired height of the generated image")]
    [Range(64, 2048)]
    public int? Height { get; set; }

    [ApiMember(Description = "Number of images to generate in a single batch")]
    [Description("Number of images to generate in a single batch")]
    [Range(1, 10)]
    public int? BatchSize { get; set; }

    [ApiMember(Description = "The AI model to use for image generation")]
    [Description("The AI model to use for image generation")]
    public string? Model { get; set; }

    [ApiMember(Description = "Optional seed for reproducible results")]
    [Description("Optional seed for reproducible results")]
    [Range(0, int.MaxValue)]
    public int? Seed { get; set; }
    
    [ApiMember(Description = "Optional client-provided identifier for the request")]
    [Description("Optional client-provided identifier for the request")]
    public string? RefId { get; set; }

    [ApiMember(Description = "Tag to identify the request")]
    [Description("Tag to identify the request")]
    public string? Tag { get; set; }
}

[ValidateApiKey]
[Tag("AI")]
[Api("Generate image from another image")]
[Description("Create a new image based on an existing image and a text prompt")]
[SystemJson(UseSystemJson.Response)]
public class ImageToImage : IGeneration, IReturn<GenerationResponse>
{
    [ApiMember(Description = "The image to use as input")]
    [Description("The image to use as input")]
    [Required]
    [Input(Type = "file")]
    public Stream Image { get; set; }

    [ApiMember(Description = "Prompt describing the desired output")]
    [Description("Prompt describing the desired output")]
    [ValidateNotEmpty]
    [Input(Type = "textarea")]
    public string PositivePrompt { get; set; }

    [ApiMember(Description = "Negative prompt describing what should not be in the image")]
    [Description("Negative prompt describing what should not be in the image")]
    [Input(Type = "textarea")]
    public string? NegativePrompt { get; set; }

    [ApiMember(Description = "Optional specific amount of denoise to apply")]
    [Description("Optional specific amount of denoise to apply")]
    [Range(0, 1)]
    public float? Denoise { get; set; }

    [ApiMember(Description = "Number of images to generate in a single batch")]
    [Description("Number of images to generate in a single batch")]
    [Range(1, 10)]
    public int? BatchSize { get; set; }

    [ApiMember(Description = "Optional seed for reproducible results in image generation")]
    [Description("Optional seed for reproducible results in image generation")]
    [Range(0, int.MaxValue)]
    public int? Seed { get; set; }
    
    [ApiMember(Description = "Optional client-provided identifier for the request")]
    [Description("Optional client-provided identifier for the request")]
    public string? RefId { get; set; }

    [ApiMember(Description = "Tag to identify the request")]
    [Description("Tag to identify the request")]
    public string? Tag { get; set; }
}

[ValidateApiKey]
[Tag("AI")]
[Api("Upscale an image")]
[Description("Increase the resolution and quality of an input image")]
[SystemJson(UseSystemJson.Response)]
public class ImageUpscale : IGeneration, IReturn<GenerationResponse>
{
    [ApiMember(Description = "The image to upscale")]
    [Description("The image to upscale")]
    [Required]
    [Input(Type = "file")]
    public Stream Image { get; set; }

    [ApiMember(Description = "Optional seed for reproducible results in image generation")]
    [Description("Optional seed for reproducible results in image generation")]
    [Range(0, int.MaxValue)]
    public int? Seed { get; set; }
    
    [ApiMember(Description = "Optional client-provided identifier for the request")]
    [Description("Optional client-provided identifier for the request")]
    public string? RefId { get; set; }

    [ApiMember(Description = "Tag to identify the request")]
    [Description("Tag to identify the request")]
    public string? Tag { get; set; }
}

[ValidateApiKey]
[Tag("AI")]
[Api("Generate image with masked area")]
[Description("Create a new image by applying a mask to an existing image and generating content for the masked area")]
[SystemJson(UseSystemJson.Response)]
public class ImageWithMask : IGeneration, IReturn<GenerationResponse>
{
    [ApiMember(Description = "Prompt describing the desired output in the masked area")]
    [Description("Prompt describing the desired output in the masked area")]
    [ValidateNotEmpty]
    [Input(Type = "textarea")]
    public string PositivePrompt { get; set; }

    [ApiMember(Description = "Negative prompt describing what should not be in the masked area")]
    [Description("Negative prompt describing what should not be in the masked area")]
    [Input(Type = "textarea")]
    public string? NegativePrompt { get; set; }

    [ApiMember(Description = "The image to use as input")]
    [Description("The image to use as input")]
    [Required]
    [Input(Type = "file")]
    public Stream Image { get; set; }

    [ApiMember(Description = "The mask to use as input")]
    [Description("The mask to use as input")]
    [Required]
    [Input(Type = "file")]
    public Stream Mask { get; set; }

    [ApiMember(Description = "Optional specific amount of denoise to apply")]
    [Description("Optional specific amount of denoise to apply")]
    [Range(0, 1)]
    public float? Denoise { get; set; }

    [ApiMember(Description = "Optional seed for reproducible results in image generation")]
    [Description("Optional seed for reproducible results in image generation")]
    [Range(0, int.MaxValue)]
    public int? Seed { get; set; }
    
    [ApiMember(Description = "Optional client-provided identifier for the request")]
    [Description("Optional client-provided identifier for the request")]
    public string? RefId { get; set; }

    [ApiMember(Description = "Tag to identify the request")]
    [Description("Tag to identify the request")]
    public string? Tag { get; set; }
}

[ValidateApiKey]
[Tag("AI")]
[Api("Convert image to text")]
[Description("Extract text content from an image")]
[SystemJson(UseSystemJson.Response)]
public class ImageToText : IGeneration, IReturn<GenerationResponse>
{
    [ApiMember(Description = "The image to convert to text")]
    [Description("The image to convert to text")]
    [Required]
    [Input(Type = "file")]
    public Stream Image { get; set; }
    
    [ApiMember(Description = "Optional client-provided identifier for the request")]
    [Description("Optional client-provided identifier for the request")]
    public string? RefId { get; set; }

    [ApiMember(Description = "Tag to identify the request")]
    [Description("Tag to identify the request")]
    public string? Tag { get; set; }
}

[Description("Response object for generation requests")]
public class GenerationResponse
{
    [ApiMember(Description = "List of generated outputs")]
    [Description("List of generated outputs")]
    public List<ArtifactOutput>? Outputs { get; set; }

    [ApiMember(Description = "List of generated text outputs")]
    [Description("List of generated text outputs")]
    public List<TextOutput>? TextOutputs { get; set; }

    [ApiMember(Description = "Detailed response status information")]
    [Description("Detailed response status information")]
    public ResponseStatus? ResponseStatus { get; set; }
}

public interface IGeneration
{
    string? RefId { get; set; }
    string? Tag { get; set; }
}

[Description("Output object for generated artifacts")]
public class ArtifactOutput
{
    [ApiMember(Description = "URL to access the generated image")]
    [Description("URL to access the generated image")]
    public string? Url { get; set; }

    [ApiMember(Description = "Filename of the generated image")]
    [Description("Filename of the generated image")]
    public string? FileName { get; set; }

    [ApiMember(Description = "Provider used for image generation")]
    [Description("Provider used for image generation")]
    public string? Provider { get; set; }
}

[Description("Output object for generated text")]
public class TextOutput
{
    [ApiMember(Description = "The generated text")]
    [Description("The generated text")]
    public string? Text { get; set; }
}