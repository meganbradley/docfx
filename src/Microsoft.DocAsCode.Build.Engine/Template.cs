// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Utility;

    public class Template
    {
        private const string Primary = ".primary";
        private const string Auxiliary = ".aux";
        private static readonly Regex IsRegexPatternRegex = new Regex(@"^\s*/(.*)/\s*$", RegexOptions.Compiled);
        private readonly object _locker = new object();
        private readonly ResourcePoolManager<ITemplateRenderer> _rendererPool = null;

        private readonly ResourcePoolManager<ITemplatePreprocessor> _preprocessorPool = null;
        private readonly string _script;

        public string Name { get; }
        public string ScriptName { get; }
        public string Extension { get; }
        public string Type { get; }
        public TemplateType TemplateType { get; }
        public IEnumerable<TemplateResourceInfo> Resources { get; }

        public Template(string name, TemplateRendererResource templateResource, TemplatePreprocessorResource scriptResource, ResourceCollection resourceCollection, int maxParallelism)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
            var templateInfo = GetTemplateInfo(Name);
            Extension = templateInfo.Extension;
            Type = templateInfo.DocumentType;
            TemplateType = templateInfo.TemplateType;
            _script = scriptResource?.Content;
            if (!string.IsNullOrWhiteSpace(_script))
            {
                ScriptName = Name + ".js";
                _preprocessorPool = ResourcePool.Create(() => CreatePreprocessor(resourceCollection, scriptResource), maxParallelism);
                try
                {
                    using (_preprocessorPool.Rent())
                    {
                    }
                }
                catch (Exception e)
                {
                    _preprocessorPool = null;
                    Logger.LogWarning($"{ScriptName} is not a valid template preprocessor, ignored: {e.Message}");
                }
            }

            if (!string.IsNullOrEmpty(templateResource?.Content) && resourceCollection != null)
            {
                _rendererPool = ResourcePool.Create(() => CreateRenderer(resourceCollection, templateResource), maxParallelism);
            }

            Resources = ExtractDependentResources(Name);
        }

        /// <summary>
        /// Transform from raw model to view model
        /// TODO: refactor to merge model and attrs into one input model
        /// </summary>
        /// <param name="model">The raw model</param>
        /// <param name="attrs">The system generated attributes</param>
        /// <returns>The view model</returns>
        public object TransformModel(object model, object attrs, object global)
        {
            if (_preprocessorPool == null) return model;
            using (var lease = _preprocessorPool.Rent())
            {
                return lease.Resource.Process(model, attrs, global);
            }
        }

        /// <summary>
        /// Transform from view model to the final result using template
        /// Supported template languages are mustache and liquid
        /// </summary>
        /// <param name="model">The input view model</param>
        /// <returns>The output after applying template</returns>
        public string Transform(object model)
        {
            if (_rendererPool == null || model == null) return null;
            using (var lease = _rendererPool.Rent())
            {
                return lease.Resource.Render(model);
            }
        }

        private string GetRelativeResourceKey(string templateName, string relativePath)
        {
            if (string.IsNullOrEmpty(templateName))
            {
                return relativePath;
            }
            // Make sure resource keys are combined using '/'
            return Path.GetDirectoryName(templateName).ToNormalizedPath().ForwardSlashCombine(relativePath);
        }

        private static TemplateInfo GetTemplateInfo(string templateName)
        {
            // Remove folder and .tmpl
            templateName = Path.GetFileName(templateName);
            var splitterIndex = templateName.IndexOf('.');
            if (splitterIndex < 0)
            {
                return new TemplateInfo(templateName, string.Empty, TemplateType.Default);
            }

            var type = templateName.Substring(0, splitterIndex);
            var extension = templateName.Substring(splitterIndex);
            TemplateType templateType = TemplateType.Default;
            if (extension.EndsWith(Primary))
            {
                templateType = TemplateType.Primary;
                extension = extension.Substring(0, extension.Length - Primary.Length);
            }
            else if (extension.EndsWith(Auxiliary))
            {
                templateType = TemplateType.Auxiliary;
                extension = extension.Substring(0, extension.Length - Auxiliary.Length);
            }

            return new TemplateInfo(type, extension, templateType);
        }

        /// <summary>
        /// Dependent files are defined in following syntax in Mustache template leveraging Mustache Comments
        /// {{! include('file') }}
        /// file path can be wrapped by quote ' or double quote " or none
        /// </summary>
        /// <param name="template"></param>
        private IEnumerable<TemplateResourceInfo> ExtractDependentResources(string templateName)
        {
            if (_rendererPool == null) yield break;
            using (var lease = _rendererPool.Rent())
            {
                var _renderer = lease.Resource;
                if (_renderer.Dependencies == null) yield break;
                foreach (var dependency in _renderer.Dependencies)
                {
                    string filePath = dependency;
                    if (string.IsNullOrWhiteSpace(filePath)) continue;
                    if (filePath.StartsWith("./")) filePath = filePath.Substring(2);
                    var regexPatternMatch = IsRegexPatternRegex.Match(filePath);
                    if (regexPatternMatch.Groups.Count > 1)
                    {
                        filePath = regexPatternMatch.Groups[1].Value;
                        yield return new TemplateResourceInfo(GetRelativeResourceKey(templateName, filePath), filePath, true);
                    }
                    else
                    {
                        yield return new TemplateResourceInfo(GetRelativeResourceKey(templateName, filePath), filePath, false);
                    }
                }
            }
        }

        private static ITemplatePreprocessor CreatePreprocessor(ResourceCollection resourceCollection, TemplatePreprocessorResource scriptResource)
        {
            return new TemplateJintPreprocessor(resourceCollection, scriptResource);
        }

        private static ITemplateRenderer CreateRenderer(ResourceCollection resourceCollection, TemplateRendererResource templateResource)
        {
            if (templateResource.Type == TemplateRendererType.Liquid)
            {
                return LiquidTemplateRenderer.Create(resourceCollection, templateResource.Content);
            }
            else
            {
                return new MustacheTemplateRenderer(resourceCollection, templateResource.Content);
            }
        }

        private sealed class TemplateInfo
        {
            public string DocumentType { get; }
            public string Extension { get; }
            public TemplateType TemplateType { get; }

            public TemplateInfo(string documentType, string extension, TemplateType type)
            {
                DocumentType = documentType;
                Extension = extension;
                TemplateType = type;
            }
        }
    }
}
