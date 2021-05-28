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
 * Summary  : Creates user context instances
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2021
 * Modified : 2021
 */

using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Scada.Lang;
using Scada.Log;

namespace Scada.Web.Code
{
    /// <summary>
    /// Creates user context instances.
    /// <para>Создает экземпляры контекста пользователя.</para>
    /// </summary>
    internal static class UserContextFactory
    {
        /// <summary>
        /// Gets a new instance of the user context.
        /// </summary>
        public static IUserContext GetUserContext(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));

            ILog log = null;

            try
            {
                IWebContext webContext = serviceProvider.GetRequiredService<IWebContext>();
                log = webContext.Log;

                IHttpContextAccessor httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
                HttpContext httpContext = httpContextAccessor.HttpContext;

                if (httpContext == null)
                {
                    throw new ScadaException(Locale.IsRussian ?
                        "HttpContext не определён" :
                        "HttpContext is undefined");
                }

                int userID = int.Parse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
                string username = httpContext.User.FindFirstValue(ClaimTypes.Name);

                return new UserContext
                {
                    IsLoggedIn = httpContext.User.Identity.IsAuthenticated,
                    UserModel = new Data.Entities.User
                    {
                        UserID = userID,
                        Name = username
                    }
                };
            }
            catch (Exception ex)
            {
                if (log == null)
                    throw;

                log.WriteException(ex, Locale.IsRussian ?
                    "Ошибка при создании контекста пользователя" :
                    "Error creating user context");
                return new UserContext();
            }
        }
    }
}
