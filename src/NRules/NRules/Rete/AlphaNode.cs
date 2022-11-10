﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using NRules.Diagnostics;

namespace NRules.Rete;

internal abstract class AlphaNode : IObjectSink
{
    protected AlphaNode(int id, Type? outputType = null)
    {
        Id = id;
        NodeInfo = new(outputType);
    }

    public int Id { get; }
    public NodeInfo NodeInfo { get; }
    public AlphaMemoryNode? MemoryNode { get; set; }

    [DebuggerDisplay("Count = {ChildNodes.Count}")]
    public List<AlphaNode> ChildNodes { get; } = new();

    protected abstract bool IsSatisfiedBy(IExecutionContext context, Fact fact);

    public virtual void PropagateAssert(IExecutionContext context, IReadOnlyCollection<Fact> facts)
    {
        var toAssert = new List<Fact>();
        using (var counter = PerfCounter.Assert(context, this))
        {
            foreach (var fact in facts)
            {
                if (IsSatisfiedBy(context, fact))
                    toAssert.Add(fact);
            }
            counter.AddInputs(facts.Count);
            counter.AddOutputs(toAssert.Count);
        }

        if (toAssert.Count > 0)
        {
            foreach (var childNode in ChildNodes)
            {
                childNode.PropagateAssert(context, toAssert);
            }
            MemoryNode?.PropagateAssert(context, toAssert);
        }
    }

    public virtual void PropagateUpdate(IExecutionContext context, IReadOnlyCollection<Fact> facts)
    {
        var toUpdate = new List<Fact>();
        var toRetract = new List<Fact>();
        using (var counter = PerfCounter.Update(context, this))
        {
            foreach (var fact in facts)
            {
                if (IsSatisfiedBy(context, fact))
                    toUpdate.Add(fact);
                else
                    toRetract.Add(fact);
            }
            counter.AddInputs(facts.Count);
            counter.AddOutputs(toUpdate.Count + toRetract.Count);
        }

        PropagateUpdateInternal(context, toUpdate);
        PropagateRetractInternal(context, toRetract);
    }

    public virtual void PropagateRetract(IExecutionContext context, IReadOnlyCollection<Fact> facts)
    {
        using (var counter = PerfCounter.Retract(context, this))
        {
            counter.AddItems(facts.Count);
        }
        PropagateRetractInternal(context, facts);
    }

    protected void PropagateUpdateInternal(IExecutionContext context, IReadOnlyCollection<Fact> facts)
    {
        if (facts.Count == 0)
            return;
        foreach (var childNode in ChildNodes)
        {
            childNode.PropagateUpdate(context, facts);
        }
        MemoryNode?.PropagateUpdate(context, facts);
    }

    protected void PropagateRetractInternal(IExecutionContext context, IReadOnlyCollection<Fact> facts)
    {
        if (facts.Count == 0)
            return;
        foreach (var childNode in ChildNodes)
        {
            childNode.PropagateRetract(context, facts);
        }
        MemoryNode?.PropagateRetract(context, facts);
    }

    public abstract void Accept<TContext>(TContext context, ReteNodeVisitor<TContext> visitor);
}