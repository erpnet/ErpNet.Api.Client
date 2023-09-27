using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace ErpNet.Api.Client.DomainApi
{
    /// <summary>
    /// A base class for all typed Domain Api model classes - entities or complex types.
    /// </summary>
    public abstract class ApiResource
    {
        const string ODataNamespace = "Erp.";
        // ISO 8061 compliance
        const string DefaultDateTimeFormat = "o";
        // The Constant("c") Format Specifier
        const string DefaultTimeSpanFormat = "c";

        /// <summary>
        /// The internal data of the resource. The values must be of type bool, double, string, dictionary or list.
        /// </summary>
        protected IDictionary<string, object?> data;
        HashSet<string> changedProperties = new HashSet<string>();
        ConcurrentDictionary<string, object> collections = new ConcurrentDictionary<string, object>();
        static Lazy<Dictionary<string, Type>> entitySetToType = new Lazy<Dictionary<string, Type>>(() =>
        {
            Dictionary<string, Type> dict = new Dictionary<string, Type>();
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var t in a.GetTypes())
                {
                    if (t.IsDefined(typeof(EntityAttribute)))
                    {
                        var e = t.GetCustomAttribute<EntityAttribute>()?.EntitySet;
                        if (e != null)
                            dict.Add(e, t);
                    }
                }
            }
            return dict;
        });


        /// <summary>
        /// Creates an empty <see cref="ApiResource"/>
        /// </summary>
        public ApiResource() : this(null)
        {

        }

        /// <summary>
        /// Creates an instance of <see cref="ApiResource"/> with provided raw data.
        /// </summary>
        /// <param name="rawData">The inner dictionary that contains the resource properties</param>
        public ApiResource(IDictionary<string, object?>? rawData)
        {
            if (rawData == null)
            {
                data = new Dictionary<string, object?>();
                var edmType = this.GetType().FullName.Substring(typeof(ApiResource).Namespace.Length + 1).Replace(".", "_");
                data["@odata.type"] = ODataNamespace + edmType;
            }
            else
            {
                data = rawData;
            }
        }


        /// <summary>
        /// Searches the corresponding type for the provided entity set name.
        /// </summary>
        /// <param name="entitySetName"></param>
        /// <returns></returns>
        public static Type? TryGetTypeByEntitySet(string entitySetName)
        {
            entitySetToType.Value.TryGetValue(entitySetName, out var type);
            return type;
        }

        /// <summary>
        /// Creates an <see cref="EntityIdentifier"/> for the given entity type and id.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static EntityIdentifier CreateEntityIdentifier(Type type, Guid id)
        {
            return new EntityIdentifier(GetEntitySetName(type), id);
        }

        /// <summary>
        /// Creates an entity resource that only contains properties @odata.id and Id.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static EntityResource CreateIdObj(Type type, Guid id)
        {
            IDictionary<string, object?> rawData = new Dictionary<string, object?>();
            rawData["@odata.id"] = $"{GetEntitySetName(type)}({id})";
            rawData["Id"] = id;
            return (EntityResource)Create(type, rawData);
        }

        /// <summary>
        /// Creates an <see cref="EntityIdentifier"/> for the given entity type and id.
        /// </summary>
        /// <typeparam name="TResource"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public static EntityIdentifier CreateEntityIdentifier<TResource>(Guid id) where TResource : ApiResource
        {
            return CreateEntityIdentifier(typeof(TResource), id);
        }

        /// <summary>
        /// Creates an entity resource instance containing only Id.
        /// </summary>
        /// <typeparam name="TResource"></typeparam>

        /// <param name="id"></param>
        /// <returns></returns>
        public static TResource CreateIdObj<TResource>(Guid id) where TResource : EntityResource
        {
            return (TResource)CreateIdObj(typeof(TResource), id);
        }

        /// <summary>
        /// Creates typed entity resource providing raw data.
        /// </summary>
        /// <typeparam name="TResource"></typeparam>

        /// <param name="rawData"></param>
        /// <returns></returns>
        public static TResource Create<TResource>(IDictionary<string, object?> rawData) where TResource : ApiResource
        {
            return (TResource)Create(typeof(TResource), rawData);
        }

        /// <summary>
        /// Creates typed entity resource for the specified entity set.
        /// </summary>
        /// <param name="entitySetName"></param>

        /// <param name="rawData"></param>
        /// <returns></returns>
        public static EntityResource Create(string entitySetName, IDictionary<string, object?>? rawData)
        {
            var type = TryGetTypeByEntitySet(entitySetName);
            if (type == null)
                throw new InvalidOperationException($"There is no entity type for entity set {entitySetName}.");
            return (EntityResource)Create(type, rawData);
        }

        /// <summary>
        /// Creates typed entity resource with initial data.
        /// </summary>

        /// <param name="resourceType">The resource type</param>
        /// <param name="rawData">The initial values</param>
        /// <returns></returns>
        public static ApiResource Create(Type resourceType, IDictionary<string, object?>? rawData)
        {
            return (ApiResource)Activator.CreateInstance(resourceType, rawData);
        }

        /// <summary>
        /// Converts the specified object to entity resource. The object is used as a raw data.
        /// </summary>
        /// <typeparam name="TResource"></typeparam>

        /// <param name="obj"></param>
        /// <returns></returns>
        public static TResource Convert<TResource>(object obj) where TResource : ApiResource
        {
            var json = JsonHelper.ToJson(obj);
            if (json == null)
                throw new InvalidOperationException("Invalid json from object " + obj);
            var rawData = (IDictionary<string, object?>?)JsonHelper.Parse(json);
            if (rawData == null)
                throw new InvalidOperationException("Unexpected null.");
            return (TResource)Create(typeof(TResource), rawData);
        }

        /// <summary>
        /// Gets the internal dictionary with resource properties.
        /// </summary>
        /// <returns></returns>
        internal IDictionary<string, object?> RawData()
        {
            return InitData();
        }

        /// <summary>
        /// Gets a dictionary with only the changed properties.
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, object?>? GetRawChanges()
        {
            if (!changedProperties.Any())
                return null;
            var data = InitData();
            return changedProperties
                .Where(name => data.ContainsKey(name))
                .ToDictionary(
                    name => name,
                    name =>
                    {
                        var value = data[name];
                        if (value is IDictionary<string, object?> dict)
                        {
                            value = CleanupNestedEntities(dict);
                        }
                        else if (value is IEnumerable array && !(value is string))
                        {
                            List<object?> list = new List<object?>();
                            foreach (object? item in array)
                            {
                                if (item is IDictionary<string, object?> itemDict)
                                    list.Add(CleanupNestedEntities(itemDict));
                                else
                                    list.Add(item);
                            }
                            value = list;
                        }
                        return value;
                    });
        }

        private IDictionary<string, object?> CleanupNestedEntities(IDictionary<string, object?> dict)
        {
            var result = new Dictionary<string, object?>();

            if (dict.TryGetValue("@odata.id", out var odataId) && odataId != null)
            {
                result.Add("@odata.id", odataId);
            }
            else
            {
                foreach (var entry in dict)
                {
                    if (entry.Value is IDictionary<string, object?> child)
                    {
                        result.Add(entry.Key, CleanupNestedEntities(child));
                    }
                    else
                    {
                        result.Add(entry.Key, entry.Value);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Gets a value indicating whether the resouce object is modified.
        /// </summary>
        /// <returns></returns>
        public bool IsModified() => changedProperties.Any();

        /// <summary>
        /// Sets the specified property as modified.
        /// </summary>
        /// <param name="propertyName"></param>
        public void SetModified(string propertyName)
        {
            changedProperties.Add(propertyName);
            OnModified(propertyName);
        }

        /// <summary>
        /// Occurs when a property is modified through <see cref="SetPropertyValue(string, object?, Type)"/> method.
        /// </summary>
        /// <param name="propertyName"></param>
        protected virtual void OnModified(string propertyName)
        {
            
        }

        /// <summary>
        /// Marks the specified properties as not changed. If no property is specified all properties are marked as not changed.
        /// </summary>
        public void ClearChanges(params string[] propertyNames)
        {
            if (propertyNames == null || propertyNames.Length == 0)
                changedProperties.Clear();
            else
            {
                foreach (var pn in propertyNames)
                    changedProperties.Remove(pn);
            }
        }

        /// <summary>
        /// Returns the name of the entity set for the specified model type. If the type is not marked with <see cref="EntityAttribute"/> an exception is thrown.
        /// </summary>
        /// <param name="entityType">The entity resource type.</param>
        /// <returns></returns>
        public static string GetEntitySetName(Type entityType)
        {
            return entityType.GetTypeInfo().GetCustomAttribute<EntityAttribute>()?.EntitySet
                ?? throw new ArgumentException($"The type {entityType} is not marked with {nameof(EntityAttribute)}.");
            //return entityType.FullName.Replace(".", "_").Substring(ODataNamespace.Length);

        }

        /// <summary>
        /// Returns a JSON string representing the current resource.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return InitData().ToJson(true);
        }

        /// <summary>
        /// Gets the json string representation of the resource.
        /// </summary>
        /// <param name="indent"></param>
        /// <returns></returns>
        public string ToJson(bool indent = false)
        {
            return InitData().ToJson(indent);
        }

        /// <summary>
        /// Gets the entity set name or null if the current type is not marked with <see cref="EntityAttribute"/>.
        /// </summary>
        /// <returns></returns>
        public string? GetEntitySetName()
        {
            return GetEntitySetName(GetType());
        }

        /// <summary>
        /// Updates the current resource with property values provided as dictionary. Changes are not tracked.
        /// </summary>
        /// <param name="values">The property values to update</param>
        public void Update(IDictionary<string, object?> values)
        {
            if (!ReferenceEquals(data, values))
            {
                var data = InitData();
                data.DeepMerge(values);
            }
        }

        /// <summary>
        /// Converts the specified raw value to the provided type.
        /// </summary>

        /// <param name="rawValue">The raw value from JSON result</param>
        /// <param name="type">The target object type</param>
        /// <returns></returns>
        public static object? ConvertFromRaw(object? rawValue, Type type)
        {
            if (rawValue == null)
                return null;

            type = Nullable.GetUnderlyingType(type) ?? type;

            if (rawValue is IDictionary<string, object?> objVal && typeof(ApiResource).IsAssignableFrom(type))
            {
                var resource = Create(type, objVal);
                return resource;
            }
            else
            {
                if (rawValue is string str)
                {
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        type = Nullable.GetUnderlyingType(type);
                    }
                    if (type == typeof(Guid))
                        return new Guid(str);
                    if (type.IsEnum)
                        return Enum.Parse(type, str);
                    if (type == typeof(byte[]))
                        return System.Convert.FromBase64String(str);
                    if (type == typeof(DateTime))
                        return DateTime.Parse(str, CultureInfo.InvariantCulture);
                    if (type == typeof(DateTimeOffset))
                        return DateTimeOffset.Parse(str, CultureInfo.InvariantCulture);
                    if (type == typeof(TimeSpan))
                        return TimeSpan.Parse(str, CultureInfo.InvariantCulture);


                    if (typeof(EntityResource).IsAssignableFrom(type))
                    {
                        //Validate.
                        var i = new EntityIdentifier(str);
                        var raw = new Dictionary<string, object?>();
                        raw["@odata.id"] = str;
                        var resource = Create(type, raw);
                        return resource;
                    }
                    return System.Convert.ChangeType(str, type, CultureInfo.InvariantCulture);
                }

                return System.Convert.ChangeType(rawValue, type, CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Gets a property value converted to the specified property type.
        /// </summary>
        /// <param name="propertyName">The property name</param>
        /// <param name="propertyType">The property type</param>
        /// <returns></returns>
        public object? GetPropertyValue(string propertyName, Type propertyType)
        {
            var data = InitData();
            if (data.TryGetValue(propertyName, out var value))
            {
                if (propertyType == typeof(object))
                    return value;

                var obj = ConvertFromRaw(value, propertyType);
                if (obj is ComplexTypeResource complex)
                {
                    complex.Owner = this;
                    complex.OwnerPropertyName = propertyName;
                }
                return obj;
            }

            if (propertyType.IsValueType)
                return Activator.CreateInstance(propertyType);
            return null;
        }

        /// <summary>
        /// Gets a property value converted to the specified property type.
        /// </summary>
        /// <typeparam name="T">The property type</typeparam>
        /// <param name="propertyName">The property name</param>
        /// <returns></returns>
        public T? GetPropertyValue<T>(string propertyName)
        {
            return (T?)GetPropertyValue(propertyName, typeof(T));
        }

        /// <summary>
        /// If the resource type defines property with the specified name the returned object is of the property type. Otherwise the raw value is returned.
        /// </summary>
        /// <param name="propertyName">The name of the property</param>
        /// <returns></returns>
        public object? GetPropertyValue(string propertyName)
        {
            var pi = GetType().GetProperty(propertyName);
            var type = pi?.PropertyType ?? typeof(object);
            return GetPropertyValue(propertyName, type);
        }

        /// <summary>
        /// Gets or sets a property value.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public object? this[string propertyName]
        {
            get => GetPropertyValue(propertyName);
            set => SetPropertyValue(propertyName, value);
        }

        /// <summary>
        /// Sets a property value with the specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        public void SetPropertyValue<T>(string propertyName, T? value)
        {
            SetPropertyValue(propertyName, value, typeof(T));
        }

        /// <summary>
        /// Sets a property value with the type inferred from a declared property.
        /// </summary>
        /// <param name="propertyName">The name of the property</param>
        /// <param name="value"></param>
        public void SetPropertyValue(string propertyName, object? value)
        {
            var pi = GetType().GetProperty(propertyName);
            var type = pi?.PropertyType ?? typeof(object);
            SetPropertyValue(propertyName, value, type);
        }

        /// <summary>
        /// Sets a property value with the specified type.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        /// <param name="propertyType"></param>
        public void SetPropertyValue(string propertyName, object? value, Type propertyType)
        {
            var data = InitData();

            var oldValue = GetPropertyValue(propertyName, propertyType);
            if (!object.Equals(oldValue, value))
            {
                SetModified(propertyName);
                //var rawOldValue = data[propertyName];

                if (value == null)
                {
                    data[propertyName] = null;
                    return;
                }

                if (value is ApiResource c && oldValue is ApiResource c2)
                {
                    c2.Update(c.RawData());
                }
                else if (value is IDictionary<string, object?> d && oldValue is IDictionary<string, object?> d2)
                {
                    d2.DeepMerge(d);
                }
                else
                {
                    data[propertyName] = ConvertToRaw(value);
                }

            }
        }

        private object? ConvertToRaw(object? value)
        {
            if (value == null)
                return null;

            if (value is string str)
                return str;
            if (value is DateTime dt)
                return dt.ToString(DefaultDateTimeFormat);
            if (value is TimeSpan ts)
                return ts.ToString(DefaultTimeSpanFormat);
            if (value is Guid guid)
                return guid.ToString("D");
            if (value is Enum)
                return Enum.GetName(value.GetType(), value);
            if (value is ApiResource res)
                return res.data;
            if (value is byte[] byteArray)
                return System.Convert.ToBase64String(byteArray);

            if (value is IEnumerable array)
            {
                var list = new List<object?>();
                foreach (var item in array)
                {
                    var rawItem = ConvertToRaw(item);
                    list.Add(rawItem);
                }

                return list;
            }

            return value;
        }

        /// <summary>
        /// Gets a typed resource collection from the specified property.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public IEnumerable<T>? GetCollection<T>(string propertyName) where T : ApiResource
        {
            var data = InitData();

            if (collections.TryGetValue(propertyName, out var c))
                return (IEnumerable<T>)c;

            IEnumerable array;
            if (data.TryGetValue(propertyName, out var value) && value != null)
                array = (IEnumerable)value;
            else
            {
                return null;
            }
            c = CreateResourceCollection<T>(array);
            collections[propertyName] = c;
            return (IEnumerable<T>)c;
        }
        /// <summary>
        /// Sets a typed resource collection to the specified property.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyName"></param>
        /// <param name="array">The raw items from json response</param>
        public void SetCollection<T>(string propertyName, IEnumerable? array) where T : ApiResource
        {
            var data = InitData();
            SetModified(propertyName);
            if (array == null)
            {
                ((IDictionary)collections).Remove(propertyName);
                data.Remove(propertyName);

            }
            else
            {
                var c = CreateResourceCollection<T>(array);
                collections[propertyName] = c;
                data[propertyName] = ConvertToRaw(array);
            }
        }

        private IEnumerable<T> CreateResourceCollection<T>(IEnumerable array) where T : ApiResource
        {
            foreach (var o in array)
                yield return Create<T>((IDictionary<string, object?>)o);
        }

        /// <summary>
        /// Gets the type of the entity by odata entity name.
        /// </summary>
        /// <param name="entityName">Name of the odata entity.</param>
        /// <returns></returns>
        public static Type GetEntityType(string entityName)
        {
            string typeName = entityName.Replace("_", ".");
            if (!typeName.StartsWith(ODataNamespace))
                typeName = ODataNamespace + typeName;
            return Type.GetType(typeName);
        }

        /// <summary>
        /// Enumerates the property values of the resource.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<KeyValuePair<string, object?>> Values()
        {
            foreach (var item in RawData())
                yield return new KeyValuePair<string, object?>(item.Key, GetPropertyValue(item.Key));
        }

        /// <summary>
        /// Returns a value indicating if this resource instance contains the specified property
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public bool ContainsProperty(string propertyName)
        {
            if (data == null)
                return false;
            return data.ContainsKey(propertyName);
        }

        IDictionary<string, object?> InitData()
        {
            if (data == null)
                data = new Dictionary<string, object?>();
            return data;
        }




    }
}
