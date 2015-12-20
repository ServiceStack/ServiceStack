using System;
using System.Drawing;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using PclTest.SharedLogic;
using ServiceStack;
using PclTest.ServiceModel;

namespace PclTest.Ios
{
	public partial class PclTest_IosViewController : UIViewController
	{
		JsonServiceClient client;
	    SharedGateway gateway = new SharedGateway();

		public PclTest_IosViewController () : base ("PclTest_IosViewController", null)
		{
            //IosPclExportClient.Configure();
            //IosPclExportWithXml.Configure();
            client = new JsonServiceClient("http://localhost:81/");
		}

		public override void DidReceiveMemoryWarning ()
		{
			// Releases the view if it doesn't have a superview.
			base.DidReceiveMemoryWarning ();
			
			// Release any cached data, images, etc that aren't in use.
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			
			// Perform any additional setup after loading the view, typically from a nib.
		}

		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			// Return true for supported orientations
			return (toInterfaceOrientation != UIInterfaceOrientation.PortraitUpsideDown);
		}

		partial void btnGoSync_Click (NSObject sender)
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

        partial void btnGoAsync_Click(NSObject sender)
        {
            client.GetAsync(new Hello { Name = txtName.Text })
                .Success(response => lblResults.Text = response.Result)
                .Error(ex => lblResults.Text = ex.ToString());
        }

        //async partial void btnGoShared_Click(NSObject sender)
        //{
        //    try
        //    {
        //        var greeting = gateway.SayHello(txtName.Text);
        //        lblResults.Text = greeting;
        //    }
        //    catch (Exception ex)
        //    {
        //        lblResults.Text = ex.ToString();
        //    }
        //}
    }
}

