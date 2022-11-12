using System.Linq.Expressions;
using FastExpressionCompiler;
using NRules.RuleModel;

namespace NRules.Aggregators;

/// <summary>
/// Aggregator factory for flattening aggregator.
/// </summary>
internal class FlatteningAggregatorFactory : IAggregatorFactory
{
    private Func<IAggregator>? _factory;

    public void Compile(AggregateElement element, IEnumerable<IAggregateExpression> compiledExpressions)
    {
        var sourceType = element.Source.ValueType;
        //Flatten selector is Source -> IEnumerable<Result>
        var resultType = element.ResultType;
        var aggregatorType = typeof(FlatteningAggregator<,>).MakeGenericType(sourceType, resultType);

        var compiledSelector = compiledExpressions.FindSingle(AggregateElement.SelectorName);
        var ctor = aggregatorType.GetConstructors().Single();
        var factoryExpression = Expression.Lambda<Func<IAggregator>>(
            Expression.New(ctor, Expression.Constant(compiledSelector)));
        _factory = factoryExpression.CompileFast();
    }

    public IAggregator Create()
    {
        return _factory?.Invoke() ?? throw new InvalidOperationException("Aggregator was not compiled");
    }
}