using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PclTest.ServiceModel;
using PclTest.SharedLogic;
using ServiceStack;

namespace PclTest.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly JsonServiceClient client;
        private readonly SharedGateway gateway = new SharedGateway();

        public MainWindow()
        {
            InitializeComponent();
            //Net40PclExportClient.Configure();
            client = new JsonServiceClient("http://localhost:81/");
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

        private async void btnGoShared_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var greeting = await gateway.SayHello(txtName.Text);
                lblResults.Text = greeting;
            }
            catch (Exception ex)
            {
                lblResults.Text = ex.ToString();
            }
        }
    }
}
