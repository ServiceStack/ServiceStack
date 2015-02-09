﻿using System;
using Android.App;
using Android.Widget;
using Android.OS;
using PclTest.ServiceModel;
using PclTest.SharedLogic;
using ServiceStack;
using ServiceStack.Text;

namespace PclTest.Android
{
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
        }

    }
}

