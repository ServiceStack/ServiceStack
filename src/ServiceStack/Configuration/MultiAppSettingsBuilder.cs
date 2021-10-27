using System.Collections.Generic;

namespace ServiceStack.Configuration
{
    /// <summary>
    /// Represents a builder for the <see cref="MultiAppSettings"/> class.
    /// </summary>
    public class MultiAppSettingsBuilder
    {
        private readonly Queue<IAppSettings> appSettingsQueue = new Queue<IAppSettings>();
        private readonly string tier;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiAppSettingsBuilder"/> class with a specified <paramref name="tier"/>.
        /// </summary>
        /// <param name="tier">The <paramref name="tier"/> of the <see cref="MultiAppSettingsBuilder"/>.</param>
        public MultiAppSettingsBuilder(string tier = null)
        {
            this.tier = tier;
        }

        /// <summary>
        /// Adds an <see cref="AppSettings"/> that reads configuration values from the Web.config file.
        /// </summary>
        /// <returns>The <see cref="MultiAppSettingsBuilder"/>.</returns>
        public MultiAppSettingsBuilder AddAppSettings()
        {
            return AddAppSettings(tier);
        }

        /// <summary>
        /// Adds an <see cref="AppSettings"/> that reads configuration values from the Web.config file.
        /// </summary>
        /// <param name="tier">The tier of the <see cref="AppSettings"/>.</param>
        /// <returns>The <see cref="MultiAppSettingsBuilder"/>.</returns>
        public MultiAppSettingsBuilder AddAppSettings(string tier)
        {
            appSettingsQueue.Enqueue(
                new AppSettings(tier)
            );

            return this;
        }

        /// <summary>
        /// Adds a <see cref="DictionarySettings"/> that reads configuration values from a dictionary.
        /// </summary>
        /// <param name="map">The dictionary of settings to add.</param>
        /// <returns>The <see cref="MultiAppSettingsBuilder"/>.</returns>
        public MultiAppSettingsBuilder AddDictionarySettings(Dictionary<string, string> map)
        {
            appSettingsQueue.Enqueue(
                new DictionarySettings(map)
            );

            return this;
        }

        /// <summary>
        /// Adds an <see cref="EnvironmentVariableSettings"/> that reads configuration values from environmental variables.
        /// </summary>
        /// <returns>The <see cref="MultiAppSettingsBuilder"/>.</returns>
        public MultiAppSettingsBuilder AddEnvironmentalVariables()
        {
            return AddEnvironmentalVariables(tier);
        }

        /// <summary>
        /// Adds an <see cref="EnvironmentVariableSettings"/> that reads configuration values from environmental variables.
        /// </summary>
        /// <param name="tier">The tier of the <see cref="AppSettings"/>.</param>
        /// <returns>The <see cref="MultiAppSettingsBuilder"/>.</returns>
        public MultiAppSettingsBuilder AddEnvironmentalVariables(string tier)
        {
            appSettingsQueue.Enqueue(
                new EnvironmentVariableSettings { Tier = tier }
            );

            return this;
        }

        /// <summary>
        /// Adds an <see cref="TextFileSettings"/> that reads configuration values from a text file at <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The path of the text file.</param>
        /// <returns>The <see cref="MultiAppSettingsBuilder"/>.</returns>
        public MultiAppSettingsBuilder AddTextFile(string path)
        {
            return AddTextFile(path, " ", tier);
        }

        /// <summary>
        /// Adds an <see cref="TextFileSettings"/> that reads configuration values from a text file at <paramref name="path"/> with a specified <paramref name="delimeter"/>.
        /// </summary>
        /// <param name="path">The path of the text file.</param>
        /// <param name="delimeter">The delimeter fo the text file.</param>
        /// <returns>The <see cref="MultiAppSettingsBuilder"/>.</returns>
        public MultiAppSettingsBuilder AddTextFile(string path, string delimeter)
        {
            return AddTextFile(path, delimeter, tier);
        }

        /// <summary>
        /// Adds an <see cref="TextFileSettings"/> that reads configuration values from a text file at <paramref name="path"/> with a specified <paramref name="delimeter"/>.
        /// </summary>
        /// <param name="path">The path of the text file.</param>
        /// <param name="delimeter">The delimeter fo the text file.</param>
        /// <param name="tier">The tier of the <see cref="TextFileSettings"/>.</param>
        /// <returns>The <see cref="MultiAppSettingsBuilder"/>.</returns>
        public MultiAppSettingsBuilder AddTextFile(string path, string delimeter, string tier)
        {
            appSettingsQueue.Enqueue(
                new TextFileSettings(path, delimeter) { Tier = tier }
            );

            return this;
        }
        
#if NETCORE
        public MultiAppSettingsBuilder AddNetCore(Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            appSettingsQueue.Enqueue(
                new NetCoreAppSettings(configuration)
            );

            return this;
        }
#endif

        /// <summary>
        /// Builds an <see cref="IAppSettings"/>.
        /// </summary>
        /// <returns>An <see cref="IAppSettings"/>.</returns>
        public IAppSettings Build()
        {
            return new MultiAppSettings(
                appSettingsQueue.ToArray()
            )
            {
                Tier = tier
            };
        }
    }
}
