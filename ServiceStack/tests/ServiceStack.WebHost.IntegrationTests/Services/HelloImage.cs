using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
    [Route("/HelloImage")]
    public class HelloImage {}

    public class HelloImageService : IService
    {
        public object Any(HelloImage request)
        {
            using (Bitmap image = new Bitmap(10, 10))
            {
                using (Graphics g = Graphics.FromImage(image))
                {
                    g.Clear(Color.Red);
                }
                var ms = new MemoryStream();
                image.Save(ms, ImageFormat.Png);
                return new HttpResult(ms, "image/png"); //writes stream directly to response
            }
        }
    }

    [Route("/HelloImage2")]
    public class HelloImage2 {}

    public class HelloImage2Service : IService
    {
        public object Any(HelloImage2 request)
        {
            using (Bitmap image = new Bitmap(10, 10))
            {
                using (Graphics g = Graphics.FromImage(image))
                {
                    g.Clear(Color.Red);
                }
                using (MemoryStream m = new MemoryStream())
                {
                    image.Save(m, ImageFormat.Png);
                    var imageData = m.ToArray(); //buffers
                    return new HttpResult(imageData, "image/png");
                }
            }
        }
    }

    [Route("/HelloImage3")]
    public class HelloImage3 {}

    //Your own Custom Result, writes directly to response stream
    public class ImageResult : IDisposable, IStreamWriterAsync, IHasOptions
    {
        private readonly Image image;
        private readonly ImageFormat imgFormat;

        public ImageResult(Image image, ImageFormat imgFormat=null)
        {
            this.image = image;
            this.imgFormat = imgFormat ?? ImageFormat.Png;
            this.Options = new Dictionary<string, string> {
                { HttpHeaders.ContentType, "image/" + this.imgFormat.ToString().ToLower() }
            };
        }

        public void Dispose()
        {
            this.image.Dispose();
        }

        public IDictionary<string, string> Options { get; set; }

        public async Task WriteToAsync(Stream responseStream, CancellationToken token = new CancellationToken())
        {
            using (var ms = MemoryStreamFactory.GetStream())
            {
                image.Save(ms, imgFormat);

                ms.Position = 0;
                await ms.WriteToAsync(responseStream, token);
            }
        }
    }

    public class HelloImage3Service : IService
    {
        public object Any(HelloImage3 request)
        {
            var image = new Bitmap(10, 10);
            using (var g = Graphics.FromImage(image))
                g.Clear(Color.Red);

            return new ImageResult(image); //terse + explicit is good :)
        }
    }

}