using System;
using System.Collections.Generic;
using System.Configuration;
using ServiceStack.Common.Services.Support.Config;
using ServiceStack.Common.Services.Config;

namespace ServiceStack.Common.Services.Utils
{
    /// <summary>
    /// Utilities for creating instances of types
    /// </summary>
    public class FactoryUtils 
    {
        private const string ERROR_APP_SETTING_NOT_FOUND = "AppSetting {0} not specified";
        private const string ERROR_TYPE_NOT_FOUND = "Could not find type {0}";
        private const string ERROR_CREATING_TYPE = "Error creating {0} type: '{1}'";
        private const string ERROR_CONFIG_SECTION_NOT_FOUND = "The config section {0} is not defined";

        /// <summary>
        /// Creates a type from an AppSetting in App.Config file
        /// Format: [assemblyName],[typeName]
        /// </summary>
        /// <typeparam name="T">Type to create</typeparam>
        /// <param name="appSettingName">AppSetting name in App.Config file</param>
        /// <returns>Instance of type T</returns>
        public static T CreateFromAppSetting<T>(string appSettingName) where T : class
        {
            string typeName = ConfigurationManager.AppSettings[appSettingName];
            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentException(string.Format(ERROR_APP_SETTING_NOT_FOUND, appSettingName));
            }
            try
            {
                return Create<T>(typeName);
            }
            catch (Exception e)
            {
                throw new TypeLoadException(string.Format(ERROR_CREATING_TYPE, typeof(T).Name, typeName), e);
            }
        }

        /// <summary>
        /// Creates an object factory from an object definition stored in the App.config file.
        /// <![CDATA[
        ///   <objects>
        ///     <object name="CustomerServiceClient" type="ServiceStack.Common.Services.Client.WebServiceClient, ServiceStack.Common.Services">
        ///        <property name="UseBasicHttp" value="true"/>
        ///     </object>
        ///   </objects>
        /// ]]>
        /// 
        /// The syntax is compatible with the objects defintion defined in:
        /// http://www.springframework.net/doc/reference/html/springobjectsxsd.html
        /// </summary>
        /// <returns>Instance of T</returns>
        public static IObjectFactory CreateObjectFactoryFromConfig(string sectionName)
        {
            var objectTypes = ConfigurationManager.GetSection(sectionName) as Dictionary<string, ObjectConfigurationType>;

            if (objectTypes == null)
            {
                throw new ConfigurationErrorsException(
                    string.Format(ERROR_CONFIG_SECTION_NOT_FOUND, sectionName));
            }
            return CreateObjectFactoryFromConfig(objectTypes);
        }

        /// <summary>
        /// Creates an IObjectFactory from the supplied objectTypes.
        /// Provided so the IObjectFactory can be created without an App.config (i.e. so it can be tested)
        /// </summary>
        /// <param name="objectTypes">The object types.</param>
        /// <returns></returns>
        public static IObjectFactory CreateObjectFactoryFromConfig(Dictionary<string, ObjectConfigurationType> objectTypes)
        {
            if (objectTypes == null)
            {
                throw new ArgumentNullException("objectTypes");
            }
            return new ObjectConfigurationTypeFactory(objectTypes);
        }


        /// <summary>
        /// Creates an instance of Type T from list of loaded assemblies
        /// </summary>
        /// <typeparam name="T">Type to create</typeparam>
        /// <param name="factoryTypeName">Name of type to create</param>
        /// <returns>Instance of Type T</returns>
        public static T Create<T>(string factoryTypeName) where T : class
        {
            Type factoryType = AssemblyUtils.FindType(factoryTypeName);
            if (factoryType == null)
            {
                throw new TypeLoadException(string.Format(ERROR_TYPE_NOT_FOUND, factoryTypeName));
            }
            return Create<T>(factoryType);
        }

        /// <summary>
        /// Creates the specified factory type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="factoryType">Type of the factory.</param>
        /// <returns></returns>
        public static T Create<T>(Type factoryType)
        {
            return (T)Activator.CreateInstance(factoryType);
        }
    }
}