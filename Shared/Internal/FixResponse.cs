using System.Runtime.Serialization;

namespace BugSense.Internal
{
    [DataContract]
    internal class FixResponse
    {
        [DataMember(Name = "data")]
        public FixResponseData Data { get; set; }
        [DataMember(Name = "error")]
        public string Error { get; set; }
    }

    [DataContract]
    internal class FixResponseData
    {
        [DataMember(Name = "url")]
        public string Url { get; set; }
        [DataMember(Name = "contentText")]
        public string ContentText { get; set; }
        [DataMember(Name = "contentTitle")]
        public string ContentTitle { get; set; }
    }
}
