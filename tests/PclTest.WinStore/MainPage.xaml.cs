using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PclTest.ServiceModel;
using ServiceStack;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace PclTest.WinStore
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly JsonServiceClient client;

        public MainPage()
        {
            this.InitializeComponent();

//            WinStorePclExportClient.Configure();
            client = new JsonServiceClient("http://localhost:81/");
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        private void btnGoSync_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var response = client.Get(new Hello { Name = txtName.Text });
                lblResults.Text = response.Result;
            }
            catch (Exception ex)
            {
                lblResults.Text = ex.ToString();
            }
        }

        private void btnGoAsync_Click(object sender, RoutedEventArgs e)
        {
            client.GetAsync(new Hello { Name = txtName.Text })
                .Success(r => lblResults.Text = r.Result)
                .Error(ex => lblResults.Text = ex.ToString());
        }
    }
}
