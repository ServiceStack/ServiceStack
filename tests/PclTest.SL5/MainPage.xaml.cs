using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using PclTest.ServiceModel;
using ServiceStack;

namespace PclTest.SL5
{
    public partial class MainPage : UserControl
    {
        private JsonServiceClient client;

        public MainPage()
        {
            InitializeComponent();
            Sl5PclExportClient.Configure();
            client = new JsonServiceClient("http://localhost:81/");
        }

        private void btnSync_Click(object sender, RoutedEventArgs e)
        {

            //var url = "http://localhost:81/hello?Name={0}".Fmt(txtName.Text);
            //ThreadPool.QueueUserWorkItem(_ =>
            //{
            //    //var creator = System.Net.Browser.WebRequestCreator.BrowserHttp;
            //    var creator = System.Net.Browser.WebRequestCreator.ClientHttp;
            //    var client = (HttpWebRequest)creator.Create(new Uri(url));
            //    client.Accept = MimeTypes.Json;
            //    Task<WebResponse> task = client.GetResponseAsync();
            //    task.Wait();
            //    var json = task.Result.GetResponseStream().ReadFully().FromUtf8Bytes();
            //    var response = json.FromJson<HelloResponse>();

            //    Deployment.Current.Dispatcher.BeginInvoke(() =>
            //    {
            //        lblResults.Content = response.Result;
            //    });
            //});

            var name = txtName.Text;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    var client = new JsonServiceClient("http://localhost:81/") {
                        ShareCookiesWithBrowser = false
                    };
                    var response = client.Get(new Hello { Name = name });
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        lblResults.Content = response.Result;
                    });
                }
                catch (Exception ex)
                {
                    lblResults.Content = ex.ToString();
                }
            });
        }

        private void btnAsync_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                client.GetAsync(new Hello { Name = txtName.Text })
                    .Success(r => lblResults.Content = r.Result)
                    .Error(ex => lblResults.Content = ex.ToString());
            }
            catch (Exception ex)
            {
                lblResults.Content = ex.ToString();
            }
        }

        private async void btnAwait_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //var httpReq = (HttpWebRequest)System.Net.Browser.WebRequestCreator.BrowserHttp.Create(new Uri("http://localhost:81/"));
                //var httpReq = (HttpWebRequest)System.Net.Browser.WebRequestCreator.ClientHttp.Create(new Uri("http://localhost:81/"));

                var response = await client.GetAsync(new Hello { Name = txtName.Text });
                lblResults.Content = response.Result;
            }
            catch (Exception ex)
            {
                lblResults.Content = ex.ToString();
            }
        }

        private async void btnAuth_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var client = new JsonServiceClient("http://localhost:81/");

                await client.PostAsync(new Authenticate
                {
                    provider = "credentials",
                    UserName = "user",
                    Password = "pass",
                });

                var response = await client.GetAsync(new HelloAuth { Name = "secure" });

                lblResults.Content = response.Result;
            }
            catch (Exception ex)
            {
                lblResults.Content = ex.ToString();
            }
        }

        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            var name = txtName.Text;
            ThreadPool.QueueUserWorkItem(_ => {
                try
                {
                    client.ShareCookiesWithBrowser = false;
                    var fileStream = new MemoryStream("content body".ToUtf8Bytes());
                    var response = client.PostFileWithRequest<UploadFileResponse>(
                        fileStream, "file.txt", new UploadFile { Name = name });

                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        lblResults.Content = "File Size: {0} bytes".Fmt(response.FileSize);
                    });
                }
                catch (Exception ex)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        lblResults.Content = ex.ToString();
                    });
                }
            });
        }
    }
}
