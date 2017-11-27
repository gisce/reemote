using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Linq;
using System.Text;
using Daiza.Com.License;
using Daiza.Com.Sniffer;
using Daiza.Com.Port;
using Daiza.Com.Protocol_IEC870REE;
using Daiza.Com.Protocol_IEC870REE.Meter;
using Daiza.Com.Protocol_IEC870REE.Readouts;
using System.Diagnostics;
using Daiza.Com.Datetime;
using System.Web.Script.Serialization;
using GISCE.Net.Readings;

namespace GISCE.Net
{
    class REEMote
    {
        static String GetVersion()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.FileVersion;
        }

        static void PrintLicenceInfo(CProtocolIEC870REE Protocol)
        {
            Console.WriteLine("==== LICENSE INFO =====");
            Console.WriteLine(Protocol.GetLicenseInfo());
        }

        static void Main(string[] args)
        {
            if (args.Length != 7)
            {
                Console.WriteLine("REEMote version: {0}", REEMote.GetVersion());
                Console.WriteLine("Please enter all required arguments.");
                Console.WriteLine("Arguments: IP Port LinkAddress MeasuringPointAddress Password DateFrom DateTo");

            }

            CProtocolIEC870REE ProtocolIEC870REE = null;

            try
            {
                String LicensePackage = Environment.GetEnvironmentVariable("DAIZACOM_LICENSE_PACKAGE");
                String LicenseMachine = Environment.GetEnvironmentVariable("DAIZACOM_LICENSE_MACHINE");
                ProtocolIEC870REE = new CProtocolIEC870REE(LicenseMachine, LicensePackage);

                // CPortConfigRS232 PortConfigRS232 = new CPortConfigRS232();
                // PortConfigRS232.Name = "COM3";
                // PortConfigRS232.Baudrate = new PORT_BAUDRATE(PORT_BAUDRATE.PORT_BAUDRATE_9600);
                // PortConfigRS232.Databits = new PORT_DATABITS(PORT_DATABITS.PORT_DATABITS_8);
                // PortConfigRS232.Parity = new PORT_PARITY(PORT_PARITY.PORT_PARITY_NOPARITY);
                // PortConfigRS232.Stopbits = new PORT_STOPBITS(PORT_STOPBITS.PORT_STOPBITS_ONESTOPBIT);
                // PortConfigRS232.Timeout = 10000;
                // ProtocolIEC870REE.SetPortConfig(PortConfigRS232);

                //CPortConfigGSM PortConfigGSM = new CPortConfigGSM();
                //PortConfigGSM.Name = "COM3";
                //PortConfigGSM.Baudrate = new PORT_BAUDRATE(PORT_BAUDRATE.PORT_BAUDRATE_9600);
                //PortConfigGSM.Databits = new PORT_DATABITS(PORT_DATABITS.PORT_DATABITS_8);
                //PortConfigGSM.Parity = new PORT_PARITY(PORT_PARITY.PORT_PARITY_NOPARITY);
                //PortConfigGSM.Stopbits = new PORT_STOPBITS(PORT_STOPBITS.PORT_STOPBITS_ONESTOPBIT);
                //PortConfigGSM.Timeout = 10000;
                //PortConfigGSM.ConnectionTimeout = 60000;
                //PortConfigGSM.DisconnectionTimeout = 5000;                

                int port, pass;
                short link_addr, mpoint_addr;
                int.TryParse(args[1], out port);
                short.TryParse(args[2], out link_addr);
                short.TryParse(args[3], out mpoint_addr);
                int.TryParse(args[4], out pass);

                CPortConfigTCPIP PortConfigTCPIP = new CPortConfigTCPIP();
                PortConfigTCPIP.IPAddress = args[0];
                PortConfigTCPIP.IPPort = port;
                PortConfigTCPIP.Timeout = 2000;
                ProtocolIEC870REE.SetPortConfig(PortConfigTCPIP);

                CProtocolIEC870REEConnection ProtocolIEC870REEConnection = new CProtocolIEC870REEConnection();
                ProtocolIEC870REEConnection.LinkAddress = link_addr;
                ProtocolIEC870REEConnection.MeasuringPointAddress = mpoint_addr;
                ProtocolIEC870REEConnection.Password = pass;
                ProtocolIEC870REEConnection.OpenSessionRetries = 5;
                ProtocolIEC870REEConnection.OpenSessionTimeout = 2000;
                ProtocolIEC870REEConnection.MacLayerRetries = 3;
                ProtocolIEC870REEConnection.MacLayerRetriesDelay = 1000;

                ProtocolIEC870REE.SetConnectionConfig(ProtocolIEC870REEConnection);

                DateTime DateFromArg = DateTime.Parse(args[5]);
                DateTime DateToArg = DateTime.Parse(args[6]);

                CTimeInfo DateFrom = new CTimeInfo((short)DateFromArg.Year, (byte)DateFromArg.Month, (byte)DateFromArg.Day,
                (byte)DateFromArg.Hour, (byte)DateFromArg.Minute, (byte)DateFromArg.Second, (short)DateFromArg.Millisecond);
                CTimeInfo DateTo = new CTimeInfo((short)DateToArg.Year, (byte)DateToArg.Month, (byte)DateToArg.Day,
                (byte)DateToArg.Hour, (byte)DateToArg.Minute, (byte)DateToArg.Second, (short)DateToArg.Millisecond);

                ProtocolIEC870REE.OpenPort();
                ProtocolIEC870REE.OpenSession();

                CTotals Totals = ProtocolIEC870REE.ReadTotalsHistory(1, DateFrom, DateTo);
                PersonalizedTotals PTotals = new PersonalizedTotals(Totals);

                ProtocolIEC870REE.CloseSession();
                ProtocolIEC870REE.ClosePort();

                var json = new JavaScriptSerializer().Serialize(PTotals);
                Console.WriteLine(json);

                Environment.Exit(0);
            }
            catch (LICENSE_RESULT elr)
            {
                Console.WriteLine(elr.Message);
                Console.WriteLine("You should get a valid LICENSE for one of the following MAC addresses:");
                NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface adapter in nics)
                {
                    String macAddress = adapter.GetPhysicalAddress().ToString();
                    if (macAddress != "")
                    {
                        Console.WriteLine("  Physical address: {0}", macAddress);
                    }
                }
                Console.WriteLine("And export using the environment vars: DAIZACOM_LICENSE_MACHINE and DAIZACOM_LICENSE_PACKAGE");
                Environment.Exit(1);
            }
            catch (PROTOCOL_IEC870REE_RESULT ex)
            {
                Console.WriteLine(ex.Message);

                if (ProtocolIEC870REE != null)
                {
                    CSniffer oSniffer = ProtocolIEC870REE.GetSniffer();
                    foreach (CAction Action in oSniffer.ActionsQueue)
                    {
                        Console.WriteLine(Action.Description);
                        Console.WriteLine(Action.TimeInfo.ToString());
                        Console.WriteLine(Action.Data);
                    }
                    ProtocolIEC870REE.CloseSession();
                    ProtocolIEC870REE.ClosePort();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Environment.Exit(1);
            }
        }
    }
}
