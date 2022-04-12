﻿// Copyright (c) Rapid Software LLC. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Xml;

namespace Scada.Comm.Drivers.DrvMqttClient.Config
{
    /// <summary>
    /// Represents a subscription configuration.
    /// <para>Представляет конфигурацию подписки.</para>
    /// </summary>
    [Serializable]
    public class SubscriptionConfig : BaseItemConfig
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public SubscriptionConfig()
        {
            TagCode = "";
            ReadOnly = false;
            JsEnabled = false;
            JsFileName = "";
            SubItems = new List<string>();
        }


        /// <summary>
        /// Gets or sets the tag code associated with the topic.
        /// </summary>
        public string TagCode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the topic is read only.
        /// </summary>
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to execute JavaScript when a message is received.
        /// </summary>
        public bool JsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the JavaScript file name.
        /// </summary>
        public string JsFileName { get; set; }

        /// <summary>
        /// Gets the subitems that represent multiple device tags for the topic.
        /// </summary>
        public List<string> SubItems { get; private set; }
        
        
        /// <summary>
        /// Loads the configuration from the XML node.
        /// </summary>
        public override void LoadFromXml(XmlElement xmlElem)
        {
            base.LoadFromXml(xmlElem);
            TagCode = xmlElem.GetAttrAsString("tagCode");
            ReadOnly = xmlElem.GetAttrAsBool("readOnly");
            JsEnabled = xmlElem.GetAttrAsBool("jsEnabled");
            JsFileName = xmlElem.GetAttrAsString("jsFileName");

            foreach (XmlNode subItemNode in xmlElem.SelectNodes("SubItem"))
            {
                SubItems.Add(subItemNode.InnerText);
            }
        }

        /// <summary>
        /// Saves the configuration into the XML node.
        /// </summary>
        public override void SaveToXml(XmlElement xmlElem)
        {
            base.SaveToXml(xmlElem);
            xmlElem.SetAttribute("tagCode", TagCode);
            xmlElem.SetAttribute("readOnly", ReadOnly);
            xmlElem.SetAttribute("jsEnabled", JsEnabled);
            xmlElem.SetAttribute("jsFileName", JsFileName);

            foreach (string subItem in SubItems)
            {
                xmlElem.AppendElem("SubItem").InnerText = subItem;
            }
        }
    }
}
