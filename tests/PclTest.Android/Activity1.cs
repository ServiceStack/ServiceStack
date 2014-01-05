using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using PclTest.ServiceModel;
using ServiceStack;
using ServiceStack.Text;

namespace PclTest.Android
{
    [Activity(Label = "PclTest.Android", MainLauncher = true, Icon = "@drawable/icon")]
    public class Activity1 : Activity
    {
        int count = 1;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            AndroidPclExportClient.Configure();

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // Get our button from the layout resource,
            // and attach an event to it
            var button = FindViewById<Button>(Resource.Id.MyButton);
            button.Click += delegate { button.Text = string.Format("{0} clicks!!", count++); };

            var btnGoSync = FindViewById<Button>(Resource.Id.btnGoSync);
            var btnGoAsync = FindViewById<Button>(Resource.Id.btnGoAsync);
            var txtName = FindViewById<EditText>(Resource.Id.txtName);
            var txvResults = FindViewById<TextView>(Resource.Id.txvResults);

            //10.0.2.2 = loopback
            //http://developer.android.com/tools/devices/emulator.html
            var client = new JsonServiceClient("http://10.0.2.2:81/");

            btnGoSync.Click += delegate
            {
                try
                {
                    var response = client.Get(new Hello { Name = txtName.Text });
                    txvResults.Text = response.Result;
                }
                catch (Exception ex)
                {
                    txvResults.Text = ex.ToString();
                }
            };

            btnGoAsync.Click += delegate
            {
                client.GetAsync(new Hello { Name = txtName.Text })
                    .Success(response => {
                        txvResults.Text = response.Result;
                    })
                    .Error(ex => {
                        txvResults.Text = ex.ToString();    
                    });
            };
        }
    }
}

