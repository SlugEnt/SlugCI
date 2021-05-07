// Copyright 2019 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;

namespace Nuke.Common.Tools.GitVersion
{
    partial class GitVersionSettings
    {
        private string GetProcessToolPath()
        {
            return GitVersionTasks_Custom.GetToolPath(Framework);
        }
    }

    partial class GitVersionTasks_Custom
    {
        /// <summary>
        
        /// </summary>
        /// <param name="framework"></param>
        /// <returns></returns>
        internal static string GetToolPath(string framework = null) {
	        // MODIFIED: We only support dotnet tool installs now...
	        return "dotnet-gitversion";
            
        }

        [CanBeNull]
        private static GitVersion GetResult(IProcess process, GitVersionSettings toolSettings)
        {
            try
            {
                var output = process.Output.EnsureOnlyStd().Select(x => x.Text).ToList();
                var settings = new JsonSerializerSettings { ContractResolver = new AllWritableContractResolver() };
                return JsonConvert.DeserializeObject<GitVersion>(string.Join("\r\n", output), settings);
            }
            catch (Exception exception)
            {
                throw new Exception($"{nameof(GitVersion)} exited with code {process.ExitCode}, but cannot parse output as JSON:"
                        .Concat(process.Output.Select(x => x.Text)).JoinNewLine(),
                    exception);
            }
        }
    }
}
