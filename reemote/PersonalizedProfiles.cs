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

        public static string parse_channel(byte channel_name)
        {
            if (channel_name == CLoadProfileChannel.ACTIVE_IMPORT)
            {
                return "AI";
            }
            else if (channel_name == CLoadProfileChannel.ACTIVE_EXPORT)
            {
                return "AE";
            }
            else if (channel_name == CLoadProfileChannel.REACTIVE_QUADRANT_1)
            {
                return "R1";
            }
            else if (channel_name == CLoadProfileChannel.REACTIVE_QUADRANT_2)
            {
                return "R2";
            }
            else if (channel_name == CLoadProfileChannel.REACTIVE_QUADRANT_3)
            {
                return "R3";
            }
            else if (channel_name == CLoadProfileChannel.REACTIVE_QUADRANT_4)
            {
                return "R4";
            }
            else if (channel_name == CLoadProfileChannel.RES_7)
            {
                return "RES7";
            }
            else if (channel_name == CLoadProfileChannel.RES_8)
            {
                return "RES8";
            }
            else
            {
                string message = String.Format("Unexpected ChannelName found : {0}.",
                                               channel_name);
                Console.WriteLine(message);
                Environment.Exit(1);
                return "";
            }
        }
    }

    public class PersonalizedProfiles
    {
        public string SerialNumber;
        public byte Number;
        public bool Absolute;
        public string DateFrom;
        public string DateTo;
        public List<PersonalizedProfileRecord> Records;
        public PersonalizedProfiles(CLoadProfile profiles, int SerialNumber)
        {
            this.SerialNumber = SerialNumber.ToString();
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
        public List<PersonalizedProfileChannel> Channels;
        public PersonalizedProfileRecord(CLoadProfileRecord record)
        {
            TimeInfo = Utilities.parse_date(record.TimeInfo.ToString());
            Channels = new List<PersonalizedProfileChannel>();
            foreach (CLoadProfileChannel channel in record.Channels)
            {
                Channels.Add(new PersonalizedProfileChannel(channel));
            }
        }
    }

    public class PersonalizedProfileChannel
    {
        public string Magnitude;
        public int Value;
        public byte Quality;

        public PersonalizedProfileChannel(CLoadProfileChannel channel)
        {
            Magnitude = Utilities.parse_channel(channel.ChannelName);
            Value = channel.Value;
            Quality = channel.Quality;
        }
    }

}