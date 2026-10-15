using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json; // 提供JSON读写工具类
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq; // 用于XML文档处理（作为JSON解析的中间格式）

namespace BasicClassLibrary
{
    /// <summary>
    /// 动态JSON处理类，继承自DynamicObject，支持：
    /// 1. 将JSON字符串/流解析为动态对象（可通过属性/索引访问）
    /// 2. 将动态对象序列化为JSON字符串
    /// 3. 动态操作JSON对象（添加/删除属性、数组元素）
    /// 4. 反序列化为指定类型（类、数组、集合等）
    /// 内部通过XML作为中间格式处理JSON结构（依赖JsonReaderWriterFactory）
    /// </summary>
    public class DynamicJson : DynamicObject
    {
        /// <summary>
        /// JSON数据类型枚举，对应JSON的基础类型
        /// </summary>
        private enum JsonType
        {
            @string,   // 字符串类型
            number,    // 数字类型（整数、浮点数等）
            boolean,   // 布尔类型（true/false）
            @object,   // JSON对象（键值对集合）
            array,     // JSON数组（有序值集合）
            @null      // 空值
        }

        #region 静态解析方法（JSON → 动态对象）

        /// <summary>
        /// 将JSON字符串解析为动态对象（默认使用Unicode编码）
        /// </summary>
        /// <param name="json">JSON格式字符串</param>
        /// <returns>动态对象（可通过.访问属性或[]访问数组元素）</returns>
        public static dynamic Parse(string json)
        {
            return Parse(json, Encoding.Unicode);
        }

        /// <summary>
        /// 将JSON字符串解析为动态对象（指定编码）
        /// </summary>
        /// <param name="json">JSON格式字符串</param>
        /// <param name="encoding">字符串编码（如UTF8、Unicode）</param>
        /// <returns>动态对象</returns>
        public static dynamic Parse(string json, Encoding encoding)
        {
            // 使用JsonReaderWriterFactory创建JSON读取器，将JSON转换为XML格式
            using (var reader = JsonReaderWriterFactory.CreateJsonReader(
                encoding.GetBytes(json),
                XmlDictionaryReaderQuotas.Max // 最大配额（避免解析大JSON时受限）
            ))
            {
                // 加载XML并转换为动态值
                return ToValue(XElement.Load(reader));
            }
        }

        /// <summary>
        /// 从流中读取JSON数据并解析为动态对象
        /// </summary>
        /// <param name="stream">包含JSON数据的流（如文件流、网络流）</param>
        /// <returns>动态对象</returns>
        public static dynamic Parse(Stream stream)
        {
            using (var reader = JsonReaderWriterFactory.CreateJsonReader(
                stream,
                XmlDictionaryReaderQuotas.Max
            ))
            {
                return ToValue(XElement.Load(reader));
            }
        }

        /// <summary>
        /// 从流中读取JSON数据并解析为动态对象（指定编码）
        /// </summary>
        /// <param name="stream">包含JSON数据的流</param>
        /// <param name="encoding">流的编码格式</param>
        /// <returns>动态对象</returns>
        public static dynamic Parse(Stream stream, Encoding encoding)
        {
            using (var reader = JsonReaderWriterFactory.CreateJsonReader(
                stream,
                encoding,
                XmlDictionaryReaderQuotas.Max,
                _ => { } // 忽略解析事件
            ))
            {
                return ToValue(XElement.Load(reader));
            }
        }

        #endregion

        #region 静态序列化方法（对象 → JSON字符串）

        /// <summary>
        /// 将对象序列化为JSON字符串（支持基本类型、集合、自定义类）
        /// </summary>
        /// <param name="obj">待序列化的对象（如int、string、List、自定义类等）</param>
        /// <returns>JSON格式字符串</returns>
        public static string Serialize(object obj)
        {
            // 创建XML根元素，包含类型属性和JSON节点，再转换为JSON字符串
            return CreateJsonString(new XStreamingElement(
                "root",
                CreateTypeAttr(GetJsonType(obj)), // 添加类型属性（如type="object"）
                CreateJsonNode(obj) // 生成对象对应的XML节点
            ));
        }

