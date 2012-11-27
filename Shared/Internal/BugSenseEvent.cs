using System;

namespace BugSense.Internal
{
    internal class BugSenseEvent
    {
        public BugSenseEvent() { }
        public BugSenseEvent(AppEnvironment environment, string tag)
        {
            ApiVer = G.BUGSENSE_API_VER;
            Tag = tag;
            PhoneModel = environment.PhoneModel;
            PhoneManufacturer = environment.PhoneManufacturer;
            OsVer = environment.OsVersion;
            AppVer = environment.AppVersion;
            Locale = environment.Locale;
            TimeStamp = ((long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds).ToString();
        }
        public string ApiVer { get; set; }
        public string Tag { get; set; }
        public string PhoneModel { get; set; }
        public string PhoneManufacturer { get; set; }
        public string OsVer { get; set; }
        public string AppVer { get; set; }
        public string Locale { get; set; }
        public string TimeStamp { get; set; }

        public string getFlatLine()
        {
            return ApiVer + ":" + Tag + ":" + PhoneModel + ":" + PhoneManufacturer + ":"
                            + OsVer + ":" + AppVer + ":" + Locale
                            + ":" + TimeStamp;
        }
    }
}
