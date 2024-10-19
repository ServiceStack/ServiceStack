
using MyApp.ServiceModel;
using ServiceStack;

namespace MyApp.ServiceInterface;

public class OpenAiServices : Service
{
    public object Any(GetWorkerStats request) => new GetWorkerStatsResponse();
    public object Any(OpenAiChatCompletion request) => new OpenAiChatResponse();
    public object Any(ActiveMediaModels request) => new StringsResponse();
    public object Any(TextToSpeech request) => new GenerationResponse();
    public object Any(SpeechToText request) => new GenerationResponse();
    public object Any(TextToImage request) => new GenerationResponse();
    public object Any(ImageToImage request) => new GenerationResponse();
    public object Any(ImageUpscale request) => new GenerationResponse();
    public object Any(ImageWithMask request) => new GenerationResponse();
    public object Any(ImageToText request) => new GenerationResponse();
}
