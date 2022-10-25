namespace MovementPass.Public.Api.BackgroundJob;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using Infrastructure;

public class ApplyRequest : IValidatableObject
{
    [Required]
    public string Token { get; set; }

    [Required, MaxLength(64)]
    public string FromLocation { get; set; }

    [Required, MaxLength(64)]
    public string ToLocation { get; set; }

    [Required, Range(1001, 1075)]
    public int District { get; set; }

    [Required, Range(10001, 10626)]
    public int Thana { get; set; }

    [Required, DataType(DataType.DateTime)]
    public DateTime DateTime { get; set; }

    [Required, Range(1, 12)]
    public int DurationInHour { get; set; }

    [Required, RegularExpression("^R|O$")]
    public string Type { get; set; }

    [Required, MaxLength(64)]
    public string Reason { get; set; }

    [Required]
    public bool IncludeVehicle { get; set; }

    [MaxLength(64)]
    public string VehicleNo { get; set; }

    public bool SelfDriven { get; set; }

    [MaxLength(64)]
    public string DriverName { get; set; }

    [MaxLength(64)]
    public string DriverLicenseNo { get; set; }

    public string ApplicantId { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        var utc = this.DateTime.ToUniversalTime();
        var now = Clock.Now().ToUniversalTime();

        if (utc < now.AddHours(1) || utc > now.AddDays(1))
        {
            yield return new ValidationResult(
                "Date time must be between today and tomorrow!",
                new[] {nameof(this.DateTime)});
        }

        if (!this.IncludeVehicle)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(this.VehicleNo))
        {
            yield return new ValidationResult(
                "Vehicle no must be specified if vehicle is included!",
                new[] {
                    nameof(this.VehicleNo), nameof(this.IncludeVehicle)
                });
        }

        if (this.SelfDriven)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(this.DriverName))
        {
            yield return new ValidationResult(
                "Driver name must be specified if not self driven!",
                new[] {nameof(this.DriverName), nameof(this.SelfDriven)});
        }

        if (string.IsNullOrWhiteSpace(this.DriverLicenseNo))
        {
            yield return new ValidationResult(
                "Driver license no must be specified if not self driven!",
                new[] {
                    nameof(this.DriverLicenseNo), nameof(this.SelfDriven)
                });
        }
    }
}