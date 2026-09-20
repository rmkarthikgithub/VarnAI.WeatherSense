using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VarnAI.WeatherSense.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialWeatherSense : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WeatherCollectionExecutions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TriggerSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequestedForecastDays = table.Column<int>(type: "int", nullable: false),
                    LocationsProcessed = table.Column<int>(type: "int", nullable: false),
                    LocationsSucceeded = table.Column<int>(type: "int", nullable: false),
                    LocationsFailed = table.Column<int>(type: "int", nullable: false),
                    RecordsInserted = table.Column<int>(type: "int", nullable: false),
                    RecordsSkipped = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ErrorSummary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherCollectionExecutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeatherLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    Timezone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    District = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProviderLocationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CollectionEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeatherProviderRawResponses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WeatherLocationId = table.Column<int>(type: "int", nullable: false),
                    CollectionExecutionId = table.Column<long>(type: "bigint", nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequestType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequestedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ResponseReceivedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    HttpStatusCode = table.Column<int>(type: "int", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherProviderRawResponses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeatherForecasts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WeatherLocationId = table.Column<int>(type: "int", nullable: false),
                    ForecastGeneratedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ForecastForUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ForecastForLocal = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TemperatureC = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    FeelsLikeTemperatureC = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    RelativeHumidityPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    PrecipitationMm = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    RainMm = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    PrecipitationProbabilityPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    WindSpeedKmh = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    WindDirectionDegrees = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    WindGustKmh = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    CloudCoverPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    PressureHpa = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    SolarRadiationWm2 = table.Column<decimal>(type: "decimal(7,2)", nullable: true),
                    Et0Mm = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    SoilMoisture0To7Cm = table.Column<decimal>(type: "decimal(6,4)", nullable: true),
                    SoilMoisture7To28Cm = table.Column<decimal>(type: "decimal(6,4)", nullable: true),
                    SoilMoisture28To100Cm = table.Column<decimal>(type: "decimal(6,4)", nullable: true),
                    SoilMoisture100To255Cm = table.Column<decimal>(type: "decimal(6,4)", nullable: true),
                    SoilTemperatureC = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    WeatherCode = table.Column<int>(type: "int", nullable: true),
                    WeatherDescription = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProviderModel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherForecasts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeatherForecasts_WeatherLocations_WeatherLocationId",
                        column: x => x.WeatherLocationId,
                        principalTable: "WeatherLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WeatherLocationExecutionDetails",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CollectionExecutionId = table.Column<long>(type: "bigint", nullable: false),
                    WeatherLocationId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RecordsInserted = table.Column<int>(type: "int", nullable: false),
                    RecordsSkipped = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherLocationExecutionDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeatherLocationExecutionDetails_WeatherCollectionExecutions_CollectionExecutionId",
                        column: x => x.CollectionExecutionId,
                        principalTable: "WeatherCollectionExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WeatherLocationExecutionDetails_WeatherLocations_WeatherLocationId",
                        column: x => x.WeatherLocationId,
                        principalTable: "WeatherLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WeatherObservations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WeatherLocationId = table.Column<int>(type: "int", nullable: false),
                    ObservedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ObservedAtLocal = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TemperatureC = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    FeelsLikeTemperatureC = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    RelativeHumidityPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    PrecipitationMm = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    RainMm = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    PrecipitationProbabilityPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    WindSpeedKmh = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    WindDirectionDegrees = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    WindGustKmh = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    CloudCoverPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    PressureHpa = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    SolarRadiationWm2 = table.Column<decimal>(type: "decimal(7,2)", nullable: true),
                    Et0Mm = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    SoilMoisture0To7Cm = table.Column<decimal>(type: "decimal(6,4)", nullable: true),
                    SoilMoisture7To28Cm = table.Column<decimal>(type: "decimal(6,4)", nullable: true),
                    SoilMoisture28To100Cm = table.Column<decimal>(type: "decimal(6,4)", nullable: true),
                    SoilMoisture100To255Cm = table.Column<decimal>(type: "decimal(6,4)", nullable: true),
                    SoilTemperatureC = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    WeatherCode = table.Column<int>(type: "int", nullable: true),
                    WeatherDescription = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProviderRecordId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DataSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherObservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeatherObservations_WeatherLocations_WeatherLocationId",
                        column: x => x.WeatherLocationId,
                        principalTable: "WeatherLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeatherCollectionExecutions_StartedAtUtc",
                table: "WeatherCollectionExecutions",
                column: "StartedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_WeatherCollectionExecutions_Status",
                table: "WeatherCollectionExecutions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WeatherForecasts_Location_ForecastForUtc",
                table: "WeatherForecasts",
                columns: new[] { "WeatherLocationId", "ForecastForUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WeatherForecasts_Location_ForecastGeneratedAtUtc",
                table: "WeatherForecasts",
                columns: new[] { "WeatherLocationId", "ForecastGeneratedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WeatherForecasts_Snapshot_Unique",
                table: "WeatherForecasts",
                columns: new[] { "WeatherLocationId", "Provider", "ForecastGeneratedAtUtc", "ForecastForUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeatherLocationExecutionDetails_Execution_Location",
                table: "WeatherLocationExecutionDetails",
                columns: new[] { "CollectionExecutionId", "WeatherLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_WeatherLocationExecutionDetails_WeatherLocationId",
                table: "WeatherLocationExecutionDetails",
                column: "WeatherLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_WeatherLocations_Code",
                table: "WeatherLocations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeatherLocations_IsActive_CollectionEnabled",
                table: "WeatherLocations",
                columns: new[] { "IsActive", "CollectionEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_WeatherObservations_Location_ObservedAtUtc",
                table: "WeatherObservations",
                columns: new[] { "WeatherLocationId", "ObservedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WeatherObservations_Location_Provider_ObservedAtUtc",
                table: "WeatherObservations",
                columns: new[] { "WeatherLocationId", "Provider", "ObservedAtUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeatherProviderRawResponses_CollectionExecutionId",
                table: "WeatherProviderRawResponses",
                column: "CollectionExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_WeatherProviderRawResponses_Location_RequestedAtUtc",
                table: "WeatherProviderRawResponses",
                columns: new[] { "WeatherLocationId", "RequestedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeatherForecasts");

            migrationBuilder.DropTable(
                name: "WeatherLocationExecutionDetails");

            migrationBuilder.DropTable(
                name: "WeatherObservations");

            migrationBuilder.DropTable(
                name: "WeatherProviderRawResponses");

            migrationBuilder.DropTable(
                name: "WeatherCollectionExecutions");

            migrationBuilder.DropTable(
                name: "WeatherLocations");
        }
    }
}
