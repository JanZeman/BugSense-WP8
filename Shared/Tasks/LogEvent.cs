using BugSense.Extensions;
using BugSense.Internal;
using System;
using System.Threading.Tasks;

namespace BugSense.Tasks
{
    internal class LogEvent
    {
        private readonly string _line;

        public LogEvent(string line)
        {
            _line = line;
        }

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

        private async Task<string> SaveToFile(string postData)
        {
            Helpers.Log("LogEvent :: Saving to file");
            string fileName = string.Format(G.PingFileName, DateTime.UtcNow.ToString("yyyyMMddHHmmss"), Guid.NewGuid());
            bool result = await Files.WriteTo(G.FolderName, fileName, postData);
            Helpers.Log("LogEvent :: Done saving to file: " + result);
            if (result)
                return fileName;

            // Getting in here means the device is about to explode!
            return string.Empty;
        }
    }
}
