using System.Runtime.Serialization;

namespace BugSense.Internal
{
	#region [ FixResponse ]
    [DataContract]
    internal class FixResponse
    {
		#region [ FixResponse:Attributes ]
        [DataMember(Name = "data")]
        public FixResponseData Data { get; set; }
        [DataMember(Name = "error")]
        public string Error { get; set; }
		#endregion
    }
	#endregion

	#region [ FixResponseData ]
    [DataContract]
    internal class FixResponseData
    {
		#region [ FixResponseData:Attributes ]
        [DataMember(Name = "url")]
        public string Url { get; set; }
        [DataMember(Name = "contentText")]
        public string ContentText { get; set; }
        [DataMember(Name = "contentTitle")]
        public string ContentTitle { get; set; }
		#endregion
    }
	#endregion
}
