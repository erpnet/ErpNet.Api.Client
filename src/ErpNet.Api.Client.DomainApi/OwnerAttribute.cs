using System;

namespace ErpNet.Api.Client.DomainApi
{
    /// <summary>
    /// Indicates that the referenced object is an owner of the current object.
    /// </summary>
    /// <seealso cref="System.Attribute" />
    [AttributeUsage(AttributeTargets.Property)]
    public class OwnerAttribute : Attribute
    {
    }
}
