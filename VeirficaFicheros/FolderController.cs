using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VeirficaFicheros.Properties;

namespace VeirficaFicheros
{
    class FolderController
    {
        public List<string> MonitoredFolders = new List<string>();
        public List<Record> FilesNotMatching = new List<Record>();

        private const string m_databaseFileName = "zxFileList.txt";
        private string m_fullPath, m_monitoredFolderList;

        private string m_selectedFolder, m_selectedDatabaseFileName;
        public void Load(string basePath)
        {
            m_fullPath = basePath;
            if (!Directory.Exists(m_fullPath))
                Directory.CreateDirectory(m_fullPath);

            m_monitoredFolderList = Path.Combine(m_fullPath, "MonitoredFolders.txt");

            if (File.Exists(m_monitoredFolderList))
                MonitoredFolders = File.ReadAllLines(m_monitoredFolderList).ToList<string>();
        }

        public void Add(string path)
        {
            MonitoredFolders.Add(path);
            SaveFolderList();
            
            Select(path);
            Create(path, false);
        }

        public void Select(string path)
        {
            if (m_selectedFolder == path)
                return;

            m_selectedFolder = path;
            m_selectedDatabaseFileName = Path.Combine(path, m_databaseFileName);
            FilesNotMatching.Clear();
        }
        public void Delete(string path)
        {
            var detailFile = Path.Combine(path, m_databaseFileName);
            if (File.Exists(detailFile))
                File.Delete(detailFile);

            var found = MonitoredFolders.IndexOf(path);
            if (found != -1)
                MonitoredFolders.RemoveAt(found);

            SaveFolderList();
        }

        public void Verify()
        {
            FilesNotMatching.Clear();

            Create(m_selectedFolder, true);

            var detailFile = Path.Combine(m_selectedFolder, m_databaseFileName);
            var lines = Array.Empty<String>();
            if (File.Exists(detailFile))
                lines = File.ReadAllLines(detailFile);

            var savedItems = lines.Select(Record.FromString).ToList();

            var exceptInSaved = savedItems.Except(m_records).ToList();
            var exceptInReal = m_records.Except(savedItems).ToList();

            foreach (var item in exceptInReal)
                item.What = "Added";

            foreach (var item in exceptInSaved)
                item.What = "Deleted";
            m_records.AddRange(exceptInSaved);

            //Differences
            FilesNotMatching.AddRange(exceptInReal);
            FilesNotMatching.AddRange(exceptInSaved);

            //Now, if someone does not match
            foreach (var item in from item in m_records let storedRecord = savedItems.FirstOrDefault(r => r.Name == item.Name) where storedRecord != null where item.Size != storedRecord.Size select item)
            {
                FilesNotMatching.Add(new Record { Name = item.Name, Size = item.Size, What = "SizeChanged" });
            }
        }

        internal void Update(IList selectedItems)
        {
            foreach (var item in selectedItems)
            {
                var r = (Record)item;
                if (r == null)
                    continue;

                Record found;
                var removeFromList = false;
                var info = new FileInfo(Path.Combine(m_selectedFolder, ".." + Path.DirectorySeparatorChar + r.Name));
                switch (r.What)
                {
                    case "Added":
                        if (info.Exists)
                        {
                            r.What = "Ok";
                            r.Size = info.Length;
                            File.AppendAllText(Path.Combine(m_selectedFolder, m_databaseFileName), r + Environment.NewLine);
                            m_records.Add(r);
                            removeFromList = true;
                        }
                        break;
                    case "Deleted":
                        found = m_records.FirstOrDefault(i => i.Name == r.Name);
                        if (found != null)
                        {
                            m_records.Remove(found);
                            SaveDetailFileList();
                            removeFromList = true;
                        }
                        break;
                    default:
                        if (info.Exists)
                        {
                            found = m_records.FirstOrDefault(i => i.Name == r.Name);
                            if (found != null)
                            {
                                found.Size = info.Length;
                                found.What = "Ok";
                                SaveDetailFileList();
                                removeFromList = true;
                            }
                        }
                        break;
                }

                if (removeFromList)
                {
                    FilesNotMatching.Remove(r);
                }
            }
        }

        private void SaveFolderList()
        {
            File.WriteAllLines(m_monitoredFolderList, MonitoredFolders);
        }

        private void SaveDetailFileList()
        {
            var lines = m_records.Select(item => item.ToString()).ToList();

            File.WriteAllLines(m_selectedDatabaseFileName, lines);
        }

        public class Record
        {
            public string Name;
            public long Size;
            public string What;

            public override string ToString()
            {
                return Name + "|" + Size.ToString() + "|" + What;
            }

            public override bool Equals(object obj)
            {
                var r = (Record)obj;
                if (r == null)
                    return false;
                return Name == r.Name;// && Size == r.Size;
            }

            public override int GetHashCode()
            {
                // ReSharper disable once NonReadonlyMemberInGetHashCode
                return Name == null ? 0 : Name.GetHashCode();
            }

            public static Record FromString(string record)
            {
                var r = new Record();

                var tokens = record.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length == 3)
                {
                    r.Name = tokens[0];
                    r.Size = long.Parse(tokens[1]);
                    r.What = tokens[2];
                }

                return r;
            }
        }
        private readonly List<Record> m_records = new List<Record>();
        private void Create(string path, bool doNotSave)
        {
            m_records.Clear();

            RecursiveLoad(path, path);

            if (doNotSave)
                return;

            var detailFile = Path.Combine(path, m_databaseFileName);
            if (File.Exists(detailFile))
                File.Delete(detailFile);

            SaveDetailFileList();
        }
        private void RecursiveLoad(string path, string basePath)
        {
            try
            {
                foreach (string filePathName in Directory.GetFiles(path, "*.*"))
                {
                    if (IsIgnoredName(filePathName))
                        continue;

                    FileInfo info = new FileInfo(filePathName);

                    Record record = new Record
                    {
                        Name = MakeRelative(filePathName, basePath),
                        Size = info.Length,
                        What = "Ok",
                    };
                    m_records.Add(record);
                }

                foreach (string d in Directory.GetDirectories(path))
                {
                    if (IsIgnoredName(d))
                        continue;

                    RecursiveLoad(d, basePath);
                }
            }
            catch (System.Exception e)
            {
                Console.WriteLine(Resources.FolderController_RecursiveLoad_An_exception_happened__ + e.Message);
            }
        }

        public static string MakeRelative(string filePath, string referencePath)
        {
            var fileUri = new Uri(filePath);
            var referenceUri = new Uri(referencePath);
            return Uri.UnescapeDataString(referenceUri.MakeRelativeUri(fileUri).ToString()).Replace('/', Path.DirectorySeparatorChar);
        }

        private static bool IsIgnoredName(string filePathName)
        {
            var fileName = Path.GetFileName(filePathName);
            return fileName.StartsWith(".") || fileName.StartsWith("$") || fileName == m_databaseFileName;
        }
    }
}
