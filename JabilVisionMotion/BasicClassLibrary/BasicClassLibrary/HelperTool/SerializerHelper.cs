using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Formatters.Soap;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace BasicClassLibrary.HelperTool
{
    /// <summary>
    /// 序列化类
    /// </summary>
    public class SerializerHelper
    {
        /// <summary>
        /// 是否注册
        /// </summary>
        private static bool IsValid = (Security.GetRemainDays() > 0);

        /// <summary>
        /// 深度拷贝
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        /// 
        public static T DeepCopy<T>(T obj)
        {
            object obj2;
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, obj);
                stream.Seek(0L, SeekOrigin.Begin);
                obj2 = formatter.Deserialize(stream);
                stream.Close();
            }
            return (T)obj2;
        }

        /// <summary>
        /// 二进制反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="file"></param>
        /// <returns></returns>
        public static T DeserializeBinary<T>(string file) where T : class
        {
            Stream serializationStream = File.Open(file, FileMode.Open);
            object obj2 = new BinaryFormatter().Deserialize(serializationStream);
            serializationStream.Close();
            return (obj2 as T);
        }

        /// <summary>
        /// 从文件反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="file"></param>
        /// <returns></returns>
        public static T DeserializeDataContract<T>(string file)
        {
            //T local = default(T);
            DataContractSerializer serializer = new DataContractSerializer(typeof(T));
            using (XmlReader reader = new XmlTextReader(file))
            {
                return (T)serializer.ReadObject(reader);
            }
        }

        /// <summary>
        /// 反序列化XmlNode中的数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node"></param>
        public static T DeserializeDataContract<T>(System.Xml.XmlNode node)
        {
            //T local = default(T);
            DataContractSerializer serializer = new DataContractSerializer(typeof(T));
            using (XmlReader reader = new XmlNodeReader(node))
            {
                return (T)serializer.ReadObject(reader);
            }
        }

        /// <summary>
        /// SOAP反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="file"></param>
        /// <returns></returns>
        public static T DeserializeSoap<T>(string file) where T : class
        {
            using (Stream stream = File.Open(file, FileMode.Open))
            {
                object obj2 = new SoapFormatter().Deserialize(stream);
                stream.Close();
                return (obj2 as T);
            }
        }

        /// <summary>
        /// XML反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="file"></param>
        /// <returns></returns>
        public static T DeserializeXml<T>(string file) where T : class
        {
            using (Stream stream = File.Open(file, FileMode.Open))
            {
                object obj2 = new XmlSerializer(typeof(T)).Deserialize(stream);
                stream.Close();
                return (obj2 as T);
            }
        }

        /// <summary>
        /// 从二进制序列化文件加载对象
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static object LoadObjectFromFile(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                return formatter.Deserialize(stream);
            }
        }

        /// <summary>
        /// 把对象序列化为二进制进行保存
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="path"></param>
        public static void SaveObjectToFile(object obj, string path)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }
            using (FileStream stream = File.Create(path))
            {
                new BinaryFormatter().Serialize(stream, obj);
            }
        }

        /// <summary>
        /// 二进制序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="file"></param>
        /// <param name="obj"></param>
        public static void SerializeBinary<T>(string file, T obj) where T : class
        {
            Stream serializationStream = File.Open(file, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            new BinaryFormatter().Serialize(serializationStream, obj);
            serializationStream.Close();
        }

        /// <summary>
        /// 序列化到XmlNode
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="node"></param>
        public static void SerializeDataContract<T>(T obj, out System.Xml.XmlNode node)
        {
            DataContractSerializer serializer = new DataContractSerializer(typeof(T));
            XmlDocument document = new XmlDocument();
            using (MemoryStream stream = new MemoryStream())
            {
                serializer.WriteObject(stream, obj);
                stream.Position = 0L;
                document.Load(stream);
                node = document.LastChild;
            }
        }

        /// <summary>
        /// 序列化到文件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="file"></param>
        public static void SerializeDataContract<T>(string file, T obj)
        {
            DataContractSerializer serializer = new DataContractSerializer(typeof(T));
            using (XmlTextWriter writer = new XmlTextWriter(file, Encoding.UTF8))
            {
                writer.Formatting = Formatting.Indented;
                serializer.WriteObject(writer, obj);
            }
        }

        /// <summary>
        /// SOAP序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="file"></param>
        /// <param name="obj"></param>
        public static void SerializeSoap<T>(string file, T obj) where T : class
        {
            Stream serializationStream = File.Open(file, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            new SoapFormatter().Serialize(serializationStream, obj);
            serializationStream.Close();
        }

        /// <summary>
        /// XML序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="file"></param>
        /// <param name="obj"></param>
        public static void SerializeXml<T>(string file, T obj) where T : class
        {
            Stream stream = File.Open(file, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            new XmlSerializer(typeof(T)).Serialize(stream, obj);
            stream.Close();
        }
    }
}
