using System;

namespace ErpNet.Api.Client.DomainApi
{
    /// <summary>
    /// Marks a typed <see cref="EntityResource"/> as entity providing EntitSetName and EntityName.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class EntityAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the name of the entity. This is the table name.
        /// </summary>
        /// <value>
        /// The name of the entity.
        /// </value>
        public string? TableName { get; set; }
        /// <summary>
        /// Gets or sets the name of the odata entity set.
        /// </summary>
        /// <value>
        /// The name of the entity set.
        /// </value>
        public string? EntitySet { get; set; }
        /// <summary>
        /// Gets or sets the name of the odata entity type.
        /// </summary>
        public string? EntityType { get; set; }
    }

}
