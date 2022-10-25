namespace MovementPass.Public.Api.BackgroundJob.ExtensionMethods;

using System;
using System.Collections;
    
using Infrastructure;

internal static class EnumerableExtensions
{
    public static IEnumerable Cast(this IEnumerable instance, Type type)
    {
        if (instance == null)
        {
            return null;
        }

        var caster = EnumerableCaster.Get(type);

        return caster(instance);
    }
}