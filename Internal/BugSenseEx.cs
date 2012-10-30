using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace BugSense.Internal {
    [DataContract]
    public class BugSenseEx {
        internal Exception OriginalException { get; set; }
        [DataMember(Name = "message")]
        public string Name { get; set; }
        [DataMember(Name = "backtrace")]
        public string StackTrace { get; set; }
        [DataMember(Name = "occured_at")]
        public DateTime DateOccured { get; set; }
        [DataMember(Name = "klass")]
        public string ClassName { get; set; }
        [DataMember(Name = "where")]
        public string Where { get; set; }
        public string Tag { get; set; }
        [DataMember(Name = "breadcrumbs")]
        public string Breadcrumbs { get; set; }
        [DataMember(Name = "handled")]
        public int Handled { get; set; }
    }

    [DataContract]
    public class AppEnvironment {
        [DataMember(Name = "uid")]
        public string Uid { get; set; }
        [DataMember(Name = "phone")]
        public string PhoneModel { get; set; }
        [DataMember(Name = "manufacturer")]
        public string PhoneManufacturer { get; set; }
        [DataMember(Name = "appver")]
        public string AppVersion { get; set; }
        [DataMember(Name = "appname")]
        public string AppName { get; set; }
        [DataMember(Name = "osver")]
        public string OsVersion { get; set; }
        [DataMember(Name = "wifi_on")]
        public int WifiOn { get; set; }
        [DataMember(Name = "gps_on")]
        public string GpsOn { get; set; }
        [DataMember(Name = "cellular_data")]
        public string  CellularData { get; set; }
        [DataMember(Name = "carrier")]
        public string Carrier { get; set; }
        [DataMember(Name = "screen:width")]
        public double ScreenWidth { get; set; }
        [DataMember(Name = "screen:height")]
        public double ScreenHeight { get; set; }
        [DataMember(Name = "screen:orientation")]
        public string ScreenOrientation { get; set; }
        [DataMember(Name = "screen_dpi(x:y)")]
        public string ScreenDpi { get; set; }
        [DataMember(Name = "rooted")]
        public bool Rooted { get; set; }
        [DataMember(Name = "locale")]
        public string Locale { get; set; }
    }

    [DataContract]
    public class BugSenseRequest {
        public BugSenseRequest() { }
        public BugSenseRequest(BugSenseEx ex, AppEnvironment environment, Dictionary<string, string> extradata)
        {
            Client = new BugSenseClient();
            Request = new BugSenseInternalRequest();
            Request.Tag = string.IsNullOrEmpty(ex.Tag) ? null : ex.Tag;
            // TODO: Comment should ask user for feedback
            Request.Comment = "";
            Exception = ex;
            AppEnvironment = environment;
            LogData = extradata;
        }
        [DataMember(Name = "log_data")]
        public Dictionary<string, string> LogData { get; set; }
        [DataMember(Name = "exception")]
        public BugSenseEx Exception { get; set; }
        [DataMember(Name = "application_environment")]
        public AppEnvironment AppEnvironment { get; set; }
        [DataMember(Name = "client")]
        public BugSenseClient Client { get; set; }
        [DataMember(Name = "request")]
        public BugSenseInternalRequest Request { get; set; }
    }

    [DataContract]
    public class BugSenseClient {

        public BugSenseClient()
        {
            Version = "bugsense-version-" + G.BUGSENSE_API_VER;
            Name = "bugsense-wp8";
        }

        [DataMember(Name = "version")]
        public string Version { get; set; }
        [DataMember(Name = "name")]
        public string Name { get; set; }
    }

    [DataContract]
    public class BugSenseInternalRequest {
        [DataMember(Name = "tag")]
        public string Tag { get; set; }
        [DataMember(Name = "comment")]
        public string Comment { get; set; }
    }
}
