﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace ODataValidator.Rule
{
    #region Namespaces
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Net;
    using ODataValidator.Rule.Helper;
    using ODataValidator.RuleEngine;
    
    #endregion

    /// <summary>
    /// Class of service implemenation feature to verify an Asynchronous Get request.
    /// </summary>
    [Export(typeof(ExtensionRule))]
    public class ServiceImpl_AsynchronousRequest : ServiceImplExtensionRule
    {
        /// <summary>
        /// Gets rule name
        /// </summary>
        public override string Name
        {
            get
            {
                return "ServiceImpl_AsynchronousRequest";
            }
        }

        /// <summary>
        /// Gets rule description
        /// </summary>
        public override string Description
        {
            get
            {
                return this.CategoryInfo.CategoryFullName + ",Asynchronous Requests";
            }
        }

        /// <summary>
        /// Gets rule specification in OData document
        /// </summary>
        public override string V4SpecificationSection
        {
            get
            {
                return "11.6";
            }
        }

        /// <summary>
        /// Gets the requirement level.
        /// </summary>
        public override RequirementLevel RequirementLevel
        {
            get
            {
                return RequirementLevel.Should;
            }
        }

        /// <summary>
        /// Gets the service implementation category.
        /// </summary>
        public override ServiceImplCategory CategoryInfo
        {
            get
            {
                return new ServiceImplCategory(ServiceImplCategoryName.AsynchronousRequests, null);
            }
        }

        /// <summary>
        /// Verifies the extension rule.
        /// </summary>
        /// <param name="context">The Interop service context</param>
        /// <param name="info">out parameter to return violation information when rule does not pass</param>
        /// <returns>true if rule passes; false otherwise</returns>
        public override bool? Verify(ServiceContext context, out ExtensionRuleViolationInfo info)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            bool? passed = null;
            ExtensionRuleResultDetail detail = new ExtensionRuleResultDetail(this.Name);
            var entitySets = MetadataHelper.GetFeeds(context.MetadataDocument);

            if (null == entitySets || !entitySets.Any())
            {
                detail.ErrorMessage = "Cannot find any entity-sets in metadata document.";
                info = new ExtensionRuleViolationInfo(context.Destination, context.ResponsePayload, detail);
                
                return passed;
            }

            string entitySet = string.Empty;

            foreach (var set in entitySets)
            {
                if (true == set.IsSupportAsynchronousOperation(context.MetadataDocument, new List<string>() { context.VocCapabilities }))
                {
                    entitySet = set;
                    break;
                }
            }

            if (string.IsNullOrEmpty(entitySet))
            {
                detail.ErrorMessage = "Cannot find an appropriate entity-set which supports asynchronous operation in metadata document.";
                info = new ExtensionRuleViolationInfo(context.Destination, context.ResponsePayload, detail);

                return passed;
            }

            string url = string.Format("{0}/{1}", context.ServiceBaseUri, entitySet);
            List<KeyValuePair<string, string>> requestHeaders = new List<KeyValuePair<string,string>>();

            foreach (KeyValuePair<string, string> kvp in context.RequestHeaders)
            {
                requestHeaders.Add(new KeyValuePair<string, string>(kvp.Key, kvp.Value));
            }

            // Add respond-async preference in request header.
            KeyValuePair<string, string> prefer = new KeyValuePair<string, string>("Prefer", "respond-async");
            requestHeaders.Add(prefer);
            Response response = WebHelper.Get(new Uri(url), Constants.V4AcceptHeaderJsonFullMetadata, RuleEngineSetting.Instance().DefaultMaximumPayloadSize, requestHeaders);
            detail = new ExtensionRuleResultDetail(this.Name, url, "GET", StringHelper.MergeHeaders(Constants.V4AcceptHeaderJsonFullMetadata, requestHeaders), response);

            if (response != null )
            {
                if(response.StatusCode == HttpStatusCode.Accepted)
                {
                    passed = true;
                }
                else if (!string.IsNullOrEmpty(response.ResponseHeaders))
                {
                    string preferHeader = response.ResponseHeaders.GetHeaderValue("Preference-Applied");                    

                    if (!string.IsNullOrEmpty(preferHeader) && preferHeader.Contains("respond-async"))
                    {
                        passed = true;
                    }
                    else
                    {
                        passed = false;
                        detail.ErrorMessage = "The service does not support Asynchronous operations.";
                    }
                }
            }
            else
            {
                detail.ErrorMessage = "The service does not support Asynchronous operations.";
            }

            info = new ExtensionRuleViolationInfo(context.Destination, context.ResponsePayload, detail);
            return passed;
        }
    }
}
