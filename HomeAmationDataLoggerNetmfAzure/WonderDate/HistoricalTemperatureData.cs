using System;
using Microsoft.Azure.Zumo.MicroFramework.Core;
//using Microsoft.SPOT;

namespace WonderDate
{
    public class HistoricalTemperatureData
    {
        public DateTime Time { get; set; }
        public double Temperature0 { get; set; }
        public double Temperature1 { get; set; }
    }

    public class HistoricalTemperatureDataForAzure : HistoricalTemperatureData, IMobileServiceEntity
    {
        public int Id { get; set; }
        public string DataLoggerName { get; set; }
    }
}
