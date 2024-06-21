using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using System.IO;
using System.Text;
using System.Xml.Serialization;

public class HCCoreModulesPreprocess : IPreprocessBuild
{
    public int callbackOrder { get { return 0; } }

    [XmlRoot("installedModules")]
    public class InstalledModules
    {
        [XmlElement("modules")]
        public Modules moduleRoot{ get; set; }
    }
    
    public class Modules
    {
        [XmlElement("module")]
        public Module[] modules { get; set; }
    }
    
    public class Module
    {
        [XmlAttribute("id")]
        public string Id { get; set; }
        [XmlAttribute("version")]
        public string version { get; set; }
        [XmlAttribute("subscriptionStream")]
        public string subscriptionStream { get; set; }
    }

    public void OnPreprocessBuild(BuildTarget target, string path)
    {
        Debug.Log("HCCoreModulesPreprocess :: OnPreprocessBuild");
        string pathInstaller = "Assets/Core/Settings/Installer.xml";
        // Read the TMX file
        try
        {
            XmlSerializer serial = new XmlSerializer(typeof(InstalledModules));
            Stream reader = new FileStream(pathInstaller, FileMode.Open);
            InstalledModules modules = (InstalledModules)serial.Deserialize(reader);

            Dictionary<string,string> dict = new Dictionary<string, string>();
            
            foreach (var VARIABLE in modules.moduleRoot.modules)
            {
                dict.Add(VARIABLE.Id, VARIABLE.version + "/" + VARIABLE.subscriptionStream );
            }
        
            dict.Add("Bundle Version",PlayerSettings.bundleVersion);
            dict.Add("Product Name",PlayerSettings.productName);
            dict.Add("Application Identifier",PlayerSettings.applicationIdentifier);

            createCodeFile(dict);
            reader.Close();
            
        }
        catch (Exception e)
        {
            Dictionary<string,string> dict = new Dictionary<string, string>();
        
            dict.Add("Bundle Version",PlayerSettings.bundleVersion);
            dict.Add("Product Name",PlayerSettings.productName);
            dict.Add("Application Identifier",PlayerSettings.applicationIdentifier);

            createCodeFile(dict);
        }
    }
    
    public void createCodeFile( Dictionary<string,string> dict )
    {
        string pathCodeFile = string.Concat(Application.dataPath, Path.DirectorySeparatorChar, "HCSDK/Scripts/HCCoreModules.cs");

        try
        {
            // opens the file if it allready exists, creates it otherwise
            using (FileStream stream = File.Open(pathCodeFile, FileMode.Create, FileAccess.Write))
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    StringBuilder builder = new StringBuilder();
                    builder.AppendLine("// ----- AUTO GENERATED CODE ----- //");
                    builder.AppendLine("using System.Collections;");
                    builder.AppendLine("using System.Collections.Generic;");
                    builder.AppendLine("using UnityEngine;");
                    builder.AppendLine("namespace BoomBit.HyperCasual");
                    builder.AppendLine("{");
                    
                    builder.AppendLine("\tpublic class HCCoreModules");
                    builder.AppendLine("\t{");
                    builder.AppendLine("\t\tpublic static Dictionary<string, string> Modules = new Dictionary<string, string> {");

                    foreach (var VARIABLE in dict)
                    {
                        builder.AppendLine("\t\t\t{\""+VARIABLE.Key+"\",\""+VARIABLE.Value+"\"}," );
                    }
                    
                    builder.AppendLine("\t\t\t};");

                    builder.AppendLine("\t}");
                    builder.AppendLine("}");
                    writer.Write(builder.ToString());
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);

            // if we have an error, it is certainly that the file is screwed up. Delete to be save
            if (File.Exists(pathCodeFile) == true) File.Delete(pathCodeFile);
        }

        AssetDatabase.Refresh();
    }

}
