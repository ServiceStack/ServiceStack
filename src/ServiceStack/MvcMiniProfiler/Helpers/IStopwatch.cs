using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace MvcMiniProfiler.Helpers
{

    internal interface IStopwatch
    {
        long ElapsedTicks { get; }
        long Frequency { get; }
        bool IsRunning { get; }
        void Stop();
    }

    internal class StopwatchWrapper : IStopwatch
    {
        public static IStopwatch StartNew()
        {
            return new StopwatchWrapper();
        }

        private Stopwatch _sw;

        private StopwatchWrapper()
        {
            _sw = Stopwatch.StartNew();
        }

        public long ElapsedTicks
        {
            get { return _sw.ElapsedTicks; }
        }

        public long Frequency
        {
            get { return Stopwatch.Frequency; }
        }

        public bool IsRunning
        {
            get { return _sw.IsRunning; }
        }

        public void Stop()
        {
            _sw.Stop();
        }
    }

}
