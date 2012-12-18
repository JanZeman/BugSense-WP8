using BugSense.Extensions;
using BugSense.Internal;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BugSense.Tasks
{
    internal class UUIDFactory
    {
        private static readonly Semaphore _lock = new Semaphore(1, 1);

        public async static Task<string> Get()
        {
            string uuid = "";

            _lock.WaitOne();
            Tuple<string, bool> res = await Files.ReadFrom(Path.Combine(G.FolderName, G.UUIDFileName));
            _lock.Release();
            if (res.Item2)
			{
				Helpers.Log("UUIDFactory :: UUID file read");
				uuid = res.Item1;
			} else
				Helpers.Log("UUIDFactory :: UUID file not found");
            if (uuid.Length != G.UuidLen)
            {
				Helpers.Log("UUIDFactory :: generating UUID");
				uuid = EntropyUUID.UUID.GetNew();
				bool wrote = await Files.CreatWriteTo(G.FolderName, G.UUIDFileName, uuid);
				Helpers.Log("UUIDFactory :: trying to write UUID to file: " + wrote);
			}
			Helpers.Log("UUIDFactory :: UUID is " + uuid);

            return uuid;
        }
    }
}
