using System;
using System.IO;
using System.Text.RegularExpressions;
using ServiceStack.Logging;

namespace ServiceStack.Common.Services.Resources
{
	public class StringResourceManager
	{
		private ILogFactory LogFactory { get; set; }
		private readonly ILog log;

		public StringResourceManager(ILogFactory logFactory)
		{
			this.LogFactory = logFactory;
			this.log = this.LogFactory.GetLogger(GetType());

			this.Errors = new StringResources<int>("error", this.LogFactory);
		}

		public StringResources<int> Errors { get; private set; }

		private delegate void AddKeyValue(string key, string value);

		public void LoadTextFile(string path, char keyValueSeparator)
		{
			using (TextReader reader = new StreamReader(path, true))
			{
				Regex categoryRegex = new Regex(@"\[(?<Category>([^\]])*)\]");

				AddKeyValue AddResource = null; 
				string input = reader.ReadLine();

				while (input != null)
				{
					string[] tokens = input.Trim().Split(new[] { keyValueSeparator }, StringSplitOptions.RemoveEmptyEntries);
					
					if (tokens.Length == 1)
					{
						Match match = categoryRegex.Match(tokens[0]);
						if (match.Success)
						{
							AddResource = SetAddResourceDelegate(match.Groups["Category"].Value);
						}
					}
					else if (AddResource != null && tokens.Length > 1)
					{
						AddResource.Invoke(tokens[0], tokens[1]);
					}

					input = reader.ReadLine();
				}
			}

			this.log.WarnFormat("Loaded {0} error string resources from '{1}'", this.Errors.Count, path);
		}

		private AddKeyValue SetAddResourceDelegate(string category)
		{
			string upperCategory = category.Trim().ToUpper();
			switch (upperCategory)
			{
				case "ERRORS":
				{
					return (key, value) => this.Errors[int.Parse(key)] = value; 
				}
				default:
				{
					return null;
				}
			}
		}
	}
}