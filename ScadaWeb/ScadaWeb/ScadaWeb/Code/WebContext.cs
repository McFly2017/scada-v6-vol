﻿/*
 * Copyright 2021 Rapid Software LLC
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
 * Module   : Webstation Application
 * Summary  : Contains web application level data
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2021
 * Modified : 2021
 */

using Microsoft.Extensions.Primitives;
using Scada.Client;
using Scada.Config;
using Scada.Data.Entities;
using Scada.Data.Models;
using Scada.Data.Tables;
using Scada.Lang;
using Scada.Log;
using Scada.Web.Config;
using Scada.Web.Lang;
using Scada.Web.Plugins;
using Scada.Web.Services;
using System;
using System.IO;
using System.Threading;

namespace Scada.Web.Code
{
    /// <summary>
    /// Contains web application level data.
    /// <para>Содержит данные уровня веб-приложения.</para>
    /// </summary>
    internal class WebContext : IWebContext
    {
        /// <summary>
        /// Specifies the configuration update steps.
        /// </summary>
        private enum UpdateConfigStep { Idle, ReadBase }

        /// <summary>
        /// The period of attempts to read the configuration database.
        /// </summary>
        private static readonly TimeSpan ReadBasePeriod = TimeSpan.FromSeconds(10);

        private Thread configThread;                // the configuration update thread
        private volatile bool terminated;           // necessary to stop the thread
        private volatile bool pluginsReady;         // plugins are loaded
        private volatile bool configUpdateRequired; // indicates that the configuration should be updated


        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public WebContext()
        {
            configThread = null;
            terminated = false;
            pluginsReady = false;
            configUpdateRequired = false;

            IsReady = false;
            IsReadyToLogin = false;
            InstanceConfig = new InstanceConfig();
            AppConfig = new WebConfig();
            AppDirs = new WebDirs();
            Log = LogStub.Instance;
            BaseDataSet = new BaseDataSet();
            RightMatrix = new RightMatrix();
            DataFormatter = new CnlDataFormatter(BaseDataSet);
            ClientPool = new ScadaClientPool();
            PluginHolder = new PluginHolder();
            CacheExpirationTokenSource = new CancellationTokenSource();
        }


        /// <summary>
        /// Gets a value indicating whether the application is ready for operating.
        /// </summary>
        public bool IsReady { get; private set; }

        /// <summary>
        /// Gets a value indicating whether a user can login.
        /// </summary>
        public bool IsReadyToLogin { get; private set; }

        /// <summary>
        /// Gets the instance configuration.
        /// </summary>
        public InstanceConfig InstanceConfig { get; }

        /// <summary>
        /// Gets the application configuration.
        /// </summary>
        public WebConfig AppConfig { get; }

        /// <summary>
        /// Gets the application directories.
        /// </summary>
        public WebDirs AppDirs { get; }

        /// <summary>
        /// Gets the application log.
        /// </summary>
        public ILog Log { get; private set; }

        /// <summary>
        /// Gets the cached configuration database.
        /// </summary>
        public BaseDataSet BaseDataSet { get; private set; }

        /// <summary>
        /// Gets the access rights.
        /// </summary>
        public RightMatrix RightMatrix { get; private set; }

        /// <summary>
        /// Gets the channel data formatter.
        /// </summary>
        public CnlDataFormatter DataFormatter { get; private set; }

        /// <summary>
        /// Gets the client pool.
        /// </summary>
        public ScadaClientPool ClientPool { get; }

        /// <summary>
        /// Gets the object containing plugins.
        /// </summary>
        public PluginHolder PluginHolder { get; }

        /// <summary>
        /// Gets the source object that can send expiration notification to the memory cache.
        /// </summary>
        public CancellationTokenSource CacheExpirationTokenSource { get; }


        /// <summary>
        /// Loads the instance configuration.
        /// </summary>
        private void LoadInstanceConfig()
        {
            if (InstanceConfig.Load(Path.Combine(AppDirs.ExeDir, "..", "Config", InstanceConfig.DefaultFileName),
                out string errMsg))
            {
                Locale.SetCulture(InstanceConfig.Culture);
            }
            else
            {
                Log.WriteError(errMsg);
            }
        }

