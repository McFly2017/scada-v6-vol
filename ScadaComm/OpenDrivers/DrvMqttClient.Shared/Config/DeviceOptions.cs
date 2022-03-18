﻿// Copyright (c) Rapid Software LLC. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Xml;

namespace Scada.Comm.Drivers.DrvMqttClient.Config
{
    internal class DeviceOptions
    {
        /// <summary>
        /// Gets or sets the root topic used as a prefix for all device topics.
        /// </summary>
        public string RootTopic { get; set; } = "";


        /// <summary>
        /// Loads the options from the XML node.
        /// </summary>
        public void LoadFromXml(XmlNode xmlNode)
        {
            ArgumentNullException.ThrowIfNull(xmlNode, nameof(xmlNode));
            RootTopic = xmlNode.GetChildAsString("RootTopic");
        }

        /// <summary>
        /// Saves the options into the XML node.
        /// </summary>
        public void SaveToXml(XmlElement xmlElem)
        {
            ArgumentNullException.ThrowIfNull(xmlElem, nameof(xmlElem));
            xmlElem.AppendElem("RootTopic", RootTopic);
        }
    }
}
