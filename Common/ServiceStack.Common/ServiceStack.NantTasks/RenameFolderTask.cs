using System.IO;
using NAnt.Core;
using NAnt.Core.Attributes;

namespace ServiceStack.NantTasks
{
    [TaskName("renameFolder")]
	public class RenameFolderTask : Task 
	{
	    [TaskAttribute("from", Required = true)]
	    public string From { get; set; }

	    [TaskAttribute("to", Required = true)]
	    public string To { get; set; }

	    protected override void ExecuteTask() {
			Directory.Move(From, To);
		}
	}
}