        #endregion

        #region 私有静态工具方法（解析与序列化的辅助逻辑）

        /// <summary>
        /// 将XML元素转换为对应的动态值（递归处理嵌套结构）
        /// </summary>
        /// <param name="element">JSON转换后的XML元素</param>
        /// <returns>基础类型值（string/number/boolean/null）或DynamicJson对象（对应object/array）</returns>
        private static dynamic ToValue(XElement element)
        {
            // 从XML属性获取JSON类型
            var type = (JsonType)Enum.Parse(typeof(JsonType), element.Attribute("type").Value);
            switch (type)
            {
                case JsonType.boolean:
                    return (bool)element; // 布尔值直接转换
                case JsonType.number:
                    return (double)element; // 数字转换为double
                case JsonType.@string:
                    return (string)element; // 字符串直接返回
                case JsonType.@object:
                case JsonType.array:
                    // 对象或数组返回DynamicJson实例（包含XML结构和类型）
                    return new DynamicJson(element, type);
                case JsonType.@null:
                default:
                    return null; // null返回null
            }
        }

        /// <summary>
        /// 判断对象对应的JSON类型
        /// </summary>
        /// <param name="obj">待判断的对象</param>
        /// <returns>对应的JsonType枚举值</returns>
        private static JsonType GetJsonType(object obj)
        {
            if (obj == null) return JsonType.@null;

            // 根据对象类型的TypeCode判断JSON类型
            switch (Type.GetTypeCode(obj.GetType()))
            {
                case TypeCode.Boolean:
                    return JsonType.boolean;
                case TypeCode.String:
                case TypeCode.Char:
                case TypeCode.DateTime: // 日期时间序列化为字符串
                    return JsonType.@string;
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                case TypeCode.SByte:
                case TypeCode.Byte:
                    return JsonType.number; // 所有数值类型对应JSON number
                case TypeCode.Object:
                    // 集合类型对应array，其他对象对应object
                    return (obj is IEnumerable) ? JsonType.array : JsonType.@object;
                case TypeCode.DBNull:
                case TypeCode.Empty:
                default:
                    return JsonType.@null; // 未识别类型视为null
            }
        }

        /// <summary>
        /// 创建XML的"type"属性（用于标记JSON类型）
        /// </summary>
        /// <param name="type">JSON类型</param>
        /// <returns>XML属性（如type="string"）</returns>
        private static XAttribute CreateTypeAttr(JsonType type)
        {
            return new XAttribute("type", type.ToString());
        }

        /// <summary>
        /// 将对象转换为XML节点（用于序列化）
        /// </summary>
        /// <param name="obj">待转换的对象</param>
        /// <returns>XML节点内容（文本或子节点）</returns>
        private static object CreateJsonNode(object obj)
        {
            var type = GetJsonType(obj);
            switch (type)
            {
                case JsonType.@string:
                case JsonType.number:
                    return obj; // 字符串和数字直接作为节点值
                case JsonType.boolean:
                    return obj.ToString().ToLower(); // 布尔值转换为小写字符串（true/false）
                case JsonType.@object:
                    return CreateXObject(obj); // 对象转换为XML子节点集合
                case JsonType.array:
                    return CreateXArray(obj as IEnumerable); // 集合转换为XML数组节点
                case JsonType.@null:
                default:
                    return null; // null值返回空
            }
        }

