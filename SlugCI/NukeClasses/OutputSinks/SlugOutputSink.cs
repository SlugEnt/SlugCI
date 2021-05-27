// Copyright 2019 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.Build.Tasks;
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
    public abstract class SlugOutputSink
    {
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


        /// <summary>
        /// Writes End of slugBuilder Summary info.
        /// </summary>
        internal virtual void WriteSummary (ExecutionPlan plan, bool isInteractive) {
	        Console.WriteLine();
	        Misc.WriteFinalHeader(plan.PlanStatus);

	        if ( !isInteractive ) {
		        if ( SevereMessages.Count > 0 ) {
			        WriteNormal();
			        WriteSevereMessages();
		        }
	        }


			WriteNormal();
            WriteSummaryTable(plan);
            WriteNormal();
            
            if (plan.WasSuccessful)
                WriteSuccessfulBuild();
            else
                WriteFailedBuild();

            if ( isInteractive ) {
	            bool continueLooping = true;
	            while ( continueLooping ) {
		            Console.WriteLine("Press (x) to exit, (d) to view detailed error information");
		            ConsoleKeyInfo keyInfo = Console.ReadKey();
		            if ( keyInfo.Key == ConsoleKey.X ) return;
		            if ( keyInfo.Key == ConsoleKey.D ) {
                        WriteNormal();
                        WriteSevereMessages();
                        return;
		            }
	            }
            }
        }


        protected virtual void WriteSuccessfulBuild()
        {
            WriteSuccess($"Build succeeded on {DateTime.Now.ToString(CultureInfo.CurrentCulture)}");
        }

        protected virtual void WriteFailedBuild()
        {
            WriteError($"Build failed on {DateTime.Now.ToString(CultureInfo.CurrentCulture)}");
        }


        /// <summary>
        /// Writes the Summary of the Slug Build Process
        /// </summary>
        /// <param name="StageStats"></param>
        protected virtual void WriteSummaryTable(ExecutionPlan plan) {
	        List<BuildStage> StageStats = plan.Plan.ToList();

            // MODIFIED: This has been customized
            int firstColumn = Math.Max(StageStats.Max(x => x.Name.Length) + 4, 20);
            int secondColumn = 10;
            int thirdColumn = 10;
            int totalColWidth = firstColumn + secondColumn + thirdColumn;
            //long totalDuration = StageStats.Aggregate((long) 0, (t, x) => t += x.RunTimeDuration());   //(0, (t, x) => t.Add(x.RunTimeDuration()));


            // FX Creates a writable line
            string CreateLine (string name, string status, string duration) => 
	            name.PadRight(firstColumn, paddingChar: ' ') +
	            status.PadRight(secondColumn, paddingChar: ' ') +
	            duration.PadLeft(thirdColumn, paddingChar: ' ');


            // FX builds duration value
            static string GetDurationOrBlank(BuildStage target)
                => target.CompletionStatus == StageCompletionStatusEnum.Success ||
                    target.CompletionStatus == StageCompletionStatusEnum.Failure ||
					target.CompletionStatus == StageCompletionStatusEnum.Warning ||
                    target.CompletionStatus == StageCompletionStatusEnum.Warning
	                ? GetDuration(target.RunTimeDuration())
                    : string.Empty;


            // FX Builds the duration from the Runtime
            static string GetDuration(long duration)
                => $"{(long) (duration / 1000)}".Replace("0", "< 1sec");


            /// Begin writing table
            WriteNormal(new string(c: '═', count: totalColWidth));
            WriteInformation(CreateLine("Target", "Status", "Duration"));
            WriteNormal(new string(c: '─', count: totalColWidth));

            long totalDuration = 0;
            foreach ( BuildStage stageStat in StageStats ) {
                string line = CreateLine(stageStat.Name, stageStat.CompletionStatus.ToString(), GetDurationOrBlank(stageStat));
                totalDuration += stageStat.RunTimeDuration();

                switch (stageStat.CompletionStatus)
                {
                    case StageCompletionStatusEnum.Skipped:
                        WriteNormal(line);
                        break;
                    case StageCompletionStatusEnum.Success:
                        WriteSuccess(line);
                        break;
                    case StageCompletionStatusEnum.Aborted:
                        WriteWarning(line);
                        break;
                    case StageCompletionStatusEnum.Failure:
                        WriteError(line);
                        break;
                    default:
                        throw new NotSupportedException(stageStat.CompletionStatus.ToString());
                }
            }


            WriteNormal(new string(c: '─', count: totalColWidth));
            WriteInformation(CreateLine("Total", string.Empty, GetDuration(totalDuration)));
            WriteNormal(new string(c: '═', count: totalColWidth));
            
        }

        protected virtual void WriteSevereMessages()
        {
            WriteInformation("Repeating warnings and errors:");

            foreach (OutputRecord record in SevereMessages)
            {
                switch (record.LogLevel)
                {
                    case LogLevel.Warning:
                        WriteWarning(record.Text,record.Details);
                        break;
                    case LogLevel.Error:
                        WriteError(record.Text, record.Details);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
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
	            WriteError("-->(ERROR):  " + text, details);
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

        protected abstract void WriteWarning(string text, string details = null);
        protected abstract void WriteError(string text, string details = null);
    }
}
