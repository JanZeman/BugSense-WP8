using System;

namespace BugSense.Tasks
{
    internal class FixNotificationEventArgs : EventArgs
    {
		#region [ Attributes ]
        public string FixResponse { get; set; }
        public bool IsFatal { get; set; }
		#endregion

		#region [ Ctor ]
        public FixNotificationEventArgs(string fixResponse, bool isFatal)
            : base()
        {
            FixResponse = fixResponse;
            IsFatal = isFatal;
        }
		#endregion
    }
}
