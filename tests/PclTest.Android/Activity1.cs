using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using PclTest.ServiceModel;
using PclTest.SharedLogic;
using ServiceStack;
using ServiceStack.Text;

namespace PclTest.Android
{
    public class Test
    {
        public string Name { get; set; }
    }

    public class TestModel
    {
        public string Name { get; set; }
    }

    [Activity(Label = "PclTest.Android", MainLauncher = true, Icon = "@drawable/icon")]
    public class Activity1 : Activity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            //AndroidPclExportClient.Configure();

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            var btnGoSync = FindViewById<Button>(Resource.Id.btnGoSync);
            var btnGoAsync = FindViewById<Button>(Resource.Id.btnGoAsync);
            var btnGoShared = FindViewById<Button>(Resource.Id.btnGoShared);
            var btnTest = FindViewById<Button>(Resource.Id.btnTest);
            var txtName = FindViewById<EditText>(Resource.Id.txtName);
            var lblResults = FindViewById<TextView>(Resource.Id.lblResults);

            //10.0.2.2 = loopback
            //http://developer.android.com/tools/devices/emulator.html
            var client = new JsonServiceClient("http://10.0.2.2:81/");
            var gateway = new SharedGateway("http://10.0.2.2:81/");

            btnGoSync.Click += delegate
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
            };

            btnGoAsync.Click += delegate
            {
                client.GetAsync(new Hello { Name = txtName.Text })
                    .Success(response => lblResults.Text = response.Result)
                    .Error(ex => lblResults.Text = ex.ToString());
            };

            btnTest.Click += delegate
            {
                try
                {
                }
                catch (Exception ex)
                {
                    lblResults.Text = ex.ToString();
                }
            };

            btnGoShared.Click += async delegate
            {
                try
                {
                    //PclExport.Instance.SupportsExpression = false;
                    var test = new Test { Name = "Name" };
                    var testModel = test.ConvertTo<TestModel>();
                    lblResults.Text = testModel.Dump();

                    //var test = TestPropertySetter();
                    lblResults.Text = test.Dump();

                    //var greeting = await gateway.SayHello(txtName.Text);
                    //lblResults.Text = greeting;
                }
                catch (Exception ex)
                {
                    var lbl = ex.ToString();
                    lbl.Print();

                    lblResults.Text = ex.ToString();
                }
            };
        }

        private static Test TestPropertySetter()
        {
            var nameProperty = typeof (Test).GetProperty("Name");

            var instance = Expression.Parameter(typeof (object), "i");
            var argument = Expression.Parameter(typeof (object), "a");

            var instanceParam = Expression.Convert(instance, nameProperty.ReflectedType());
            var valueParam = Expression.Convert(argument, nameProperty.PropertyType);

            var setterCall = Expression.Call(instanceParam, nameProperty.SetMethod(), valueParam);

            var fn = Expression.Lambda<Action<object, object>>(setterCall, instance, argument).Compile();

            var test = new Test();
            fn(test, "Foo");
            return test;
        }
    }
}

