namespace MovementPass.Public.Api.BackgroundJob.ExtensionMethods
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Infrastructure;

    public static class ObjectExtensions
    {
        public static TTarget Merge<TTarget, TSource>(
            this TTarget target,
            TSource source,
            params string[] excludedProperties)
            where TTarget : class
            where TSource : class
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            const BindingFlags flags =
                BindingFlags.Public | BindingFlags.Instance;

            var sourceProperties = source.GetType()
                .GetProperties(flags)
                .Where(p => p.CanRead)
                .Where(p =>
                    excludedProperties == null ||
                    !excludedProperties.Contains(p.Name,
                        StringComparer.OrdinalIgnoreCase))
                .ToList();

            var mappings = target.GetType()
                .GetProperties(flags)
                .Where(p => p.CanRead && p.CanWrite)
                .Select(t => new {
                    targetProperty = t,
                    sourceProperty = sourceProperties
                        .FirstOrDefault(s =>
                            s.Name == t.Name &&
                            s.PropertyType == t.PropertyType)
                })
                .Where(x => x.sourceProperty != null)
                .ToList();

            foreach (var map in mappings)
            {
                var sourceGetter =
                    PropertyAccessor.GetGetter(map.sourceProperty);
                var targetGetter =
                    PropertyAccessor.GetGetter(map.targetProperty);

                var newValue = sourceGetter(source);
                var oldValue = targetGetter(target);

                if (newValue == oldValue)
                {
                    continue;
                }

                var setter = PropertyAccessor.GetSetter(map.targetProperty);

                setter(target, newValue);
            }

            return target;
        }
    }
}
