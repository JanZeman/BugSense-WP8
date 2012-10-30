using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace BugSense_WP8.Internal
{
    [DataContract]
    public class FixResponse
    {
        [DataMember(Name = "data")]
        public FixResponseData Data { get; set; }
        [DataMember(Name = "error")]
        public string Error { get; set; }
    }

    [DataContract]
    public class FixResponseData {
        [DataMember(Name = "url")]
        public string Url { get; set; }
        [DataMember(Name = "contentText")]
        public string ContentText { get; set; }
        [DataMember(Name = "contentTitle")]
        public string ContentTitle { get; set; }
    }
}
