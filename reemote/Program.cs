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
using GISCE.Net.Profiles;
using NDesk.Options;


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

        static void ShowHelp (OptionSet p)
        {
            Console.WriteLine ("Usage: reemote.exe [OPTIONS]");
            Console.WriteLine ("Choose either a requests for billings (-b) or profiles (-p).");
            Console.WriteLine ("If no request option is specified, a generic output is shown.");
            Console.WriteLine ();
            Console.WriteLine ("Options:");
            p.WriteOptionDescriptions (Console.Out);
        }

        static void Main(string[] args)
        {

            bool show_help = false;
            string ip_address = "";
            string option = "";
            int port = 0;
            int pass = 0;
            short link_addr = 0;
            short mpoint_addr = 0;
            DateTime DateFromArg = new DateTime();
            DateTime DateToArg = new DateTime();
            var p = new OptionSet () {
                { "h|help",  "Shows this message and exits.",
                  v => show_help = v != null },
                { "b", "To request for billings.",
                  v => option="b" },
                { "p", "To request for profiles.",
                  v => option="p" },
                { "i|ip|ipaddr=", "The IP adress of the meter.",
                  v => ip_address=v },
                { "o|port=", "The port of the meter.",
                  v => port=Int32.Parse(v) },
                { "l|link=", "The LinkAddress of the connection.",
                  v => link_addr=short.Parse(v) },
                { "m|mpoint=", "The MeasuringPointAddress of the connection.",
                  v => mpoint_addr=short.Parse(v) },
                { "w|pass=", "The password of the connection.",
                  v => pass=Int32.Parse(v) },
                { "f|df|datefrom=", "The starting date of the period.",
                  v => DateFromArg=DateTime.Parse(v) },
                { "t|dt|dateto=", "The ending date of the period.",
                  v => DateToArg=DateTime.Parse(v) },
            };
            try {
                p.Parse(args);
            }
            catch (OptionException e) {
                Console.Write ("reemote: ");
                Console.WriteLine (e.Message);
                Console.WriteLine ("Try `reemote.exe --help' for more information.");
                return;
            }

            if (show_help) {
                ShowHelp (p);
                return;
            }

            CProtocolIEC870REE ProtocolIEC870REE = null;
            try
            {
                String LicensePackage = Environment.GetEnvironmentVariable("DAIZACOM_LICENSE_PACKAGE");
                String LicenseMachine = Environment.GetEnvironmentVariable("DAIZACOM_LICENSE_MACHINE");
                ProtocolIEC870REE = new CProtocolIEC870REE(LicenseMachine, LicensePackage);

                CPortConfigTCPIP PortConfigTCPIP = new CPortConfigTCPIP();
                PortConfigTCPIP.IPAddress = ip_address;
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

                CTimeInfo DateFrom = new CTimeInfo((short)DateFromArg.Year, (byte)DateFromArg.Month, (byte)DateFromArg.Day,
                (byte)DateFromArg.Hour, (byte)DateFromArg.Minute, (byte)DateFromArg.Second, (short)DateFromArg.Millisecond);
                CTimeInfo DateTo = new CTimeInfo((short)DateToArg.Year, (byte)DateToArg.Month, (byte)DateToArg.Day,
                (byte)DateToArg.Hour, (byte)DateToArg.Minute, (byte)DateToArg.Second, (short)DateToArg.Millisecond);

                string json_result = "ERROR: No result generated! You may need to specify a request.";
                if (option != "")
                {
                    ProtocolIEC870REE.OpenPort();
                    ProtocolIEC870REE.OpenSession();

                    int SerialNumber = ProtocolIEC870REE.GetSerialNumber();
                    if (option == "b")
                    {
                        // Get billings
                        CTotals Totals = ProtocolIEC870REE.ReadTotalsHistory(1, DateFrom, DateTo);
                        PersonalizedTotals Result = new PersonalizedTotals(Totals, SerialNumber);
                        json_result = new JavaScriptSerializer().Serialize(Result);
                    }
                    else if (option == "p")
                    {
                        // Get profiles
                        CLoadProfile Profiles = ProtocolIEC870REE.ReadLoadProfile(3, 1, false, DateFrom, DateTo);
                        PersonalizedProfiles Result = new PersonalizedProfiles(Profiles, SerialNumber);
                        json_result = new JavaScriptSerializer().Serialize(Result);
                    }

                    try {
                        ProtocolIEC870REE.CloseSession();
                    }
                    catch (PROTOCOL_IEC870REE_RESULT) {

                    }
                    ProtocolIEC870REE.ClosePort();
                }
                Console.WriteLine(json_result);

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

// CPortConfigRS232 PortConfigRS232 = new CPortConfigRS232();
// PortConfigRS232.Name = "COM3";
// PortConfigRS232.Baudrate = new PORT_BAUDRATE(PORT_BAUDRATE.PORT_BAUDRATE_9600);
// PortConfigRS232.Databits = new PORT_DATABITS(PORT_DATABITS.PORT_DATABITS_8);
// PortConfigRS232.Parity = new PORT_PARITY(PORT_PARITY.PORT_PARITY_NOPARITY);
// PortConfigRS232.Stopbits = new PORT_STOPBITS(PORT_STOPBITS.PORT_STOPBITS_ONESTOPBIT);
// PortConfigRS232.Timeout = 10000;
// ProtocolIEC870REE.SetPortConfig(PortConfigRS232);

// CPortConfigGSM PortConfigGSM = new CPortConfigGSM();
// PortConfigGSM.Name = "COM3";
// PortConfigGSM.Baudrate = new PORT_BAUDRATE(PORT_BAUDRATE.PORT_BAUDRATE_9600);
// PortConfigGSM.Databits = new PORT_DATABITS(PORT_DATABITS.PORT_DATABITS_8);
// PortConfigGSM.Parity = new PORT_PARITY(PORT_PARITY.PORT_PARITY_NOPARITY);
// PortConfigGSM.Stopbits = new PORT_STOPBITS(PORT_STOPBITS.PORT_STOPBITS_ONESTOPBIT);
// PortConfigGSM.Timeout = 10000;
// PortConfigGSM.ConnectionTimeout = 60000;
// PortConfigGSM.DisconnectionTimeout = 5000;
