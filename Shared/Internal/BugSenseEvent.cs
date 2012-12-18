using System;

namespace BugSense.Internal
{
    internal class BugSenseEventRequest
    {
		#region [ Attributes ]
        private const int MAX_BYTES = 256 - 1;

		public string ApiVer { get; set; }
		public string Tag { get; set; }
		public string PhoneModel { get; set; }
		public string PhoneManufacturer { get; set; }
		public string OsVer { get; set; }
		public string AppVer { get; set; }
		public string Locale { get; set; }
		public string TimeStamp { get; set; }
		#endregion

		#region [ Ctor ]
		public BugSenseEventRequest() { }
        public BugSenseEventRequest(AppEnvironment environment, string tag, bool reserved = false)
        {
            ApiVer = G.BUGSENSE_API_VER;
            string tmp = tag.Trim();
            if (!reserved)
            {
                if (tmp[0] == '_')
                {
                    char[] tmpc = tmp.ToCharArray();
                    tmpc[0] = '-';
                    tmp = new string(tmpc);
                }
                tmp = tmp.Replace("|", "-");
            }
            Tag = tmp;
            PhoneModel = environment.PhoneModel;
            PhoneManufacturer = environment.PhoneManufacturer;
            OsVer = environment.OsVersion;
            AppVer = environment.AppVersion;
            Locale = environment.Locale;
            TimeStamp = ((long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds).ToString();
        }
		#endregion

		#region [ Public methods ]
        public string getFlatLine()
        {
            string s1 = ApiVer + ":";
            string s2 = ":" + PhoneModel + ":" + PhoneManufacturer + ":"
                            + OsVer + ":" + AppVer + ":" + Locale
                            + ":" + TimeStamp;
            int len1 = MAX_BYTES - (s1.Length + s2.Length);
            string sx = Tag;
            if (sx.Length > len1)
                sx = Tag.Substring(0, len1);

            return s1 + sx + s2;
        }

		public bool IsSimilarTo(BugSenseEventRequest that)
		{
			bool res = true;

			res = ApiVer.Equals(that.ApiVer);
			res = res && Tag.Equals(that.Tag);
			res = res && PhoneModel.Equals(that.PhoneModel);
			res = res && PhoneManufacturer.Equals(that.PhoneManufacturer);
			res = res && OsVer.Equals(that.OsVer);
			res = res && AppVer.Equals(that.AppVer);
			res = res && Locale.Equals(that.Locale);

			return res;
		}
		#endregion
    }
}
