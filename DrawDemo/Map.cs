using System;
using System.Collections.Generic;
using System.Linq;

namespace DrawDemo
{
    public class Map<T,U>
    {
        protected Dictionary<T, U> map1 = new Dictionary<T, U>();
        protected Dictionary<U, T> map2 = new Dictionary<U, T>();

        public U this[T key]
        {
            get
            {
                return map1[key];
            }
        }

        public T this[U key]
        {
            get
            {
                return map2[key];
            }
        }

        public bool Remove(T key)
        {
            if (key == null)
                throw new ArgumentNullException();

            if (map1.ContainsKey(key))
            {
                var value = map1[key];
                map1.Remove(key);
                map2.Remove(value);
                return true;
            }
            return false;
        }

        public bool Remove(U key)
        {
            if (key == null)
                throw new ArgumentNullException();

            if (map2.ContainsKey(key))
            {
                var value = map2[key];
                map2.Remove(key);
                map1.Remove(value);
                return true;
            }
            return false;
        }

        public void Add(T tObj, U uObj)
        {
            if (tObj == null)
                throw new ArgumentNullException(nameof(tObj));
            if (uObj == null)
                throw new ArgumentNullException(nameof(uObj));

            if (map1.ContainsKey(tObj))
                throw new ArgumentException("表中已存在相同的元素", "visual");
            if (map2.ContainsKey(uObj))
                throw new ArgumentException("表中已存在相同的元素", "visual");

            map1.Add(tObj, uObj);
            map2.Add(uObj, tObj);
        }

        public void Add(U uObj, T tObj)
        {
            if (tObj == null)
                throw new ArgumentNullException(nameof(tObj));
            if (uObj == null)
                throw new ArgumentNullException(nameof(uObj));

            if (map1.ContainsKey(tObj))
                throw new ArgumentException("表中已存在相同的元素", "visual");
            if (map2.ContainsKey(uObj))
                throw new ArgumentException("表中已存在相同的元素", "visual");

            map1.Add(tObj, uObj);
            map2.Add(uObj, tObj);
        }

        public bool Conatins(U value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            return map2.ContainsKey(value);
        }

        public bool Conatins(T value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            return map1.ContainsKey(value);
        }

        public IEnumerable<ValueTuple<T, U>> Values => map1.Select(p => (p.Key, p.Value)).ToList();

        public void Clear()
        {
            map1 = new Dictionary<T, U>();
            map2 = new Dictionary<U, T>();
        }
    }
}
