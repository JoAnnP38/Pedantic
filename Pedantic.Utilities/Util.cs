// ***********************************************************************
// Assembly         : Pedantic.Utilities
// Author           : JoAnn D. Peeler
// Created          : 01-17-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 01-17-2023
// ***********************************************************************
// <copyright file="Util.cs" company="Pedantic.Utilities">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary>
//     Methods used to ensure correctness of the application in real-time.
// </summary>
// ***********************************************************************
#undef DEBUG
using System.Diagnostics;

namespace Pedantic.Utilities
{
    public static class Util
    {
        public sealed class AssertionException : Exception
        {
            public AssertionException() : base() { }

            public AssertionException(string message) : base(message)
            { }

            public AssertionException(string message, Exception inner) : base(message, inner)
            { }

            public AssertionException(string message, string detailedMessage)
                : base(message)
            {
                DetailedMessage = detailedMessage;
            }

            public AssertionException(string message, string detailedMessage, Exception inner)
                : base(message, inner)
            {
                DetailedMessage = detailedMessage;
            }

            public string DetailedMessage
            {
                get => Data["DetailedMessage"] as string ?? string.Empty;
                set => Data["DetailedMessage"] = value;
            }
        }

#pragma warning disable CA2211
        public static TraceSwitch TraceSwitch = new("General", "Entire Application")
        {
            Level = TraceLevel.Verbose
        };
#pragma warning restore CA2211

        [Conditional("DEBUG")]
        public static void Assert(bool condition)
        {
            if (Debugger.IsAttached)
            {
                Debug.Assert(condition);
            }
            else if (!condition)
            {
                throw new AssertionException();
            }
        }

        [Conditional("DEBUG")]
        public static void Assert(bool condition, string message)
        {
            if (Debugger.IsAttached)
            {
                Debug.Assert(condition, message);
            }
            else if (!condition)
            {
                throw new AssertionException(message);
            }
        }

        [Conditional("DEBUG")]
        public static void Fail(string message)
        {
            if (Debugger.IsAttached)
            {
                Debug.Fail(message);
            }
            else
            {
                throw new AssertionException(message);
            }
        }

        [Conditional("DEBUG")]
        public static void Fail(string message, string detailedMessage)
        {
            if (Debugger.IsAttached)
            {
                Debug.Fail(message, detailedMessage);
            }
            else
            {
                throw new AssertionException(message, detailedMessage);
            }
        }

        [Conditional("TRACE")]
        public static void TraceInfo(string message)
        {
            if (TraceSwitch.TraceInfo)
            {
                Trace.TraceInformation(message);
            }
        }

        [Conditional("TRACE")]
        public static void TraceInfo(string format, params object[] args)
        {
            if (TraceSwitch.TraceInfo)
            {
                Trace.TraceInformation(format, args);
            }
        }

        [Conditional("TRACE")]
        public static void TraceWarning(string message)
        {
            if (TraceSwitch.TraceWarning)
            {
                Trace.TraceWarning(message);
            }
        }

        [Conditional("TRACE")]
        public static void TraceWarning(string format, params object[] args)
        {
            if (TraceSwitch.TraceWarning)
            {
                Trace.TraceWarning(format, args);
            }
        }

        [Conditional("TRACE")]
        public static void TraceError(string message)
        {
            if (TraceSwitch.TraceError)
            {
                Trace.TraceError(message);
            }
        }

        [Conditional("TRACE")]
        public static void TraceError(string format, params object[] args)
        {
            if (TraceSwitch.TraceError)
            {
                Trace.TraceError(format, args);
            }
        }

        [Conditional("TRACE")]
        public static void Indent()
        {
            if (TraceSwitch.TraceVerbose)
            {
                Trace.Indent();
            }
        }

        [Conditional("TRACE")]
        public static void Unindent()
        {
            if (TraceSwitch.TraceVerbose)
            {
                Trace.Unindent();
            }
        }

        [Conditional("TRACE")]
        public static void Write(string message)
        {
            if (TraceSwitch.TraceVerbose)
            {
                Trace.Write(message);
            }
        }

        [Conditional("TRACE")]
        public static void WriteIf(bool condition, string message)
        {
            if (TraceSwitch.TraceVerbose)
            {
                Trace.WriteIf(condition, message);
            }
        }

        [Conditional("TRACE")]
        public static void WriteLine()
        {
            if (TraceSwitch.TraceVerbose)
            {
                Trace.Write(Environment.NewLine);
            }
        }

        [Conditional("TRACE")]
        public static void WriteLine(string message)
        {
            if (TraceSwitch.TraceVerbose)
            {
                Trace.WriteLine(message);
            }
        }

        [Conditional("TRACE")]
        public static void WriteLineIf(bool condition, string message)
        {
            if (TraceSwitch.TraceVerbose)
            {
                Trace.WriteLineIf(condition, message);
            }
        }
    }
}
