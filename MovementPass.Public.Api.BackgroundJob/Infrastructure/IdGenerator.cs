namespace MovementPass.Public.Api.BackgroundJob.Infrastructure
{
    using System;
    using System.Globalization;

    public static class IdGenerator
    {
        public static string Generate()
        {
            var id = Create().ToString("N", CultureInfo.InvariantCulture)
                .ToLowerInvariant();

            return id;
        }

        private static Guid Create()
        {
            var buffer = Guid.NewGuid().ToByteArray();

            var time = new DateTime(0x76c, 1, 1);
            var now = Clock.Now();
            var span = new TimeSpan(now.Ticks - time.Ticks);
            var timeOfDay = now.TimeOfDay;

            var bytes = BitConverter.GetBytes(span.Days);
            var array = BitConverter.GetBytes(
                (long)(timeOfDay.TotalMilliseconds / 3.333333));

            Array.Reverse(bytes);
            Array.Reverse(array);

            Array.Copy(
                bytes,
                bytes.Length - 2,
                buffer,
                buffer.Length - 6,
                2);

            Array.Copy(
                array,
                array.Length - 4,
                buffer,
                buffer.Length - 4,
                4);

            return new Guid(buffer);
        }
    }
}
