﻿// Copyright (c) Rapid Software LLC. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Scada.Client;
using Scada.Data.Entities;
using Scada.Data.Models;
using Scada.Lang;
using System;
using System.Globalization;
using System.Text;
using System.Web;

namespace Scada.Web.Plugins.PlgChart
{
    /// <summary>
    /// Builds a JavaScript representation of chart properties and data.
    /// <para>Строит JavaScript-представление свойств и данных графика.</para>
    /// </summary>
    public class ChartDataBuilder2
    {
        /// <summary>
        /// The number of array elements in a JavaScript string.
        /// </summary>
        private const int ItemsPerLine = 100;

        private readonly BaseDataSet baseDataSet;         // the configuration database
        private readonly ScadaClient scadaClient;         // interacts with the server
        private readonly ChartDataBuilderOptions options; // the builder options
        private readonly CnlDataFormatter formatter;      // formats channel data

        private Cnl[] cnls;              // the channels of the chart
        private Trend singleTrend;       // the chart data if only one channel is used in the chart
        private TrendBundle trendBundle; // the chart data of many channels


        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public ChartDataBuilder2(BaseDataSet baseDataSet, ScadaClient scadaClient, ChartDataBuilderOptions options)
        {
            this.baseDataSet = baseDataSet ?? throw new ArgumentNullException(nameof(baseDataSet));
            this.scadaClient = scadaClient ?? throw new ArgumentNullException(nameof(scadaClient));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            options.Validate();
            formatter = new CnlDataFormatter(baseDataSet);

            cnls = Array.Empty<Cnl>();
            singleTrend = null;
            trendBundle = null;
        }


        /// <summary>
        /// Appends the timestamp converted to local time to the string builder.
        /// </summary>
        private void AppendLocalTime(StringBuilder stringBuilder, DateTime timestamp)
        {
            if (timestamp.Kind == DateTimeKind.Utc)
                timestamp = TimeZoneInfo.ConvertTimeFromUtc(timestamp, options.TimeZone);

            stringBuilder.Append('\'')
                .Append(timestamp.Year.ToString("D4")).Append('-')
                .Append(timestamp.Month.ToString("D2")).Append('-')
                .Append(timestamp.Day.ToString("D2")).Append('T')
                .Append(timestamp.Hour.ToString("D2")).Append(':')
                .Append(timestamp.Minute.ToString("D2")).Append(':')
                .Append(timestamp.Second.ToString("D2")).Append('\'');
        }

        /// <summary>
        /// Appends the timestamps of the trend bundle to the string builder.
        /// </summary>
        private void AppendTimestamps(StringBuilder stringBuilder, TrendBundle trendBundle)
        {
            stringBuilder.Append('[');

            if (trendBundle != null)
            {
                int counter = 0;

                foreach (DateTime timestamp in trendBundle.Timestamps)
                {
                    stringBuilder.Append('[').Append(GetMilliseconds(timestamp)).Append(", ");
                    AppendLocalTime(stringBuilder, timestamp);
                    stringBuilder.Append("], ");

                    if (++counter == ItemsPerLine)
                    {
                        counter = 0;
                        stringBuilder.AppendLine();
                    }
                }
            }

            stringBuilder.Append(']');
        }

        /// <summary>
        /// Appends the timestamps of the single trend to the string builder.
        /// </summary>
        private void AppendTimestamps(StringBuilder stringBuilder, Trend trend)
        {
            stringBuilder.Append('[');

            if (trend != null)
            {
                int counter = 0;

                foreach (TrendPoint point in trend.Points)
                {
                    stringBuilder.Append('[').Append(GetMilliseconds(point.Timestamp)).Append(", ");
                    AppendLocalTime(stringBuilder, point.Timestamp);
                    stringBuilder.Append("], ");

                    if (++counter == ItemsPerLine)
                    {
                        counter = 0;
                        stringBuilder.AppendLine();
                    }
                }
            }

            stringBuilder.Append(']');
        }

