#if NET472 || NET6_0_OR_GREATER
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ServiceStack.Messaging;

namespace ServiceStack;

// sync with ClientDiagnosticUtils
public static class CommonDiagnosticUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Init(this DiagnosticListener listener, IMessage msg)
    {
        if (listener.IsEnabled(Diagnostics.Events.ServiceStack.WriteMqRequestBefore))
        {
            var activity = Activity.Current;
            if (activity != null)
            {
                msg.TraceId ??= activity.GetTraceId();
                msg.Tag ??= activity.GetTag();
            }
        }
    }
}
#endif