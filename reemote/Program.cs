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
            Console.Out.WriteLine("==== LICENSE INFO =====");
            Console.Out.WriteLine(Protocol.GetLicenseInfo());
        }

        static void ShowHelp (OptionSet p)
        {
            Console.Out.WriteLine ("Usage: reemote.exe [OPTIONS]");
            Console.Out.WriteLine ("Choose either a requests for billings (-b) or profiles (-p).");
            Console.Out.WriteLine ("If no request option is specified, a generic output is shown.");
            Console.Out.WriteLine ();
            Console.Out.WriteLine ("Options:");
            p.WriteOptionDescriptions (Console.Out);
        }

        static CProtocolIEC870REEConnection ConfigConnection (short link_addr, short mpoint_addr, int pass)
        {
            CProtocolIEC870REEConnection ProtocolIEC870REEConnection = new CProtocolIEC870REEConnection();
            ProtocolIEC870REEConnection.LinkAddress = link_addr;
            ProtocolIEC870REEConnection.MeasuringPointAddress = mpoint_addr;
            ProtocolIEC870REEConnection.Password = pass;
            ProtocolIEC870REEConnection.OpenSessionRetries = 5;
            ProtocolIEC870REEConnection.OpenSessionTimeout = 2000;
            ProtocolIEC870REEConnection.MacLayerRetries = 3;
            ProtocolIEC870REEConnection.MacLayerRetriesDelay = 1000;

            return ProtocolIEC870REEConnection;
        }

        static CPortConfigTCPIP ConfigPortTCPIP (string ip_address, int port)
        {
            CPortConfigTCPIP PortConfigTCPIP = new CPortConfigTCPIP();
            PortConfigTCPIP.IPAddress = ip_address;
            PortConfigTCPIP.IPPort = port;
            PortConfigTCPIP.Timeout = 2000;

            return PortConfigTCPIP;
        }

        static CPortConfigGSM ConfigPortGSM ()
        {
            CPortConfigGSM PortConfigGSM = new CPortConfigGSM();
            PortConfigGSM.Name = "USB0";
            PortConfigGSM.Baudrate = new PORT_BAUDRATE(PORT_BAUDRATE.PORT_BAUDRATE_9600);
            PortConfigGSM.Databits = new PORT_DATABITS(PORT_DATABITS.PORT_DATABITS_8);
            PortConfigGSM.Parity = new PORT_PARITY(PORT_PARITY.PORT_PARITY_NOPARITY);
            PortConfigGSM.Stopbits = new PORT_STOPBITS(PORT_STOPBITS.PORT_STOPBITS_ONESTOPBIT);
            PortConfigGSM.Timeout = 10000;
            PortConfigGSM.ConnectionTimeout = 60000;
            PortConfigGSM.DisconnectionTimeout = 5000;

            return PortConfigGSM;
        }

        static void Main(string[] args)
        {

            bool show_help = false;
            string ip_address = "";
            string option = "";
            int port = 0;
            int pass = 0;
            byte request = 2;
            bool contract1 = false;
            bool contract2 = false;
            bool contract3 = false;
            short link_addr = 0;
            short mpoint_addr = 0;
            DateTime DateFromArg = new DateTime();
            DateTime DateToArg = new DateTime();
            string phone = "";
            var p = new OptionSet () {
                { "h|help",  "Shows this message and exits.",
                  v => show_help = v != null },
                { "b", "To request for billings.",
                  v => option="b" },
                { "p", "To request for profiles.",
                  v => option="p" },
                { "i|ip|ipaddr=", "The IP adress of the meter.",
                  v => ip_address=v },
                { "n|phone=", "The phone number of the meter.",
                  v => phone=v },
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
                { "r|request=", "The Type of request to use (0, 1, 2, 3, 4).",
                  v => request=byte.Parse(v) },
                { "c1|contract1", "The contract to request from the meter.",
                    v => contract1=true },
                { "c2|contract2", "The contract to request from the meter.",
                    v => contract2=true },
                { "c3|contract3", "The contract to request from the meter.",
                    v => contract3=true },
            };
            try {
                p.Parse(args);
            }
            catch (OptionException e) {
                Console.Error.Write ("reemote: ");
                Console.Error.WriteLine (e.Message);
                Console.Error.WriteLine ("Try `reemote.exe --help' for more information.");
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

                if (ip_address != "")
                {
                    CPortConfigTCPIP configured_port = ConfigPortTCPIP(ip_address, port);
                    ProtocolIEC870REE.SetPortConfig(configured_port);
                }
                else if (phone != "")
                {
                    CPortConfigGSM configured_port = ConfigPortGSM();
                    ProtocolIEC870REE.SetPortConfig(configured_port);
                }
                else{
                    // TODO: print error message
                    return;
                }

                CProtocolIEC870REEConnection ProtocolIEC870REEConnection = ConfigConnection(link_addr, mpoint_addr, pass);
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
                        List<PersonalizedTotals> results = new List<PersonalizedTotals>();
                        // Get billings
                        if (contract1)
                            try{
                                CContract Contract1Tariffs = ProtocolIEC870REE.GetContractTariffs(1, true);
                                CTotals Totals1 = ProtocolIEC870REE.ReadTotalsHistory(1, DateFrom, DateTo);
                                results.Add(new PersonalizedTotals(Totals1, SerialNumber, Contract1Tariffs.TariffScheme.EnergyFlowImport));
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine("Error getting contract 1 information");
                                Console.Error.WriteLine(ex.Message);
                            }
                        if (contract2)
                            try{
                                CContract Contract2Tariffs = ProtocolIEC870REE.GetContractTariffs(2, true);
                                CTotals Totals2 = ProtocolIEC870REE.ReadTotalsHistory(2, DateFrom, DateTo);
                                results.Add(new PersonalizedTotals(Totals2, SerialNumber, Contract2Tariffs.TariffScheme.EnergyFlowImport));
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine("Error getting contract 2 information");
                                Console.Error.WriteLine(ex.Message);
                            }
                        if (contract3)
                            try{
                                CContract Contract3Tariffs = ProtocolIEC870REE.GetContractTariffs(3, true);
                                CTotals Totals3 = ProtocolIEC870REE.ReadTotalsHistory(3, DateFrom, DateTo);
                                results.Add(new PersonalizedTotals(Totals3, SerialNumber, Contract3Tariffs.TariffScheme.EnergyFlowImport));
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine("Error getting contract 3 information");
                                Console.Error.WriteLine(ex.Message);
                            }

                        PersonalizedResult Result = new PersonalizedResult(results);
                        json_result = new JavaScriptSerializer().Serialize(Result);
                    }
                    else if (option == "p")
                    {
                        // Get profiles
                        CLoadProfile Profiles = ProtocolIEC870REE.ReadLoadProfile(request, 1, false, DateFrom, DateTo);
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
                Console.Out.WriteLine(json_result);

                Environment.Exit(0);
            }
            catch (LICENSE_RESULT elr)
            {
                Console.Error.WriteLine(elr.Message);
                Console.Error.WriteLine("You should get a valid LICENSE for one of the following MAC addresses:");
                NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface adapter in nics)
                {
                    String macAddress = adapter.GetPhysicalAddress().ToString();
                    if (macAddress != "")
                    {
                        Console.Error.WriteLine("  Physical address: {0}", macAddress);
                    }
                }
                Console.Error.WriteLine("And export using the environment vars: DAIZACOM_LICENSE_MACHINE and DAIZACOM_LICENSE_PACKAGE");
                Environment.Exit(1);
            }
            catch (PROTOCOL_IEC870REE_RESULT ex)
            {
                Console.Error.WriteLine(ex.Message);

                if (ProtocolIEC870REE != null)
                {
                    CSniffer oSniffer = ProtocolIEC870REE.GetSniffer();
                    foreach (CAction Action in oSniffer.ActionsQueue)
                    {
                        Console.Error.WriteLine(Action.Description);
                        Console.Error.WriteLine(Action.TimeInfo.ToString());
                        Console.Error.WriteLine(Action.Data);
                    }
                    ProtocolIEC870REE.CloseSession();
                    ProtocolIEC870REE.ClosePort();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
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
