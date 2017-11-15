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

namespace GISCE
{
    class REEMote
    {
        static void Main(string[] args)
        {            
			String LicenseMachine = "KzkV6wsWRp8jsdnTPhNwxkkDivQiSw6R0J1HnFohTgoqlcrb1GwB/8fm23SviE37Hy6vMtrA+FeEoi3Yd8FRdCWjdluAFJz8FtbcrQKPeoI=";
			String LicensePackage = "Njgcnp9o7aFRwX51VTopp5V80saPxkUjvbL0XXAIonWfnsr7A0U9DBjpuh8CvB44QrfVVvlUktk9AxLp+5TwHWxb1sSzbLltGYBt3s1vL6Lel6MkWWLTxsdi8M63UPSj";

            CProtocolIEC870REE ProtocolIEC870REE = null;

            Console.WriteLine("Getting interfaces....");
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in nics)
            {
                Console.WriteLine("Physical address: {0}", adapter.GetPhysicalAddress().ToString());
            }

            try
            {
				ProtocolIEC870REE = new CProtocolIEC870REE(LicenseMachine, LicensePackage);

                Console.WriteLine("==== LICENSE INFO =====");
				Console.WriteLine(ProtocolIEC870REE.GetLicenseInfo());

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

                CPortConfigTCPIP PortConfigTCPIP = new CPortConfigTCPIP();
                PortConfigTCPIP.IPAddress = "95.124.247.201";
                PortConfigTCPIP.IPPort = 20000;
                PortConfigTCPIP.Timeout = 2000;
                ProtocolIEC870REE.SetPortConfig(PortConfigTCPIP);

                CProtocolIEC870REEConnection ProtocolIEC870REEConnection = new CProtocolIEC870REEConnection();
                ProtocolIEC870REEConnection.LinkAddress = 1;
                ProtocolIEC870REEConnection.MeasuringPointAddress = 1;
                ProtocolIEC870REEConnection.Password = 1;
                ProtocolIEC870REEConnection.OpenSessionRetries = 1;
                ProtocolIEC870REEConnection.OpenSessionTimeout = 2000;
                ProtocolIEC870REEConnection.MacLayerRetries = 1;
                ProtocolIEC870REEConnection.MacLayerRetriesDelay = 1000;

                ProtocolIEC870REE.SetConnectionConfig(ProtocolIEC870REEConnection);

                CTimeInfo DateFrom = new CTimeInfo(2017, 10, 1, 1, 0, 0, 0);
                CTimeInfo DateTo = new CTimeInfo(2017, 11, 3, 0, 0, 0, 0);                

                ProtocolIEC870REE.OpenPort();
                ProtocolIEC870REE.OpenSession();
                               
                CTotals Totals = ProtocolIEC870REE.ReadTotalsHistory(1, DateFrom, DateTo);

                foreach (CTotal Total in Totals.Totals)
                {
                    
                    Console.WriteLine(String.Format("Periodo A {0:d} - {1:d}: [tarifa {2}] = {3}", Total.PeriodStart.ToString() , Total.PeriodEnd.ToString(), Total.Tariff, Total.ActiveEnergyAbs));
                    Console.WriteLine(String.Format("Periodo R {0:d} - {1:d}: [tarifa {2}] = {3}", Total.PeriodStart.ToString() , Total.PeriodEnd.ToString(), Total.Tariff, Total.ReactiveInductiveEnergyAbs));
                }                                               
                
                ProtocolIEC870REE.CloseSession();

                ProtocolIEC870REE.ClosePort();
                Environment.Exit(0);
            }
            catch (LICENSE_RESULT elr)
            {
                Console.WriteLine(elr.Message);                
                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Environment.Exit(1);
            }            
        }        
    }
}
