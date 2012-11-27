using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
#if WINDOWS_PHONE
using System.IO.IsolatedStorage;
#elif NETFX_CORE
using Windows.Storage;
using Windows.Storage.Streams;
#endif

namespace BugSense.Extensions
{
    internal class FilesReadResult
    {
        public string Str;
        public bool Result;

        public FilesReadResult(string str, bool result)
        {
            this.Str = str;
            this.Result = result;
        }

        public FilesReadResult()
        {
            this.Str = "";
            this.Result = false;
        }
    }

    internal class Files
    {
        public async static Task<bool> Exists(string fpathname)
        {
#if WINDOWS_PHONE
            return await Task.Run(() =>
                {
                    using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        try
                        {
                            if (!storage.FileExists(fpathname))
                                return false;
                            return true;
                        }
                        catch (Exception)
                        {
                            return false;
                        }
                    }
                });
#elif NETFX_CORE
            try
            {
                var file = await ApplicationData.Current.LocalFolder.GetFileAsync(fpathname);
                if (file != null)
                    return true;
            }
            catch (Exception)
            {
            }

            return false;
#endif
        }

        public async static Task<List<string>> GetDirFilenames(string path, string prefix)
        {
#if WINDOWS_PHONE
            return await Task.Run(() =>
                {
                    List<string> result = new List<string>();

                    using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (storage.DirectoryExists(path))
                        {
                            try
                            {
                                string[] files = storage.GetFileNames(path + "\\" + prefix + "*");
                                var filesList = new List<string>(files);
                                if (filesList != null)
                                {
                                    filesList.Sort(delegate(string a, string b)
                                    {
                                        int xdiff = a.CompareTo(b);
                                        return -xdiff;
                                    });
                                    foreach (var fname in filesList)
                                    {
                                        if (fname.StartsWith(prefix))
                                            result.Add(fname);
                                    }
                                }
                                return result;
                            }
                            catch (Exception)
                            {
                                return new List<string>();
                            }
                        }
                        return new List<string>();
                    }
                });
#elif NETFX_CORE
            List<string> result = new List<string>();

            try
            {
                StorageFolder folder = await ApplicationData.Current.LocalFolder.GetFolderAsync(path);
                // no exception means file exists
                var list = await folder.GetFilesAsync();
                if (list != null)
                {
                    var list2 = new List<StorageFile>(list);
                    list2.Sort(delegate(StorageFile a, StorageFile b)
                    {
                        int xdiff = a.Name.CompareTo(b.Name);
                        return -xdiff;
                    });
                    foreach (var fname in list2)
                    {
                        if (fname.Name.StartsWith(prefix))
                            result.Add(fname.Name);
                    }
                }

                return result;
            }
            catch (Exception)
            {
                // find out through exception 
            }

            return new List<string>();
#endif
        }

        public async static Task<bool> WriteTo(string path, string fname, string str)
        {
            var fpathname = Path.Combine(path, fname);

#if WINDOWS_PHONE
            return await Task.Run(() =>
                {
                    using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        try
                        {
                            if (!storage.DirectoryExists(path))
                                storage.CreateDirectory(path);
                            if (storage.DirectoryExists(path))
                            {
                                using (var fileStream = storage.OpenFile(fpathname, FileMode.OpenOrCreate))
                                {
                                    using (StreamWriter sr = new StreamWriter(fileStream))
                                    {
                                        sr.Write(str);

                                        return true;
                                    }
                                }
                            }
                        }
                        catch (Exception)
                        {
                            return false;
                        }
                    }

                    return false;
                });
#elif NETFX_CORE
            bool result = false;
            StorageFile file = null;

            try
            {
                var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(path, CreationCollisionOption.OpenIfExists);
                file = await folder.CreateFileAsync(fname, CreationCollisionOption.ReplaceExisting);
            }
            catch (Exception)
            {
                return false;
            }

            using (var fs = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                using (var outStream = fs.GetOutputStreamAt(0))
                {
                    using (var writer = new DataWriter(outStream))
                    {
                        writer.WriteString(str);
                        await writer.StoreAsync();
                        writer.DetachStream();
                    }
                    result = await outStream.FlushAsync();
                }
            }

            return result;
#endif
        }

        public async static Task<FilesReadResult> ReadFrom(string fpathname)
        {
#if WINDOWS_PHONE
            return await Task.Run(() =>
                {
                    using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (!storage.FileExists(fpathname))
                            return new FilesReadResult("", false);
                        using (var fileStream = storage.OpenFile(fpathname, FileMode.Open))
                        {
                            using (StreamReader sr = new StreamReader(fileStream))
                            {
                                string data = sr.ReadToEnd();

                                return new FilesReadResult(data, true);
                            }
                        }
                    }
                });
#elif NETFX_CORE
            FilesReadResult result = new FilesReadResult("", false);
            StorageFile file = null;

            try
            {
                file = await ApplicationData.Current.LocalFolder.GetFileAsync(fpathname);
                if (file == null)
                    return result;
            }
            catch (Exception)
            {
                return result;
            }

            using (var fs = await file.OpenAsync(FileAccessMode.Read))
            {
                using (var inStream = fs.GetInputStreamAt(0))
                {
                    using (var reader = new DataReader(inStream))
                    {
                        await reader.LoadAsync((uint)fs.Size);
                        string data = reader.ReadString((uint)fs.Size);
                        reader.DetachStream();

                        result = new FilesReadResult(data, true);
                    }
                }
            }

            return result;
#endif
        }

        public async static Task Delete(string fpathname)
        {
#if WINDOWS_PHONE
            await Task.Run(() =>
                {
                    using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        try
                        {
                            if (storage.FileExists(fpathname))
                                storage.DeleteFile(fpathname);
                        }
                        catch (Exception)
                        {
                        }
                    }
                });
#elif NETFX_CORE
            var fileName = fpathname;
            try
            {
                var file = await ApplicationData.Current.LocalFolder.GetFileAsync(fileName);
                if (file != null)
                    await file.DeleteAsync();
            }
            catch (Exception)
            {
            }
#endif
        }

        public async static Task Delete(string path, string fname)
        {
            await Delete(Path.Combine(new string[] { path, fname }));
        }
    }
}
