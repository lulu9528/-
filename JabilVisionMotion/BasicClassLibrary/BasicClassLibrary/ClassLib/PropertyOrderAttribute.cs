using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicClassLibrary
{
    [AttributeUsage(AttributeTargets.Property)]
    public class PropertyOrderAttribute : Attribute//自定义Attribute类，向property提供
    {
        private int order;

        public PropertyOrderAttribute(int order)
        {
            this.order = order;
        }
        public int Order
        {
            get
            {
                return order;
            }
        }
    }

    #region Helper Class - PropertyOrderPair
    public class PropertyOrderPair : IComparable
    {
        private int _order;
        private string _name;
        public string Name
        {
            get
            {
                return _name;
            }
        }

        public PropertyOrderPair(string name, int order)
        {
            _order = order;
            _name = name;
        }

        public int CompareTo(object obj)
        {
            //
            // Sort the pair objects by ordering by order value
            // Equal values get the same rank
            //
            int otherOrder = ((PropertyOrderPair)obj)._order;
            if (otherOrder == _order)
            {
                //
                // If order not specified, sort by name
                //
                string otherName = ((PropertyOrderPair)obj)._name;
                return string.Compare(_name, otherName);
            }
            else if (otherOrder > _order)
            {
                return -1;
            }
            return 1;
        }
    }
    #endregion



    class TestPropertyDescriptor : PropertyDescriptor, IComparable//继承PropertyDescriptor类并实现IComparable接口
    {
        private PropertyDescriptor basePropertyDescriptor;
        private int order;

        //构造函数

        public TestPropertyDescriptor(PropertyDescriptor basePropertyDescriptor) : base(basePropertyDescriptor)
        {
            this.basePropertyDescriptor = basePropertyDescriptor;
            order = GetOrder(basePropertyDescriptor.Attributes);
        }
        //获得property的order属性
        private int GetOrder(AttributeCollection ac)
        {
            foreach (Attribute a in ac)
            {
                if (a is PropertyOrderAttribute)
                    return ((PropertyOrderAttribute)a).Order;
            }
            return 0;
        }

        #region "IComparable"
        public int CompareTo(object tpd)//实现接口，使此类的对象可以依据order进行比较、排序
        {
            TestPropertyDescriptor other = (TestPropertyDescriptor)tpd;
            if (order == other.order) return string.Compare(Name, other.Name);
            else return (order > other.order) ? 1 : -1;
        }

        public override bool CanResetValue(object component)
        {
            return false;
        }

        public override Type ComponentType
        {
            get
            {
                return this.GetType();
            }
        }

        public override object GetValue(object component)
        {
            return component;
        }

        public override bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public override Type PropertyType
        {
            get
            {
                return this.GetType();
            }
        }

        public override void ResetValue(object component)
        {
            //不重置，无动作 
        }

        public override void SetValue(object component, object value)
        {
            ;
        }
        /// <summary>
        /// 是否应该持久化保存
        /// </summary>
        /// <param name="component"></param>
        /// <returns></returns>
        public override bool ShouldSerializeValue(object component)
        {
            return false;
        }
        /// <summary>
        /// 属性说明
        /// </summary>
        public override string Description
        {
            get
            {
                return this.Description;
            }
        }
        #endregion
    }


    class ICustomTDClass1 : ICustomTypeDescriptor//Class1为需要对其属性进行排序的自定义类。
    {
        public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {

            PropertyDescriptorCollection tmpPDC = TypeDescriptor.GetProperties(typeof(ICustomTDClass1), attributes);
            PropertyDescriptorCollection result = new PropertyDescriptorCollection(null);
            ArrayList orderPdList = new ArrayList();

            foreach (PropertyDescriptor pd in tmpPDC)
            {
                TestPropertyDescriptor tpd = new TestPropertyDescriptor(pd);
                result.Add(tpd);
                orderPdList.Add(tpd);

            }
            orderPdList.Sort();//根据order排序
            ArrayList propertyNames = new ArrayList();
            foreach (TestPropertyDescriptor propertyAttributes in orderPdList)//获得排序后的DisplayName数组
            {
                propertyNames.Add(propertyAttributes.DisplayName);
            }

            return result.Sort((string[])propertyNames.ToArray(typeof(string)));//根据数组对结果排序，注意这里不能直接return 
        }

        public AttributeCollection GetAttributes()
        {
            return TypeDescriptor.GetAttributes(this, true);
        }

        public string GetClassName()
        {
            return TypeDescriptor.GetClassName(this, true);
        }

        public string GetComponentName()
        {
            return TypeDescriptor.GetClassName(this, true);
        }

        public TypeConverter GetConverter()
        {
            return TypeDescriptor.GetConverter(this, true);
        }

        public EventDescriptor GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent(this, true);
        }

        public PropertyDescriptor GetDefaultProperty()
        {
            return TypeDescriptor.GetDefaultProperty(this, true);
        }

        public object GetEditor(Type editorBaseType)
        {
            return TypeDescriptor.GetEditor(this, editorBaseType, true);
        }

        public EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(this, attributes, true);
        }

        public EventDescriptorCollection GetEvents()
        {
            return TypeDescriptor.GetEvents(this, true);
        }

        public PropertyDescriptorCollection GetProperties()
        {
            return TypeDescriptor.GetProperties(this, true);
        }

        public object GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }

    }

    public class PropertySorter : ExpandableObjectConverter
    {
        #region Methods
        public override bool GetPropertiesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
        {
            //
            // This override returns a list of properties in order
            //
            PropertyDescriptorCollection pdc = TypeDescriptor.GetProperties(value, attributes);
            ArrayList orderedProperties = new ArrayList();
            foreach (PropertyDescriptor pd in pdc)
            {
                Attribute attribute = pd.Attributes[typeof(PropertyOrderAttribute)];
                if (attribute != null)
                {
                    //
                    // If the attribute is found, then create an pair object to hold it
                    //
                    PropertyOrderAttribute poa = (PropertyOrderAttribute)attribute;
                    orderedProperties.Add(new PropertyOrderPair(pd.Name, poa.Order));
                }
                else
                {
                    //
                    // If no order attribute is specifed then given it an order of 0
                    //
                    orderedProperties.Add(new PropertyOrderPair(pd.Name, 0));
                }
            }
            //
            // Perform the actual order using the value PropertyOrderPair classes
            // implementation of IComparable to sort
            //
            orderedProperties.Sort();
            //
            // Build a string list of the ordered names
            //
            ArrayList propertyNames = new ArrayList();
            foreach (PropertyOrderPair pop in orderedProperties)
            {
                propertyNames.Add(pop.Name);
            }
            //
            // Pass in the ordered list for the PropertyDescriptorCollection to sort by
            //
            return pdc.Sort((string[])propertyNames.ToArray(typeof(string)));
        }
        #endregion


    }
}
