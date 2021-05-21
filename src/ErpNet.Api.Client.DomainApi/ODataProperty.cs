using System;
using System.Collections.Generic;
using System.Text;

namespace ErpNet.Api.Client.DomainApi
{
    /// <summary>
    /// Marks a class property as OData property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ODataPropertyAttribute : Attribute
    {
    }
}
