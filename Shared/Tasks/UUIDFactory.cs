using BugSense.Extensions;
using BugSense.Internal;
using System.IO;
using System.Threading.Tasks;

namespace BugSense.Tasks
{
    internal class UUIDFactory
    {
        public async static Task<string> Get()
        {
            string uuid = "";

            FilesReadResult res = await Files.ReadFrom(Path.Combine(G.FolderName, G.UUIDFileName));
            if (res.Result)
                uuid = res.Str;
            if (uuid.Length != G.UuidLen)
            {
                uuid = EntropyUUID.UUID.getNew();
                await Files.WriteTo(G.FolderName, G.UUIDFileName, uuid);
            }

            return uuid;
        }
    }
}
