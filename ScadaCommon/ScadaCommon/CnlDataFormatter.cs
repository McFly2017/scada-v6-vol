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
 * Module   : ScadaCommon
 * Summary  : Formats channel and event data for display
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2016
 * Modified : 2021
 */

using Scada.Data.Const;
using Scada.Data.Entities;
using Scada.Data.Models;
using Scada.Data.Tables;
using Scada.Lang;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Scada
{
    /// <summary>
    /// Formats channel and event data for display.
    /// <para>Форматирует данные канала и события для отображения.</para>
    /// </summary>
    public class CnlDataFormatter
    {
        /// <summary>
        /// The default number format.
        /// </summary>
        public const string DefaultFormat = "N3";
        /// <summary>
        /// The formatting result indicating an error.
        /// </summary>
        public const string FormatError = "!!!";
        /// <summary>
        /// The command data display length in bytes.
        /// </summary>
        public const int DataDisplayLength = 8;

        /// <summary>
        /// The culture for formatting values.
        /// </summary>
        protected readonly CultureInfo culture;
        /// <summary>
        /// The configuration database.
        /// </summary>
        protected readonly BaseDataSet baseDataSet;
        /// <summary>
        /// The enumeration dictionary.
        /// </summary>
        protected readonly EnumDict enums;
        /// <summary>
        /// The user's time zone.
        /// </summary>
        protected readonly TimeZoneInfo timeZone;


        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public CnlDataFormatter(BaseDataSet baseDataSet)
            : this(baseDataSet, new EnumDict(baseDataSet), TimeZoneInfo.Local)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public CnlDataFormatter(BaseDataSet baseDataSet, EnumDict enums)
            : this(baseDataSet, enums, TimeZoneInfo.Local)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public CnlDataFormatter(BaseDataSet baseDataSet, EnumDict enums, TimeZoneInfo timeZone)
        {
            culture = Locale.Culture;
            this.baseDataSet = baseDataSet ?? throw new ArgumentNullException(nameof(baseDataSet));
            this.enums = enums ?? throw new ArgumentNullException(nameof(enums));
            this.timeZone = timeZone ?? throw new ArgumentNullException(nameof(timeZone));
        }


        /// <summary>
        /// Determines whether the specified format represents a hexadecimal number.
        /// </summary>
        private bool FormatIsHex(string format)
        {
            return format != null && format.Length > 0 && (format[0] == 'x' || format[0] == 'X');
        }

        /// <summary>
        /// Formats the channel value depending on the data type.
        /// </summary>
        protected string FormatByDataType(double cnlVal, int dataTypeID)
        {
            switch (dataTypeID)
            {
                case DataTypeID.Double:
                    return cnlVal.ToString(DefaultFormat, culture);

                case DataTypeID.Int64:
                    return CnlDataConverter.DoubleToInt64(cnlVal).ToString(culture);

                case DataTypeID.ASCII:
                    return CnlDataConverter.DoubleToAscii(cnlVal);

                case DataTypeID.Unicode:
                    return CnlDataConverter.DoubleToUnicode(cnlVal);

                default:
                    return FormatError;
            }
        }

        /// <summary>
        /// Formats the channel value, which is a number.
        /// </summary>
        protected string FormatNumber(double cnlVal, int dataTypeID, string format)
        {
            switch (dataTypeID)
            {
                case DataTypeID.Double:
                    return FormatIsHex(format)
                        ? ((int)cnlVal).ToString(format, culture)
                        : cnlVal.ToString(format, culture);

                case DataTypeID.Int64:
                    return CnlDataConverter.DoubleToInt64(cnlVal).ToString(format, culture);

                default:
                    return FormatError;
            }
        }

        /// <summary>
        /// Formats the channel value, which is an enumeration.
        /// </summary>
        protected string FormatEnum(double cnlVal, int dataTypeID, EnumFormat format)
        {
            string GetEnumValue(int intVal)
            {
                return format != null && 0 <= intVal && intVal < format.Values.Length
                    ? format.Values[intVal]
                    : intVal.ToString();
            }

            switch (dataTypeID)
            {
                case DataTypeID.Double:
                    return GetEnumValue((int)cnlVal);

                case DataTypeID.Int64:
                    return GetEnumValue((int)CnlDataConverter.DoubleToInt64(cnlVal));

                default:
                    return FormatError;
            }
        }

        /// <summary>
        /// Formats the channel value, which is a date and time.
        /// </summary>
        protected string FormatDate(double cnlVal, int dataTypeID, string format)
        {
            string DateToString(DateTime dt)
            {
                return TimeZoneInfo.ConvertTimeFromUtc(dt, timeZone).ToString(format, culture);
            }

            switch (dataTypeID)
            {
                case DataTypeID.Double:
                    return DateToString(DateTime.FromOADate(cnlVal));

                case DataTypeID.Int64:
                    return DateToString(ScadaUtils.TicksToTime(CnlDataConverter.DoubleToInt64(cnlVal)));

                default:
                    return FormatError;
            }
        }


        /// <summary>
        /// Formats the input channel data according to the specified data type and format.
        /// </summary>
        public CnlDataFormatted FormatCnlData(CnlData cnlData, int dataTypeID, int formatID)
        {
            CnlDataFormatted cnlDataFormatted = new CnlDataFormatted();
            Format format = formatID > 0 ? baseDataSet.FormatTable.GetItem(formatID) : null;
            EnumFormat enumFormat = null;

            if (format != null && format.IsEnum)
                enums.TryGetValue(format.FormatID, out enumFormat);

            // displayed value
            try
            {
                if (cnlData.IsUndefined)
                    cnlDataFormatted.DispVal = CommonPhrases.UndefinedSign;
                else if (format == null)
                    cnlDataFormatted.DispVal = FormatByDataType(cnlData.Val, dataTypeID);
                else if (format.IsNumber)
                    cnlDataFormatted.DispVal = FormatNumber(cnlData.Val, dataTypeID, format.Frmt);
                else if (format.IsEnum)
                    cnlDataFormatted.DispVal = FormatEnum(cnlData.Val, dataTypeID, enumFormat);
                else if (format.IsDate)
                    cnlDataFormatted.DispVal = FormatDate(cnlData.Val, dataTypeID, format.Frmt);
                else // format.IsString or not specified
                    cnlDataFormatted.DispVal = FormatByDataType(cnlData.Val, dataTypeID);
            }
            catch
            {
                cnlDataFormatted.DispVal = FormatError;
            }

            // color
            try
            {
                // color determined by status
                CnlStatus cnlStatus = baseDataSet.CnlStatusTable.GetItem(cnlData.Stat);
                cnlDataFormatted.SetColors(cnlStatus);

                // color determined by value
                if (enumFormat != null && cnlData.Stat == CnlStatusID.Defined &&
                    0 <= cnlData.Val && cnlData.Val < enumFormat.Colors.Length &&
                    enumFormat.Colors[(int)cnlData.Val] is string color && color != "")
                {
                    cnlDataFormatted.SetFirstColor(color);
                }
            }
            catch
            {
                cnlDataFormatted.SetColorsToDefault();
            }

            return cnlDataFormatted;
        }

        /// <summary>
        /// Formats the input channel data according to the channel properties.
        /// </summary>
        public CnlDataFormatted FormatCnlData(CnlData cnlData, InCnl inCnl)
        {
            return FormatCnlData(cnlData, inCnl?.DataTypeID ?? DataTypeID.Double, inCnl?.FormatID ?? 0);
        }

        /// <summary>
        /// Formats the input channel data according to the channel properties.
        /// </summary>
        public CnlDataFormatted FormatCnlData(CnlData cnlData, int cnlNum)
        {
            return FormatCnlData(cnlData, cnlNum > 0 ? baseDataSet.InCnlTable.GetItem(cnlNum) : null);
        }

        /// <summary>
        /// Formats the event according to the channel properties.
        /// </summary>
        public EventFormatted FormatEvent(Event ev)
        {
            if (ev == null)
                throw new ArgumentNullException(nameof(ev));

            EventFormatted eventFormatted = new EventFormatted
            {
                Time = TimeZoneInfo.ConvertTimeFromUtc(ev.Timestamp, timeZone).ToLocalizedString()
            };

            // object
            if (ev.ObjNum > 0)
                eventFormatted.Obj = baseDataSet.ObjTable.GetItem(ev.ObjNum)?.Name ?? "";

            // device
            if (ev.DeviceNum > 0)
                eventFormatted.Dev = baseDataSet.DeviceTable.GetItem(ev.DeviceNum)?.Name ?? "";

            // channel
            InCnl inCnl = null;
            OutCnl outCnl = null;

            if (ev.OutCnlNum > 0)
            {
                outCnl = baseDataSet.OutCnlTable.GetItem(ev.OutCnlNum);
                eventFormatted.Cnl = outCnl?.Name ?? "";
            }
            else if (ev.CnlNum > 0)
            {
                inCnl = baseDataSet.InCnlTable.GetItem(ev.CnlNum);
                eventFormatted.Cnl = inCnl?.Name ?? "";
            }

            // description
            StringBuilder sbDescr = new StringBuilder();
            CnlDataFormatted dataFormatted;

            if (ev.OutCnlNum > 0)
            {
                // Command Value, Data. Custom text
                sbDescr.Append(Locale.IsRussian ? "Команда: " : "Command: ");
                dataFormatted = FormatCnlData(new CnlData(ev.CnlVal, CnlStatusID.Defined), 
                    DataTypeID.Double, outCnl?.FormatID ?? 0);

                if (ev.CnlStat > 0)
                    sbDescr.Append(dataFormatted.DispVal);

                if (ev.Data != null && ev.Data.Length > 0)
                {
                    sbDescr
                        .Append(ev.CnlStat > 0 ? ", " : "")
                        .Append("0x")
                        .Append(ScadaUtils.BytesToHex(ev.Data, 0, Math.Min(DataDisplayLength, ev.Data.Length)))
                        .Append(DataDisplayLength < ev.Data.Length ? "..." : "");
                }
            }
            else
            {
                // Status, Value. Custom text
                dataFormatted = FormatCnlData(new CnlData(ev.CnlVal, ev.CnlStat), inCnl);

                if (ev.TextFormat == EventTextFormat.Full || ev.TextFormat == EventTextFormat.AutoText)
                {
                    string statusName = baseDataSet.CnlStatusTable.GetItem(ev.CnlStat)?.Name;

                    if (string.IsNullOrEmpty(statusName))
                        statusName = (Locale.IsRussian ? "Статус " : "Status ") + ev.CnlStat;

                    sbDescr.Append(statusName).Append(", ").Append(dataFormatted.DispVal);
                }
            }

            if (!string.IsNullOrEmpty(ev.Text))
            {
                if (ev.TextFormat == EventTextFormat.Full)
                    sbDescr.Append(". ");

                if (ev.TextFormat == EventTextFormat.Full || ev.TextFormat == EventTextFormat.CustomText)
                    sbDescr.Append(ev.Text);
            }

            eventFormatted.Descr = sbDescr.ToString();

            // severity
            int knownSeverity = Severity.Closest(ev.Severity);

            if (knownSeverity != Severity.Undefined)
            {
                switch (knownSeverity)
                {
                    case Severity.Critical:
                        eventFormatted.Sev = CommonPhrases.CriticalSeverity;
                        break;
                    case Severity.Major:
                        eventFormatted.Sev = CommonPhrases.MajorSeverity;
                        break;
                    case Severity.Minor:
                        eventFormatted.Sev = CommonPhrases.MinorSeverity;
                        break;
                    case Severity.Info:
                        eventFormatted.Sev = CommonPhrases.InfoSeverity;
                        break;
                }

                eventFormatted.Sev += ", " + ev.Severity;
            }

            // acknowledgement
            if (ev.Ack)
            {
                eventFormatted.Ack = string.Join(", ",
                    baseDataSet.UserTable.GetItem(ev.AckUserID)?.Name ?? "",
                    TimeZoneInfo.ConvertTimeFromUtc(ev.AckTimestamp, timeZone));
            }

            // color
            if (dataFormatted.Colors.Length > 0)
                eventFormatted.Color = dataFormatted.Colors[0];

            // beep
            if (inCnl != null && new EventMask(inCnl.EventMask).Beep)
                eventFormatted.Beep = true;

            return eventFormatted;
        }
    }
}
