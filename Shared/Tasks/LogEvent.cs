using BugSense.Extensions;
using BugSense.Internal;
using System;
using System.Threading.Tasks;

namespace BugSense.Tasks
{
    internal class LogEvent
    {
		#region [ Attributes ]
        private readonly string _line;
        private readonly bool _reserved;
		#endregion

		#region [ Ctor ]
        public LogEvent(string line, bool reserved)
        {
            _line = line;
            _reserved = reserved;
        }
		#endregion

		#region [ Public methods ]
        public async Task<string> Execute()
        {
            if (_line == null)
                return string.Empty;
            string flat = _line;
            if (!string.IsNullOrEmpty(flat))
            {
                string fileName = await SaveToFile(flat);
                if (!string.IsNullOrEmpty(fileName))
                    return fileName;
            }
            return string.Empty;
        }
		#endregion

		#region [ Private methods ]
        private async Task<string> SaveToFile(string postData)
        {
            Helpers.Log("LogEvent :: Saving to file");
            string tmp = _reserved ? G.PingFileName : G.EventFileName;
            string fileName = string.Format(tmp, DateTime.UtcNow.ToString("yyyyMMddHHmmss"), Guid.NewGuid());
            bool result = await Files.CreatWriteTo(G.FolderName, fileName, postData);
            Helpers.Log("LogEvent :: Done saving to file: " + result);
            if (result)
                return fileName;

            // Getting in here means the device is about to explode!
            return string.Empty;
        }
		#endregion
    }
}
