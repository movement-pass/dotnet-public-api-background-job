namespace MovementPass.Public.Api.BackgroundJob.ExtensionMethods
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    using Infrastructure;

    internal static class EnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> instance, int size)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (size < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(size), "Must be a positive value!");
            }

            return instance.Select((item, index) => new { Index = index, Item = item })
                .GroupBy(x => x.Index / size)
                .Select(g => g.Select(x => x.Item));
        }

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
}
