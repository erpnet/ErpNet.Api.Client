
using System.Collections.Generic;

namespace ErpNet.Api.Client.DomainApi
{
    /// <summary>
    /// A base class for complex type resources.
    /// </summary>
    public class ComplexTypeResource : ApiResource
    {
        /// <summary>
        /// Creates an instance of <see cref="ComplexTypeResource"/> providing dictionary data.
        /// </summary>
        /// <param name="rawData"></param>
        public ComplexTypeResource(IDictionary<string,object?>? rawData) : base(rawData) { }


        /// <summary>
        /// The owner of the comlpex object. If not null when this instance is modifed the owner porperty is marked as modified too.
        /// </summary>
        public ApiResource? Owner { get; set; }
        /// <summary>
        /// The name of the owner property. 
        /// </summary>
        public string? OwnerPropertyName { get; set; }


        ///<inheritdoc/>
        protected override void OnModified(string propertyName)
        {
            base.OnModified(propertyName);
            if (Owner != null && OwnerPropertyName != null)
                Owner.SetModified(OwnerPropertyName);
        }
    }
}
