﻿// Copyright 2019 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using JetBrains.Annotations;

namespace Nuke.Common.Tooling
{
	[PublicAPI]
    public struct Output
    {
        public OutputType Type;
        public string Text;
        public override string ToString () { return "[" + Type + "] : " + Text; }
    }
}
