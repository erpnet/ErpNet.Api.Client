using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;

namespace ErpNet.Api.Client.DomainApi
{
    /// <summary>
    /// Represents a result from the <see cref="DomainApiTransaction.GetChangesAsync"/> method.
    /// Contains changed properties and their values.
    /// </summary>
    public class GetChangesResult : IEnumerable<ChangedItem>
    {
        IDictionary<string,object?> dict;
        /// <summary>
        /// Creates an instance.
        /// </summary>
        /// <param name="dict"></param>
        public GetChangesResult(IDictionary<string,object?> dict)
        {
            this.dict = dict;
        }

        /// <summary>
        /// Returns all changed items for the specified <see cref="ChangeType"/>
        /// </summary>
        /// <param name="changeType"></param>
        /// <returns></returns>
        public IEnumerable<ChangedItem> GetChangedItems(ChangeType changeType)
        {
            string action = changeType.ToString().ToLower();
            var actionObject = dict[action] as IDictionary<string,object?>;
            if (actionObject == null)
                yield break;
            foreach (var entitySet in actionObject)
            {
                var array = entitySet.Value as IDictionary<string,object?>;
                if (array != null)
                    foreach (var item2 in array)
                    {
                        yield return CreateItem(changeType, entitySet.Key, item2.Key, item2.Value);
                    }
            }
        }


        /// <summary>
        /// Gets the <see cref="ChangedItem"/> enumerator.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<ChangedItem> GetEnumerator()
        {
            foreach (var item in GetChangedItems(ChangeType.Insert))
                yield return item;
            foreach (var item in GetChangedItems(ChangeType.Update))
                yield return item;
            foreach (var item in GetChangedItems(ChangeType.Delete))
                yield return item;
        }

        ChangedItem CreateItem(ChangeType type, string entitySetName, string key, object? value)
        {
            Guid id = Guid.Parse(key);
            var values = value as IDictionary<string, object?>;
            if (type == ChangeType.Delete)
                values = null;
            return new ChangedItem(
                entitySetName,
                type,
                id,
                values);
        }

        /// <summary>
        /// The JSON representation of this object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return dict.ToJson();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// Represents an item returned by <see cref="DomainApiTransaction.GetChangesAsync"/>
    /// </summary>
    public class ChangedItem
    {
        ///
        public ChangedItem(string entityName, ChangeType type, Guid id, IDictionary<string, object?>? values)
        {
            EntityName = entityName;
            Type = type;
            Id = id;
            Values = values;
        }
        /// <summary>
        /// The resource name of the changed item. 
        /// </summary>
        public string EntityName { get; }
        /// <summary>
        /// The type of the change.
        /// </summary>
        public ChangeType Type { get; }
        /// <summary>
        /// The id of the changed entity object.
        /// </summary>
        public Guid Id { get; }
        /// <summary>
        /// The changed property values.
        /// </summary>
        public IDictionary<string, object?>? Values { get; }
    }

    /// <summary>
    /// Represents a CRUD change type.
    /// </summary>
    public enum ChangeType
    {
        /// <summary>
        /// The entity object is created
        /// </summary>
        Insert,
        /// <summary>
        /// The entity object is updated
        /// </summary>
        Update,
        /// <summary>
        /// The entity object is deleted
        /// </summary>
        Delete
    }
}
