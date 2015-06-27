using NScumm.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NScumm.MonoGame
{
    class Trace : ITrace
    {
        readonly IEnableTrace _source;
        readonly HashSet<string> _switches;

        public Trace(IEnableTrace trace, IEnumerable<string> switches)
        {
            _source = trace;
            _switches = new HashSet<string>(switches, StringComparer.OrdinalIgnoreCase);
        }

        public void Write(string traceName, string format, params object[] args)
        {
            if (_switches.Contains(traceName))
            {
                Debug.WriteLine("{0,-16} {1}", _source.GetType().Name, string.Format(format, args));
            }
        }
    }

    public class TraceFactory : ITraceFactory
    {
        readonly IEnumerable<string> _switches;

        public TraceFactory(params string[] switches)
        {
            _switches = switches;
        }

        public ITrace CreateTrace(IEnableTrace trace)
        {
            return new Trace(trace, _switches);
        }
    }
}
