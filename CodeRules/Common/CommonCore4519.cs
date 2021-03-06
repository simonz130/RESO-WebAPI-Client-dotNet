﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace ODataValidator.Rule
{
    #region Namespaces
    using System;
    using System.ComponentModel.Composition;
    using System.Net;
    using Newtonsoft.Json.Linq;
    using ODataValidator.RuleEngine;
    
    #endregion

    /// <summary>
    /// Class of code rule applying to feed payload.  
    /// </summary>
    [Export(typeof(ExtensionRule))]
    public class CommonCore4519_Feed : CommonCore4519
    {
        /// <summary>
        /// Gets the payload type to which the rule applies.
        /// </summary>
        public override PayloadType? PayloadType
        {
            get
            {
                return RuleEngine.PayloadType.Feed;
            }
        }
    }

    /// <summary>
    /// Class of code rule applying to entity reference payload.  
    /// </summary>
    [Export(typeof(ExtensionRule))]
    public class CommonCore4519_Entry : CommonCore4519
    {
        /// <summary>
        /// Gets the payload type to which the rule applies.
        /// </summary>
        public override PayloadType? PayloadType
        {
            get
            {
                return RuleEngine.PayloadType.Entry;
            }
        }
    }

    /// <summary>
    /// Class of extension rule for Common.Core.4519
    /// </summary>
    public abstract class CommonCore4519 : ExtensionRule
    {
        /// <summary>
        /// Gets Category property
        /// </summary>
        public override string Category
        {
            get
            {
                return "core";
            }
        }

        /// <summary>
        /// Gets rule name
        /// </summary>
        public override string Name
        {
            get
            {
                return "Common.Core.4519";
            }
        }

        /// <summary>
        /// Gets rule description
        /// </summary>
        public override string Description
        {
            get
            {
                return "The full list of annotations that may appear in an odata.metadata=full response are include odata.associationLink.";
            }
        }

        /// <summary>
        /// Gets rule specification in OData document
        /// </summary>
        public override string V4SpecificationSection
        {
            get
            {
                return "3.1.2";
            }
        }

        /// <summary>
        /// Gets the version.
        /// </summary>
        public override ODataVersion? Version
        {
            get
            {
                return ODataVersion.V3_V4;
            }
        }

        /// <summary>
        /// Gets location of help information of the rule
        /// </summary>
        public override string HelpLink
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the error message for validation failure
        /// </summary>
        public override string ErrorMessage
        {
            get
            {
                return this.Description;
            }
        }

        /// <summary>
        /// Gets the requirement level.
        /// </summary>
        public override RequirementLevel RequirementLevel
        {
            get
            {
                return RequirementLevel.May;
            }
        }

        /// <summary>
        /// Gets the payload format to which the rule applies.
        /// </summary>
        public override PayloadFormat? PayloadFormat
        {
            get
            {
                return RuleEngine.PayloadFormat.JsonLight;
            }
        }

        /// <summary>
        /// Gets the RequireMetadata property to which the rule applies.
        /// </summary>
        public override bool? RequireMetadata
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the flag whether this rule applies to offline context
        /// </summary>
        public override bool? IsOfflineContext
        {
            get
            {
                return false;
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
            info = null;
            JObject jo;
            context.ResponsePayload.TryToJObject(out jo);

            string acceptHeader = Constants.V4AcceptHeaderJsonFullMetadata;
            string associationLinkAnnotation = Constants.OdataAssociationLinkPropertyNameSuffix;

            if (context.Version == ODataVersion.V3)
            {
                acceptHeader = Constants.V3AcceptHeaderJsonFullMetadata;
                associationLinkAnnotation = Constants.OdataAssociationLinkPropertyNameSuffix + "Url";
            } 

            if (context.PayloadType == RuleEngine.PayloadType.Feed || context.PayloadType == RuleEngine.PayloadType.Entry)
            {
                if (context.OdataMetadataType != ODataMetadataType.FullOnly)
                {
                    Response response = WebHelper.Get(context.Destination, acceptHeader, RuleEngineSetting.Instance().DefaultMaximumPayloadSize, context.RequestHeaders);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        response.ResponsePayload.TryToJObject(out jo);
                    }
                }

                if (context.PayloadType == RuleEngine.PayloadType.Entry)
                {
                    passed = false;

                    foreach (JProperty jPro in jo.Children())
                    {
                        if (jPro.Name.EndsWith(associationLinkAnnotation))
                        {
                            passed = true;
                            break;
                        }
                    }
                }
                else
                {
                    foreach (JObject ob in (JArray)jo[Constants.Value])
                    {
                        passed = false;

                        foreach (JProperty jPro in ob.Children())
                        {
                            if (jPro.Name.EndsWith(associationLinkAnnotation))
                            {
                                passed = true;
                                break;
                            }
                        }

                        if (passed == false)
                        {
                            break;
                        }
                    }
                }

                if (passed == false)
                {
                    info = new ExtensionRuleViolationInfo(this.ErrorMessage, context.Destination, context.ResponsePayload);
                }
            }

            return passed;
        }
    }
}
