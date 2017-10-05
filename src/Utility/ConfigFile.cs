//////////////////////////////////////////////////
//                                              //
//   See License.txt for Licensing information  //
//                                              //
//////////////////////////////////////////////////

using PoeHUD.Plugins;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Utility
{
    public class ConfigFile : BasePlugin
    {
        private static string path;


        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        public void Initialize()
        {
            PluginName = "AuraHandler";
            path = PluginDirectory + @"/config/settings.ini";

             // LogMessage(path.ToString(), 5000);
            if (!File.Exists(path))
            {
                WriteValue("AuraHandler", "TestFunction", "Hello World");
                WriteValue("AuraHandler", "TestFunction2", "Sexy Beast");
            }
        }

        public static void WriteValue(string section, string key, string value)
        {
            WritePrivateProfileString(section, key, value, path);
        }

        public static string ReadValue(string section, string key)
        {
            var temp = new StringBuilder(255);
            GetPrivateProfileString(section, key, "", temp, 255, path);
            return temp.ToString().Trim();
        }

        public static T ReadValue<T>(string section, string key)
        {
            var temp = new StringBuilder(255);
            GetPrivateProfileString(section, key, "", temp, 255, path);

            if ((temp.ToString() == "") && (typeof(T) == typeof(int)))
            {
                temp.Append("0");
            }
            if ((temp.ToString() == "") && (typeof(T) == typeof(short)))
            {
                temp.Append("0");
            }
            return (T)Convert.ChangeType(temp.ToString(), typeof(T));
        }
    }
}