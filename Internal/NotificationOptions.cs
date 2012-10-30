namespace BugSense {
    /// <summary>
    /// Notification Options
    /// </summary>
    public class NotificationOptions {
        /// <summary>
        /// The type of notification that the user receives when an error occurs.
        /// </summary>
        public enNotificationType Type { get; set; }
        /// <summary>
        /// Header text of notification popup
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// Body of notification popup
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// If true then the error is sent even when the Debugger is attached. Default false.
        /// </summary>
        public bool HandleWhileDebugging { get; set; }
    }
}