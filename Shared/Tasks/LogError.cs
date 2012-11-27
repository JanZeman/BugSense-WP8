using BugSense.Extensions;
using BugSense.Internal;
using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace BugSense.Tasks
{
    internal class LogError
    {
        private readonly BugSenseRequest _request;
        private readonly bool _isFatal;
        private static readonly DataContractJsonSerializer _jsonSerializer =
            new DataContractJsonSerializer(typeof(BugSenseRequest));

        public LogError(BugSenseRequest request, bool isFatal)
        {
            _request = request;
            _isFatal = isFatal;
        }

        public async Task<string> Execute()
        {
            if (_request == null)
                return string.Empty;
            string json = GetJson(_request);
            if (!string.IsNullOrEmpty(json))
            {
                string fileName = await SaveToFile(json);
                if (!string.IsNullOrEmpty(fileName))
                    return fileName;
            }
            return string.Empty;
        }

        private string GetJson(BugSenseRequest request)
        {
            try
            {
                Helpers.Log("LogError 1/2 :: Serializing JSON");
                using (MemoryStream ms = new MemoryStream())
                {
                    _jsonSerializer.WriteObject(ms, request);
                    var array = ms.ToArray();
                    string json = Encoding.UTF8.GetString(array, 0, array.Length);
                    Helpers.Log("LogError 1/2 :: Done serializing JSON");
                    return json;
                }
            }
            catch (Exception)
            {
                Helpers.Log("LogError 1/2 :: Error during JSON serialization");
                return string.Empty;
            }
        }

        private async Task<string> SaveToFile(string postData)
        {
            Helpers.Log("LogError 2/2 :: Saving to file");
            string fname = _isFatal ? G.CrashFileName : G.LoggedExceptionFileName;
            string fileName = string.Format(fname, DateTime.UtcNow.ToString("yyyyMMddHHmmss"), Guid.NewGuid());
            bool result = await Files.WriteTo(G.FolderName, fileName, postData);
            Helpers.Log("LogError 2/2 :: Done saving to file: " + result);
            if (result)
                return fileName;

            // Getting in here means the device is about to explode!
            return string.Empty;
        }
    }
}
