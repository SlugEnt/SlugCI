// Copyright 2019 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
/*using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.Tools.GitHub;
*/
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using Slug.CI;
using Console = Colorful.Console;

// MODIFIED:  Significant Modifications to this code.

namespace Nuke.Common.OutputSinks
{
	[PublicAPI]
    public abstract class SlugOutputSink {
	    private const string ERR_LINE = "********************************** ERROR ****************************************";


        public static SlugOutputSink Default
        {
            get
            {
                var term = Environment.GetEnvironmentVariable("TERM");
                return term == null || !term.StartsWithOrdinalIgnoreCase("xterm")
                    ? (SlugOutputSink) new SystemColorOutputSink()
                    : new AnsiColorOutputSink();
            }
        }

        
        internal readonly List<OutputRecord> SevereMessages = new List<OutputRecord>();


        internal virtual IDisposable WriteBlock(string text)
        {
            return DelegateDisposable.CreateBracket(
                () =>
                {
                    var formattedBlockText = FormatBlockText(text)
                        .Split(new[] { EnvironmentInfo.NewLine }, StringSplitOptions.None);

                    Console.WriteLine();
                    Console.WriteLine("╬" + new string(c: '═', text.Length + 5));
                    formattedBlockText.ForEach(x => Console.WriteLine($"║ {x}"));
                    Console.WriteLine("╬" + new string(c: '═', Math.Max(text.Length - 4, 2)));
                    Console.WriteLine();
                });
        }

        internal void WriteAndReportWarning(string text, string details = null)
        {
            SevereMessages.Add(new OutputRecord(LogLevel.Warning,text, details));
            ReportWarning(text, details);
            if (EnableWriteWarnings)
                WriteWarning(text, details);
        }

        internal void WriteAndReportError(string text, string details = null)
        {
	        SevereMessages.Add(new OutputRecord(LogLevel.Error, text, details));
            ReportError(text, details);
            if ( EnableWriteErrors ) {
	            Console.WriteLine();
	            string errMsg = ERR_LINE + Environment.NewLine + "-->(Error):  " + text + Environment.NewLine + ERR_LINE + Environment.NewLine;
	            WriteError(errMsg, details);
            }
        }


        internal void ReportErrorOnly (string text, string details) {
	        SevereMessages.Add(new OutputRecord(LogLevel.Error, text, details));
        }

        protected virtual string FormatBlockText(string text)
        {
            return text;
        }

        protected virtual bool EnableWriteWarnings => true;
        protected virtual bool EnableWriteErrors => true;

        protected virtual void ReportWarning(string text, string details = null)
        {
        }

        protected virtual void ReportError(string text, string details = null)
        {
        }

        protected void WriteNormal()
        {
            WriteNormal(string.Empty);
        }

        internal abstract void WriteNormal(string text);
        internal abstract void WriteSuccess(string text);
        internal abstract void WriteTrace(string text);
        internal abstract void WriteInformation(string text);

        public abstract void WriteWarning(string text, string details = null);
        public abstract void WriteError(string text, string details = null);
    }
}
