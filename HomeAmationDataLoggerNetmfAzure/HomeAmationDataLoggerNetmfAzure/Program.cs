using System;
using System.Ext.Xml;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
// using Microsoft.WindowsAzure.MobileServices;
// using Rodaw.Netmf.Led;
using Microsoft.WindowsAzure.MobileServices;
using SecretLabs.NETMF.Hardware.Netduino;
// using SecretLabs.NETMF.Hardware.NetduinoPlus;
using System.Text.RegularExpressions;
// using Microsoft.Azure.Zumo.MicroFramework.Core;
using Toolbox.NETMF.NET;

// Adapted from:
//  http://wiki.tinyclr.com/index.php?title=TCP_/_Web_Server_Tutorial

namespace HomeAmationDataLoggerNetmfAzure
{
    public class Program
    {
        const string DataLoggerName = "JHA NetDuino"; // TODO name your data logger here.

        public static MobileServiceClient MobileService = new MobileServiceClient(
            new Uri("http://homeamationnetmf.azure-mobile.net/"),
            "QYJSjbMXAmEIdlWJdzEYLYCujoENkj23"
            );

        // static SingleLed singleLed0 = new SingleLed(new OutputPort(Pins.ONBOARD_LED, true));
        // static AnalogInput a0 = new AnalogInput((Cpu.AnalogChannel)Cpu.AnalogChannel.ANALOG_0, 3.3, 0.0, 10);
        static Microsoft.SPOT.Hardware.AnalogInput a0 = new AnalogInput(AnalogChannels.ANALOG_PIN_A0);
        static double a = 0;
        static double temperature0 = 0;

        //public static SummaryTemperatureData std =
        //    new SummaryTemperatureData(
        //        DataLoggerName, 
        //        DateTime.Now,
        //        45.6745f,
        //        0.0f);      // Temperature1 is not measured. Indicate by setting 0.

        public static void Main()
        {

            // Initializes the time client
            //SNTP_Client TimeClient = new SNTP_Client(new IntegratedSocket("time-a.nist.gov", 123));
            //// Displays the time in three ways:
            //Debug.Print("Amount of seconds since 1 jan. 1900: " + TimeClient.Timestamp.ToString());
            //Debug.Print("UTC time: " + TimeClient.UTCDate.ToString());
            //Debug.Print("Local time: " + TimeClient.LocalDate.ToString());
            //// Synchronizes the internal clock
            //// TimeClient.Synchronize();

            //var foo = a; 

            //singleLed0.LedOnState = true;
            //singleLed0.BlinkDuration = 250;
            //singleLed0.Mode = SingleLedModes.Blink;
            //Thread.Sleep(250);
            //singleLed0.Mode = SingleLedModes.Blink;

            //Thread ledThread = new Thread(LedThread);                   // I'm blinking an LED just for the Halibut
            //ledThread.Start();

            //Thread temperatureThread = new Thread(TemperatureThread);   // Update the temperature once per minute
            //temperatureThread.Start();

            //Thread updateAzureThread = new Thread(AzureUpdateThread);   // Update Azure mobile services every 5 minutes
            //updateAzureThread.Start();

            //SetTheClock();                                              // Set system time from NTP

            //Thread updateSystemTimeThread = new Thread(UpdateSystemTimeThread); // Update system time once per day
            //updateSystemTimeThread.Start();


            #region Check we have a valid NIC
            // First, make sure we actually have a network interface to work with!
            if (Microsoft.SPOT.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces().Length < 1)
            {
                Debug.Print("No Active network interfaces. Bombing out.");
                Thread.CurrentThread.Abort();
            }
            #endregion

            // OK, retrieve the network interface
            Microsoft.SPOT.Net.NetworkInformation.NetworkInterface NI = Microsoft.SPOT.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()[0];

            #region DHCP Code
            ////// If DHCP is not enabled, then enable it and get an IP address, else renew the lease. Most iof us have a DHCP server
            ////// on a network, even at home (in the form of an internet modem or wifi router). If you want to use a static IP
            ////// then comment out the following code in the "DHCP" region and uncomment the code in the "fixed IP" region.
            //if (NI.IsDhcpEnabled == false)
            //{
            //    Debug.Print("Enabling DHCP.");
            //    NI.EnableDhcp();
            //    Debug.Print("DCHP - IP Address = " + NI.IPAddress + " ... Net Mask = " + NI.SubnetMask + " ... Gateway = " + NI.GatewayAddress);
            //}
            //else
            //{
            //    Debug.Print("Renewing DHCP lease.");
            //    NI.RenewDhcpLease();
            //    Debug.Print("DCHP - IP Address = " + NI.IPAddress + " ... Net Mask = " + NI.SubnetMask + " ... Gateway = " + NI.GatewayAddress);
            //}
            #endregion

            #region Static IP code
            // Uncomment the following line if you want to use a static IP address, and comment out the DHCP code region above
            // TODO enter your ip address here. Don't forget your default gateway.
            NI.EnableStaticIP("192.168.1.210", "255.255.255.0", "192.168.1.1");
            #endregion


            Thread temperatureThread = new Thread(TemperatureThread);   // Update the temperature once per minute
            temperatureThread.Start();



            // SetTheClock();                                              // Set system time from NTP

            //Thread updateSystemTimeThread = new Thread(UpdateSystemTimeThread); // Update system time once per day
            //updateSystemTimeThread.Start();


            // Initializes the time client
            SNTP_Client TimeClient = new SNTP_Client(new IntegratedSocket("time-a.nist.gov", 123));
            // ExtendedTimeZone.SetTimeZone(TimeZoneId.Arizona); // not supported
            TimeClient.Synchronize();

            // Displays the time in three ways:
            Debug.Print("Amount of seconds since 1 jan. 1900: " + TimeClient.Timestamp.ToString());
            Debug.Print("UTC time: " + TimeClient.UTCDate.ToString());
            Debug.Print("Local time: " + TimeClient.LocalDate.ToString());

            Thread updateAzureThread = new Thread(AzureUpdateThread);   // Update Azure mobile services every 5 minutes
            updateAzureThread.Start();

            #region Create and Bind the listening socket
            // Create the socket            
            Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Bind the listening socket to the portum  
            IPAddress hostIP = IPAddress.Parse(NI.IPAddress);
            IPEndPoint ep = new IPEndPoint(hostIP, 80);
            listenSocket.Bind(ep);
            #endregion

            // Start listening
            listenSocket.Listen(1);

            // Main thread loop
            while (true)
            {
                try
                {
                    Debug.Print("listening...");
                    Socket connection = listenSocket.Accept();

                    Debug.Print("Accepted a connection from " + connection.RemoteEndPoint.ToString());
                    if (connection.Poll(-1, SelectMode.SelectRead) && connection.Available > 0)
                    {
                        byte[] buffer = new byte[connection.Available];
                        int bytesRead = connection.Receive(buffer);
                        string request = new string(Encoding.UTF8.GetChars(buffer));
                        // Debug.Print(request);

                        Match match = Regex.Match(request.ToUpper(), "GET /XML");
                        if (match.Length > 0)
                        {
                            byte[] response = Encoding.UTF8.GetBytes(ReturnSummaryDataXml());
                            string header = "HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nCache-Control: no-cache\r\nConnection: close\r\n\r\n";
                            connection.Send(Encoding.UTF8.GetBytes(header));
                            connection.Send(response);
                        }
                    }
                    //connection.SendTimeout = 2000; // TODO 2 seconds? 
                    //Thread.Sleep(1000);
                    connection.Close();
                }
                catch (Exception e)
                {
                    Debug.Print(e.Message);
                }
            }


        }

