namespace MovementPass.Public.Api.BackgroundJob.Infrastructure
{
    using System.Text.Json;

    public interface IRecordDeserializer
    {
        T Deserialize<T>(string payload);
    }

    public class RecordDeserializer : IRecordDeserializer
    {
        private static readonly JsonSerializerOptions Settings =
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

        public T Deserialize<T>(string payload) =>
            JsonSerializer.Deserialize<T>(payload, Settings);
    }
}
