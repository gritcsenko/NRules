using System.Linq.Expressions;
using FastExpressionCompiler;
using NRules.RuleModel;

namespace NRules.Aggregators;

/// <summary>
/// Aggregator factory for group by aggregator.
/// </summary>
internal class GroupByAggregatorFactory : IAggregatorFactory
{
    private Func<IAggregator>? _factory;

    public void Compile(AggregateElement element, IEnumerable<IAggregateExpression> compiledExpressions)
    {
        var keySelector = element.Expressions[AggregateElement.KeySelectorName];
        var elementSelector = element.Expressions[AggregateElement.ElementSelectorName];

        var sourceType = element.Source.ValueType;
        var keyType = keySelector.Expression.ReturnType;
        var elementType = elementSelector.Expression.ReturnType;
        var aggregatorType = typeof(GroupByAggregator<,,>).MakeGenericType(sourceType, keyType, elementType);

        var compiledKeySelector = compiledExpressions.FindSingle(AggregateElement.KeySelectorName);
        var compiledElementSelector = compiledExpressions.FindSingle(AggregateElement.ElementSelectorName);
        var ctor = aggregatorType.GetConstructors().Single();
        var factoryExpression = Expression.Lambda<Func<IAggregator>>(
            Expression.New(ctor, Expression.Constant(compiledKeySelector), Expression.Constant(compiledElementSelector)));
        _factory = factoryExpression.CompileFast();
    }

    public IAggregator Create()
    {
        return _factory?.Invoke() ?? throw new InvalidOperationException("Aggregator was not compiled");
    }
}