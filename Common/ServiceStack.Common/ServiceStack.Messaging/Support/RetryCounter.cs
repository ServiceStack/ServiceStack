using System;
using System.Collections.Generic;
using System.Threading;
using ServiceStack.Logging;

namespace ServiceStack.Messaging.Support
{
	public class RetryCounter
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(RetryCounter));
		private readonly FailoverSettings failoverSettings;

		private TimeSpan lastDelay;

		public RetryCounter(FailoverSettings failoverSettings)
		{
			this.failoverSettings = failoverSettings;
			this.TotalRetryAttempts = 0;
			this.RetryAttempts = 0;
		}

		public int RetryAttempts { get; private set; }

		public int TotalRetryAttempts { get; private set; }

		public void Reset()
		{
			this.RetryAttempts = 0;
		}

		public bool Retry()
		{
			this.TotalRetryAttempts++;
			this.RetryAttempts++;

			if (failoverSettings.MaxReconnectAttempts.HasValue 
				&& this.RetryAttempts >= failoverSettings.MaxReconnectAttempts.Value)
			{
				return false;
			}
			if (failoverSettings.UseExponentialBackOff)
			{
				lastDelay = TimeSpan.FromMilliseconds(lastDelay.TotalMilliseconds * failoverSettings.BackOffMultiplier);
				if (lastDelay.TotalMilliseconds > failoverSettings.MaxReconnectDelay.TotalMilliseconds)
				{
					lastDelay = failoverSettings.MaxReconnectDelay;
				}
			}
			Log.WarnFormat("Retrying in {0}ms, for the {1} time.", lastDelay.TotalMilliseconds, this.RetryAttempts);
			Thread.Sleep(lastDelay);
			return true;
		}
	}


	public class FailoverSettings
	{
		public FailoverSettings()
		{
			this.BrokerUris = new List<string>();
			this.InitialReconnectDelay = TimeSpan.FromMilliseconds(100);
			this.MaxReconnectDelay = TimeSpan.FromSeconds(10);
			this.UseExponentialBackOff = true;
			this.BackOffMultiplier = 2;
			this.MaxReconnectAttempts = null;
		}

		/// <summary>
		/// Gets or sets the broker uris.
		/// e.g. tcp://localhost, tcp://wwvis7020
		/// </summary>
		/// <value>The broker uris.</value>
		public List<string> BrokerUris
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets or sets the initial reconnect delay.
		/// </summary>
		/// <value>The initial reconnect delay.</value>
		public TimeSpan InitialReconnectDelay { get; set; }

		/// <summary>
		/// Gets or sets the max reconnect delay.
		/// </summary>
		/// <value>The max reconnect delay.</value>
		public TimeSpan MaxReconnectDelay { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether to use an exponential back off.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if [use exponential back off]; otherwise, <c>false</c>.
		/// </value>
		public bool UseExponentialBackOff { get; set; }

		/// <summary>
		/// Gets or sets the back off multiplier.
		/// </summary>
		/// <value>The back off multiplier.</value>
		public int BackOffMultiplier { get; set; }

		/// <summary>
		/// Gets or sets the max reconnect attempts.
		/// </summary>
		/// <value>The max reconnect attempts.</value>
		public int? MaxReconnectAttempts { get; set; }

		/// <summary>
		/// Loads the specified settings.
		/// </summary>
		/// <param name="settings">The settings.</param>
		public void Load(FailoverSettings settings)
		{
			this.BackOffMultiplier = settings.BackOffMultiplier;
			this.BrokerUris = settings.BrokerUris;
			this.InitialReconnectDelay = settings.InitialReconnectDelay;
			this.MaxReconnectAttempts = settings.MaxReconnectAttempts;
			this.MaxReconnectDelay = settings.MaxReconnectDelay;
			this.MaxReconnectAttempts = settings.MaxReconnectAttempts;
		}
	}

}