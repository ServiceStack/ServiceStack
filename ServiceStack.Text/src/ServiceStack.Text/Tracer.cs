using System;

namespace ServiceStack.Text
{
    public class Tracer
    {
        public static ITracer Instance = new NullTracer();

        public class NullTracer : ITracer
        {
            public void WriteDebug(string error) { }

            public void WriteDebug(string format, params object[] args) { }

            public void WriteWarning(string warning) { }

            public void WriteWarning(string format, params object[] args) { }

            public void WriteError(Exception ex)
            {
                if (JsConfig.ThrowOnError)
                    throw ex;
            }

            public void WriteError(string error)
            {
                if (JsConfig.ThrowOnError)
                    throw new Exception(error);
            }

            public void WriteError(string format, params object[] args)
            {
                if (JsConfig.ThrowOnError)
                    throw new Exception(string.Format(format, args));
            }
        }

        public class ConsoleTracer : ITracer
        {
            public void WriteDebug(string error)
            {
                PclExport.Instance.WriteLine(error);
            }

            public void WriteDebug(string format, params object[] args)
            {
                PclExport.Instance.WriteLine(format, args);
            }

            public void WriteWarning(string warning)
            {
                PclExport.Instance.WriteLine(warning);
            }

            public void WriteWarning(string format, params object[] args)
            {
                PclExport.Instance.WriteLine(format, args);
            }

            public void WriteError(Exception ex)
            {
                PclExport.Instance.WriteLine(ex.ToString());
            }

            public void WriteError(string error)
            {
                PclExport.Instance.WriteLine(error);
            }

            public void WriteError(string format, params object[] args)
            {
                PclExport.Instance.WriteLine(format, args);
            }
        }
    }

    public static class TracerExceptions
    {
        public static T Trace<T>(this T ex) where T : Exception
        {
            Tracer.Instance.WriteError(ex);
            return ex;
        }
    }
}