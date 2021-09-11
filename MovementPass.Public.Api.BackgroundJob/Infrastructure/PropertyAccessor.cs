namespace MovementPass.Public.Api.BackgroundJob.Infrastructure
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq.Expressions;
    using System.Reflection;

    public static class PropertyAccessor
    {
        private static readonly
            ConcurrentDictionary<(RuntimeTypeHandle, string),
                Func<object, object>> GetterCache =
                new ConcurrentDictionary<(RuntimeTypeHandle, string),
                    Func<object, object>>();

        private static readonly
            ConcurrentDictionary<(RuntimeTypeHandle, string),
                Action<object, object>> SetterCache =
                new ConcurrentDictionary<(RuntimeTypeHandle, string),
                    Action<object, object>>();

        public static Func<object, object> GetGetter(PropertyInfo property)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            if (property.DeclaringType == null)
            {
                throw new ArgumentException("Invalid property.");
            }

            var getter = GetterCache.GetOrAdd(
                (property.DeclaringType.TypeHandle, property.Name),
                _ => CreateGetter(property));

            return getter;
        }

        public static Action<object, object> GetSetter(PropertyInfo property)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            if (property.DeclaringType == null)
            {
                throw new ArgumentException("Invalid property.");
            }

            var setter = SetterCache.GetOrAdd(
                (property.DeclaringType.TypeHandle, property.Name),
                _ => CreateSetter(property));

            return setter;
        }

        private static Func<object, object> CreateGetter(PropertyInfo property)
        {
            var instance = Expression.Parameter(typeof(object), "i");

            // ReSharper disable once AssignNullToNotNullAttribute
            var instanceConvert =
                Expression.Convert(instance, property.DeclaringType);
            var get = Expression.Property(instanceConvert, property);
            var getConvert = Expression.Convert(get, typeof(object));

            var expression =
                Expression.Lambda<Func<object, object>>(getConvert, instance);

            return expression.Compile();
        }

        private static Action<object, object> CreateSetter(
            PropertyInfo property)
        {
            var instance = Expression.Parameter(typeof(object), "i");
            var value = Expression.Parameter(typeof(object), "v");

            // ReSharper disable once AssignNullToNotNullAttribute
            var instanceConvert =
                Expression.Convert(instance, property.DeclaringType);
            var valueConvert = Expression.Convert(value, property.PropertyType);

            var set = Expression
                .Assign(Expression.Property(instanceConvert, property),
                    valueConvert);

            var expression =
                Expression.Lambda<Action<object, object>>(set, instance, value);

            return expression.Compile();
        }
    }
}
