﻿// Copyright 2019 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Reflection;
using JetBrains.Annotations;
using Nuke.Common.IO;
using Nuke.Common.Utilities;

namespace Nuke.Common.ProjectModel
{
	/// <summary>
	///     Injects an instance of <see cref="Solution"/>. The solution path is resolved in the following order:
	///     <ul>
	///         <li>From the constructor argument</li>
	///         <li>From command-line arguments (e.g., <c>-[MemberName] path/to/solution.sln</c>)</li>
	///         <li>From environment variables (e.g., <c>[MemberName]=path/to/solution.sln</c>)</li>
	///         <li>From the <c>.nuke</c> configuration file</li>
	///     </ul>
	/// </summary>
	/// <example>
	///     <code>
	/// [Solution("common.sln")] readonly Solution Solution;
	///     </code>
	/// </example>
	[PublicAPI]
    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public class SolutionAttribute : ParameterAttribute
    {
        private readonly string _relativePath;

        public SolutionAttribute()
            : this(relativePath: null)
        {
        }

        public SolutionAttribute(string relativePath)
            : base("Path to a solution file that is automatically loaded."
                   + (relativePath != null ? $" Default is {relativePath}." : string.Empty))
        {
            _relativePath = relativePath;
        }

        public override bool List { get; set; }
        public bool GenerateProjects { get; set; }

        public override object GetValue(MemberInfo member, object instance)
        {
            var solutionFile = TryGetSolutionFileFromNukeFile() ??
                               GetSolutionFileFromParametersFile(member);
            var deserializer = typeof(SolutionSerializer).GetMethod(nameof(SolutionSerializer.DeserializeFromFile)).NotNull()
                .MakeGenericMethod(member.GetMemberType());
            return deserializer.Invoke(obj: null, new object[] { solutionFile });
        }
        
        private string GetSolutionFileFromParametersFile(MemberInfo member)
        {
            return _relativePath != null
                ? PathConstruction.Combine(NukeBuild.RootDirectory, _relativePath)
                : EnvironmentInfo.GetParameter<AbsolutePath>(member).NotNull($"No solution file defined for '{member.Name}'.");
        }
        
        private string TryGetSolutionFileFromNukeFile() {
	        throw new NotImplementedException("Reading from .nuke file not supported");
            /*
            var nukeFile = Path.Combine(NukeBuild.RootDirectory, Constants.NukeFileName);
            if (!File.Exists(nukeFile))
                return null;

            var solutionFileRelative = File.ReadAllLines(nukeFile).ElementAtOrDefault(0);
            ControlFlow.Assert(solutionFileRelative != null && !solutionFileRelative.Contains(value: '\\'),
                $"First line of {Constants.NukeFileName} must provide solution path using UNIX separators");

            var solutionFile = Path.GetFullPath(Path.Combine(NukeBuild.RootDirectory, solutionFileRelative));
            ControlFlow.Assert(File.Exists(solutionFile),
                $"Solution file '{solutionFile}' provided via {Constants.NukeFileName} does not exist.");

            return (AbsolutePath) solutionFile;

            */
        }
        
    }
}