        /// <summary>
        /// Loads the application configuration.
        /// </summary>
        private void LoadAppConfig()
        {
            if (AppConfig.Load(Path.Combine(AppDirs.ConfigDir, WebConfig.DefaultFileName), out string errMsg))
            {
                if (Log is LogFile logFile)
                    logFile.Capacity = AppConfig.GeneralOptions.MaxLogSize;
            }
            else
            {
                Log.WriteError(errMsg);
            }
        }

        /// <summary>
        /// Localizes the application.
        /// </summary>
        private void LocalizeApp()
        {
            if (!Locale.LoadDictionaries(AppDirs.LangDir, "ScadaCommon", out string errMsg))
                Log.WriteError(errMsg);

            if (!Locale.LoadDictionaries(AppDirs.LangDir, "ScadaWeb", out errMsg))
                Log.WriteError(errMsg);

            CommonPhrases.Init();
            WebPhrases.Init();
        }

        /// <summary>
        /// Initializes plugins.
        /// </summary>
        private void InitPlugins()
        {
            PluginHolder.Log = Log;

            foreach (string pluginCode in AppConfig.PluginCodes)
            {
                if (PluginFactory.GetPluginLogic(AppDirs.ExeDir, pluginCode, this,
                    out PluginLogic pluginLogic, out string message))
                {
                    Log.WriteAction(message);
                    PluginHolder.AddPlugin(pluginLogic);
                }
                else
                {
                    Log.WriteError(message);
                }
            }
        }

        /// <summary>
        /// Updates the application culture according to the configuration.
        /// </summary>
        private void UpdateCulture()
        {
            string cultureName = ScadaUtils.FirstNonEmpty(
                InstanceConfig.Culture,
                AppConfig.GeneralOptions.DefaultCulture,
                Locale.DefaultCulture.Name);

            if (Locale.Culture.Name != cultureName)
            {
                Locale.SetCulture(cultureName);
                LocalizeApp();
            }
        }

        /// <summary>
        /// Stops the configuration update thread.
        /// </summary>
        private void StopProcessing()
        {
            try
            {
                if (configThread != null)
                {
                    terminated = true;
                    configThread.Join();
                    configThread = null;
                }
            }
            catch (Exception ex)
            {
                Log.WriteException(ex, Locale.IsRussian ?
                    "Ошибка при остановке обновления конфигурации" :
                    "Error stopping configuration update");
            }
        }

