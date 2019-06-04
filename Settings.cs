using System;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using System.Xml.Serialization;

namespace SimpleXmlSetting
{
    public class Settings<T> where T : new()
    {
        private ReaderWriterLockSlim cacheLock;
        private T _default;
        private string settingsXmlPath;

        public Settings(string settingsXmlPath = null)
        {
            cacheLock = new ReaderWriterLockSlim();
            this.settingsXmlPath = string.IsNullOrEmpty(settingsXmlPath) 
                                    ? Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), "Settings.xml")
                                    : settingsXmlPath;
        }

        public T Default => _default == null ? Open() : _default;

        private T Open()
        {
            if (!File.Exists(settingsXmlPath))
            {
                _default = new T();
                Save();
                return _default;
            }
            cacheLock.TryEnterReadLock(Timeout.Infinite);
            try
            {
                var mySerializer = new XmlSerializer(typeof(T));
                var myFileStream = new FileStream(settingsXmlPath, FileMode.Open, FileAccess.Read);
                _default = (T)mySerializer.Deserialize(myFileStream);
                myFileStream.Close();
                return _default;
            }
            finally
            {
                cacheLock.ExitReadLock();
            }
        }

        public void Save()
        {
            cacheLock.TryEnterWriteLock(Timeout.Infinite);
            try
            {
                var mySerializer = new XmlSerializer(typeof(T));
                var myWriter = new StreamWriter(settingsXmlPath);
                mySerializer.Serialize(myWriter, Default);
                myWriter.Close();

            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
        }

        ~Settings()
        {
            if (cacheLock != null)
                cacheLock.Dispose();
        }
    }
}
