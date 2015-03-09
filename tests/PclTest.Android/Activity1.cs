using System;
using System.Collections.Generic;
using Android.App;
using Android.Widget;
using Android.OS;
using PclTest.ServiceModel;
using PclTest.SharedLogic;
using ServiceStack;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace PclTest.Android
{
    [Flags]
    public enum DbModelComparisonTypesEnum : int
    {
        /// <summary>
        /// Compare only the PrimaryKey values
        /// </summary>
        PkOnly = 1,
        /// <summary>
        /// Compare only the non PrimaryKey values
        /// </summary>
        NonPkOnly = 2,
        /// <summary>
        /// Compare all values
        /// (The PrimaryKey and non PrimaryKey values too)
        /// </summary>
        All = 3 // PkOnly & NonPkOnly
    }

    public partial class Question
    {
        public static DbModelComparisonTypesEnum DefaultComparisonType { get; set; }

        public Guid Id { get; set; }
        public string Title { get; set; }
    }

    [Activity(Label = "PclTest.Android", MainLauncher = true, Icon = "@drawable/icon")]
    public class Activity1 : Activity
    {
        TextView lblResults = null;

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
            lblResults = FindViewById<TextView>(Resource.Id.lblResults);

            //10.0.2.2 = loopback
            //http://developer.android.com/tools/devices/emulator.html
            var client = new JsonServiceClient("http://10.0.2.2:81/");
            var gateway = new SharedGateway("http://10.0.2.2:81/");
            LogManager.LogFactory = new LogFactory(AddMessage);

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

            btnGoShared.Click += async delegate
            {
                try
                {
                    var greeting = await gateway.SayHello(txtName.Text);
                    lblResults.Text = greeting;
                }
                catch (Exception ex)
                {
                    var lbl = ex.ToString();
                    lbl.Print();

                    lblResults.Text = ex.ToString();
                }
            };

            btnTest.Click += delegate
            {
                try
                {
                    //var dto = new Question
                    //{
                    //    Id = Guid.NewGuid(),
                    //    Title = "Title",
                    //};

                    //var json = dto.ToJson();
                    //var q = json.FromJson<Question>();
                    //lblResults.Text = "{0}:{1}".Fmt(q.Id, q.Title);

                    ConnectServerEvents();
                }
                catch (Exception ex)
                {
                    lblResults.Text = ex.ToString();
                }
            };
        }

        void AddMessage(string message)
        {
            RunOnUiThread(() => {
                lblResults.Text = "{0}  {1}\n".Fmt(DateTime.Now.ToLongTimeString(), message) + lblResults.Text;
            });
        }

        private static ServerEventsClient serverEventsClient = null;
        static ServerEventConnect connectMsg = null;
        static List<ServerEventMessage> msgs = new List<ServerEventMessage>();
        static List<ServerEventMessage> commands = new List<ServerEventMessage>();
        static List<System.Exception> errors = new List<System.Exception>();
        static string lastText = "";

        class LogFactory : ILogFactory 
        {
            public Action<string> OnMessage;

            public LogFactory(Action<string> onMessage)
            {
                OnMessage = onMessage;
            }

            public ILog GetLogger(Type type)
            {
                return new GenericLog(type) { OnMessage = OnMessage };
            }

            public ILog GetLogger(string typeName)
            {
                return new GenericLog(typeName) { OnMessage = OnMessage };
            }
        }

        private void ConnectServerEvents()
        {
            if (serverEventsClient == null)
            {
                // var client = new ServerEventsClient("http://chat.servicestack.net", channels: "home")

                // bla.ybookz.com is a copy of the SS Chat sample, that sends 'HAHAHAHA' every second to all listeners
                //serverEventsClient = new ServerEventsClient("http://bla.ybookz.com/", channels: "home")
                //serverEventsClient = new ServerEventsClient("http://10.0.2.2:1337/", channels: "home")
                serverEventsClient = new ServerEventsClient("http://chat.servicestack.net", channels: "home")
                {
                    OnConnect = e => {
                        connectMsg = e;
                    },
                    OnCommand = a => {
                        commands.Add(a);
                    },
                    OnHeartbeat = () => {

                        RunOnUiThread(() => {
                            try
                            {
                                Toast.MakeText(this, "Heartbeat", ToastLength.Short).Show();
                            }
                            catch {}
                        });
                    },
                    OnMessage = a => {
                        msgs.Add(a);
                        if (lastText != a.Data)
                        {
                            lastText = a.Data ?? "";
                            RunOnUiThread(() => {
                                try
                                {
                                    Toast.MakeText(this, lastText, ToastLength.Short).Show();
                                }
                                catch {}
                            });
                        }
                    },
                    OnException = ex => {
                        AddMessage("OnException: " + ex.Message);
                        errors.Add(ex);
                    },
                    HeartbeatRequestFilter = x => {
                        AddMessage("HeartbeatRequestFilter");
                    },
                };

                AddMessage("Started Listening...");
                serverEventsClient.Start();
            }
        }
    }
}

