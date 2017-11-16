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

namespace GISCE
{
    class REEMote
    {
        static void Main(string[] args)
        {
			String LicenseMachine = Environment.GetEnvironmentVariable("DAIZACOM_LICENSE_MACHINE");
			String LicensePackage = Environment.GetEnvironmentVariable("DAIZACOM_LICENSE_PACKAGE");

			Console.WriteLine (LicenseMachine);
			Console.WriteLine (LicensePackage);

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

                ProtocolIEC870REE.OpenPort();
                ProtocolIEC870REE.OpenSession();

                CTimeInfo DateFrom = new CTimeInfo(2017, 10, 1, 1, 0, 0, 0);
                CTimeInfo DateTo = new CTimeInfo(2017, 11, 3, 0, 0, 0, 0);
                CTotals Totals = ProtocolIEC870REE.ReadTotalsHistory(1, DateFrom, DateTo);
                PersonalizedTotals PTotals = new PersonalizedTotals(Totals);

                var json = new JavaScriptSerializer().Serialize(PTotals);
                Console.WriteLine(json);

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

    public class PersonalizedTotals
    {
        public byte Contract;
        public string DateFrom;
        public string DateTo;
        public List<PersonalizedTotal> Totals;
        public PersonalizedTotals(CTotals totals)
        {
            Contract = totals.Contract;
            DateFrom = totals.DateFrom.ToString();
            DateTo   = totals.DateTo.ToString();
            Totals = new List<PersonalizedTotal>();
            foreach (CTotal total in totals.Totals)
            {
                Totals.Add(new PersonalizedTotal(total));
            }
        }
    }

    public class PersonalizedTotal
    {
        public byte Tariff;
        public int Excess;
        public string MaximumDemandTimeStamp;
        public byte QualityMaximumDemand;
        public int MaximumDemand;
        public byte QualityReservedField8;
        public int ReservedField8;
        public byte QualityReservedField7;
        public int ReservedField7;
        public byte QualityReactiveCapacitiveEnergy;
        public int ReactiveCapacitiveEnergyInc;
        public int ReactiveCapacitiveEnergyAbs;
        public byte QualityReactiveInductiveEnergy;
        public int ReactiveInductiveEnergyInc;
        public int ReactiveInductiveEnergyAbs;
        public byte QualityActiveEnergy;
        public int ActiveEnergyInc;
        public int ActiveEnergyAbs;
        public string PeriodEnd;
        public string PeriodStart;
        public byte QualityExcess;
        public PersonalizedTotal(CTotal total)
        {
            Tariff = total.Tariff;
            PeriodStart = total.PeriodStart.ToString();
            PeriodEnd = total.PeriodEnd.ToString();
            ActiveEnergyAbs = total.ActiveEnergyAbs;
            ActiveEnergyInc = total.ActiveEnergyInc;
            QualityActiveEnergy = total.QualityActiveEnergy;
            ReactiveInductiveEnergyAbs = total.ReactiveInductiveEnergyAbs;
            ReactiveInductiveEnergyInc = total.ReactiveInductiveEnergyInc;
            QualityReactiveInductiveEnergy = total.QualityReactiveInductiveEnergy;
            ReactiveCapacitiveEnergyAbs = total.ReactiveCapacitiveEnergyAbs;
            ReactiveCapacitiveEnergyInc = total.ReactiveCapacitiveEnergyInc;
            QualityReactiveCapacitiveEnergy = total.QualityReactiveCapacitiveEnergy;
            ReservedField7 = total.ReservedField7;
            QualityReservedField7 = total.QualityReservedField7;
            ReservedField8 = total.ReservedField8;
            QualityReservedField8 = total.QualityReservedField8;
            MaximumDemand = total.MaximumDemand;
            QualityMaximumDemand = total.QualityMaximumDemand;
            MaximumDemandTimeStamp = total.MaximumDemandTimeStamp.ToString();
            Excess = total.Excess;
            QualityExcess = total.QualityExcess;
        }

    }

}