        //private static void UpdateSystemTimeThread()
        //{
        //    SetTheClock();
        //    Thread.Sleep(1000*60*60*24);
        //}

        private static void TemperatureThread()
        {
            while (true)
            {
                var times = 10;
                a = 0;
                for (int i = 0; i < times; i++)
                {
                    a += a0.Read();
                }
                // temperature0 = (a / times) * 100;
                temperature0 = ((a / times) * 3.3 / 1023) * 100000;


                // Debug.Print(temperature0.ToString("F2"));
                Thread.Sleep(1000 * 60);
            }
        }

        public static void AzureUpdateThread()
        {
            while (true)
            {
                string strTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                // DateTime dateTimeMinusMilliSeconds = DateTime.Parse(strTime); // Apparently no DateTime.Parse
                Debug.Print(strTime); 

                HistoricalTemperatureDataForAzure htdForAzure = new HistoricalTemperatureDataForAzure()
                {
                    DataLoggerName = DataLoggerName,
                    // Time = DateTime.Now,
                    Time = DateTime.UtcNow,
                    // Time = dateTimeMinusMilliSeconds,
                    Temperature0 = temperature0,
                    Temperature1 = 0
                };
                Debug.Print(htdForAzure.Time.ToString()); 
                var json = MobileService.GetTable("HistoricalTemperatureData").Insert(htdForAzure);
                Thread.Sleep(5 * 60 * 1000);
            }
        }

        //private static void LedThread()
        //{
        //    while (true)
        //    {
        //        singleLed0.Mode = SingleLedModes.Blink;
        //        Thread.Sleep(1750);
        //    }
        //}

        //private static void SetTheClock()
        //{
        //    // http://www.tinyclr.com/codeshare/entry/404
        //    var adjustTz = new System.TimeSpan(0, 9, 0, 0);
        //    // I'm in Arizona where they don't adjust DST, when I go back to Spokane this will need more elegance
        //    // Utility.SetLocalTime(Rodaw.Netmf.Util.GetNetworkTime() - adjustTz); //Set the RTC
        //    // Utility.SetLocalTime(Util.GetNetworkTime() - adjustTz); //Set the RTC
        //    Debug.Print("DateTime.Now... " + DateTime.Now.ToString());
        //}

        static string ReturnSummaryDataXml()
        {
            MemoryStream ms = new MemoryStream();

            using (XmlWriter xmlWriter = XmlWriter.Create(ms))
            {
                // TODO add style information? 
                xmlWriter.WriteProcessingInstruction("xml", "version=\"1.0\" encoding=\"utf-8\"");
                xmlWriter.WriteStartElement("SummaryTemperatureData");
                xmlWriter.WriteAttributeString("xmlns:xsd", "http://www.w3.org/2001/XMLSchema");
                xmlWriter.WriteAttributeString("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
                // xmlWriter.WriteElementString("DataLoggerDeviceName", std.DataLoggerDeviceName);
                xmlWriter.WriteElementString("DataLoggerDeviceName", DataLoggerName);
                // TODO would this fix the ToString("F2") ?? <xs:element name="startdate" type="xs:dateTime"/>
                xmlWriter.WriteElementString("CurrentMeasuredTime", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));
                xmlWriter.WriteElementString("CurrentTemperature0", temperature0.ToString("F2"));
                xmlWriter.WriteElementString("CurrentTemperature1", 0.0.ToString("F2"));
                xmlWriter.WriteEndElement();
                xmlWriter.Flush();
                xmlWriter.Close();
            }

            byte[] byteArray = ms.ToArray();
            char[] cc = UTF8Encoding.UTF8.GetChars(byteArray);
            string str = new string(cc);
            return str;
        }
    }
}
