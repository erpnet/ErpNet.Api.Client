using System;
using System.Collections.Generic;
using System.Text;

namespace ErpNet.Api.Client.DomainApi
{
    /// <summary>
    /// A helper class used to build odata actions or functions.
    /// </summary>
    public class OperationParameter
    {
        /// <summary>
        /// Creates an instance of <see cref="OperationParameter"/>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="value"></param>
        public OperationParameter(string name, Type type, object? value)
        {
            Name = name;
            Type = type;
            Value = value;
        }

        /// <summary>
        /// Parameter name.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Parameter type.
        /// </summary>
        public Type Type { get; }
        /// <summary>
        /// Parameter value.
        /// </summary>
        public object? Value { get; }
    }

    /// <summary>
    /// Generic class descendant of <see cref="OperationParameter"/>
    /// </summary>
    /// <typeparam name="T">The parameter type</typeparam>
    public class Param<T> : OperationParameter
    {
        ///
        public Param(string name, T value) : base(name, typeof(T), value) { }
    }
}
