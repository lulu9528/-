using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Jabil.Module.AutoDeleteImage
{
    public static class SettingsManager
    {
        private static string GetSettingsFilePath()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataPath, "ImageAutoDeleter");
            
            // 确保应用程序文件夹存在
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            
            return Path.Combine(appFolder, "settings.dat");
        }
        
        public static void SaveSettings(Settings settings)
        {
            try
            {
                string filePath = GetSettingsFilePath();
                
                using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(fileStream, settings);
                }
            }
            catch (Exception ex)
            {
                // 实际应用中可以添加日志记录
                Console.WriteLine($"保存设置失败: {ex.Message}");
            }
        }
        
        public static Settings LoadSettings()
        {
            try
            {
                string filePath = GetSettingsFilePath();
                
                if (!File.Exists(filePath))
                {
                    return new Settings(); // 返回默认设置
                }
                
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    return (Settings)formatter.Deserialize(fileStream);
                }
            }
            catch (Exception ex)
            {
                // 实际应用中可以添加日志记录
                Console.WriteLine($"加载设置失败: {ex.Message}");
                return new Settings(); // 返回默认设置
            }
        }
    }
}
    