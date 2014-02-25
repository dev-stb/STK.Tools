using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;

namespace STK.Tools
{
    public class SettingsManager
    {
        private Dictionary<string, object> values;
        public string FileAssociation { get; private set; }
        public FileInfo CurrentLoaded { get; private set; }

        public SettingsManager(Dictionary<string, object> defaultValues, string fileAssociation)
        {
            if (defaultValues == null)
                throw new ArgumentNullException("defaultValues");
            if (defaultValues.Keys.Any(a => String.IsNullOrWhiteSpace(a)))
                throw new ArgumentNullException("defaultValues", "No Key may be null or empty");
            if (defaultValues.Values.Any(a => a == null))
                throw new ArgumentNullException("defaultValues", "No value may be null");
            if (String.IsNullOrWhiteSpace(fileAssociation))
                throw new ArgumentNullException("fileAssociation", "Value may be not null or empty");
            if (fileAssociation.Any(a => Path.GetInvalidFileNameChars().Contains(a)))
                throw new ArgumentException("fileAssociation", "May not contain any illegal characters");

            FileAssociation = fileAssociation;
            values = new Dictionary<string, object>(defaultValues); // Copy dictionary to prevent external manipulation 
        }

        /// <summary>
        /// Saves the current settings in the default documents folder or 
        /// if filename is null and the current settings are loaded from a file,
        /// the current settings will be stored in the loaded file.
        /// </summary>
        public void Save(string filename = null)
        {
            bool filenameIsNotOK = String.IsNullOrWhiteSpace(filename);

            if (filenameIsNotOK && CurrentLoaded != null)
            {
                Save(CurrentLoaded);
            }
            else
            {
                if (filenameIsNotOK)
                    throw new ArgumentNullException("filename");
                if (filename.Any(a => Path.GetInvalidFileNameChars().Contains(a)))
                    throw new ArgumentException("filename", "May not contain any illegal characters");

                Save(new FileInfo(
                    Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                        , filename + "." + FileAssociation
                    )
                    ));
            }
        }

        /// <summary>
        /// Saves the current settings in the specified file.
        /// </summary>
        public void Save(FileInfo saveFile)
        {
            if (saveFile == null)
                throw new ArgumentNullException("saveFile");

            using (var stream = saveFile.Create())
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(stream, values);
                CurrentLoaded = new FileInfo(saveFile.FullName);
            }
        }

        public void Load(FileInfo fileToLoad)
        {
            if (fileToLoad == null)
                throw new ArgumentNullException("fileToLoad");
            if (!fileToLoad.Exists)
                throw new FileNotFoundException();

            using (var stream = fileToLoad.OpenRead())
            {
                BinaryFormatter bf = new BinaryFormatter();
                var ser = (Dictionary<string, object>)bf.Deserialize(stream);

                foreach (var e in ser)
                    if (values.ContainsKey(e.Key))
                        values[e.Key] = e.Value;
                    else
                        values.Add(e.Key, e.Value);

                CurrentLoaded = new FileInfo(fileToLoad.FullName);
            }
        }

        public class item
        {
            [XmlAttribute]
            public string key;
            [XmlAttribute]
            public object value;
        }

        public T Get<T>(String key)
        {
            if (String.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException("key");
            if (typeof(T).GetType().FullName.CompareTo(values[key].GetType().FullName) == 0)
                throw new ArgumentException("key",
                    String.Format("Type does not match the default value type of setting \"{0}\""
                    , key
                    ));

            return (T)values[key];
        }

        public void Set<T>(String key, T value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            if (value.GetType().FullName.CompareTo(values[key].GetType().FullName) != 0)
                throw new ArgumentException("value", "Type does not match the default value type");

            values[key] = value;
        }

        public void RegisterFileAssociation()
        {
            // Visual Studio integration test
        }

        public void DeregisterFileAssociation()
        {

        }

        public bool IsFileAssociationRegisterd
        {
            get
            {
                bool ret = false;

                return ret;
            }
        }
    }
}
