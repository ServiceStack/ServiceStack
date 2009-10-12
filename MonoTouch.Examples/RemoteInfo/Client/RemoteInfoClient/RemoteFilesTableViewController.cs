
using System;

using System;
using System.Collections.Generic;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

using RemoteInfo.ServiceModel.Operations;
using RemoteInfo.ServiceModel.Types;

namespace RemoteInfoClient
{
	/// <summary>
	/// Standard Table View Controller, for tutorials check out:
	/// 		- (video)  		http://www.vimeo.com/6689472
	/// 		- (tutorial) 	http://www.alexyork.net/blog/post/UINavigationController-with-MonoTouch-Building-a-simple-RSS-reader-Part-1.aspx
	/// </summary>
	public partial class RemoteFilesTableViewController : UITableViewController
	{
		static NSString kCellIdentifier = new NSString ("RFTVCIdentifier");

		public string CurrentPath { get; set; }
		
		public List<object> Items { get; set; }

		//
		// Constructor invoked from the NIB loader
		//
		public RemoteFilesTableViewController (IntPtr p) : base(p)
		{
		}
		
		public RemoteFilesTableViewController(string currentPath): base() 
		{
			this.CurrentPath = currentPath;			
		}	
		
		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			
			var directoryNames = (this.CurrentPath ?? string.Empty).Split('/');
			var path = directoryNames.Length > 0
				? directoryNames[directoryNames.Length - 1].Trim() : null;				
			
			Title = !string.IsNullOrEmpty(path) ? path : "/";
			
			var request = new GetDirectoryInfo { ForPath = this.CurrentPath };
			var response = AppConfig.ServiceClient.Send<GetDirectoryInfoResponse> (request);
			
			this.Items = new List<object>();
			response.Directories.ForEach(x => this.Items.Add(x));
			response.Files.ForEach(x => this.Items.Add(x));
			
			TableView.Delegate = new TableDelegate (this);
			TableView.DataSource = new DataSource (this);
		}
		

		//
		// The data source for our TableView
		//
		class DataSource : UITableViewDataSource
		{
			RemoteFilesTableViewController tvc;

			public DataSource (RemoteFilesTableViewController tvc)
			{
				this.tvc = tvc;
			}

			public override int RowsInSection (UITableView tableView, int section)
			{
				return tvc.Items.Count;
			}

			public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
			{
				var cell = tableView.DequeueReusableCell (kCellIdentifier);
				if (cell == null) {
					cell = new UITableViewCell (UITableViewCellStyle.Default, kCellIdentifier);
				}
				
				// Customize the cell here...
				
				var dirResult = tvc.Items[indexPath.Row] as DirectoryResult;
				if (dirResult != null)
				{
					cell.Accessory = UITableViewCellAccessory.DisclosureIndicator;
					
					cell.TextLabel.Text = string.Format("/{0} ({1})", dirResult.Name, dirResult.FileCount);
					var greenColor = new UIColor(0, 0.7f, 0, 1);
					cell.TextLabel.TextColor = greenColor;
				}
				else
				{
					var fileResult = (FileResult) tvc.Items[indexPath.Row];

					cell.Accessory = fileResult.IsTextFile 
						? UITableViewCellAccessory.DisclosureIndicator
						: UITableViewCellAccessory.None;

					cell.TextLabel.Text = fileResult.Name;
				}
				
				return cell;
			}
		}

		//
		// This class receives notifications that happen on the UITableView
		//
		class TableDelegate : UITableViewDelegate
		{
			RemoteFilesTableViewController tvc;

			public TableDelegate (RemoteFilesTableViewController tvc)
			{
				this.tvc = tvc;
			}

			public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
			{
				Console.WriteLine ("RemoteInfoClient: Row selected {0}", indexPath.Row);
				
				var dirResult = tvc.Items[indexPath.Row] as DirectoryResult;
				if (dirResult != null)
				{
					var nextPath = string.Format("{0}/{1}", tvc.CurrentPath, dirResult.Name);				
					tvc.NavigationController.PushViewController(new RemoteFilesTableViewController(nextPath), true); 
				}
				else
				{
					var fileResult = (FileResult) tvc.Items[indexPath.Row];
					if (!fileResult.IsTextFile) return;
					
					var request = new GetTextFile { AtPath = string.Format("{0}/{1}", tvc.CurrentPath, fileResult.Name) };
					var response = AppConfig.ServiceClient.Send<GetTextFileResponse>(request);

					Console.WriteLine("response for: " + request.AtPath + ", len: " + response.Contents.Length);
					
					var controller = new ViewTextFileController(fileResult.Name, response.Contents);
					tvc.NavigationController.PushViewController(controller, true);
				}
				
			}
		}
		
		
	}
}
