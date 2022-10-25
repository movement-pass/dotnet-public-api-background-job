namespace MovementPass.Public.Api.BackgroundJob.ExtensionMethods;

using System;
using System.Collections.Generic;
using System.Linq;

public static class TypeExtensions
{
    public static bool Is<T>(this Type instance) =>
        Is(instance, typeof(T));

    public static bool Is(this Type instance, Type other)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        if (other == null)
        {
            throw new ArgumentNullException(nameof(other));
        }

        return other.IsAssignableFrom(instance);
    }

    public static bool IsGenericDictionary(this Type instance) =>
        IsGenericType(instance, typeof(IDictionary<,>));

    public static bool IsGenericEnumerable(this Type instance) =>
        IsGenericType(instance, typeof(IEnumerable<>));

    public static Type RealType(this Type instance) =>
        Nullable.GetUnderlyingType(instance) ?? instance;

    private static bool IsGenericType(Type instance, Type other)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        bool Matches(Type t)
        {
            var td = t.GetGenericTypeDefinition();

            return td == other;
        }

        if (instance.IsGenericType && Matches(instance))
        {
            return true;
        }

        var interfaces = instance.GetInterfaces();

        return interfaces
            .Any(i => i.IsGenericType && Matches(i));
    }
}