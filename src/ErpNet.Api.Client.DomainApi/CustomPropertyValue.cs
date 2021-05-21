using System;
using System.Collections.Generic;
using System.Text;

namespace ErpNet.Api.Client.DomainApi.General
{
    /// <summary>
    /// Represents a custom property value.
    /// </summary>
    public partial class CustomPropertyValue : ComplexTypeResource
    {
        ///
        public CustomPropertyValue() : base(null) { }

        /// <summary>
        /// Compares the <see cref="CustomPropertyValue.Value"/> with the specified string value.
        /// </summary>
        /// <param name="pv"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool operator ==(CustomPropertyValue? pv, string? value)
        {
            return pv?.Value == value;
        }

        /// <summary>
        /// Compares the <see cref="CustomPropertyValue.Value"/> with the specified string value.
        /// </summary>
        /// <param name="pv"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool operator !=(CustomPropertyValue? pv, string? value)
        {
            return pv?.Value != value;
        }

        ///<inheritdoc/>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        ///<inheritdoc/>
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
    }
}
