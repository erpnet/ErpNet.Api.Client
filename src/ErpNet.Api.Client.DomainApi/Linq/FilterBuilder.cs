using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ErpNet.Api.Client.DomainApi.Linq
{
    class FilterBuilder : ExpressionVisitor
    {
        StringBuilder filter = new StringBuilder();
        const string Space = " ";
        bool appendSpace = true;

        private FilterBuilder() { }

        public static string GetFilterString(Expression expression)
        {
            FilterBuilder b = new FilterBuilder();
            b.Visit(expression);
            return b.filter.ToString();
        }

        protected override Expression VisitUnary(UnaryExpression u)
        {
            switch (u.NodeType)
            {
                case ExpressionType.Quote:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                    break;
                case ExpressionType.Negate:
                    AppendToken("-", false);
                    break;
                case ExpressionType.Not:
                    AppendToken("not");
                    break;
            }

            return base.VisitUnary(u);

            //we do not support unary operators at all
            throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "LINQ: The unary operator '{0}' is not supported", u.NodeType));
        }

        protected override Expression VisitMemberAccess(MemberExpression m)
        {
            bool success = false;
            if (typeof(ApiResource).IsAssignableFrom(m.Member.DeclaringType))
            {
                success = true;
                var tok = m.Member.Name;
                var exp = m.Expression as MemberExpression;
                while (exp != null)
                {
                    if (exp.Member.MemberType != MemberTypes.Property)
                    {
                        success = false;
                        break;
                    }
                    if (typeof(EntityResource).IsAssignableFrom(exp.Type))
                        tok = exp.Member.Name + "/" + tok;
                    else
                        tok = exp.Member.Name + tok;
                    exp = exp.Expression as MemberExpression;
                }
                if (success)
                {
                    AppendToken(tok);
                }
            }

            if (!success)
            {
                var value = GetValue(m);
                AppendConstant(value, m.Type);
            }
            return m;
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            Visit(b.Left);

            switch (b.NodeType)
            {
                case ExpressionType.AndAlso:
                    AppendToken("and");
                    break;
                case ExpressionType.OrElse:
                    AppendToken("or");
                    break;
                case ExpressionType.Add:
                    AppendToken("add");
                    break;
                case ExpressionType.Subtract:
                    AppendToken("sub");
                    break;
                case ExpressionType.Multiply:
                    AppendToken("mul");
                    break;
                case ExpressionType.Divide:
                    AppendToken("div");
                    break;
                case ExpressionType.Modulo:
                    AppendToken("mod");
                    break;
                case ExpressionType.Equal:
                    AppendToken("eq");
                    break;
                case ExpressionType.GreaterThan:
                    AppendToken("gt");
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    AppendToken("ge");
                    break;
                case ExpressionType.NotEqual:
                    AppendToken("ne");
                    break;
                case ExpressionType.LessThan:
                    AppendToken("lt");
                    break;
                case ExpressionType.LessThanOrEqual:
                    AppendToken("le");
                    break;
                default:
                    throw new NotImplementedException($"Binary operator {b.Type} is not supported.");
            }
            Visit(b.Right);
            return b;
        }

        string ToStringConstant(object? val, Type type)
        {
            if (val == null)
            {
                return "null";
            }
            else if (type.IsEnum || val is string || val is EntityIdentifier)
            {
                return $"'{val}'";
            }
            else if (val is DateTimeOffset d)
            {
                return d.ToString("s") + "Z";
            }
            else if (val is DateTime dt)
            {
                return dt.ToString("s") + "Z";
            }
            else if (val is bool b)
            {
                return b ? "true" : "false";
            }
            else if (val is EntityResource res)
            {
                return ToStringConstant((string)res.ODataId.GetValueOrDefault(), typeof(string));
            }
            else
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}", val);
            }
        }

        void AppendConstant(object? val, Type type)
        {
            AppendToken(ToStringConstant(val, type));
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            AppendConstant(c.Value, c.Type);
            return base.VisitConstant(c);
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method.DeclaringType == typeof(FilterMethods) && m.Method.Name == nameof(FilterMethods.In))
            {
                Visit(m.Object);
                Visit(m.Arguments[0]);
                filter.Append(" in (");
                var list = (System.Collections.IEnumerable?)GetValue(m.Arguments[1]);
                bool first = true;
                if (list != null)
                    foreach (var item in list)
                    {
                        if (!first)
                            filter.Append(",");
                        var c = ToStringConstant(item, item?.GetType() ?? typeof(object));
                        filter.Append(c);
                        first = false;
                    }
                filter.Append(")");
                return m;
            }
            else if (m.Method.DeclaringType == typeof(ApiResource) && m.Method.Name == "get_Item")
            {
                var propName = GetValue(m.Arguments[0])?.ToString();
                AppendToken(propName);
                return m;
            }
            else if (m.Method.DeclaringType == typeof(ApiResource) && m.Method.Name == "GetPropertyValue")
            {
                var propName = GetValue(m.Arguments[0])?.ToString();
                AppendToken(propName);
                return m;
            }
            else
            {
                AppendToken(m.Method.Name.ToLower(), false).Append("(");
                int n = 0;
                if (!m.Method.IsStatic)
                {
                    Visit(m.Object);
                    n++;
                }
                foreach (var arg in m.Arguments)
                {
                    if (n > 0)
                        filter.Append(",");
                    n++;
                    Visit(arg);
                }
                filter.Append(")");
                return m;
            }
        }

        protected override ReadOnlyCollection<Expression?> VisitExpressionList(ReadOnlyCollection<Expression?> original)
        {
            bool first = true;
            foreach (var exp in original)
            {
                if (!first)
                    filter.Append(",");
                Visit(exp);
                first = false;
            }
            return original;
        }

        private static Expression StripQuotes(Expression e)
        {
            while (e.NodeType == ExpressionType.Quote)
            {
                e = ((UnaryExpression)e).Operand;
            }

            return e;
        }

        StringBuilder AppendToken(string? tok, bool appendSpace = true)
        {
            if (filter.Length > 0 && this.appendSpace)
                filter.Append(Space);
            filter.Append(tok);
            this.appendSpace = appendSpace;
            return filter;
        }

        private object? GetValue(Expression expression)
        {
            if (expression == null)
                return null;

            if (expression is UnaryExpression)
            {
                UnaryExpression u = (UnaryExpression)expression;
                if (u.NodeType == ExpressionType.Convert)
                {
                    object? value = GetValue(u.Operand);
                    return value;
                }

                if (u.NodeType == ExpressionType.Quote)
                {
                    return StripQuotes(u);
                }
            }
            else if (expression is MethodCallExpression)
            {
                MethodCallExpression m = (MethodCallExpression)expression;
                List<object?> args = new List<object?>();
                foreach (var ae in m.Arguments)
                    args.Add(GetValue(ae));
                object? target = GetValue(m.Object);
                return m.Method.Invoke(target, args.ToArray());
            }
            else if (expression is MemberExpression)
            {
                MemberExpression m = (MemberExpression)expression;
                object? target = GetValue(m.Expression);

                if (m.Member is FieldInfo fi)
                    return fi.GetValue(target);

                if (m.Member is PropertyInfo pi)
                    return pi.GetValue(target, null);
            }
            else if (expression is ConstantExpression)
            {
                ConstantExpression c = (ConstantExpression)expression;
                return c.Value;
            }
            else if (expression is NewExpression)
            {
                NewExpression e = (NewExpression)expression;
                List<object?> args = new List<object?>();
                foreach (var ae in e.Arguments)
                    args.Add(GetValue(ae));
                return e.Constructor.Invoke(args.ToArray());
            }
            else if (expression is NewArrayExpression)
            {
                NewArrayExpression e = (NewArrayExpression)expression;
                Array a = Array.CreateInstance(e.Type.GetElementType(), e.Expressions.Count);
                for (int i = 0; i < a.Length; i++)
                {
                    a.SetValue(GetValue(e.Expressions[i]), i);
                }
                return a;
            }
            else if (expression is ListInitExpression)
            {
                ListInitExpression e = (ListInitExpression)expression;
                List<ApiResource?> a = new List<ApiResource?>();
                foreach (var item in e.Initializers)
                {
                    List<object?> args = new List<object?>();
                    foreach (var ae in item.Arguments)
                        args.Add(GetValue(ae));
                    a.Add((ApiResource?)args[0]);
                    //item.AddMethod.Invoke(a, args.ToArray());
                }
                return a;
            }
            throw new InvalidOperationException("Expression type is not supported");
        }

    }



}