        /// <summary>
        /// 将集合转换为XML数组节点（item为子节点名）
        /// </summary>
        /// <typeparam name="T">集合类型（需实现IEnumerable）</typeparam>
        /// <param name="obj">集合对象</param>
        /// <returns>XML元素集合（每个元素对应集合中的一项）</returns>
        private static IEnumerable<XStreamingElement> CreateXArray<T>(T obj) where T : IEnumerable
        {
            // 遍历集合元素，每个元素生成一个<item>节点（包含类型属性和值）
            return obj.Cast<object>()
                .Select(o => new XStreamingElement(
                    "item",
                    CreateTypeAttr(GetJsonType(o)),
                    CreateJsonNode(o)
                ));
        }

        /// <summary>
        /// 将对象转换为XML对象节点（属性名为子节点名）
        /// </summary>
        /// <param name="obj">自定义对象</param>
        /// <returns>XML元素集合（每个元素对应对象的公共属性）</returns>
        private static IEnumerable<XStreamingElement> CreateXObject(object obj)
        {
            // 反射获取对象的公共实例属性，每个属性生成一个XML节点（节点名=属性名）
            return obj.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(pi => new {
                    Name = pi.Name,
                    Value = pi.GetValue(obj, null) // 获取属性值
                })
                .Select(a => new XStreamingElement(
                    a.Name,
                    CreateTypeAttr(GetJsonType(a.Value)),
                    CreateJsonNode(a.Value)
                ));
        }

        /// <summary>
        /// 将XML元素转换为JSON字符串
        /// </summary>
        /// <param name="element">包含JSON结构的XML元素</param>
        /// <returns>JSON格式字符串</returns>
        private static string CreateJsonString(XStreamingElement element)
        {
            // 使用JsonWriter将XML转换为JSON
            using (var ms = new MemoryStream())
            using (var writer = JsonReaderWriterFactory.CreateJsonWriter(ms, Encoding.Unicode))
            {
                element.WriteTo(writer); // 将XML写入JSON writer
                writer.Flush(); // 刷新缓冲区
                return Encoding.Unicode.GetString(ms.ToArray()); // 转换为字符串
            }
        }

        #endregion

        #region 实例字段与构造函数（动态JSON对象/数组的核心数据）

        // 存储JSON解析后的XML结构（中间格式，用于动态访问）
        private readonly XElement xml;
        // 当前动态对象的JSON类型（object或array）
        private readonly JsonType jsonType;

        /// <summary>
        /// 创建空的JSON对象（默认初始化为object类型）
        /// </summary>
        public DynamicJson()
        {
            xml = new XElement("root", CreateTypeAttr(JsonType.@object));
            jsonType = JsonType.@object;
        }

        /// <summary>
        /// 从XML元素和类型初始化DynamicJson实例（私有，供解析时使用）
        /// </summary>
        /// <param name="element">JSON转换后的XML元素</param>
        /// <param name="type">JSON类型（必须是object或array）</param>
        private DynamicJson(XElement element, JsonType type)
        {
            Debug.Assert(type == JsonType.array || type == JsonType.@object); // 确保类型合法
            xml = element;
            jsonType = type;
        }

        #endregion

        #region 实例属性与基础操作方法

        /// <summary>
        /// 判断当前动态对象是否为JSON对象（{...}）
        /// </summary>
        public bool IsObject { get { return jsonType == JsonType.@object; } }

        /// <summary>
        /// 判断当前动态对象是否为JSON数组（[...]）
        /// </summary>
        public bool IsArray { get { return jsonType == JsonType.array; } }

        /// <summary>
        /// 检查JSON对象中是否包含指定名称的属性
        /// </summary>
        /// <param name="name">属性名</param>
        /// <returns>存在返回true，否则false（非对象类型恒为false）</returns>
        public bool IsDefined(string name)
        {
            return IsObject && (xml.Element(name) != null);
        }

        /// <summary>
        /// 检查JSON数组中是否包含指定索引的元素
        /// </summary>
        /// <param name="index">元素索引（从0开始）</param>
        /// <returns>存在返回true，否则false（非数组类型恒为false）</returns>
        public bool IsDefined(int index)
        {
            return IsArray && (xml.Elements().ElementAtOrDefault(index) != null);
        }

