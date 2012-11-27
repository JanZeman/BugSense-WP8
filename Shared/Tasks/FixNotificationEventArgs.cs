using System;

namespace BugSense.Tasks
{
    internal class FixNotificationEventArgs : EventArgs
    {
        public string FixResponse { get; set; }
        public bool IsFatal { get; set; }

        public FixNotificationEventArgs(string fixResponse, bool isFatal)
            : base()
        {
            FixResponse = fixResponse;
            IsFatal = isFatal;
        }
    }
}
