namespace BugSense
{
    /// <summary>
    /// Notification Options Type
    /// </summary>
    public enum enNotificationType
    {
#if (WINDOWS_PHONE || NETFX_CORE)
        /// <summary>None</summary>
        None,
        /// <summary>Confirmation MessageBox</summary>
        MessageBoxConfirm
#else
        /// <summary>None</summary>
		None
#endif
    }
}
