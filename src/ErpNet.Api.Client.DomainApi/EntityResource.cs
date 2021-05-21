using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ErpNet.Api.Client.DomainApi
{
    /// <summary>
    /// Represents an entity object.
    /// </summary>
    public class EntityResource : ApiResource
    {
        /// <summary>
        /// Creates an instace of <see cref="EntityResource"/>
        /// </summary>
        /// <param name="rawData">the initial values</param>
        public EntityResource(IDictionary<string, object?>? rawData) : base(rawData)
        {

        }

        /// <summary>
        /// The entity Id.
        /// </summary>
        public Guid? Id
        {
            get
            {
                var id = GetPropertyValue<Guid?>("Id");
                if (id != null)
                    return id;
                var idStr = GetPropertyValue<string>("@odata.id");
                if (idStr != null)
                    return ((EntityIdentifier)idStr).Id;
                return null;
            }
        }

        /// <summary>
        /// Gets the ODATA Id of the entity.
        /// </summary>
        /// <returns></returns>
        public EntityIdentifier? ODataId
        {
            get
            {
                var odataId = GetPropertyValue<string>("@odata.id");
                if (odataId != null)
                    return new EntityIdentifier(odataId);
                var entitySet = GetEntitySetName();
                if (entitySet != null)
                {
                    var id = (Guid?)GetPropertyValue("Id", typeof(Guid?));
                    if (id != null)
                        return new EntityIdentifier(entitySet, id.Value);
                }
                return null;
            }
        }

        /// <summary>
        /// Returns a value indicating if the entity contains properies or contains only Id.
        /// </summary>
        /// <returns></returns>
        public bool IsExpanded() => RawData().Any(e => e.Key != "Id" && e.Key != "@odata.id");

        /*
        /// <summary>
        /// Returns true if all properties are selected.
        /// </summary>
        /// <returns></returns>
        public bool IsSelectAll()
        {
            var data = RawData();
            foreach (var pi in GetType().GetTypeInfo().GetRuntimeProperties().Where(pi => pi.IsDefined(typeof(ODataPropertyAttribute), true)))
            {
                if (!data.ContainsKey(pi.Name))
                    return false;
            }
            return true;
        }
        */

        /// <summary>
        /// Compares the odata id-s of the objects.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object? obj)
        {
            if (obj is EntityResource e)
            {
                var odataId = ODataId;
                if (odataId != null)
                    return e.ODataId == odataId;
            }
            return base.Equals(obj);
        }

        ///<inheritdoc/>
        public override int GetHashCode()
        {
            var odataId = ODataId;
            if (odataId != null)
                return odataId.GetHashCode();
            return base.GetHashCode();
        }

        /// <summary>
        /// Compares the resources for equality.
        /// </summary>
        /// <param name="res1"></param>
        /// <param name="res2"></param>
        /// <returns></returns>
        public static bool operator ==(EntityResource? res1, EntityResource? res2)
        {
            if (ReferenceEquals(res1, res2))
                return true;
            if (ReferenceEquals(res1, null) && ReferenceEquals(res2, null))
                return true;           
            return res1?.Equals(res2) == true;
        }
        /// <summary>
        /// Compares the resources for non equality.
        /// </summary>
        /// <param name="res1"></param>
        /// <param name="res2"></param>
        /// <returns></returns>
        public static bool operator !=(EntityResource? res1, EntityResource? res2)
        {
            if (ReferenceEquals(res1, res2))
                return false;
            if (ReferenceEquals(res1, null) && ReferenceEquals(res2, null))
                return false;
            return res1?.Equals(res2) != true;
        }

        /// <summary>
        /// Compares the <see cref="EntityResource.ODataId"/> with the specified entity identifier
        /// </summary>
        /// <param name="res1"></param>
        /// <param name="oid"></param>
        /// <returns></returns>
        public static bool operator ==(EntityResource? res1, EntityIdentifier? oid)
        {
            var oid1 = res1?.ODataId;

            return oid1 == oid;
        }

        /// <summary>
        /// Compares the <see cref="EntityResource.ODataId"/> with the specified entity identifier
        /// </summary>
        /// <param name="res1"></param>
        /// <param name="oid"></param>
        /// <returns></returns>
        public static bool operator !=(EntityResource? res1, EntityIdentifier? oid)
        {
            var oid1 = res1?.ODataId;

            return oid1 != oid;
        }

        /// <summary>
        /// Gets a <see cref="EntityIdentifier"/> from a reference property.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        protected EntityIdentifier? GetODataId(string propertyName)
        {
            if (RawData().TryGetValue(propertyName, out var token))
            {
                if (token is IDictionary<string, object?> o
                    && o.TryGetValue("@odata.id", out var value)
                    && value is string str
                    && str != null)
                    return (EntityIdentifier)str;
            }
            return null;
        }

        /// <summary>
        /// Sets a <see cref="EntityIdentifier"/> to a reference property.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        protected void SetODataId(string propertyName, EntityIdentifier? value)
        {
            var data = RawData();
            if (value == null)
            {
                data[propertyName] = null;
            }
            IDictionary<string, object?> j = new Dictionary<string, object?>();
            j["@odata.id"] = (string?)value;
            RawData()[propertyName] = j;
        }
    }
}
