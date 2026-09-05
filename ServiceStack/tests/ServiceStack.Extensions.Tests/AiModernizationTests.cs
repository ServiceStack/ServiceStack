using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.AI;
using ServiceStack.IO;

namespace ServiceStack.Extensions.Tests
{
    [TestFixture]
    public class AiModernizationTests
    {
        [Test]
        public void HttpClientUtils_Validates_Null_Arguments()
        {
            Assert.Throws<ArgumentNullException>(() => HttpClientUtils.ToHttpContent(null!));

            var vfs = new MemoryVirtualFiles();
            vfs.WriteFile("test.txt", "hello world");
            var file = vfs.GetFile("test.txt");

            using var content = new MultipartFormDataContent();
            Assert.Throws<ArgumentNullException>(() => HttpClientUtils.AddFile(null!, "file", file));
            Assert.Throws<ArgumentNullException>(() => HttpClientUtils.AddFile(content, "file", null!));

            Assert.Throws<ArgumentNullException>(() => HttpClientUtils.AddFileInfo(null!, "file", "test.txt"));
            Assert.Throws<ArgumentNullException>(() => HttpClientUtils.AddParam(null!, "key", "val"));
            Assert.Throws<ArgumentNullException>(() => HttpClientUtils.AddParam(content, null!, "val"));
        }

        [Test]
        public void HttpClientUtils_AddParam_Handles_Null_Value()
        {
            using var content = new MultipartFormDataContent();
            HttpClientUtils.AddParam(content, "key", null!);
            Assert.That(content, Is.Not.Null);

            using var content2 = new MultipartFormDataContent();
            content2.AddParam("key", (string)null!);
            Assert.That(content2, Is.Not.Null);
        }

        [Test]
        public void HttpClientUtils_AddFileInfo_Handles_Empty_FileName()
        {
            using var strContent = new StringContent("hello");
            HttpClientUtils.AddFileInfo(strContent, "testField", "");
            Assert.That(strContent.Headers.ContentDisposition?.FileName, Is.EqualTo("file"));
            Assert.That(strContent.Headers.ContentDisposition?.Name, Is.EqualTo("testField"));

            using var strContent2 = new StringContent("hello");
            strContent2.AddFileInfo("testField", "");
            Assert.That(strContent2.Headers.ContentDisposition?.FileName, Is.EqualTo("file"));
        }

        [Test]
        public void KernelTypeChat_Validates_Null_Inputs()
        {
            Assert.Throws<ArgumentNullException>(() => new KernelTypeChat(null!));
        }

        [Test]
        public void NodeTypeChat_Validates_Null_Request()
        {
            var nodeChat = new NodeTypeChat();
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await nodeChat.TranslateMessageAsync(null!);
            });
        }

        [Test]
        public async Task NodeTypeChat_Cleans_Up_Temp_Schema_File_On_Error()
        {
            var nodeChat = new NodeTypeChat
            {
                ProcessFilter = psi => throw new InvalidOperationException("Simulated process start failure")
            };

            var req = new TypeChatRequest(
                schema: "export interface Test { name: string; }",
                prompt: "Translate",
                userMessage: "test")
            {
                NodePath = "node",
            };

            try
            {
                await nodeChat.TranslateMessageAsync(req);
                Assert.Fail("Expected exception");
            }
            catch (InvalidOperationException ex)
            {
                Assert.That(ex.Message, Is.EqualTo("Simulated process start failure"));
            }
        }

        [Test]
        public void WhisperApiSpeechToText_Validates_Inputs()
        {
            var whisper = new WhisperApiSpeechToText();

            Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await whisper.TranscribeAsync(null!);
            });

            Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await whisper.TranscribeAsync("");
            });

            Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await whisper.TranscribeAsync("recording.mp3");
            });

            var vfs = new MemoryVirtualFiles();
            whisper.VirtualFiles = vfs;

            // Missing file in VFS
            Assert.ThrowsAsync<FileNotFoundException>(async () =>
            {
                await whisper.TranscribeAsync("recording.mp3");
            });

            // Write a dummy file to VFS
            vfs.WriteFile("recording.mp3", "dummy audio content");

            // No API Key set and no OPENAI_API_KEY env var
            var origKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            try
            {
                Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
                whisper.ApiKey = null;

                var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                {
                    await whisper.TranscribeAsync("recording.mp3");
                });
                Assert.That(ex!.Message, Does.Contain("OpenAI API Key was not found"));
            }
            finally
            {
                Environment.SetEnvironmentVariable("OPENAI_API_KEY", origKey);
            }
        }

        [Test]
        public void WhisperLocalSpeechToText_Validates_Inputs()
        {
            var whisper = new WhisperLocalSpeechToText();

            Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await whisper.TranscribeAsync(null!);
            });

            Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await whisper.TranscribeAsync("");
            });

            whisper.WhisperPath = "/non/existent/path/to/whisper_binary_that_does_not_exist";
            // Should fail trying to run non-existent binary or process start
            Assert.CatchAsync(async () =>
            {
                await whisper.TranscribeAsync("test.mp3");
            });
        }
    }
}
