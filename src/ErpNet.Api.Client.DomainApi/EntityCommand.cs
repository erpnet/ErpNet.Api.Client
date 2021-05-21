using ErpNet.Api.Client.DomainApi.Linq;
using ErpNet.Api.Client.OData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace ErpNet.Api.Client.DomainApi
{
    /// <summary>
    /// Represents a command for the specified resource in the first type argument.
    /// </summary>
    /// <typeparam name="TEntity">The type of the command resource</typeparam>
    /// <typeparam name="TResult">The type of the command result. Can be different from the resource type if $select clause is used.</typeparam>
    public class EntityCommand<TEntity, TResult> : ODataCommand where TEntity : EntityResource
    {
        Func<TEntity, TResult>? selector;
        
        /// <summary>
        /// Creates an instance of <see cref="EntityCommand{TEntity,TResult}"/>
        /// </summary>
        /// <param name="service"></param>
        public EntityCommand(DomainApiService service)
            : base(ApiResource.GetEntitySetName(typeof(TEntity)))
        {
            this.Service = service;
        }

        /// <summary>
        /// The service.
        /// </summary>
        public DomainApiService Service { get; }


        /// <summary>
        /// Sets the entity id for this command.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public EntityCommand<TEntity, TResult> Id(Guid id)
        {
            Key = id;
            Type = ErpCommandType.SingleEntity;
            return this;
        }

        /// <summary>
        /// Sets the $filter clause. Can be used several times in one command for composing long filters.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public EntityCommand<TEntity, TResult> Filter(Expression<Func<TEntity, bool>> filter)
        {
            if (!string.IsNullOrEmpty(FilterClause))
                FilterClause += " and ";
            FilterClause += FilterBuilder.GetFilterString(filter);
            Type = ErpCommandType.Query;
            return this;
        }

        /// <summary>
        /// Creates new command for the provided TSelectResult.
        /// </summary>
        /// <typeparam name="TSelectResult"></typeparam>
        /// <param name="selector"></param>
        /// <returns></returns>
        public EntityCommand<TEntity, TSelectResult> Select<TSelectResult>(Func<TEntity, TSelectResult> selector)
        {
            var resultType = typeof(TSelectResult);
            List<string> rootSelect = new List<string>();
            List<string> expand = new List<string>();
            foreach (var pi in resultType.GetProperties())
            {
                if (ShouldExpand(pi.PropertyType))
                    expand.Add(GetExpandClause(pi));
                if (pi.PropertyType == typeof(EntityIdentifier) && pi.Name.EndsWith("Id"))
                    rootSelect.Add(pi.Name.Substring(0, pi.Name.Length - 2));
                else
                    rootSelect.Add(pi.Name);
            }

            if (rootSelect.Any())
                this.SelectClause = string.Join(",", rootSelect);
            if (expand.Any())
                this.ExpandClause = string.Join(",", expand);

            var command = new EntityCommand<TEntity, TSelectResult>(Service);
            command.CopyFrom(this);
            command.selector = selector;
            return command;
        }


        /// <summary>
        /// Sets an $expand clause for this query command.
        /// </summary>
        /// <param name="expandExpressions">A function expression array with the properties to expand</param>
        /// <returns></returns>
        public EntityCommand<TEntity, TResult> Expand(params Expression<Func<TEntity, object?>>[] expandExpressions)
        {
            var root = ExpandNode.Parse(ExpandClause, SelectClause);

            foreach (var exp in expandExpressions)
            {
                var path = exp.Body.ToString().Split('.');//.Where(s => !s.Contains("(")).Select(s => s.TrimEnd(')')).Skip(1);

                Stack<ExpandNode> stack = new Stack<ExpandNode>();
                stack.Push(root);

                for (int i = 1; i < path.Length; i++)
                {
                    var s = path[i];
                    if (s.Contains("("))
                        continue;


                    int pop = s.Count((c) => c == ')');
                    s = s.TrimEnd(')');
                    var current = stack.Peek();
                    stack.Push(current.GetOrAdd(s));
                    for (int j = 0; j < pop; j++)
                        stack.Pop();
                }
            }

            if (root.Select.Any())
                this.SelectClause = string.Join(",", root.Select);
            if (root.Expand.Any())
                this.ExpandClause = string.Join(",", root.Expand);

            return this;
        }

        /// <summary>
        /// Sets a $top clause for this query command.
        /// </summary>
        /// <param name="top">The top rows to return</param>
        /// <returns></returns>
        public EntityCommand<TEntity, TResult> Top(int top)
        {
            Type = ErpCommandType.Query;
            this.TopClause = top;
            return this;
        }

        /// <summary>
        /// Sets a $skip clause for this query command.
        /// </summary>
        /// <param name="skip">The $skip row count to skip</param>
        /// <returns></returns>
        public EntityCommand<TEntity, TResult> Skip(int skip)
        {
            Type = ErpCommandType.Query;
            this.SkipClause = skip;
            return this;
        }

        /// <summary>
        /// Sets a $orderby clause for this query command.
        /// </summary>
        /// <returns></returns>
        public EntityCommand<TEntity, TResult> OrderBy()
        {
            throw new NotImplementedException();
            //Type = ErpCommandType.Query;
            //return this;
        }


        /// <summary>
        /// Executes the command and returns the result.
        /// </summary>
        /// <returns></returns>
        public async Task<EntityCommandResult<TEntity, TResult>> LoadAsync()
        {
            EntityCommandResult<TEntity, TResult> result;
            var json = await Service.ExecuteDictionaryAsync(this);
            if (json == null)
                return EntityCommandResult<TEntity, TResult>.Empty;

            result = new EntityCommandResult<TEntity, TResult>(
                    GetResources(json),
                    selector);

            return result;
        }

        // Private ----------------------

        static IEnumerable<TEntity> GetResources(IDictionary<string,object?> json)
        {
            if (json.ContainsKey("@odata.id"))
            {
                yield return ApiResource.Create<TEntity>(json);
            }
            else
            {
                if (json["value"] is IEnumerable array)
                foreach (var item in array)
                    yield return ApiResource.Create<TEntity>((IDictionary<string, object?>)item);
            }
        }

        static bool IsAnonymous(Type type) => type.Name.Contains("AnonymousType");

        static string GetExpandClause(PropertyInfo prop)
        {
            var type = prop.PropertyType;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                type = type.GetGenericArguments()[0];

            bool hasSelect = IsAnonymous(type);
            if (hasSelect)
            {
                List<string> select = new List<string>();
                List<string> expand = new List<string>();
                foreach (var pi in type.GetProperties())
                {
                    if (ShouldExpand(pi.PropertyType))
                        expand.Add(GetExpandClause(pi));
                    if (pi.PropertyType == typeof(EntityIdentifier) && pi.Name.EndsWith("Id"))
                        select.Add(pi.Name.Substring(0, pi.Name.Length - 2));
                    else
                        select.Add(pi.Name);
                }

                if (expand.Any())
                    return $"{prop.Name}($select={string.Join(",", select)};$expand={string.Join(",", expand)})";
                else if (select.Any())
                    return $"{prop.Name}($select={string.Join(",", select)})";
                else
                    return prop.Name;
            }
            else
            {
                return prop.Name;
            }
        }

        static bool ShouldExpand(Type type)
        {
            if (type.IsGenericType
                && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var elementType = type.GetGenericArguments()[0];
                return IsAnonymous(elementType) || typeof(EntityResource).IsAssignableFrom(elementType);
            }

            return IsAnonymous(type) || typeof(EntityResource).IsAssignableFrom(type);
        }
    }

    /// <summary>
    /// A result from a <see cref="EntityCommand{TEntity, TResult}"/> command.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    public class EntityCommandResult<TEntity, TResult> : IEnumerable<TResult>
    {
        /// <summary>
        /// Creates an instance
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="selector"></param>
        public EntityCommandResult(
            IEnumerable<TEntity> resources, 
            Func<TEntity, TResult>? selector)
        {
            Resources = resources;
            if (selector != null)
                Result = resources.Select(selector);
            else
                Result = Resources.Cast<TResult>();
        }

        /// <summary>
        /// Represents an empty result.
        /// </summary>
        public static EntityCommandResult<TEntity, TResult> Empty
        {
            get
            {
                return new EntityCommandResult<TEntity, TResult>(Enumerable.Empty<TEntity>(), (r) => (TResult)(object)r);
            }
        }

        /// <summary>
        /// The typed collection of entities. Note that only properties included in $select clause are present in the entity resource instance.
        /// </summary>
        public IEnumerable<TEntity> Resources { get; }

        /// <summary>
        /// The collection of {TResult} instances. 
        /// </summary>
        public IEnumerable<TResult> Result { get; }

        /// <summary>
        /// Gets the result enumerator.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<TResult> GetEnumerator() => Result.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
