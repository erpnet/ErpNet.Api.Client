using System;
using System.Collections;
using System.Linq;

namespace ErpNet.Api.Client.DomainApi
{
    /// <summary>
    /// Provides extension methods that are implemented as a custom URI functions in ERP.net Domain Api.
    /// </summary>
    public static class FilterMethods
    {
        /// <summary>
        /// Returns true if an object value is contained in the provided collection.
        /// </summary>
        /// <param name="value">The object value</param>
        /// <param name="items">The collection</param>
        /// <returns></returns>
        public static bool In(this object value, params object[] items)
        {
            return In(value, (IEnumerable)items);
        }
        /// <summary>
        /// Returns true if an object value is contained in the provided collection.
        /// </summary>
        /// <param name="value">The object value</param>
        /// <param name="items">The collection</param>
        /// <returns></returns>
        public static bool In(this object? value, IEnumerable items)
        {
            return items.Cast<object>().Contains(value);
        }
        /// <summary>
        /// Returns true if value1 is equal to the value2 or value1 is null.
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        public static bool EqualNull(this object value1, object value2)
        {
            if (value1 == null)
                return true;
            return value1.Equals(value2);
        }

        /// <summary>
        /// Returns true if value1 is greather than or equal to the value2 or value1 is null.
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        public static bool GreaterEqualNull(this object value1, object value2)
        {
            if (value1 == null)
                return true;
            if (value1.Equals(value2))
                return true;
            if (value1 is IComparable c1 && value2 is IComparable c2)
                return c1.CompareTo(c2) > 0;

            return false;
        }

        /// <summary>
        /// Returns true if value1 is less than or equal to the value2 or value1 is null.
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        public static bool LessEqualNull(this object value1, object value2)
        {
            if (value1 == null)
                return true;
            if (value1.Equals(value2))
                return true;
            if (value1 is IComparable c1 && value2 is IComparable c2)
                return c1.CompareTo(c2) < 0;

            return false;
        }
    }
}
