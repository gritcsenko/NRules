﻿using System.Collections.Generic;
using NRules.Fluent.Dsl;
using NRules.IntegrationTests.TestAssets;
using Xunit;

namespace NRules.IntegrationTests;

public class CoJoinedCollectAndExistsRulesTest : BaseRulesTestFixture
{
    [Fact]
    public void Fire_MatchingFactOfFirstKindNoFactsOfOtherKind_FiresCollect()
    {
        //Arrange
        var fact1 = new FactType1 { TestProperty = "Valid Value 1" };

        Session.Insert(fact1);

        //Act
        Session.Fire();

        //Assert
        Verify.Rule<CollectionRule>().FiredTimes(1);
        Verify.Rule<ExistsRule>().FiredTimes(0);
    }

    [Fact]
    public void Fire_MatchingFactOfFirstKindAndMatchingFactOfOtherKind_EachFiresOnce()
    {
        //Arrange
        var fact1 = new FactType1 { TestProperty = "Valid Value 1" };
        var fact2 = new FactType2 { TestProperty = "Valid Value 2", JoinProperty = fact1.TestProperty };

        Session.Insert(fact1);
        Session.Insert(fact2);

        //Act
        Session.Fire();

        //Assert
        Verify.Rule<CollectionRule>().FiredTimes(1);
        Verify.Rule<ExistsRule>().FiredTimes(1);
    }

    protected override void SetUpRules(Testing.IRepositorySetup setup)
    {
        setup.Rule<ExistsRule>();
        setup.Rule<CollectionRule>();
    }

    public class FactType1
    {
        public string TestProperty { get; set; }
    }

    public class FactType2
    {
        public string TestProperty { get; set; }
        public string JoinProperty { get; set; }
    }

    public class ExistsRule : Rule
    {
        public override void Define()
        {
            FactType1 fact = null;

            When()
                .Match(() => fact, f => f.TestProperty.StartsWith("Valid"))
                .Exists<FactType2>(f => f.TestProperty.StartsWith("Valid"),
                    f => f.JoinProperty == fact.TestProperty);
            Then()
                .Do(ctx => ctx.NoOp());
        }
    }

    public class CollectionRule : Rule
    {
        public override void Define()
        {
            FactType1 fact = null;
            IEnumerable<FactType2> collection = null;

            When()
                .Match(() => fact, f => f.TestProperty.StartsWith("Valid"))
                .Query(() => collection, x => x
                    .Match<FactType2>(
                        f => f.TestProperty.StartsWith("Valid"),
                        f => f.JoinProperty == fact.TestProperty)
                    .Collect());
            Then()
                .Do(ctx => ctx.NoOp());
        }
    }
}