        /// <summary>
        /// Updates the configuration in a separate thread.
        /// </summary>
        private void ExecuteConfigUpdate()
        {
            LoadAppConfig();
            UpdateCulture();

            InitPlugins();
            PluginHolder.DefineFeaturedPlugins(AppConfig.PluginAssignment);
            PluginHolder.LoadDictionaries();
            PluginHolder.LoadConfig();
            pluginsReady = true;

            UpdateConfigStep step = UpdateConfigStep.Idle;
            DateTime readBaseDT = DateTime.MinValue;

            while (!terminated)
            {
                try
                {
                    switch (step)
                    {
                        case UpdateConfigStep.Idle:
                            if (configUpdateRequired)
                            {
                                configUpdateRequired = false;
                                step = UpdateConfigStep.ReadBase;
                            }
                            break;

                        case UpdateConfigStep.ReadBase:
                            DateTime utcNow = DateTime.UtcNow;

                            if (utcNow - readBaseDT >= ReadBasePeriod)
                            {
                                readBaseDT = utcNow;

                                if (ReadBase(out BaseDataSet baseDataSet))
                                {
                                    step = UpdateConfigStep.Idle;
                                    RightMatrix rightMatrix = new();
                                    rightMatrix.Init(baseDataSet);

                                    BaseDataSet = baseDataSet;
                                    RightMatrix = rightMatrix;
                                    DataFormatter = new CnlDataFormatter(baseDataSet);
                                    IsReadyToLogin = true;

                                    if (IsReady)
                                    {
                                        Log.WriteAction(Locale.IsRussian ?
                                            "Приложение готово к входу пользователей" :
                                            "The application is ready for user login");
                                    }
                                    else
                                    {
                                        IsReady = true;
                                        Log.WriteAction(Locale.IsRussian ?
                                            "Приложение готово к работе" :
                                            "The application is ready for operating");
                                    }

                                    PluginHolder.OnAppReady();
                                }
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteException(ex, Locale.IsRussian ?
                        "Ошибка при обновлении конфигурации" :
                        "Error updating configuration");
                }
                finally
                {
                    Thread.Sleep(ScadaUtils.ThreadDelay);
                }
            }
        }

        /// <summary>
        /// Reads the configuration database.
        /// </summary>
        private bool ReadBase(out BaseDataSet baseDataSet)
        {
            string tableName = Locale.IsRussian ? "неопределена" : "undefined";

            try
            {
                ScadaClient scadaClient = new(AppConfig.ConnectionOptions);
                baseDataSet = new BaseDataSet();

                foreach (IBaseTable baseTable in baseDataSet.AllTables)
                {
                    tableName = baseTable.Name;
                    scadaClient.DownloadBaseTable(baseTable);
                }

                scadaClient.TerminateSession();
                Log.WriteAction(Locale.IsRussian ?
                    "База конфигурации получена успешно" :
                    "The configuration database has been received successfully");
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteException(ex, Locale.IsRussian ?
                    "Ошибка при приёме базы конфигурации, таблица {0}" :
                    "Error receiving the configuration database, the {0} table", tableName);
                baseDataSet = null;
                return false;
            }
        }


        /// <summary>
        /// Initializes the application context.
        /// </summary>
        public void Init(string exeDir)
        {
            AppDirs.Init(exeDir);

            Log = new LogFile(LogFormat.Full)
            {
                FileName = Path.Combine(AppDirs.LogDir, WebUtils.LogFileName),
                Capacity = int.MaxValue
            };

            Log.WriteBreak();
            LoadInstanceConfig();
            LocalizeApp();

            Log.WriteAction(Locale.IsRussian ?
                "Вебстанция {0} запущена" :
                "Webstation {0} started", WebUtils.AppVersion);
        }

        /// <summary>
        /// Finalizes the application context.
        /// </summary>
        public void FinalizeContext()
        {
            StopProcessing();
            Log.WriteAction(Locale.IsRussian ?
                "Вебстанция остановлена" :
                "Webstation is stopped");
            Log.WriteBreak();
        }

        /// <summary>
        /// Starts a background process of configuration update.
        /// </summary>
        public void StartProcessing()
        {
            try
            {
                if (configThread == null)
                {
                    terminated = false;
                    pluginsReady = false;
                    configUpdateRequired = true;
                    IsReadyToLogin = false;

                    configThread = new Thread(ExecuteConfigUpdate);
                    configThread.Start();
                }
            }
            catch (Exception ex)
            {
                Log.WriteException(ex, Locale.IsRussian ?
                    "Ошибка при запуске обновления конфигурации" :
                    "Error starting configuration update");
            }
        }

        /// <summary>
        /// Waiting for plugins to be initialized.
        /// </summary>
        public void WaitForPlugins()
        {
            while (!terminated && !pluginsReady)
            {
                Thread.Sleep(ScadaUtils.ThreadDelay);
            }
        }

        /// <summary>
        /// Gets the view specification.
        /// </summary>
        public ViewSpec GetViewSpec(View viewEntity)
        {
            if (viewEntity == null)
                throw new ArgumentNullException(nameof(viewEntity));

            if (viewEntity.ViewTypeID == null)
            {
                return PluginHolder.GetViewSpecByExt(Path.GetExtension(viewEntity.Path));
            }
            else
            {
                ViewType viewType = BaseDataSet.ViewTypeTable.GetItem(viewEntity.ViewTypeID.Value);
                return viewType == null ? null : PluginHolder.GetViewSpecByCode(viewType.Code);
            }
        }
    }
}
