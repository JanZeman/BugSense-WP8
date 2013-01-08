using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace BugSense.Internal
{
	#region [ BugSenseException ]
    [DataContract]
    internal class BugSenseException
    {
		#region [ BugSenseException:Attributes ]
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
		#endregion

		#region [ BugSenseException:Public methods ]
		public bool IsSimilarTo(BugSenseException that)
		{
			bool res = false;

			res = OriginalException.Equals(that.OriginalException);
			res = res && Name.Equals(that.Name);
			res = res && StackTrace.Equals(that.StackTrace);
			res = res && ClassName.Equals(that.ClassName);
			res = res && Where.Equals(that.Where);
			res = res && Tag.Equals(that.Tag);
			res = res && Breadcrumbs.Equals(that.Breadcrumbs);
			res = res && (Handled == that.Handled);

			return res;
		}
		#endregion
	}
	#endregion

	#region [ AppEnvironment ]
    [DataContract]
    internal class AppEnvironment
    {
		#region [ AppEnvironment:Attributes ]
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
        public string CellularData { get; set; }
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
        [DataMember(Name = "geo_region")]
        public string GeoRegion { get; set; }
        [DataMember(Name = "cpu_model")]
        public string CpuModel { get; set; }
        [DataMember(Name = "cpu_bitness")]
        public int CpuBitness { get; set; }
        [DataMember(Name = "is_trial")]
        public bool IsTrial { get; set; }
		#endregion

		#region [ AppEnvironment:Public methods ]
		public bool IsSimilarTo(AppEnvironment that)
		{
			bool res = false;
			
			res = Uid.Equals(that.Uid);
			res = res && PhoneModel.Equals(that.PhoneModel);
			res = res && PhoneManufacturer.Equals(that.PhoneManufacturer);
			res = res && AppVersion.Equals(that.AppVersion);
			res = res && AppName.Equals(that.AppName);
			res = res && OsVersion.Equals(that.OsVersion);
			res = res && Locale.Equals(that.Locale);
			res = res && GeoRegion.Equals(that.GeoRegion);
			res = res && CpuModel.Equals(that.CpuModel);
			res = res && (CpuBitness == that.CpuBitness);
			res = res && (IsTrial == that.IsTrial);

			return res;
		}
		#endregion
	}
	#endregion

	#region [ BugSenseExceptionRequest ]
    [DataContract]
    internal class BugSenseExceptionRequest
    {
		#region [ BugSenseExceptionRequest:Attributes ]
		[DataMember(Name = "log_data")]
		public Dictionary<string, string> LogData { get; set; }
		[DataMember(Name = "exception")]
		public BugSenseException Exception { get; set; }
		[DataMember(Name = "application_environment")]
		public AppEnvironment AppEnvironment { get; set; }
		[DataMember(Name = "client")]
		public BugSenseClient Client { get; set; }
		[DataMember(Name = "request")]
		public BugSenseInternalRequest Request { get; set; }
		#endregion

		#region [ BugSenseExceptionRequest:Ctor ]
		public BugSenseExceptionRequest() { }
        public BugSenseExceptionRequest(BugSenseException ex, AppEnvironment environment, 
			Dictionary<string, string> extradata)
        {
            Client = new BugSenseClient();
            Request = new BugSenseInternalRequest();
            Request.Tag = string.IsNullOrEmpty(ex.Tag) ? null : ex.Tag;
            //TODO: Comment should ask user for feedback
            Request.Comment = "";
            Exception = ex;
            AppEnvironment = environment;
            LogData = extradata;
        }
		#endregion

		#region [ BugSenseExceptionRequest:Public methods ]
		public bool IsSimilarTo(BugSenseExceptionRequest that)
		{
			bool res = false;

			res = LogData.Count == that.LogData.Count && !LogData.Except(that.LogData).Any();
			res = res && Exception.IsSimilarTo(that.Exception);
			res = res && AppEnvironment.IsSimilarTo(that.AppEnvironment);
			res = res && Client.IsSimilarTo(that.Client);
			res = res && Request.IsSimilarTo(that.Request);

			return res;
		}
		#endregion
    }
	#endregion

	#region [ BugSenseClient ]
    [DataContract]
    internal class BugSenseClient
    {
		#region [ BugSenseClient:Attributes ]
		[DataMember(Name = "version")]
		public string Version { get; set; }
		[DataMember(Name = "name")]
		public string Name { get; set; }
		[DataMember(Name = "flavor")]
		public string Flavor { get; set; }
		#endregion

		#region [ BugSenseClient:Ctor ]
		public BugSenseClient()
        {
            Version = "bugsense-version-" + G.BUGSENSE_API_VER;
            Name = G.BUGSENSE_NAME;
            Flavor = G.BUGSENSE_FLAVOR;
        }
		#endregion

		#region [ BugSenseClient:Public methods ]
		public bool IsSimilarTo(BugSenseClient that)
		{
			bool res = false;

			res = Version.Equals(that.Version);
			res = res && Name.Equals(that.Name);
			res = res && Flavor.Equals(that.Flavor);

			return res;
		}
		#endregion
	}
	#endregion

	#region [ BugSenseInternalRequest ]
    [DataContract]
    internal class BugSenseInternalRequest
    {
		#region [ BugSenseInternalRequest:Attributes ]
        [DataMember(Name = "tag")]
        public string Tag { get; set; }
        [DataMember(Name = "comment")]
        public string Comment { get; set; }
		#endregion

		#region [ BugSenseInternalRequest:Public methods ]
		public bool IsSimilarTo(BugSenseInternalRequest that)
		{
			return Tag.Equals(that.Tag) && Comment.Equals(that.Comment);
		}
		#endregion
    }
	#endregion
}
