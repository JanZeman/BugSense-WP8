using BugSense.Extensions;
using BugSense.Internal;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BugSense.Tasks
{
    internal class ProcessRequests
    {
		#region [ Attributes ]
        private string _uuid = "";
		#endregion

		#region [ Ctor ]
        public ProcessRequests(string uuid)
        {
            _uuid = uuid;
        }
		#endregion

		#region [ Public methods ]
        public async Task<int> Execute()
        {
            int r = 0;

            try
            {
                var taskList = new List<SendRequest>();
                int counter = 0;

                List<string> list = await Files.GetDirFilenames(G.FolderName, G.CrashFileNamePrefix);
                List<string> list2 = await Files.GetDirFilenames(G.FolderName, G.LoggedExceptionFileNamePrefix);
                List<string> list3 = await Files.GetDirFilenames(G.FolderName, G.PingFileNamePrefix);
                List<string> list4 = await Files.GetDirFilenames(G.FolderName, G.EventFileNamePrefix);

                counter = await ProcessList(taskList, list, G.MaxCrashes, true, _uuid);
				Helpers.Log("ProcessRequests 1/5 :: gotExceptions: " + counter);
                counter = await ProcessList(taskList, list2, G.MaxLoggedExceptions, true, _uuid);
				Helpers.Log("ProcessRequests 2/5 :: gotLoggedExceptions: " + counter);
                counter = await ProcessList(taskList, list3, G.MaxPings, false, _uuid);
				Helpers.Log("ProcessRequests 3/5 :: gotPings: " + counter);
                counter = await ProcessList(taskList, list4, G.MaxEvents, false, _uuid);
                Helpers.Log("ProcessRequests 4/5 :: gotEvents: " + counter);

				Helpers.Log("ProcessRequests 5/5 :: sending: " + taskList.Count);
                if (taskList != null)
                {
                    foreach (SendRequest err in taskList)
                    {
                        //NOTE: r is equal to the _attempted_ executions
                        //  (we don't know if each web request was successful)
                        bool rr = await err.Execute();
                        if (rr)
                            r++;
                    }
                }
            }
            catch (Exception)
            { // Swallow like a fish - Not much that we can do here
            }

            return r;
        }
		#endregion

		#region [ Private methods ]
        private async static Task<int> ProcessList(List<SendRequest> taskList, List<string> list, int max,
            bool isException, string uuid)
        {
            int counter = 0;

            if (list != null)
            {
                foreach (var fileName in list)
                {
                    if (string.IsNullOrEmpty(fileName))
                        continue;
                    // If there are more messages in the pool we just delete them
                    if (counter < max)
                    {
                        if (isException)
                            taskList.Add(new SendRequest(fileName));
                        else
                            taskList.Add(new SendRequest(WebRequests.GetEventURL(uuid), fileName, false));
                    }
                    else
                        await Files.Delete(G.FolderName, fileName);
                    counter++;
                    //
                }
            }

            return counter;
        }
		#endregion
    }
}