        /// <summary>
        /// Appends the points of the trend bundle to the string builder.
        /// </summary>
        private void AppendTrendPoints(StringBuilder stringBuilder, TrendBundle trendBundle, int trendIndex)
        {
            stringBuilder.Append('[');

            if (trendBundle != null)
            {
                Cnl cnl = cnls[trendIndex];
                int counter = 0;

                foreach (CnlData cnlData in trendBundle.Trends[trendIndex])
                {
                    AppendTrendPoint(stringBuilder, cnlData, cnl);

                    if (++counter == ItemsPerLine)
                    {
                        counter = 0;
                        stringBuilder.AppendLine();
                    }
                }
            }

            stringBuilder.Append(']');
        }

        /// <summary>
        /// Appends the points of the single trend to the string builder.
        /// </summary>
        private void AppendTrendPoints(StringBuilder stringBuilder, Trend trend, Cnl cnl)
        {
            stringBuilder.Append('[');

            if (trend != null)
            {
                int counter = 0;

                foreach (TrendPoint point in trend.Points)
                {
                    AppendTrendPoint(stringBuilder, new CnlData(point.Val, point.Stat), cnl);

                    if (++counter == ItemsPerLine)
                    {
                        counter = 0;
                        stringBuilder.AppendLine();
                    }
                }
            }

            stringBuilder.Append(']');
        }

        /// <summary>
        /// Appends the trend point to the string builder.
        /// </summary>
        private void AppendTrendPoint(StringBuilder stringBuilder, CnlData cnlData, Cnl cnl)
        {
            // HttpUtility.JavaScriptStringEncode() is skipped for performance
            CnlDataFormatted cnlDataFormatted = formatter.FormatCnlData(cnlData, cnl, false);
            stringBuilder
                .Append('[')
                .Append(cnlData.Stat > 0 && !double.IsNaN(cnlData.Val) ? 
                    cnlData.Val.ToString(CultureInfo.InvariantCulture) : "NaN").Append(", ")
                .Append(cnlData.Stat).Append(", \"")
                .Append(cnlDataFormatted.DispVal).Append("\"], ");
        }

        /// <summary>
        /// Gets the quantity name and unit of the specified channel.
        /// </summary>
        private string GetQuantityName(Cnl cnl)
        {
            Quantity quantity = cnl.QuantityID == null ? null : baseDataSet.QuantityTable.GetItem(cnl.QuantityID.Value);
            return quantity?.Name ?? "";
        }

        /// <summary>
        /// Gets the unit name (with leading space) of the specified channel.
        /// </summary>
        private string GetUnitName(Cnl cnl)
        {
            Unit unit = cnl.UnitID == null ? null : baseDataSet.UnitTable.GetItem(cnl.UnitID.Value);
            return unit?.Name ?? "";
        }

        /// <summary>
        /// Gets the number of milliseconds since 1970-01-01.
        /// </summary>
        private static long GetMilliseconds(DateTime timestamp)
        {
            return new DateTimeOffset(timestamp).ToUnixTimeMilliseconds();
        }


        /// <summary>
        /// Fills the channel array.
        /// </summary>
        public void FillCnls()
        {
            int cnlCnt = options.CnlNums.Length;
            cnls = new Cnl[cnlCnt];

            for (int i = 0; i < cnlCnt; i++)
            {
                int cnlNum = options.CnlNums[i];
                cnls[i] = baseDataSet.CnlTable.GetItem(cnlNum) ?? new Cnl { CnlNum = cnlNum };
            }
        }

        /// <summary>
        /// Fills chart data for the normalized start date.
        /// </summary>
        public void FillData()
        {
            singleTrend = null;
            trendBundle = null;
            int cnlCnt = options.CnlNums.Length;

            try
            {
                if (cnlCnt == 1)
                    singleTrend = scadaClient.GetTrend(options.ArchiveBit, options.TimeRange, options.CnlNums[0]);
                else if (cnlCnt > 1)
                    trendBundle = scadaClient.GetTrends(options.ArchiveBit, options.TimeRange, options.CnlNums);
            }
            catch (Exception ex)
            {
                throw new ScadaException(Locale.IsRussian ?
                    "Ошибка при получении одного или нескольких трендов" :
                    "Error getting one or several trends", ex);
            }
        }

