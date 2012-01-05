using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NHibernate.Hql.Ast;
using NHibernate.Linq.Expressions;
using Remotion.Linq.Parsing;

namespace NHibernate.Linq.Visitors
{
	public class SelectClauseVisitor : ExpressionTreeVisitor
	{
		private readonly HqlTreeBuilder _hqlTreeBuilder = new HqlTreeBuilder();
		private HashSet<Expression> _hqlNodes;
		private readonly ParameterExpression _inputParameter;
		private readonly VisitorParameters _parameters;
		private int _iColumn;
		private List<HqlExpression> _hqlTreeNodes = new List<HqlExpression>();
		private readonly HqlGeneratorExpressionTreeVisitor _hqlVisitor;

		public SelectClauseVisitor(System.Type inputType, VisitorParameters parameters)
		{
			_inputParameter = Expression.Parameter(inputType, "input");
			_parameters = parameters;
			_hqlVisitor = new HqlGeneratorExpressionTreeVisitor(_parameters);
		}

		public LambdaExpression ProjectionExpression { get; private set; }

		public IEnumerable<HqlExpression> GetHqlNodes()
		{
			return _hqlTreeNodes;
		}

		public void Visit(Expression expression)
		{
//		    var count = expression as NhCountExpression;
//            if (count != null)
//            {
//                expression = count.Expression;
//            }

			var distinct = expression as NhDistinctExpression;
			if (distinct != null)
			{
				expression = distinct.Expression;
			}

			// Find the sub trees that can be expressed purely in HQL
			_hqlNodes = new SelectClauseHqlNominator(_parameters).Nominate(expression);

			// Now visit the tree
			var projection = VisitExpression(expression);

			if ((projection != expression) && !_hqlNodes.Contains(expression))
			{
				ProjectionExpression = Expression.Lambda(projection, _inputParameter);
			}

			// Handle any boolean results in the output nodes
			_hqlTreeNodes = BooleanToCaseConvertor.Convert(_hqlTreeNodes).ToList();

			var hqlTreeNodesCount = _hqlTreeNodes.Count;
			if (distinct != null)
			{
				var treeNodes = new List<HqlTreeNode>(hqlTreeNodesCount + 1) {_hqlTreeBuilder.Distinct()};
				foreach (var treeNode in _hqlTreeNodes)
				{
					treeNodes.Add(treeNode);
				}
				_hqlTreeNodes = new List<HqlExpression>(1) {_hqlTreeBuilder.ExpressionSubTreeHolder(treeNodes)};
			}

//            if (count != null)
//            {
//                if(distinct!=null && hqlTreeNodesCount > 1)
//                {
//                    var hqlSelectFrom = _hqlTreeBuilder.SelectFrom(
//                        _hqlTreeBuilder.From()/*_hqlTreeBuilder.Range(_hqlTreeNodes.Single(), _hqlTreeBuilder.Alias("x")))*/,
//                        _hqlTreeBuilder.Select(_hqlTreeBuilder.Count(_hqlTreeBuilder.Star())));
//
//                    _hqlTreeNodes = new List<HqlExpression>(1)
//                                        {
//                                            _hqlTreeBuilder.Query(hqlSelectFrom).AsExpression()
//                                        };
//                }
//                _hqlTreeNodes = new List<HqlExpression>(1) { _hqlTreeBuilder.Cast(_hqlTreeBuilder.Count(_hqlTreeNodes.Single()), count.Type) };
//                ProjectionExpression = Expression.Lambda(Expression.Convert(Expression.ArrayIndex(_inputParameter, Expression.Constant(0)), count.Type), _inputParameter);
//            }
		}

		public override Expression VisitExpression(Expression expression)
		{
			if (expression == null)
			{
				return null;
			}

			if (_hqlNodes.Contains(expression))
			{
				// Pure HQL evaluation
				_hqlTreeNodes.Add(_hqlVisitor.Visit(expression).AsExpression());

				return Expression.Convert(Expression.ArrayIndex(_inputParameter, Expression.Constant(_iColumn++)), expression.Type);
			}

			// Can't handle this node with HQL.  Just recurse down, and emit the expression
			return base.VisitExpression(expression);
		}
	}

	public static class BooleanToCaseConvertor
	{
		public static IEnumerable<HqlExpression> Convert(IEnumerable<HqlExpression> hqlTreeNodes)
		{
			return hqlTreeNodes.Select(node => ConvertBooleanToCase(node));
		}

		public static HqlExpression ConvertBooleanToCase(HqlExpression node)
		{
			if (node is HqlBooleanExpression)
			{
				var builder = new HqlTreeBuilder();

				return builder.Case(
					new[] {builder.When(node, builder.True())},
					builder.False());
			}

			return node;
		}
	}
}