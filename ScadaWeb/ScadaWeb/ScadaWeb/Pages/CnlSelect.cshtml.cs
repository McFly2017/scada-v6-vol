﻿// Copyright (c) Rapid Software LLC. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Scada.Data.Entities;
using Scada.Web.Api;
using Scada.Web.Authorization;
using Scada.Web.Components;
using Scada.Web.Services;
using Scada.Web.Users;
using System.Collections.Generic;

namespace Scada.Web.Pages
{
    /// <summary>
    /// Represents a page for selecting channels of the configuration database.
    /// <para>Представляет страницу для выбора каналов базы конфигурации.</para>
    /// </summary>
    [Authorize(Policy = PolicyName.Restricted)]
    public class CnlSelectModel : PageModel
    {
        private readonly IWebContext webContext;
        private readonly IUserContext userContext;


        public CnlSelectModel(IWebContext webContext, IUserContext userContext)
        {
            this.webContext = webContext;
            this.userContext = userContext;
        }


        public ModalPostbackArgs PostbackArgs { get; private set; } = null;
        public List<SelectListItem> ObjList { get; private set; } = new();
        public List<Cnl> SelectedCnls { get; private set; } = new();

        [BindProperty]
        public int ObjNum { get; set; }
        [BindProperty]
        public bool OnlySelected { get; set; }
        [BindProperty]
        public List<int> SelectedCnlNums { get; set; }


        private void FillObjList()
        {
            ObjList.Add(new SelectListItem("-- Select an object --", "0"));

            foreach (ObjectItem objectItem in userContext.Objects)
            {
                ObjList.Add(new SelectListItem(
                    objectItem.Text, 
                    objectItem.ObjNum.ToString()));
            }
        }

        public void OnGet(IdList cnlNums)
        {
            ObjNum = 0;
            OnlySelected = true;
            FillObjList();

            foreach (int cnlNum in cnlNums)
            {
                if (webContext.ConfigDatabase.CnlTable.GetItem(cnlNum) is Cnl cnl)
                    SelectedCnls.Add(cnl);
            }
        }

        public void OnPost()
        {
            FillObjList();
        }
    }
}