using ErpNet.Api.Client.OData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ErpNet.Api.Client.DomainApi
{
    /// <summary>
    /// Provides methods to
    /// </summary>
    public static class ModelExtensions
    {
        /// <summary>
        /// Creates a <see cref="EntityCommand{TResource, TResult}"/> for the specified resource.
        /// </summary>
        /// <typeparam name="TResource"></typeparam>
        /// <param name="connection"></param>
        /// <returns></returns>
        public static EntityCommand<TResource, TResource> Command<TResource>(this DomainApiService connection) where TResource : EntityResource
        {
            return new EntityCommand<TResource, TResource>(connection);
        }

        /// <summary>
        /// Executes the provided query command and gets only the first result. If there are no results the return value is null.
        /// </summary>
        /// <typeparam name="TResource"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="command"></param>
        /// <returns></returns>
        public static async Task<TResource> FirstOrDefaultAsync<TResource, TResult>(this EntityCommand<TResource, TResult> command) where TResource : EntityResource
        {
            var result = await command.Top(1).LoadAsync();
            return result.Resources.FirstOrDefault();
        }

        /// <summary>
        /// Returns the count of the entity objects matching the command filter.
        /// </summary>
        /// <typeparam name="TResource"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="command"></param>
        /// <returns></returns>
        public static async Task<long> CountAsync<TResource, TResult>(this EntityCommand<TResource, TResult> command) where TResource : EntityResource
        {
            command.Type = ErpCommandType.Count;
            var str = await command.Service.ExecuteStringAsync(command);
            return long.Parse(str);
        }

        /// <summary>
        /// Inserts the specified entity into odata service.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static async Task InsertAsync<TEntity>(this DomainApiService service, TEntity entity) where TEntity: EntityResource
        {
            ODataCommand command = new ODataCommand(ApiResource.GetEntitySetName(typeof(TEntity)));
            command.Type = ErpCommandType.Insert;
            command.Payload = entity.GetRawChanges()?.ToJson(writeIndented: false);
            if (command.Payload == null)
                return;
            var values = await service.ExecuteDictionaryAsync(command);
            entity.Update(values);
        }

        /// <summary>
        /// Updates the odata service with the changes in the specified entity. 
        /// Afther the update the entity instance is not refreshed. 
        /// The odata server may change some properties and therefore it is good to refresh the local data by calling <see cref="ModelExtensions.ReloadAsync{TEntity}(DomainApiService, TEntity, Expression{Func{TEntity, object?}}[])"/>.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="service"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static async Task UpdateAsync<TEntity>(this DomainApiService service, TEntity entity) where TEntity : EntityResource
        {
            var oid = entity.ODataId ?? throw new InvalidOperationException("The specified entity does not have @odata.id.");

            ODataCommand command = new ODataCommand(oid.EntitySet);
            command.Key = oid.Id;
            command.Type = ErpCommandType.Update;
            command.Payload = entity.GetRawChanges()?.ToJson(writeIndented: false);
            if (command.Payload == null)
                return;
           var str = await service.ExecuteStringAsync(command);
           
        }

        /// <summary>
        /// Refreshes the values of the specified entity by getting the data from the odata service.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="service"></param>
        /// <param name="entity"></param>
        /// <param name="expandExpressions">A function expression array with the properties to expand</param>
        /// <returns></returns>
        public static async Task ReloadAsync<TEntity>(this DomainApiService service, TEntity entity, params Expression<Func<TEntity, object?>>[] expandExpressions) where TEntity : EntityResource
        {
            var oid = entity.ODataId ?? throw new InvalidOperationException("The specified entity does not have @odata.id.");

            var command = service.Command<TEntity>()
                .Id(oid.Id);
            if (expandExpressions != null && expandExpressions.Any())
            {
                command = command.Expand(expandExpressions);
            }
            var values = await service.ExecuteDictionaryAsync(command);
            entity.Update(values);
        }

        /// <summary>
        /// Refreshes the values of the specified entities by getting the data from the odata service.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="service"></param>
        /// <param name="entities"></param>
        /// <param name="expandExpressions"></param>
        /// <returns></returns>
        public static async Task ReloadAsync<TEntity>(this DomainApiService service, IEnumerable<TEntity> entities, params Expression<Func<TEntity, object?>>[] expandExpressions) where TEntity : EntityResource
        {
            var dict = entities.ToDictionary(e => e.Id.GetValueOrDefault());
            var ids = dict.Values.Select(e => e.Id);
            var result = await service.Command<TEntity>()
                .Filter(e => e.Id.In(ids))
                .Expand(expandExpressions)
                .LoadAsync();

            foreach (var e in result)
            {
                var id = e.Id.GetValueOrDefault();
                if (dict.TryGetValue(id, out var old))
                    old.Update(e.RawData());
            }
        }

        /// <summary>
        /// Deletes the entity specified by it's odata id.
        /// </summary>
        /// <param name="service">the odata service</param>
        /// <param name="entityIdentifier">the odata id of the entity</param>
        /// <returns></returns>
        public static async Task DeleteAsync(this DomainApiService service, EntityIdentifier entityIdentifier)
        {
            ODataCommand command = new ODataCommand(entityIdentifier.EntitySet);
            command.Key = entityIdentifier.Id;
            command.Type = ErpCommandType.Delete;
            await service.ExecuteStringAsync(command);
        }

        /// <summary>
        /// Invokes an entity action.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        /// <param name="service"></param>
        /// <param name="actionName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static Task<object?> InvokeActionAsync<TEntity>(this TEntity entity, DomainApiService service, string actionName, params OperationParameter[] args) where TEntity : EntityResource
        {
            return InvokeOperationAsync<TEntity>(entity, service, actionName, true, args);
        }
        /// <summary>
        /// Invokes an entity function.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        /// <param name="service"></param>
        /// <param name="actionName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static Task<object?> InvokeFunctionAsync<TEntity>(this TEntity entity, DomainApiService service, string actionName, params OperationParameter[] args) where TEntity : EntityResource
        {
            return InvokeOperationAsync<TEntity>(entity, service, actionName, false, args);
        }

        private static async Task<object?> InvokeOperationAsync<TEntity>(TEntity entity, DomainApiService service, string operationName, bool isAction, OperationParameter[] args) where TEntity : EntityResource
        {
            var command = new ODataCommand(entity.ODataId + "/" + operationName);
            command.Type =  isAction ? ErpCommandType.Action : ErpCommandType.Function;
            // use OpenObject to set the typed properties.
            OpenObject openObject = new OpenObject();
            foreach (var par in args)
                openObject.SetPropertyValue(par.Name, par.Value, par.Type);
            var data = openObject.RawData();
            //@odata.type annotation is not supported and not needed here.
            data.Remove("@odata.type");
            command.Payload = data.ToJson(false);
            var result = await service.ExecuteObjectAsync(command);
            return result;
        }

        /// <summary>
        /// Transforms the provided object to a different object representing a sub-set of the original properties.
        /// </summary>
        /// <typeparam name="T">The type of object to transform</typeparam>
        /// <typeparam name="S">The type of transformed object</typeparam>
        /// <param name="obj">The object to transform</param>
        /// <param name="transformer">The transformer function</param>
        /// <returns>The transformed object</returns>
        public static S Subset<T, S>(this T obj, Func<T, S> transformer) where T : ApiResource
        {
            return transformer(obj);
        }


        /// <summary>
        /// Used to provide typed $expand clause by <see cref="EntityCommand{TResource, TResult}"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="I"></typeparam>
        /// <param name="resource"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static T Expand<T, I>(this T resource, Func<T, I> func)
            where T : EntityResource?
            where I : EntityResource?
        {
            return resource;
        }
        /// <summary>
        /// Used to provide typed $expand clause by <see cref="EntityCommand{TResource, TResult}"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="I"></typeparam>
        /// <param name="resource"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static T ExpandCollection<T, I>(this T resource, Func<T, IEnumerable<I>?> func)
            where T : EntityResource?
            where I : EntityResource?
        {
            return resource;
        }

        /// <summary>
        /// Used to provide typed $expand clause by <see cref="EntityCommand{TResource, TResult}"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="I"></typeparam>
        /// <param name="items"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static IEnumerable<T>? ExpandItems<T, I>(this IEnumerable<T>? items, Func<T, I> func)
            where T : EntityResource?
            where I : EntityResource?
        {
            return items;
        }

        internal static bool TryGetODataId(this IDictionary<string, object?> dict, out EntityIdentifier oid)
        {
            if (dict.TryGetValue("@odata.id", out var value) && value is string str)
            {
                oid = (EntityIdentifier)str;
                return true;
            }
            oid = new EntityIdentifier();
            return false;
        }

        /// <summary>
        /// Merges the source dictionary to the target dictionary.
        /// </summary>
        /// <param name="target">the target dictionary</param>
        /// <param name="source">the source dictionary</param>
        /// <returns>true if there are any modifications</returns>
        internal static bool DeepMerge(this IDictionary<string, object?> target, IDictionary<string, object?> source)
        {
            bool modified = false;
            foreach (var entry in source)
            {
                if (target.TryGetValue(entry.Key, out var existing))
                {
                    if (existing is IDictionary<string, object?> existingDict 
                        && entry.Value is IDictionary<string, object?> otherDict)
                    {
                        modified |= existingDict.DeepMerge(otherDict);
                    }
                    else if (!Equals(existing, entry.Value))
                    {
                        target[entry.Key] = entry.Value;
                        modified = true;
                    }
                }
                else
                {
                    target.Add(entry.Key, entry.Value);
                    modified = true;
                }
            }
            return modified;
        }
    }
}
