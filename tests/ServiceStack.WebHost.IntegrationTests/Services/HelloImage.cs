using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ServiceStack.Common.Web;
using ServiceStack.Service;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
    [RestService("/HelloImage")]
    public class HelloImage
    {
    }

    public class HelloImageResponse
    {
        public string Result { get; set; }
    }

    public class HelloImageService : IService<HelloImage>
    {
        public object Execute(HelloImage request)
        {
            using (Bitmap image = new Bitmap(10, 10))
            {
                using (Graphics g = Graphics.FromImage(image))
                {
                    g.Clear(Color.Red);
                }
                var ms = new MemoryStream();
                image.Save(ms, ImageFormat.Png);
                return new HttpResult(ms, "image/png");
            }
        }
    }

    [RestService("/HelloImage2")]
    public class HelloImage2
    {
    }

    public class HelloImage2Response
    {
        public string Result { get; set; }
    }

    public class HelloImage2Service : IService<HelloImage2>
    {
        public object Execute(HelloImage2 request)
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
                    var imageData = m.ToArray();
                    return new HttpResult(imageData, "image/png");
                }
            }
        }
    }

    [RestService("/HelloImage3")]
    public class HelloImage3
    {
    }

    public class HelloImage3Response
    {
        public string Result { get; set; }
    }

    public class ImageResult : IDisposable, IStreamWriter, IHasOptions
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

        public void WriteTo(Stream stream)
        {
            image.Save(stream, imgFormat);
        }

        public void Dispose()
        {
            this.image.Dispose();
        }

        public IDictionary<string, string> Options { get; set; }
    }

    public class HelloImage3Service : IService<HelloImage3>
    {
        public object Execute(HelloImage3 request)
        {
            var image = new Bitmap(10, 10);
            using (var g = Graphics.FromImage(image))
            {
                g.Clear(Color.Red);
            }
            return new ImageResult(image);
        }
    }


}