        /// <summary>
        /// Converts the chart data to JavaScript.
        /// </summary>
        public void ToJs(StringBuilder stringBuilder)
        {
            ArgumentNullException.ThrowIfNull(stringBuilder, nameof(stringBuilder));

            // time range
            DateTime t1 = options.TimeRange.StartTime;
            DateTime t2 = options.TimeRange.EndTime;
            stringBuilder
                .AppendLine("var timeRange = new scada.chart.TimeRange();")
                .AppendFormat("timeRange.startTime = {0};", GetMilliseconds(t1)).AppendLine()
                .AppendFormat("timeRange.endTime = {0};", GetMilliseconds(t2)).AppendLine()
                .AppendLine()
                .AppendLine("var hourMap = timeRange.hourMap;");

            DateTime startHour = new(t1.Year, t1.Month, t1.Day, t1.Hour, 0, 0, t1.Kind);
            DateTime endHour = new(t2.Year, t2.Month, t2.Day, t2.Hour, 0, 0, t2.Kind);
            DateTime curHour = startHour;

            while (curHour <= endHour)
            {
                stringBuilder.Append("hourMap.set(").Append(GetMilliseconds(curHour)).Append(", ");
                AppendLocalTime(stringBuilder, curHour);
                stringBuilder.AppendLine(");");
                curHour = curHour.AddHours(1);
            }

            stringBuilder.AppendLine();

            // time points
            stringBuilder
                .AppendLine("var chartData = new scada.chart.ChartData();")
                .Append("chartData.timePoints = ");

            if (singleTrend == null)
                AppendTimestamps(stringBuilder, trendBundle);
            else
                AppendTimestamps(stringBuilder, singleTrend);

            stringBuilder.AppendLine(";").AppendLine();

            // trends
            StringBuilder sbTrends = new("chartData.trends = [");

            for (int i = 0, cnlCnt = cnls.Length; i < cnlCnt; i++)
            {
                string trendName = "trend" + i;
                Cnl cnl = cnls[i];
                sbTrends.Append(trendName).Append(", ");

                stringBuilder
                    .Append("var ").Append(trendName).AppendLine(" = new scada.chart.Trend();")
                    .Append(trendName).AppendFormat(".cnlNum = {0};", cnl.CnlNum).AppendLine()
                    .Append(trendName).AppendFormat(".cnlName = '{0}';",
                        HttpUtility.JavaScriptStringEncode(cnl.Name)).AppendLine()
                    .Append(trendName).AppendFormat(".quantityID = {0};", cnl.QuantityID ?? 0).AppendLine()
                    .Append(trendName).AppendFormat(".quantityName = '{0}';",
                        HttpUtility.JavaScriptStringEncode(GetQuantityName(cnl))).AppendLine()
                    .Append(trendName).AppendFormat(".unitName = '{0}';",
                        HttpUtility.JavaScriptStringEncode(GetUnitName(cnl))).AppendLine()
                    .Append(trendName).Append(".points = ");

                if (singleTrend == null)
                    AppendTrendPoints(stringBuilder, trendBundle, i);
                else
                    AppendTrendPoints(stringBuilder, singleTrend, cnl);

                stringBuilder.AppendLine(";").AppendLine();
            }

            sbTrends.AppendLine("];");
            stringBuilder.Append(sbTrends).AppendLine();

            // channel statuses
            stringBuilder.AppendLine("var statusMap = chartData.cnlStatusMap;");

            foreach (CnlStatus cnlStatus in baseDataSet.CnlStatusTable.Enumerate())
            {
                stringBuilder.AppendFormat("statusMap.set({0}, new scada.chart.CnlStatus({0}, '{1}', '{2}'));",
                    cnlStatus.CnlStatusID,
                    HttpUtility.JavaScriptStringEncode(cnlStatus.Name),
                    HttpUtility.JavaScriptStringEncode(cnlStatus.MainColor)).AppendLine();
            }

            stringBuilder.AppendLine();
        }
    }
}