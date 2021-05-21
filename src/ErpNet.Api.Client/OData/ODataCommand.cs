using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ErpNet.Api.Client.OData
{
    /// <summary>
    /// Represents a ODATA API command.
    /// </summary>
    public class ODataCommand
    {
        Dictionary<string, string> options = new Dictionary<string, string>();

        ///
        public ODataCommand(string resourceName)
        {
            this.ResourceName = resourceName;
        }

        /// <summary>
        /// Gets or sets the odata $select clause.
        /// </summary>
        public string? SelectClause { get { return GetOption("$select"); } set { SetOption("$select", value); } }
        /// <summary>
        /// Gets or sets the odata $expand clause.
        /// </summary>
        public string? ExpandClause { get { return GetOption("$expand"); } set { SetOption("$expand", value); } }

        /// <summary>
        /// Gets or sets the odata $filter clause.
        /// </summary>
        public string? FilterClause { get { return GetOption("$filter"); } set { SetOption("$filter", value); } }

        /// <summary>
        /// Gets or sets the odata $top clause.
        /// </summary>
        public int? TopClause { get { return GetOption<int>("$top"); } set { SetOption("$top", value); } }

        /// <summary>
        /// Gets or sets the odata $skip clause.
        /// </summary>
        public int? SkipClause { get { return GetOption<int>("$skip"); } set { SetOption("$skip", value); } }

        /// <summary>
        /// The entity id of the command in case <see cref="ResourceName"/> is an entity.
        /// </summary>
        public Guid? Key { get; set; }


        /// <summary>
        /// Gets or sets the bound operation name - action or function.
        /// </summary>
        /// <value>
        /// The operation.
        /// </value>
        public string? Operation { get; set; }

        /// <summary>
        /// Gets or sets the type of the command.
        /// </summary>
        public ErpCommandType Type { get; set; } = ErpCommandType.Query;

        /// <summary>
        /// Gets or sets the name of the resource. Can be entity set, function or action.
        /// </summary>
        /// <value>
        /// The name of the resource.
        /// </value>
        public string ResourceName { get; }
        /// <summary>
        /// The payload of the command in case of POST or PATCH command.
        /// </summary>
        public string? Payload { get; set; }

        /// <summary>
        /// Additional options for the command provided as url parameters.
        /// </summary>
        public IEnumerable<KeyValuePair<string, string>> Options => options;

        /// <summary>
        /// Assigns the properties of the provided command to this command.
        /// </summary>
        /// <param name="command"></param>
        public virtual void CopyFrom(ODataCommand command)
        {
            if (ResourceName != null && ResourceName != command.ResourceName)
                throw new InvalidOperationException($"Can't copy command. Resource name should be {ResourceName} but it is {command.ResourceName}");
            this.options = command.options;
            this.Type = command.Type;
            this.Key = command.Key;
            this.Payload = command.Payload;
        }

        /// <summary>
        /// Returns the uri of the command. Used to build a HTTP request.
        /// </summary>
        /// <returns></returns>
        public string GetUriString()
        {
            StringBuilder uri = new StringBuilder($"{this.ResourceName}");
            if (Key != null)
                uri.Append($"({this.Key})");

            if (Operation != null)
                uri.Append("/").Append(Operation);

            if (Type == ErpCommandType.Count)
                uri.Append("/$count");

            if (Options.Any())
            {
                uri.Append("?");
                uri.Append(string.Join("&", this.Options.Select(o => $"{o.Key}={o.Value}")));
            }
            return uri.ToString();
        }

        /// <summary>
        /// Returns the string representation of the command.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (Payload != null)
                return $"{Type} { GetUriString()}\r\n\r\n{Payload}";
            return $"{Type} { GetUriString()}";
        }

        /// <summary>
        /// Gets a URI option.
        /// </summary>
        /// <param name="name">option name</param>
        /// <returns></returns>
        public string? GetOption(string name)
        {
            options.TryGetValue(name, out var value);
            return value;
        }
        /// <summary>
        /// Sets a URI option.
        /// </summary>
        /// <param name="name">option name</param>
        /// <param name="value">option value</param>
        public void SetOption(string name, string? value)
        {
            if (value == null)
                options.Remove(name);
            else
                options[name] = value;
        }

        private T? GetOption<T>(string name) where T : struct
        {
            options.TryGetValue(name, out var value);
            if (value == null)
                return null;
            return (T)Convert.ChangeType(value, typeof(T));
        }

        private void SetOption<T>(string name, T? value) where T : struct
        {
            if (value == null)
                options.Remove(name);
            else
                options[name] = string.Format(CultureInfo.InvariantCulture, "{0}", value);
        }
    }

    /// <summary>
    /// Represents an odata command type.
    /// </summary>
    public enum ErpCommandType
    {
        /// <summary>
        /// The command is executed as GET HTTP request and returns many objects.
        /// </summary>
        Query,
        /// <summary>
        /// The command is executed as GET HTTP request and returns single object.
        /// </summary>
        SingleEntity,
        /// <summary>
        /// The command is executed as PATCH HTTP request and returns no object.
        /// </summary>
        Update,
        /// <summary>
        /// The command is for inserting an entity, is executed as POST HTTP request and returns single entity object.
        /// </summary>
        Insert,
        /// <summary>
        /// The command is executed as DELETE HTTP request and returns no object.
        /// </summary>
        Delete,
        /// <summary>
        /// The command is executed as POST HTTP request and returns the action result. The <see cref="ODataCommand.Operation"/> should be provided.
        /// </summary>
        Action,
        /// <summary>
        /// The command is executed as GET HTTP request and returns the function result. The <see cref="ODataCommand.Operation"/> should be provided.
        /// </summary>
        Function,
        /// <summary>
        /// The command is executed as GET HTTP request and returns only the row count for the specified query filter.
        /// </summary>
        Count
    }
}