        /// <summary>
        /// 删除JSON对象中指定名称的属性
        /// </summary>
        /// <param name="name">属性名</param>
        /// <returns>删除成功返回true，否则false（非对象类型恒为false）</returns>
        public bool Delete(string name)
        {
            var elem = xml.Element(name);
            if (elem != null)
            {
                elem.Remove(); // 从XML中移除该节点
                return true;
            }
            return false;
        }

        /// <summary>
        /// 删除JSON数组中指定索引的元素
        /// </summary>
        /// <param name="index">元素索引</param>
        /// <returns>删除成功返回true，否则false（非数组类型恒为false）</returns>
        public bool Delete(int index)
        {
            var elem = xml.Elements().ElementAtOrDefault(index);
            if (elem != null)
            {
                elem.Remove(); // 从XML中移除该节点
                return true;
            }
            return false;
        }

        #endregion

        #region 反序列化方法（动态对象 → 指定类型）

        /// <summary>
        /// 将动态JSON对象反序列化为指定类型（支持类、数组、集合等）
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <returns>类型为T的实例</returns>
        public T Deserialize<T>()
        {
            return (T)Deserialize(typeof(T));
        }

        /// <summary>
        /// 反序列化的核心逻辑（根据类型是数组还是对象分别处理）
        /// </summary>
        /// <param name="type">目标类型</param>
        /// <returns>目标类型的实例</returns>
        private object Deserialize(Type type)
        {
            return IsArray ? DeserializeArray(type) : DeserializeObject(type);
        }

        /// <summary>
        /// 将XML元素的值转换为目标类型（递归处理嵌套结构）
        /// </summary>
        /// <param name="element">XML元素</param>
        /// <param name="elementType">目标元素类型</param>
        /// <returns>转换后的对象</returns>
        private dynamic DeserializeValue(XElement element, Type elementType)
        {
            var value = ToValue(element); // 获取元素的动态值
            // 若值是DynamicJson对象（嵌套的object/array），递归反序列化
            if (value is DynamicJson)
            {
                value = ((DynamicJson)value).Deserialize(elementType);
            }
            // 转换为目标类型
            return Convert.ChangeType(value, elementType);
        }

        /// <summary>
        /// 将JSON对象反序列化为自定义类（通过属性名映射）
        /// </summary>
        /// <param name="targetType">目标类类型</param>
        /// <returns>目标类的实例（属性值从JSON对象中映射）</returns>
        private object DeserializeObject(Type targetType)
        {
            // 创建目标类型的实例
            var result = Activator.CreateInstance(targetType);
            // 反射获取目标类型的可写公共属性（键为属性名，值为PropertyInfo）
            var propertyDict = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToDictionary(pi => pi.Name, pi => pi);

            // 遍历XML中的子节点（对应JSON对象的属性）
            foreach (var item in xml.Elements())
            {
                PropertyInfo propertyInfo;
                // 若节点名与属性名匹配，设置属性值
                if (propertyDict.TryGetValue(item.Name.LocalName, out propertyInfo))
                {
                    var value = DeserializeValue(item, propertyInfo.PropertyType);
                    propertyInfo.SetValue(result, value, null);
                }
            }
            return result;
        }

        /// <summary>
        /// 将JSON数组反序列化为数组或集合（如T[]、List&lt;T&gt;）
        /// </summary>
        /// <param name="targetType">目标数组/集合类型</param>
        /// <returns>目标类型的实例（元素从JSON数组中映射）</returns>
        private object DeserializeArray(Type targetType)
        {
            if (targetType.IsArray) // 处理数组类型（如Foo[]）
            {
                var elementType = targetType.GetElementType(); // 获取数组元素类型
                dynamic array = Array.CreateInstance(elementType, xml.Elements().Count()); // 创建数组
                int index = 0;
                // 遍历XML中的item节点（对应JSON数组的元素）
                foreach (var item in xml.Elements())
                {
                    array[index++] = DeserializeValue(item, elementType);
                }
                return array;
            }
            else // 处理泛型集合（如List&lt;Foo&gt;）
            {
                var elementType = targetType.GetGenericArguments()[0]; // 获取集合元素类型
                dynamic list = Activator.CreateInstance(targetType); // 创建集合实例
                // 遍历元素并添加到集合
                foreach (var item in xml.Elements())
                {
                    list.Add(DeserializeValue(item, elementType));
                }
                return list;
            }
        }

