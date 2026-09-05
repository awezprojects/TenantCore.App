using System;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenantCore.Infrastructure.Persistence.ClinicMigrations
{
    /// <inheritdoc />
    public partial class AddStatesCitiesAndClinicLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "States",
                schema: "clinic",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_States", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cities",
                schema: "clinic",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cities_States_StateId",
                        column: x => x.StateId,
                        principalSchema: "clinic",
                        principalTable: "States",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClinicLocations",
                schema: "clinic",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicLocations_Cities_CityId",
                        column: x => x.CityId,
                        principalSchema: "clinic",
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClinicLocations_States_StateId",
                        column: x => x.StateId,
                        principalSchema: "clinic",
                        principalTable: "States",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            SeedStatesAndCities(migrationBuilder);

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 10, 23, 27, 258, DateTimeKind.Utc).AddTicks(6219));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 10, 23, 27, 258, DateTimeKind.Utc).AddTicks(6223));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 10, 23, 27, 258, DateTimeKind.Utc).AddTicks(6225));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 10, 23, 27, 258, DateTimeKind.Utc).AddTicks(6234));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 10, 23, 27, 259, DateTimeKind.Utc).AddTicks(2839));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 10, 23, 27, 259, DateTimeKind.Utc).AddTicks(2848));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 10, 23, 27, 259, DateTimeKind.Utc).AddTicks(2860));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 10, 23, 27, 259, DateTimeKind.Utc).AddTicks(2879));

            migrationBuilder.CreateIndex(
                name: "IX_Cities_StateId_Name",
                schema: "clinic",
                table: "Cities",
                columns: new[] { "StateId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicLocations_ApplicationId",
                schema: "clinic",
                table: "ClinicLocations",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicLocations_CityId",
                schema: "clinic",
                table: "ClinicLocations",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicLocations_StateId",
                schema: "clinic",
                table: "ClinicLocations",
                column: "StateId");

            migrationBuilder.CreateIndex(
                name: "IX_States_Code",
                schema: "clinic",
                table: "States",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_States_Name",
                schema: "clinic",
                table: "States",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicLocations",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "Cities",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "States",
                schema: "clinic");

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 9, 45, 32, 850, DateTimeKind.Utc).AddTicks(2645));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 9, 45, 32, 850, DateTimeKind.Utc).AddTicks(2647));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 9, 45, 32, 850, DateTimeKind.Utc).AddTicks(2650));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 9, 45, 32, 850, DateTimeKind.Utc).AddTicks(2651));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 9, 45, 32, 850, DateTimeKind.Utc).AddTicks(6865));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 9, 45, 32, 850, DateTimeKind.Utc).AddTicks(6870));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 9, 45, 32, 850, DateTimeKind.Utc).AddTicks(6872));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 9, 45, 32, 850, DateTimeKind.Utc).AddTicks(6874));
        }

        // Major Indian states (small North-Eastern states other than West Bengal skipped,
        // per product decision) with their major cities. Global reference data — every
        // clinic can select from these; not tenant-scoped.
        private static readonly (string Name, string Code, string[] Cities)[] StatesAndCities =
        [
            ("Maharashtra", "MH", ["Mumbai", "Pune", "Nagpur", "Nashik", "Aurangabad", "Solapur", "Kolhapur", "Amravati", "Thane", "Navi Mumbai", "Akola", "Latur"]),
            ("Gujarat", "GJ", ["Ahmedabad", "Surat", "Vadodara", "Rajkot", "Bhavnagar", "Jamnagar", "Gandhinagar", "Junagadh", "Anand", "Nadiad"]),
            ("Rajasthan", "RJ", ["Jaipur", "Jodhpur", "Udaipur", "Kota", "Ajmer", "Bikaner", "Bharatpur", "Alwar", "Sikar", "Bhilwara"]),
            ("Madhya Pradesh", "MP", ["Bhopal", "Indore", "Gwalior", "Jabalpur", "Ujjain", "Sagar", "Dewas", "Satna", "Ratlam", "Rewa"]),
            ("Uttar Pradesh", "UP", ["Lucknow", "Kanpur", "Varanasi", "Agra", "Prayagraj", "Meerut", "Ghaziabad", "Noida", "Bareilly", "Aligarh", "Moradabad", "Gorakhpur"]),
            ("Bihar", "BR", ["Patna", "Gaya", "Bhagalpur", "Muzaffarpur", "Darbhanga", "Purnia", "Arrah", "Begusarai", "Chapra", "Katihar"]),
            ("West Bengal", "WB", ["Kolkata", "Howrah", "Durgapur", "Asansol", "Siliguri", "Bardhaman", "Malda", "Kharagpur", "Haldia", "Berhampore"]),
            ("Odisha", "OD", ["Bhubaneswar", "Cuttack", "Rourkela", "Berhampur", "Sambalpur", "Puri", "Balasore", "Bhadrak"]),
            ("Jharkhand", "JH", ["Ranchi", "Jamshedpur", "Dhanbad", "Bokaro", "Deoghar", "Hazaribagh"]),
            ("Chhattisgarh", "CG", ["Raipur", "Bhilai", "Bilaspur", "Korba", "Durg", "Rajnandgaon"]),
            ("Punjab", "PB", ["Ludhiana", "Amritsar", "Jalandhar", "Patiala", "Bathinda", "Mohali", "Pathankot"]),
            ("Haryana", "HR", ["Gurugram", "Faridabad", "Panipat", "Ambala", "Karnal", "Hisar", "Rohtak", "Yamunanagar"]),
            ("Himachal Pradesh", "HP", ["Shimla", "Manali", "Dharamshala", "Solan", "Mandi", "Kullu"]),
            ("Uttarakhand", "UK", ["Dehradun", "Haridwar", "Nainital", "Roorkee", "Haldwani", "Rishikesh"]),
            ("Delhi", "DL", ["New Delhi", "Dwarka", "Rohini", "Karol Bagh", "Saket", "Janakpuri", "Pitampura"]),
            ("Karnataka", "KA", ["Bengaluru", "Mysuru", "Hubballi", "Mangaluru", "Belagavi", "Davanagere", "Ballari", "Shivamogga", "Tumakuru"]),
            ("Tamil Nadu", "TN", ["Chennai", "Coimbatore", "Madurai", "Tiruchirappalli", "Salem", "Tirunelveli", "Erode", "Vellore", "Thoothukudi", "Thanjavur"]),
            ("Andhra Pradesh", "AP", ["Visakhapatnam", "Vijayawada", "Guntur", "Nellore", "Kurnool", "Rajahmundry", "Tirupati", "Kadapa", "Anantapur"]),
            ("Telangana", "TG", ["Hyderabad", "Warangal", "Nizamabad", "Karimnagar", "Khammam", "Ramagundam"]),
            ("Kerala", "KL", ["Thiruvananthapuram", "Kochi", "Kozhikode", "Thrissur", "Kollam", "Kannur", "Alappuzha", "Palakkad", "Malappuram"]),
            ("Goa", "GA", ["Panaji", "Margao", "Vasco da Gama", "Mapusa", "Ponda"]),
        ];

        private static void SeedStatesAndCities(MigrationBuilder migrationBuilder)
        {
            var seedTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var stateRows = new object[StatesAndCities.Length, 4];
            for (var i = 0; i < StatesAndCities.Length; i++)
            {
                var (name, code, _) = StatesAndCities[i];
                stateRows[i, 0] = DeterministicGuid($"STATE:{code}");
                stateRows[i, 1] = name;
                stateRows[i, 2] = code;
                stateRows[i, 3] = seedTime;
            }
            migrationBuilder.InsertData(
                schema: "clinic",
                table: "States",
                columns: new[] { "Id", "Name", "Code", "CreatedAt" },
                values: stateRows);

            var totalCities = StatesAndCities.Sum(s => s.Cities.Length);
            var cityRows = new object[totalCities, 4];
            var row = 0;
            foreach (var (_, code, cities) in StatesAndCities)
            {
                var stateId = DeterministicGuid($"STATE:{code}");
                foreach (var city in cities)
                {
                    cityRows[row, 0] = DeterministicGuid($"CITY:{code}:{city}");
                    cityRows[row, 1] = stateId;
                    cityRows[row, 2] = city;
                    cityRows[row, 3] = seedTime;
                    row++;
                }
            }
            migrationBuilder.InsertData(
                schema: "clinic",
                table: "Cities",
                columns: new[] { "Id", "StateId", "Name", "CreatedAt" },
                values: cityRows);
        }

        private static Guid DeterministicGuid(string input)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return new Guid(hash);
        }
    }
}
