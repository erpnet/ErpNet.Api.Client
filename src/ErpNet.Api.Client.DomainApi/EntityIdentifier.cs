using System;

namespace ErpNet.Api.Client.DomainApi
{
    /// <summary>
    /// Represents Id of an ODATA entity object.
    /// </summary>
    public struct EntityIdentifier
    {
        /// <summary>
        /// Creates an instance of <see cref="EntityIdentifier"/>
        /// </summary>
        /// <param name="entitySet"></param>
        /// <param name="id"></param>
        public EntityIdentifier(string entitySet, Guid id)
        {
            RawValue = $"{entitySet}({id})";
            EntitySet = entitySet;
            Id = id;
        }
        /// <summary>
        /// Creates an instance of <see cref="EntityIdentifier"/>
        /// </summary>
        public EntityIdentifier(string rawId)
        {
            RawValue = rawId;
            try
            {
                int k = rawId.IndexOf('(');
                EntitySet = rawId.Substring(0, k);
                Id = Guid.Parse(rawId.Substring(k + 1, rawId.Length - k - 2));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Invalid @odata.id: " + rawId, ex);
            }
        }
        /// <summary>
        /// Gets the entity Id.
        /// </summary>
        public Guid Id { get; }
        /// <summary>
        /// Gets the name of the entity set.
        /// </summary>
        public string EntitySet { get; }
        /// <summary>
        /// Gets the raw string value in format "{EntitySet}({Id})"
        /// </summary>
        public string RawValue { get; }

        /// <summary>
        /// Gets a value indicating if this is an empty <see cref="EntityIdentifier"/> instance.
        /// </summary>
        public bool IsEmpty => string.IsNullOrEmpty(EntitySet) || Id == Guid.Empty;

        /// <summary>
        /// Creates an entity identifier for the given type and Id.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public static EntityIdentifier Create<T>(Guid id) where T : EntityResource
        {
            return new EntityIdentifier(ApiResource.GetEntitySetName(typeof(T)), id); 
        }

        /// <summary>
        /// Gets the raw string value in format "{EntitySet}({Id})"
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return RawValue;
        }

        ///<inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is EntityIdentifier o)
                return o.RawValue == RawValue;
            return base.Equals(obj);
        }

        ///<inheritdoc/>
        public override int GetHashCode()
        {
            return RawValue.GetHashCode();
        }

        ///<inheritdoc/>
        public static implicit operator string(EntityIdentifier id)
        {
            return id.RawValue;  // implicit conversion
        }

        ///<inheritdoc/>
        public static implicit operator EntityIdentifier?(EntityResource ent)
        {
            var str = ent.ODataId;
            if (str != null)
                return (EntityIdentifier)str;  // implicit conversion
            return null;
        }

        ///<inheritdoc/>
        public static explicit operator EntityIdentifier(string str)
        {
            EntityIdentifier d = new EntityIdentifier(str);  // explicit conversion
            return d;
        }
    }
}