        #endregion

        #region 重写DynamicObject方法（实现动态访问）

        /// <summary>
        /// 实现动态调用（用于删除操作：obj(index) 或 obj("name")）
        /// </summary>
        /// <param name="binder">调用绑定器</param>
        /// <param name="args">调用参数（索引或属性名）</param>
        /// <param name="result">删除结果（成功true，失败false）</param>
        /// <returns>操作成功返回true</returns>
        public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
        {
            // 数组通过索引删除，对象通过名称删除
            result = IsArray
                ? Delete((int)args[0])
                : Delete((string)args[0]);
            return true;
        }

        /// <summary>
        /// 实现动态方法调用（用于检查属性是否存在：obj.IsDefinedName()）
        /// </summary>
        /// <param name="binder">方法绑定器（方法名即属性名）</param>
        /// <param name="args">方法参数（无参数）</param>
        /// <param name="result">检查结果（存在true，否则false）</param>
        /// <returns>操作成功返回true</returns>
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            if (args.Length > 0) // 仅支持无参数调用
            {
                result = null;
                return false;
            }
            // 方法名即为属性名，检查是否存在
            result = IsDefined(binder.Name);
            return true;
        }

        /// <summary>
        /// 实现类型转换（支持转换为IEnumerable或自定义类型）
        /// </summary>
        /// <param name="binder">转换绑定器（目标类型）</param>
        /// <param name="result">转换后的对象</param>
        /// <returns>转换成功返回true</returns>
        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            if (binder.Type == typeof(IEnumerable) || binder.Type == typeof(object[]))
            {
                // 转换为集合：数组返回元素列表，对象返回键值对列表
                var enumerable = IsArray
                    ? xml.Elements().Select(x => ToValue(x)) // 数组元素
                    : xml.Elements().Select(x => (dynamic)new KeyValuePair<string, object>(x.Name.LocalName, ToValue(x))); // 对象键值对
                // 转换为数组或 IEnumerable
                result = binder.Type == typeof(object[]) ? enumerable.ToArray() : enumerable;
            }
            else
            {
                // 转换为自定义类型（调用反序列化方法）
                result = Deserialize(binder.Type);
            }
            return true;
        }

        /// <summary>
        /// 辅助方法：从XML元素获取值并返回
        /// </summary>
        /// <param name="element">XML元素</param>
        /// <param name="result">获取到的值（动态类型）</param>
        /// <returns>获取成功返回true</returns>
        private bool TryGet(XElement element, out object result)
        {
            if (element == null)
            {
                result = null;
                return false;
            }
            // 将XML元素转换为对应的值
            result = ToValue(element);
            return true;
        }

        /// <summary>
        /// 实现索引访问（array[index] 或 object["name"]）
        /// </summary>
        /// <param name="binder">索引绑定器</param>
        /// <param name="indexes">索引参数（数组索引或属性名）</param>
        /// <param name="result">获取到的值</param>
        /// <returns>访问成功返回true</returns>
        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            // 数组通过索引获取元素，对象通过名称获取属性
            return IsArray
                ? TryGet(xml.Elements().ElementAtOrDefault((int)indexes[0]), out result)
                : TryGet(xml.Element((string)indexes[0]), out result);
        }

        /// <summary>
        /// 实现属性访问（object.name 或 array.index（通过索引字符串））
        /// </summary>
        /// <param name="binder">属性绑定器（属性名或索引字符串）</param>
        /// <param name="result">获取到的值</param>
        /// <returns>访问成功返回true</returns>
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            // 数组支持通过字符串索引访问（如array["0"]），对象通过属性名访问
            return IsArray
                ? TryGet(xml.Elements().ElementAtOrDefault(int.Parse(binder.Name)), out result)
                : TryGet(xml.Element(binder.Name), out result);
        }

        /// <summary>
        /// 辅助方法：设置对象的属性值（更新XML节点）
        /// </summary>
        /// <param name="name">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns>设置成功返回true</returns>
        private bool TrySet(string name, object value)
        {
            var type = GetJsonType(value);
            var element = xml.Element(name);
            if (element == null)
            {
                // 若属性不存在，添加新节点
                xml.Add(new XElement(name, CreateTypeAttr(type), CreateJsonNode(value)));
            }
            else
            {
                // 若属性存在，更新类型和值
                element.Attribute("type").Value = type.ToString();
                element.ReplaceNodes(CreateJsonNode(value));
            }
            return true;
        }

        /// <summary>
        /// 辅助方法：设置数组的元素值（更新XML节点）
        /// </summary>
        /// <param name="index">元素索引</param>
        /// <param name="value">元素值</param>
        /// <returns>设置成功返回true</returns>
        private bool TrySet(int index, object value)
        {
            var type = GetJsonType(value);
            var element = xml.Elements().ElementAtOrDefault(index);
            if (element == null)
            {
                // 若索引不存在，添加新item节点
                xml.Add(new XElement("item", CreateTypeAttr(type), CreateJsonNode(value)));
            }
            else
            {
                // 若索引存在，更新类型和值
                element.Attribute("type").Value = type.ToString();
                element.ReplaceNodes(CreateJsonNode(value));
            }
            return true;
        }

        /// <summary>
        /// 实现索引赋值（array[index] = value 或 object["name"] = value）
        /// </summary>
        /// <param name="binder">索引绑定器</param>
        /// <param name="indexes">索引参数</param>
        /// <param name="value">要设置的值</param>
        /// <returns>赋值成功返回true</returns>
        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            // 数组通过索引赋值，对象通过名称赋值
            return IsArray
                ? TrySet((int)indexes[0], value)
                : TrySet((string)indexes[0], value);
        }

        /// <summary>
        /// 实现属性赋值（object.name = value 或 array.index = value（通过索引字符串））
        /// </summary>
        /// <param name="binder">属性绑定器</param>
        /// <param name="value">要设置的值</param>
        /// <returns>赋值成功返回true</returns>
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            // 数组支持通过字符串索引赋值（如array["0"] = 123），对象通过属性名赋值
            return IsArray
                ? TrySet(int.Parse(binder.Name), value)
                : TrySet(binder.Name, value);
        }

        /// <summary>
        /// 获取动态成员名称列表（用于调试或反射）
        /// </summary>
        /// <returns>成员名称集合（对象为属性名，数组为索引字符串）</returns>
        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return IsArray
                ? xml.Elements().Select((x, i) => i.ToString()) // 数组返回索引字符串（"0","1"...）
                : xml.Elements().Select(x => x.Name.LocalName); // 对象返回属性名
        }

        /// <summary>
        /// 将动态JSON对象序列化为JSON字符串
        /// </summary>
        /// <returns>JSON格式字符串</returns>
        public override string ToString()
        {
            // 处理null值节点（XML中null节点需为空）
            foreach (var elem in xml.Descendants().Where(x => x.Attribute("type").Value == "null"))
            {
                elem.RemoveNodes();
            }
            // 生成JSON字符串
            return CreateJsonString(new XStreamingElement(
                "root",
                CreateTypeAttr(jsonType),
                xml.Elements()
            ));
        }

        #endregion
    }
}