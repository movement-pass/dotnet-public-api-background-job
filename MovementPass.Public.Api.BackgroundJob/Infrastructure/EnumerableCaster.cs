namespace MovementPass.Public.Api.BackgroundJob.Infrastructure;

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;

public static class EnumerableCaster
{
    private static readonly ConcurrentDictionary<RuntimeTypeHandle,
        Func<IEnumerable, IEnumerable>> Cache =
        new ConcurrentDictionary<RuntimeTypeHandle,
            Func<IEnumerable, IEnumerable>>();

    public static Func<IEnumerable, IEnumerable> Get(Type type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        var caster = Cache.GetOrAdd(type.TypeHandle, _ => Create(type));

        return caster;
    }

    private static Func<IEnumerable, IEnumerable> Create(Type type)
    {
        var param = Expression.Parameter(typeof(IEnumerable), "e");
        var call = Expression.Call(typeof(Enumerable),
            nameof(Enumerable.Cast), new[] {type}, param);

        return Expression
            .Lambda<Func<IEnumerable, IEnumerable>>(call, param).Compile();
    }
}