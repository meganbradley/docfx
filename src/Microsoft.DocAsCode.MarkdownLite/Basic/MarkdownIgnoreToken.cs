﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    public sealed class MarkdownIgnoreToken : IMarkdownToken
    {
        public MarkdownIgnoreToken(IMarkdownRule rule, IMarkdownContext context, string rawMarkdown)
        {
            Rule = rule;
            RawMarkdown = rawMarkdown;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public string RawMarkdown { get; set; }
    }
}
