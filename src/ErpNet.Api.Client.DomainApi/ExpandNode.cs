using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ErpNet.Api.Client.DomainApi
{
    /// <summary>
    /// A helper class used to parse $expand and $select odata clauses.
    /// </summary>
    public class ExpandNode
    {
        /// <summary>
        /// Creates an instance.
        /// </summary>
        /// <param name="name"></param>
        public ExpandNode(string name) { Name = name; }
        /// <summary>
        /// The expanded property name.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// List of selected properties.
        /// </summary>
        public readonly List<string> Select = new List<string>();
        /// <summary>
        /// List of expanded properties.
        /// </summary>
        public readonly List<ExpandNode> Expand = new List<ExpandNode>();

        /// <summary>
        /// Gets or creates a sub-node.
        /// </summary>
        /// <param name="prop"></param>
        /// <returns></returns>
        public ExpandNode GetOrAdd(string prop)
        {
            var node = Expand.FirstOrDefault(n => n.Name == prop);
            if (node == null)
            {
                node = new ExpandNode(prop);
                Expand.Add(node);
            }
            return node;
        }

        /// <summary>
        /// Gets or adds a list of expanded nodes.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public ExpandNode GetOrAdd(IEnumerable<string> path)
        {
            if (path == null || !path.Any())
                return this;
            var current = this;
            foreach (var prop in path)
            {
                var node = current.GetOrAdd(prop);
                current = node;
            }
            return current;
        }
        /// <summary>
        /// The string representation of the $expand clause.
        /// </summary>
        public string ExpandClause => string.Join(",", Expand);

        /// <summary>
        /// The string representation of the $select clause.
        /// </summary>
        public string SelectClause => string.Join(",", Select);

        /// <summary>
        /// The string representation of the node.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder b = new StringBuilder();
            b.Append(Name);
            if (Select.Any() || Expand.Any())
            {
                b.Append("(");
                if (Expand.Any())
                {
                    b.Append("$expand=");
                    b.Append(string.Join(",", Expand));
                }
                if (Select.Any())
                {
                    if (Expand.Any())
                        b.Append(";");
                    b.Append("$select=");
                    b.Append(string.Join(",", Select));
                }
                b.Append(")");
            }
            return b.ToString();
        }

        enum ParseState
        {
            Expand,
            Select,
            Clause
        }
        class NodeState
        {
            public NodeState(ExpandNode node)
            {
                Node = node;
            }
            public ExpandNode Node { get; }
            public ParseState State;
        }

        /// <summary>
        /// Parses an expand and select clause into an <see cref="ExpandNode"/>
        /// </summary>
        /// <param name="expandClause"></param>
        /// <param name="selectClause"></param>
        /// <returns></returns>
        public static ExpandNode Parse(string? expandClause, string? selectClause)
        {
            NodeState root = new NodeState(new ExpandNode("."));
            if (expandClause != null && !string.IsNullOrEmpty(expandClause))
            {

                Stack<NodeState> stack = new Stack<NodeState>();
                stack.Push(root);
                StringBuilder token = new StringBuilder();

                void AddSelectOrExpand(int i)
                {
                    if (token.Length == 0)
                        return;
                    var current = stack.Peek();
                    switch (current.State)
                    {
                        case ParseState.Select:
                            current.Node.Select.Add(token.ToString());
                            token.Clear();
                            break;
                        case ParseState.Expand:
                            var n = new ExpandNode(token.ToString());
                            current.Node.Expand.Add(n);
                            token.Clear();
                            break;
                        default:
                            throw new Exception($"Invalid parser state {current.State} at char {i}.");
                    }
                }

                for (int i = 0; i < expandClause.Length; i++)
                {
                    var c = expandClause[i];
                    if (c == '(')
                    {
                        var open = new NodeState(new ExpandNode(token.ToString()))
                        {
                            State = ParseState.Clause
                        };


                        stack.Push(open);
                        token.Clear();
                        continue;
                    }
                    else if (c == ')')
                    {
                        AddSelectOrExpand(i);
                        //state = ParseState.None;
                        var item = stack.Pop();
                        stack.Peek().Node.Expand.Add(item.Node);
                        continue;
                    }
                    else if (c == '=')
                    {
                        if (stack.Peek().State != ParseState.Clause)
                            throw new Exception($"Invalid parser state {stack.Peek().State} at char {i}.");
                        switch (token.ToString())
                        {
                            case "$expand":
                                stack.Peek().State = ParseState.Expand;
                                break;
                            case "$select":
                                stack.Peek().State = ParseState.Select;
                                break;
                            default:
                                throw new Exception($"Invalid clause {token}");
                        }
                        token.Clear();
                        continue;
                    }
                    else if (c == ',')
                    {
                        AddSelectOrExpand(i);
                        continue;
                    }
                    else if (c == ';')
                    {
                        AddSelectOrExpand(i);
                        stack.Peek().State = ParseState.Clause;
                        continue;
                    }
                    token.Append(c);

                }

                AddSelectOrExpand(expandClause.Length);
            }

            if (selectClause != null && !string.IsNullOrEmpty(selectClause))
            {
                root.Node.Select.AddRange(selectClause.Split(','));
            }

            return root.Node;
        }
    }
}
