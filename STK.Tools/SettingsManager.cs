using Microsoft.Win32;
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
        private Dictionary<string, object> dValues;
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
            dValues = new Dictionary<string, object>(defaultValues); // Copy dictionary to prevent external manipulation 

        }

        /// <summary>
        /// Unloads any loaded File and restores default values
        /// </summary>
        public void Reset()
        {
            values = new Dictionary<string, object>(dValues);
            CurrentLoaded = null;
        }

        /// <summary>
        /// Saves the current settings in the default documents folder or 
        /// if filename is null and the current settings are loaded from a file,
        /// the current settings will be stored in the loaded file.
        /// </summary>
        public void Save(string filename = null)
        {
            bool filenameIsOK = !String.IsNullOrWhiteSpace(filename);

            if (filenameIsOK && CurrentLoaded != null)
            {
                Save(CurrentLoaded);
            }
            else
            {
                if (!filenameIsOK)
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
            var exe = System.Reflection.Assembly.GetEntryAssembly();
            if (exe == null)
                exe = System.Reflection.Assembly.GetCallingAssembly();
            try
            {
                Registry
                    .CurrentUser.OpenSubKey(@"Software\Classes", true)
                    .CreateSubKey("." + FileAssociation)
                    .SetValue("", FileAssociation + "_File", RegistryValueKind.String);
                Registry
                   .CurrentUser.OpenSubKey(@"Software\Classes\" + FileAssociation + "_File", true)
                    .CreateSubKey("DefaultIcon")
                    .SetValue("", exe.Location, RegistryValueKind.String);
                Registry
                   .CurrentUser.OpenSubKey(@"Software\Classes", true)
                    .CreateSubKey(FileAssociation + "_File" + @"\shell\open\command")
                    .SetValue("", exe.Location + " \"%1\"", RegistryValueKind.String);
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        public void DeregisterFileAssociation()
        {
            try
            {
                Registry
                    .CurrentUser.OpenSubKey(@"Software\Classes", true)
                   .DeleteSubKey("." + FileAssociation);
                Registry
                     .CurrentUser.OpenSubKey(@"Software\Classes", true)
                    .DeleteSubKey(FileAssociation + "_File" + @"\shell\open\command");
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        public bool IsFileAssociationRegisterd
        {
            get
            {
                bool ret = true;
                try
                {
                    var exe = System.Reflection.Assembly.GetEntryAssembly();
                    if (exe == null)
                        exe = System.Reflection.Assembly.GetCallingAssembly();

                    var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\" + FileAssociation + @"_File\shell\open\command");
                    ret = key != null;

                    if (ret)
                    {
                        var val = key.GetValue("") as string;
                        ret = val != null;
                        if (ret)
                        {
                            ret = val.CompareTo(exe.Location + " \"%1\"") == 0;
                        }
                    }
                }
                catch { ret = false; }
                return ret;
            }
        }
    }
}
