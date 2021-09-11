namespace MovementPass.Public.Api.BackgroundJob.ExtensionMethods
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;

    using Amazon.DynamoDBv2.Model;
    using Amazon.Util;

    using Infrastructure;

    public static class DynamoDBExtensions
    {
        private static readonly IEnumerable<Type> NumberTypes = new[]
        {
            typeof(byte),
            typeof(sbyte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(decimal)
        };

        public static Dictionary<string, AttributeValue>
            ToDynamoDBAttributes<T>(this T instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            var attributes = instance.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .Select(p =>
                {
                    var column = p.GetCustomAttribute<ColumnAttribute>();

                    var name = column?.Name ?? p.Name;
                    var getter = PropertyAccessor.GetGetter(p);
                    var value = getter(instance);

                    var av = Get(value, p);

                    return av == null ? null : new {Key = name, Value = av};
                })
                .Where(entry => entry != null)
                .ToDictionary(d => d.Key, d => d.Value);

            return attributes;
        }

        public static T FromDynamoDBAttributes<T>(
            this IDictionary<string, AttributeValue> instance)
            where T : class, new()
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            return (T)FromAttributes(typeof(T), instance);
        }

        private static object FromAttributes(
            Type type,
            IDictionary<string, AttributeValue> item)
        {
            if (item == null)
            {
                return null;
            }

            var target = type.GetProperties()
                .Where(p => p.CanWrite)
                .Aggregate(Activator.CreateInstance(type), (a, c) =>
                {
                    var column = c.GetCustomAttribute<ColumnAttribute>();
                    var name = column?.Name ?? c.Name;

                    if (item.TryGetValue(name, out var value))
                    {
                        Set(a, c, value);
                    }

                    return a;
                });

            return target;
        }

        private static void Set(
            object target,
            PropertyInfo property,
            AttributeValue fromAttribute)
        {
            if (fromAttribute.NULL)
            {
                return;
            }

            var type = (property?.PropertyType ?? target.GetType()).RealType();

            var setValue = new Action<object>(value =>
            {
                var setter = PropertyAccessor.GetSetter(property);

                setter(target, value);
            });

            if (type.Is<IConvertible>())
            {
                if (type.Is<string>())
                {
                    setValue(fromAttribute.S);
                    return;
                }

                if (type.Is<bool>())
                {
                    setValue(fromAttribute.BOOL);
                    return;
                }

                if (type.Is<DateTime>())
                {
                    var dateTime = DateTime.ParseExact(
                        fromAttribute.S,
                        AWSSDKUtils.ISO8601DateFormat,
                        CultureInfo.InvariantCulture);

                    setValue(dateTime);
                    return;
                }

                if (NumberTypes.Any(t => type.Is(t)))
                {
                    var number = Convert.ChangeType(fromAttribute.N, type,
                        CultureInfo.InvariantCulture);

                    setValue(number);
                    return;
                }

                if (!type.IsEnum)
                {
                    throw new InvalidCastException(
                        $"Unknown convertible type encountered \"{type.FullName}\"!");
                }

                string stringValue = null;

                if (!string.IsNullOrWhiteSpace(fromAttribute.N))
                {
                    stringValue = fromAttribute.N;
                }
                else if (!string.IsNullOrWhiteSpace(fromAttribute.S))
                {
                    stringValue = fromAttribute.S;
                }

                if (stringValue == null)
                {
                    return;
                }

                var enumValue = Enum.Parse(type, stringValue);
                setValue(enumValue);
                return;
            }

            if (type.IsValueType)
            {
                var converter = TypeDescriptor.GetConverter(type);

                if (!converter.CanConvertFrom(typeof(string)))
                {
                    throw new InvalidCastException(
                        $"Unknown value type encountered \"{type.FullName}\"!");
                }

                var stringValue = converter.ConvertTo(fromAttribute.S, type);

                setValue(stringValue);
                return;
            }

            if (type.IsGenericDictionary() && fromAttribute.IsMSet)
            {
                var genericArgs = type.GetGenericArguments();
                var keyType = genericArgs[0];

                if (!keyType.Is<string>())
                {
                    return;
                }

                var valueType = genericArgs[1];

                var dictType =
                    typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
                var dict = (IDictionary)Activator.CreateInstance(dictType);

                if (valueType.IsGenericEnumerable())
                {
                    var valueItemType = valueType.GetGenericArguments().First()
                        .RealType();

                    if (valueItemType.Is<IConvertible>())
                    {
                        if (valueItemType.Is<string>())
                        {
                            foreach (var (key, value) in fromAttribute.M)
                            {
                                dict?.Add(key, value.SS);
                            }
                        }
                    }
                }

                setValue(dict);
                return;
            }

            if (type.IsGenericEnumerable())
            {
                var elementType = type.GetGenericArguments().First().RealType();

                if (elementType.Is<IConvertible>())
                {
                    if (elementType.Is<string>())
                    {
                        var strings = fromAttribute.IsLSet
                            ? fromAttribute.L.Select(x => x.S).ToList()
                            : fromAttribute.SS;

                        setValue(strings);
                        return;
                    }

                    if (NumberTypes.Any(t => elementType.Is(t)))
                    {
                        var vals = fromAttribute.IsLSet
                            ? fromAttribute.L.Select(x => x.N).ToList()
                            : fromAttribute.NS;

                        var numbers = vals
                            .Select(v => Convert.ChangeType(v, elementType,
                                CultureInfo.InvariantCulture))
                            .Cast(elementType);

                        setValue(numbers);
                        return;
                    }

                    if (!elementType.Is<DateTime>())
                    {
                        throw new InvalidCastException(
                            $"Unknown type encountered \"{type.Name}\"!");
                    }

                    var dateTimes = fromAttribute.SS
                        .Select(v =>
                            DateTime.ParseExact(
                                v,
                                AWSSDKUtils.ISO8601DateFormat,
                                CultureInfo.InvariantCulture))
                        .ToList();

                    setValue(dateTimes);
                    return;
                }

                if (elementType.IsValueType)
                {
                    var converter = TypeDescriptor.GetConverter(elementType);

                    if (!converter.CanConvertTo(typeof(string)))
                    {
                        throw new InvalidCastException(
                            $"Unknown enumerable value type encountered \"{elementType.Name}\"!");
                    }

                    var strings = fromAttribute.SS
                        .Select(v => converter.ConvertTo(v, elementType))
                        .ToList();

                    setValue(strings);
                    return;
                }

                if (!elementType.IsClass || !fromAttribute.IsLSet)
                {
                    throw new InvalidCastException(
                        $"Unknown type enumerable encountered \"{elementType.FullName}\"!");
                }

                var objects = fromAttribute.L
                    .Where(a => a.IsMSet)
                    .Select(v => FromAttributes(elementType, v.M))
                    .Cast(elementType);

                setValue(objects);
                return;
            }

            if (!type.IsClass || !fromAttribute.IsMSet)
            {
                throw new InvalidCastException(
                    $"Unknown type encountered \"{type.FullName}\"!");
            }

            var objectValue = FromAttributes(type, fromAttribute.M);

            setValue(objectValue);
        }

        private static AttributeValue Get(
            object fromValue,
            PropertyInfo property)
        {
            if (fromValue == null)
            {
                return null;
            }

            var type =
                (property?.PropertyType ?? fromValue.GetType()).RealType();

            if (type.Is<IConvertible>())
            {
                if (type.Is<string>())
                {
                    var stringValue = (string)fromValue;

                    return string.IsNullOrEmpty(stringValue)
                        ? null
                        : new AttributeValue {S = stringValue};
                }

                if (type.Is<bool>())
                {
                    var booleanValue = (bool)fromValue;

                    return new AttributeValue {BOOL = booleanValue};
                }

                if (type.Is<DateTime>())
                {
                    var dateTime = (DateTime)fromValue;

                    var stringValue = dateTime.ToString(
                        AWSSDKUtils.ISO8601DateFormat,
                        CultureInfo.InvariantCulture);

                    return new AttributeValue {S = stringValue};
                }

                if (NumberTypes.Any(t => type.Is(t)))
                {
                    var number = (string)Convert.ChangeType(fromValue,
                        typeof(string), CultureInfo.InvariantCulture);

                    return new AttributeValue {N = number};
                }

                if (!type.IsEnum)
                {
                    throw new InvalidCastException(
                        $"Unknown convertible type encountered \"{type.Name}\"!");
                }

                var enumType = Enum.GetUnderlyingType(type);
                var enumValue = Convert.ChangeType(fromValue, enumType,
                    CultureInfo.InvariantCulture).ToString();

                return new AttributeValue {N = enumValue};
            }

            if (type.IsValueType)
            {
                var converter = TypeDescriptor.GetConverter(type);

                if (!converter.CanConvertTo(typeof(string)))
                {
                    throw new InvalidCastException(
                        $"Unknown value type encountered \"{type.Name}\"!");
                }

                var stringValue = (string)converter.ConvertTo(fromValue, type);

                return new AttributeValue {S = stringValue};
            }

            if (type.IsGenericDictionary())
            {
                if (!type.GetGenericArguments().First().Is<string>())
                {
                    return null;
                }

                var av = new AttributeValue {
                    M = new Dictionary<string, AttributeValue>()
                };

                foreach (DictionaryEntry entry in (IDictionary)fromValue)
                {
                    av.M.Add((string)entry.Key, Get(entry.Value, null));
                }

                return av;
            }

            if (type.IsGenericEnumerable())
            {
                var elementType = type.GetGenericArguments().First().RealType();

                if (elementType.Is<IConvertible>())
                {
                    if (elementType.Is<string>())
                    {
                        var strings = ((IEnumerable<string>)fromValue)
                            .Where(v => !string.IsNullOrEmpty(v)).ToList();

                        return strings.Any()
                            ? new AttributeValue {SS = strings}
                            : null;
                    }

                    if (NumberTypes.Any(t => elementType.Is(t)))
                    {
                        var numbers = ((IEnumerable)fromValue).Cast<object>()
                            .Where(o => o != null)
                            .Select(v => v.ToString())
                            .ToList();

                        return numbers.Any()
                            ? new AttributeValue {NS = numbers}
                            : null;
                    }

                    if (!elementType.Is<DateTime>())
                    {
                        throw new InvalidCastException(
                            $"Unknown enumerable convertible type encountered \"{elementType.Name}\"!");
                    }

                    var dateTimes = ((IEnumerable)fromValue).Cast<object>()
                        .Where(o => o != null)
                        .Cast<DateTime>()
                        .Select(d => d.ToString(
                            AWSSDKUtils.ISO8601DateFormat,
                            CultureInfo.InvariantCulture))
                        .ToList();

                    return dateTimes.Any()
                        ? new AttributeValue {SS = dateTimes}
                        : null;
                }

                if (elementType.IsValueType)
                {
                    var converter = TypeDescriptor.GetConverter(elementType);

                    if (!converter.CanConvertTo(typeof(string)))
                    {
                        throw new InvalidCastException(
                            $"Unknown enumerable value type encountered \"{elementType.Name}\"!");
                    }

                    var values = ((IEnumerable)fromValue)
                        .Cast<object>()
                        .Where(o => o != null)
                        .Select(v =>
                            converter.ConvertTo(v, typeof(string))?.ToString())
                        .ToList();

                    return values.Any()
                        ? new AttributeValue {SS = values}
                        : null;
                }

                if (!elementType.IsClass)
                {
                    throw new InvalidCastException(
                        $"Unknown type encountered \"{type.Name}\"!");
                }

                var objects = ((IEnumerable)fromValue)
                    .Cast<object>()
                    .Where(o => o != null)
                    .Select(o =>
                        new AttributeValue {M = o.ToDynamoDBAttributes()})
                    .ToList();

                return objects.Any() ? new AttributeValue {L = objects} : null;
            }

            if (type.IsClass)
            {
                return new AttributeValue {
                    M = fromValue.ToDynamoDBAttributes()
                };
            }

            throw new InvalidCastException(
                $"Unknown type encountered \"{type.Name}\"!");
        }
    }
}
