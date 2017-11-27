using System;
using Daiza.Com.Protocol_IEC870REE.Readouts;
using System.Collections.Generic;
using System.Globalization;

namespace GISCE.Net.Profiles {

    public static class Utilities {
        public static string parse_date(string date)
        {
            return DateTime.ParseExact(date, "yyyy/M/d H:m:s.f", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd HH:mm:ss");
        }
    }

    public class PersonalizedProfiles
    {
        public byte Number;
        public bool Absolute;
        public string DateFrom;
        public string DateTo;
        public List<PersonalizedProfileRecord> Records;
        public PersonalizedProfiles(CLoadProfile profiles)
        {
            Number = profiles.Number;
            Absolute = profiles.Absolute;
            DateFrom = profiles.DateFrom.ToString("yyyy-MM-dd HH:mm:ss");
            DateTo = profiles.DateTo.ToString("yyyy-MM-dd HH:mm:ss");
            Records = new List<PersonalizedProfileRecord>();
            foreach (CLoadProfileRecord record in profiles.Records)
            {
                Records.Add(new PersonalizedProfileRecord(record));
            }
        }
    }

    public class PersonalizedProfileRecord
    {
        public string TimeInfo;
        public CLoadProfileChannel[] Channels;
        public PersonalizedProfileRecord(CLoadProfileRecord record)
        {
            TimeInfo = Utilities.parse_date(record.TimeInfo.ToString());
            Channels = record.Channels;
        }

    }

}