namespace MovementPass.Public.Api.BackgroundJob;

using System;
using System.ComponentModel.DataAnnotations.Schema;

public class Pass
{
    [Column("id")]
    public string Id { get; set; }

    [Column("fromLocation")]
    public string FromLocation { get; set; }

    [Column("toLocation")]
    public string ToLocation { get; set; }

    [Column("district")]
    public int District { get; set; }

    [Column("thana")]
    public int Thana { get; set; }

    [Column("startAt")]
    public DateTime StartAt { get; set; }

    [Column("endAt")]
    public DateTime EndAt { get; set; }

    [Column("type")]
    public string Type { get; set; }

    [Column("reason")]
    public string Reason { get; set; }

    [Column("includeVehicle")]
    public bool IncludeVehicle { get; set; }

    [Column("vehicleNo")]
    public string VehicleNo { get; set; }

    [Column("selfDriven")]
    public bool SelfDriven { get; set; }

    [Column("driverName")]
    public string DriverName { get; set; }

    [Column("driverLicenseNo")]
    public string DriverLicenseNo { get; set; }

    [Column("applicantId")]
    public string ApplicantId { get; set; }

    [Column("status")]
    public string Status { get; set; }

    [Column("createdAt")]
    public DateTime CreatedAt { get; set; }
}