namespace MovementPass.Public.Api.BackgroundJob.Services;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using Microsoft.Extensions.Logging;

using Amazon.Lambda.KinesisEvents;

using ExtensionMethods;
using Infrastructure;

public interface IDataReducer
{
    IEnumerable<Pass> Reduce(
        IEnumerable<KinesisEvent.KinesisEventRecord> records);
}

public class DataReducer : IDataReducer
{
    private readonly IRecordDeserializer _deserializer;
    private readonly ITokenValidator _tokenValidator;
    private readonly ILogger<DataReducer> _logger;

    public DataReducer(
        IRecordDeserializer deserializer,
        ITokenValidator tokenValidator,
        ILogger<DataReducer> logger)
    {
        this._deserializer = deserializer ??
                             throw new ArgumentNullException(
                                 nameof(deserializer));

        this._tokenValidator = tokenValidator ??
                               throw new ArgumentNullException(
                                   nameof(tokenValidator));

        this._logger = logger ??
                       throw new ArgumentNullException(nameof(logger));
    }

    public IEnumerable<Pass> Reduce(
        IEnumerable<KinesisEvent.KinesisEventRecord> records)
    {
        if (records == null)
        {
            throw new ArgumentNullException(nameof(records));
        }

        var deserializedRecords = records
            .Select(record =>
                this._deserializer
                    .Deserialize<ApplyRequest>(record.Kinesis)).ToList();

        this._logger.LogInformation(
            "Deserialized records: {@DeserializedCount}",
            deserializedRecords.Count);

        var inputValidatedRecords = deserializedRecords
            .Where(request =>
                Validator.TryValidateObject(
                    request,
                    new ValidationContext(request),
                    new List<ValidationResult>()))
            .ToList();

        this._logger.LogInformation(
            "Input validated records: {@InputValidatedCount}",
            inputValidatedRecords.Count);

        var validTokenRecords = inputValidatedRecords
            .Where(request =>
            {
                var userId = this._tokenValidator.Validate(request.Token);

                if (string.IsNullOrEmpty(userId))
                {
                    return false;
                }

                request.ApplicantId = userId;

                return true;
            }).ToList();

        this._logger.LogInformation(
            "Validated token records: {@validTokenCount}",
            validTokenRecords.Count);

        var transformedRecords = validTokenRecords
            .Select(request =>
            {
                var pass = new Pass {
                    Id = IdGenerator.Generate(),
                    StartAt = request.DateTime,
                    EndAt =
                        request.DateTime.AddHours(request.DurationInHour),
                    CreatedAt = Clock.Now(),
                    Status = "APPLIED"
                }.Merge(request);

                if (!pass.IncludeVehicle)
                {
                    pass.VehicleNo = null;
                    pass.SelfDriven = false;
                    pass.DriverName = null;
                    pass.DriverLicenseNo = null;
                }

                if (pass.SelfDriven)
                {
                    pass.DriverName = null;
                    pass.DriverLicenseNo = null;
                }

                return pass;
            }).ToList();

        this._logger.LogInformation(
            "Transformed records: {@transformedCount}",
            transformedRecords.Count);

        if (deserializedRecords.Count != inputValidatedRecords.Count ||
            inputValidatedRecords.Count != validTokenRecords.Count ||
            validTokenRecords.Count != transformedRecords.Count)
        {
            this._logger.LogWarning(
                "Deserialized: {@deserialized}, valid: {@valid}, token: {@token}, transformed: {@transformed}",
                deserializedRecords.Count,
                inputValidatedRecords.Count,
                validTokenRecords.Count,
                transformedRecords.Count);
        }

        return transformedRecords;
    }
}