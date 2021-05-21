using System;
using System.Collections.Generic;
using System.Text;

namespace ErpNet.Api.Client.DomainApi
{
    /// <summary>
    /// A generic object with dynamic properties that can be returned by the Domain Api from various methods.
    /// </summary>
    partial class OpenObject: ComplexTypeResource
    {
        /// <summary>
        /// Creantes an instance
        /// </summary>
        public OpenObject() : base(null) { }
    }
}
