#if NET8_0_OR_GREATER
#nullable enable

using NUnit.Framework;
using ServiceStack.AI;

namespace ServiceStack.Text.Tests;

public class OpenAiSerializationJsonTests
{
    [Test]
    public void Can_serialize_ChatCompletion_with_Text_Content()
    {
        var request = new ChatCompletion
        {
            Messages =
            [
                Message.Text("Hello World"),
            ]
        };
        
        var json = request.ToJson();
        json.Print();
        
        var fromJson = json.FromJson<ChatCompletion>();
        var message = fromJson.Messages[0].Content;
        Assert.IsNotNull(message);
        Assert.That(message!.Count, Is.EqualTo(1));
        var text = message[0] as AiTextContent;
        Assert.IsNotNull(text);
        Assert.That(text!.Text, Is.EqualTo("Hello World"));
    }
    
    [Test]
    public void Can_serialize_ChatCompletion_with_Image_Content()
    {
        var request = new ChatCompletion
        {
            Messages =
            [
                Message.Image("https://example.org/image.png",
                    "Describe the image"),
            ]
        };
        
        var json = request.ToJson();
        json.Print();
        
        var fromJson = json.FromJson<ChatCompletion>();
        var message = fromJson.Messages[0].Content;
        Assert.IsNotNull(message);
        Assert.That(message!.Count, Is.EqualTo(2));
        var image = message[0] as AiImageContent;
        Assert.IsNotNull(image);
        Assert.That(image!.Type, Is.EqualTo("image_url"));
        Assert.That(image!.ImageUrl.Url, Is.EqualTo("https://example.org/image.png"));
        var text = message[1] as AiTextContent;
        Assert.IsNotNull(text);
        Assert.That(text!.Text, Is.EqualTo("Describe the image"));
    }
    
    [Test]
    public void Can_serialize_ChatCompletion_with_Audio_Content()
    {
        var request = new ChatCompletion
        {
            Messages =
            [
                Message.Audio("https://example.org/audio.wav",
                    format:"wav",
                    "Describe the audio"),
            ]
        };
        
        var json = request.ToJson();
        json.Print();
        
        var fromJson = json.FromJson<ChatCompletion>();
        var message = fromJson.Messages[0].Content;
        Assert.IsNotNull(message);
        Assert.That(message!.Count, Is.EqualTo(2));
        var audio = message[0] as AiAudioContent;
        Assert.IsNotNull(audio);
        Assert.That(audio!.Type, Is.EqualTo("input_audio"));
        Assert.That(audio!.InputAudio.Data, Is.EqualTo("https://example.org/audio.wav"));
        Assert.That(audio!.InputAudio.Format, Is.EqualTo("wav"));
        var text = message[1] as AiTextContent;
        Assert.IsNotNull(text);
        Assert.That(text!.Text, Is.EqualTo("Describe the audio"));
    }
    
    [Test]
    public void Can_serialize_ChatCompletion_with_File_Content()
    {
        var request = new ChatCompletion
        {
            Messages =
            [
                Message.File(filename:"file.pdf",
                    fileData:"https://example.org/file.pdf",
                    text:"Describe the file"),
            ]
        };
        
        var json = request.ToJson();
        json.Print();
        
        var fromJson = json.FromJson<ChatCompletion>();
        var message = fromJson.Messages[0].Content;
        Assert.IsNotNull(message);
        Assert.That(message!.Count, Is.EqualTo(2));
        var file = message[0] as AiFileContent;
        Assert.IsNotNull(file);
        Assert.That(file!.Type, Is.EqualTo("file"));
        Assert.That(file!.File.FileData, Is.EqualTo("https://example.org/file.pdf"));
        Assert.That(file!.File.Filename, Is.EqualTo("file.pdf"));
        var text = message[1] as AiTextContent;
        Assert.IsNotNull(text);
        Assert.That(text!.Text, Is.EqualTo("Describe the file"));
    }
}

#endif