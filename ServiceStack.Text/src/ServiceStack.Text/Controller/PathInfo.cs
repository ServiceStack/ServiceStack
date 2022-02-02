//
// https://github.com/ServiceStack/ServiceStack.Text
// ServiceStack.Text: .NET C# POCO JSON, JSV and CSV Text Serializers.
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2012 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceStack.Text.Controller
{
	/// <summary>
	/// Class to hold  
	/// </summary>
	public class PathInfo
	{
		public string ControllerName { get; private set; }

		public string ActionName { get; private set; }

		public List<string> Arguments { get; private set; }

		public Dictionary<string, string> Options { get; private set; }


		public PathInfo(string actionName, params string[] arguments)
			: this(actionName, arguments.ToList(), null)
		{
		}

		public PathInfo(string actionName, List<string> arguments, Dictionary<string, string> options)
		{
			ActionName = actionName;
			Arguments = arguments ?? new List<string>();
			Options = options ?? new Dictionary<string, string>();
		}

		public string FirstArgument
		{
			get
			{
				return this.Arguments.Count > 0 ? this.Arguments[0] : null;
			}
		}

		public T GetArgumentValue<T>(int index)
		{
			return TypeSerializer.DeserializeFromString<T>(this.Arguments[index]);
		}

		/// <summary>
		/// Parses the specified path info.
		/// e.g.
		///		MusicPage/arg1/0/true?debug&showFlows=3 => PathInfo
		///			.ActionName = 'MusicPage'
		///			.Arguments = ['arg1','0','true']
		///			.Options = { debug:'True', showFlows:'3' }
		/// </summary>
		/// <param name="pathUri">The path url.</param>
		/// <returns></returns>
		public static PathInfo Parse(string pathUri)
		{
			var actionParts = pathUri.Split(new[] { "://" }, StringSplitOptions.None);
			var controllerName = actionParts.Length == 2
									? actionParts[0]
									: null;

			var pathInfo = actionParts[actionParts.Length - 1];

			var optionMap = new Dictionary<string, string>();

			var optionsPos = pathInfo.LastIndexOf('?');
			if (optionsPos != -1)
			{
				var options = pathInfo.Substring(optionsPos + 1).Split('&');
				foreach (var option in options)
				{
					var keyValuePair = option.Split('=');

					optionMap[keyValuePair[0]] = keyValuePair.Length == 1
													? true.ToString()
													: keyValuePair[1].UrlDecode();
				}
				pathInfo = pathInfo.Substring(0, optionsPos);
			}

			var args = pathInfo.Split('/');
			var pageName = args[0];

			return new PathInfo(pageName, args.Skip(1).ToList(), optionMap) {
				ControllerName = controllerName
			};
		}
	}
}