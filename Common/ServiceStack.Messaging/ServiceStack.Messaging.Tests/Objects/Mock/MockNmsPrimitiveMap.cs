using System;
using System.Collections.Generic;
using System.Text;
using NMS;

namespace ServiceStack.Messaging.Tests.Objects.Mock
{
    public class MockNmsPrimitiveMap : IPrimitiveMap
    {
        private Dictionary<object,object> map;


        public MockNmsPrimitiveMap()
        {
            this.map = new Dictionary<object, object>();
        }

        #region IPrimitiveMap Members

        public void Clear()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool Contains(object key)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public int Count
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public bool GetBool(string key)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public byte GetByte(string key)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public char GetChar(string key)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public System.Collections.IDictionary GetDictionary(string key)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public double GetDouble(string key)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public float GetFloat(string key)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public int GetInt(string key)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public System.Collections.IList GetList(string key)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public long GetLong(string key)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public short GetShort(string key)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public string GetString(string key)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public System.Collections.ICollection Keys
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public void Remove(object key)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void SetBool(string key, bool value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void SetByte(string key, byte value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void SetChar(string key, char value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void SetDictionary(string key, System.Collections.IDictionary dictionary)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void SetDouble(string key, double value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void SetFloat(string key, float value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void SetInt(string key, int value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void SetList(string key, System.Collections.IList list)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void SetLong(string key, long value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void SetShort(string key, short value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void SetString(string key, string value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public System.Collections.ICollection Values
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public object this[string key]
        {
            get
            {
                if (!map.ContainsKey(key))
                {
                    return null;
                }
                return map[key];
            }
            set
            {
                map[key] = value;
            }
        }

        #endregion
    }
}
