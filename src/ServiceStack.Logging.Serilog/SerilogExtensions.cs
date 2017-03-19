// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/. 
namespace ServiceStack.Logging.Serilog
{
    using System;
    using System.Collections.Generic;
    using global::Serilog.Core;

    public static class SerilogExtensions
    {
        public static void Debug(this ILog logger, Exception ex, string messageTemplate, params object[] propertyValues)
        {
            (logger as SerilogLogger)?.Debug(ex, messageTemplate, propertyValues);
        }

        public static void Info(this ILog logger, Exception ex, string messageTemplate, params object[] propertyValues)
        {
            (logger as SerilogLogger)?.Info(ex, messageTemplate, propertyValues);
        }

        public static void Warn(this ILog logger, Exception ex, string messageTemplate, params object[] propertyValues)
        {
            (logger as SerilogLogger)?.Warn(ex, messageTemplate, propertyValues);
        }

        public static void Error(this ILog logger, Exception ex, string messageTemplate, params object[] propertyValues)
        {
            (logger as SerilogLogger)?.Error(ex, messageTemplate, propertyValues);
        }

        public static void Fatal(this ILog logger, Exception ex, string messageTemplate, params object[] propertyValues)
        {
            (logger as SerilogLogger)?.Fatal(ex, messageTemplate, propertyValues);
        }

        public static ILog ForContext(this ILog logger, Type type)
        {
            return (logger as SerilogLogger)?.ForContext(type);
        }

        public static ILog ForContext<T>(this ILog logger)
        {
            return (logger as SerilogLogger)?.ForContext<T>();
        }

        public static ILog ForContext(this ILog logger, ILogEventEnricher enricher)
        {
            return (logger as SerilogLogger)?.ForContext(enricher);
        }

        public static ILog ForContext(this ILog logger, IEnumerable<ILogEventEnricher> enrichers)
        {
            return (logger as SerilogLogger)?.ForContext(enrichers);
        }

        public static ILog ForContext(this ILog logger, string propertyName, object value, bool destructureObjects = false)
        {
            return (logger as SerilogLogger)?.ForContext(propertyName, value, destructureObjects);
        }
    }
}