using System;
using Daiza.Com.Protocol_IEC870REE.Readouts;
using System.Collections.Generic;
using System.Globalization;

namespace GISCE.Net.Readings {
    public static class Utilities {
        public static string parse_date(string date)
        {
            return DateTime.ParseExact(date, "yyyy/M/d H:m:s.f", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd HH:mm:ss");
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
            DateFrom = Utilities.parse_date(totals.DateFrom.ToString());
            DateTo = Utilities.parse_date(totals.DateTo.ToString());
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
            PeriodStart = Utilities.parse_date(total.PeriodStart.ToString());
            PeriodEnd = Utilities.parse_date(total.PeriodEnd.ToString());
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
            MaximumDemandTimeStamp = Utilities.parse_date(total.MaximumDemandTimeStamp.ToString());
            Excess = total.Excess;
            QualityExcess = total.QualityExcess;
        }

    }
}