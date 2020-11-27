﻿/*
 * Copyright 2020 Mikhail Shiryaev
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * 
 * 
 * Product  : Rapid SCADA
 * Module   : ScadaCommEngine
 * Summary  : Represents a wrapper for safely calling methods of device logic
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2020
 * Modified : 2020
 */

using Scada.Comm.Drivers;
using Scada.Data.Models;
using Scada.Log;
using System;
using System.IO;
using System.Text;

namespace Scada.Comm.Engine
{
    /// <summary>
    /// Represents a wrapper for safely calling methods of device logic.
    /// <param>Представляет обёртку для безопасного выполнения методов логики устройства.</param>
    /// </summary>
    internal class DeviceWrapper
    {
        private readonly ILog log; // the communication line log


        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public DeviceWrapper(DeviceLogic deviceLogic, ILog log)
        {
            DeviceLogic = deviceLogic ?? throw new ArgumentNullException(nameof(deviceLogic));
            InfoFileName = "";
            this.log = log ?? throw new ArgumentNullException(nameof(log));
        }


        /// <summary>
        /// Gets the device logic.
        /// </summary>
        public DeviceLogic DeviceLogic { get; }

        /// <summary>
        /// Gets or sets the full file name to write device information.
        /// </summary>
        public string InfoFileName { get; set;  }


        /// <summary>
        /// Calls the OnCommLineStart method of the device.
        /// </summary>
        public void OnCommLineStart()
        {
            try
            {
                DeviceLogic.OnCommLineStart();
            }
            catch (Exception ex)
            {
                log.WriteException(ex, CommPhrases.ErrorInDevice, nameof(OnCommLineStart), DeviceLogic.Title);
            }
        }

        /// <summary>
        /// Calls the OnCommLineTerminate method of the device.
        /// </summary>
        public void OnCommLineTerminate()
        {
            try
            {
                DeviceLogic.OnCommLineTerminate();
            }
            catch (Exception ex)
            {
                log.WriteException(ex, CommPhrases.ErrorInDevice, nameof(OnCommLineTerminate), DeviceLogic.Title);
            }
        }

        /// <summary>
        /// Calls the Bind method of the device.
        /// </summary>
        public void Bind(BaseDataSet baseDataSet)
        {
            try
            {
                if (DeviceLogic.IsBound)
                    DeviceLogic.Bind(baseDataSet);
            }
            catch (Exception ex)
            {
                log.WriteException(ex, CommPhrases.ErrorInDevice, nameof(Bind), DeviceLogic.Title);
            }
        }

        /// <summary>
        /// Calls the Session method of the device.
        /// </summary>
        public void Session()
        {
            try
            {
                DeviceLogic.Session();
            }
            catch (Exception ex)
            {
                log.WriteException(ex, CommPhrases.ErrorInDevice, nameof(Session), DeviceLogic.Title);
            }
        }

        /// <summary>
        /// Calls the SendCommand method of the device.
        /// </summary>
        public void SendCommand(TeleCommand cmd)
        {
            try
            {
                DeviceLogic.SendCommand(cmd);
            }
            catch (Exception ex)
            {
                log.WriteException(ex, CommPhrases.ErrorInDevice, nameof(SendCommand), DeviceLogic.Title);
            }
        }

        /// <summary>
        /// Writes device information to the file.
        /// </summary>
        public void WriteInfo()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(InfoFileName, false, Encoding.UTF8))
                {
                    writer.Write(DeviceLogic.GetInfo());
                }
            }
            catch (Exception ex)
            {
                log.WriteException(ex, Locale.IsRussian ?
                    "Ошибка при записи в файл информации о работе КП {0}" :
                    "Error writing device {0} information to the file", DeviceLogic.Title);
            }
        }
    }
}