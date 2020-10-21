﻿using Orchard;
using Orchard.ContentManagement;
using Orchard.Environment.Configuration;
using Orchard.Environment.Descriptor;
using Orchard.Environment.Extensions;
using Orchard.Environment.State;
using Orchard.Taxonomies.Models;
using Orchard.Taxonomies.Services;
using Orchard.Validation;
using System;
using System.Collections.Generic;

namespace Piedone.HelpfulExtensions.Taxonomies
{
    [OrchardFeature(Constants.FeatureNames.Taxonomies)]
    public class TaxonomySetupService : ITaxonomySetupService, ITaxonomySetupHandler
    {
        private readonly ShellSettings _shellSettings;
        private readonly IShellDescriptorManager _shellDescriptorManager;
        private readonly IProcessingEngine _processingEngine;
        private readonly IWorkContextAccessor _wca;


        public TaxonomySetupService(
            ShellSettings shellSettings,
            IShellDescriptorManager shellDescriptorManager,
            IProcessingEngine processingEngine,
            IWorkContextAccessor wca)
        {
            _shellSettings = shellSettings;
            _shellDescriptorManager = shellDescriptorManager;
            _processingEngine = processingEngine;
            _wca = wca;
        }


        public void CreateTaxonomies(params string[] names)
        {
            _processingEngine.AddTask(
                _shellSettings,
                _shellDescriptorManager.GetShellDescriptor(),
                $"{nameof(ITaxonomySetupHandler)}.{nameof(ITaxonomySetupHandler.ApplyTaxonomiesCreation)}",
                new Dictionary<string, object> { { "names", names } });
        }

        public void ApplyTaxonomiesCreation(string[] names)
        {
            var workContextScope = _wca.GetContext();
            var taxonomyService = workContextScope.Resolve<ITaxonomyService>();
            var contentManager = workContextScope.Resolve<IContentManager>();
            foreach (var name in names)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentNullException("name", "The name of the Taxonomy must be specified.");
                }

                if (taxonomyService.GetTaxonomyByName(name) == null)
                {
                    contentManager.Create<TaxonomyPart>("Taxonomy", taxonomy => taxonomy.Name = name);
                }
            }
        }

        public void CreateTaxonomiesWithTerms(string taxonomyName, List<string> termNames)
        {
            if (!string.IsNullOrEmpty(taxonomyName))
            {
                _processingEngine.AddTask(
                    _shellSettings,
                    _shellDescriptorManager.GetShellDescriptor(),
                    $"{nameof(ITaxonomySetupHandler)}.{nameof(ITaxonomySetupHandler.ApplyTaxonomyCreationWithTerms)}",
                    new Dictionary<string, object> { { "taxonomyName", taxonomyName }, { "termNames", termNames } });
            }
        }

        public void ApplyTaxonomyCreationWithTerms(string taxonomyName, List<string> termNames)
        {
            if (string.IsNullOrEmpty(taxonomyName))
            {
                Argument.ThrowIfNullOrEmpty(taxonomyName, nameof(taxonomyName));
            }

            var workContextScope = _wca.GetContext();
            var taxonomyService = workContextScope.Resolve<ITaxonomyService>();
            var contentManager = workContextScope.Resolve<IContentManager>();

            if (taxonomyService.GetTaxonomyByName(taxonomyName) == null)
            {
                contentManager.Create<TaxonomyPart>("Taxonomy", taxonomy => taxonomy.Name = taxonomyName);
            }

            var currentTaxonomy = taxonomyService.GetTaxonomyByName(taxonomyName);
            TermPart currentTerm;

            foreach (var term in termNames)
            {
                currentTerm = taxonomyService.NewTerm(currentTaxonomy);
                currentTerm.Name = term;
                contentManager.Create(currentTerm);
            }
        }
    }
}