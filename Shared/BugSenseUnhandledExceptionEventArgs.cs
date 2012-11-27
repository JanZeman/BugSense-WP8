using System;
#if WINDOWS_PHONE
using System.Windows;
#endif

namespace BugSense
{
#if NETFX_CORE
    public class BugSenseUnhandledDummyObj
    {
        public Exception Ex;
        public bool Handled { get; set; }

        public BugSenseUnhandledDummyObj(Exception ex, bool Handled)
        {
            this.Ex = ex;
            this.Handled = Handled;
        }
    }
#endif

#if WINDOWS_PHONE
    public class BugSenseUnhandledExceptionEventArgs : ApplicationUnhandledExceptionEventArgs
    {
#elif NETFX_CORE
    public class BugSenseUnhandledExceptionEventArgs : BugSenseUnhandledDummyObj
    {
#endif

        /// <summary>
        /// Cancel the error handling by BugSense. Should be used with UnhandledException event of <see cref="BugSenseHandler"/>.
        /// </summary>
        public bool Cancel { get; set; }

        /// <summary>
        /// A custom message for the exception that occured. ex: A username, user options etc.
        /// </summary>
        public string Comment { get; set; }

        public BugSenseUnhandledExceptionEventArgs(Exception ex, bool handled)
            : base(ex, handled)
        {
        }
    }
}
