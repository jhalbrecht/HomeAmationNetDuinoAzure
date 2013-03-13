using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.MobileServices;

namespace WonderDate
{
    class Program
    {

    //    public static MobileServiceClient MobileService = new MobileServiceClient(
    //new Uri("http://homeamationnetmf.azure-mobile.net/"),
    //"QYJSjbMXAmEIdlWJdzEYLYCujoENkj23"
    //);
        static System.Uri a = new Uri("http://homeamationnetmf.azure-mobile.net/"); 
        public static MobileServiceClient MobileService = new MobileServiceClient(a, "QYJSjbMXAmEIdlWJdzEYLYCujoENkj23");

        static void Main(string[] args)
        {
            Console.WriteLine("jeffa says hi");
            Console.ReadKey();

            var rec = new HistoricalTemperatureDataForAzure
                          {
                              Time = DateTime.UtcNow,
                              Temperature0 = 23.45,
                              Temperature1 = 34.43,
                              DataLoggerName = "jha console"

                          };

            var json = MobileService.GetTable("Console").Insert(rec);

        }
    }
}
