using System;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenantCore.Infrastructure.Persistence.ClinicMigrations
{
    /// <inheritdoc />
    public partial class AddMoreIndianCities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            SeedAdditionalCities(migrationBuilder);

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 10, 33, 39, 531, DateTimeKind.Utc).AddTicks(8728));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 10, 33, 39, 531, DateTimeKind.Utc).AddTicks(8735));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 10, 33, 39, 531, DateTimeKind.Utc).AddTicks(8739));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionAlertSettings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 10, 33, 39, 531, DateTimeKind.Utc).AddTicks(8777));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0001-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 10, 33, 39, 534, DateTimeKind.Utc).AddTicks(5160));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0002-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 10, 33, 39, 534, DateTimeKind.Utc).AddTicks(5175));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0003-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 10, 33, 39, 534, DateTimeKind.Utc).AddTicks(5199));

            migrationBuilder.UpdateData(
                schema: "clinic",
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-0004-0000-0000-000000000000"),
                column: "CreatedAt",
                value: new DateTime(2026, 9, 5, 10, 33, 39, 534, DateTimeKind.Utc).AddTicks(5203));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var (_, code, cities) in NewCitiesByState)
            foreach (var city in cities)
                migrationBuilder.Sql(
                    $"DELETE FROM [clinic].[Cities] WHERE [Id] = '{DeterministicGuid($"CITY:{code}:{city}"):D}'");

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
        }

        // District-level cities/towns not already seeded by AddStatesCitiesAndClinicLocation —
        // brings each of the 21 states up to (near) full district coverage. State codes and the
        // DeterministicGuid formula must match that earlier migration exactly, since these rows
        // reference the existing State rows via StateId.
        private static readonly (string StateName, string StateCode, string[] Cities)[] NewCitiesByState =
        [
            ("Maharashtra", "MH", ["Mumbai Suburban", "Palghar", "Raigad", "Ratnagiri", "Sindhudurg", "Satara", "Sangli", "Ahmednagar", "Dhule", "Nandurbar", "Jalgaon", "Jalna", "Beed", "Osmanabad", "Nanded", "Parbhani", "Hingoli", "Washim", "Buldhana", "Yavatmal", "Wardha", "Chandrapur", "Gadchiroli", "Bhandara", "Gondia"]),
            ("Gujarat", "GJ", ["Mehsana", "Patan", "Palanpur", "Himatnagar", "Modasa", "Bhuj", "Surendranagar", "Morbi", "Botad", "Amreli", "Porbandar", "Khambhalia", "Veraval", "Navsari", "Valsad", "Ahwa", "Vyara", "Rajpipla", "Bharuch", "Chhota Udaipur", "Godhra", "Dahod", "Lunawada"]),
            ("Rajasthan", "RJ", ["Sri Ganganagar", "Hanumangarh", "Churu", "Jhunjhunu", "Nagaur", "Pali", "Sirohi", "Jalore", "Barmer", "Jaisalmer", "Dungarpur", "Banswara", "Chittorgarh", "Rajsamand", "Bundi", "Baran", "Jhalawar", "Karauli", "Dholpur", "Dausa", "Tonk", "Sawai Madhopur", "Pratapgarh"]),
            ("Madhya Pradesh", "MP", ["Katni", "Singrauli", "Burhanpur", "Khandwa", "Khargone", "Barwani", "Dhar", "Mandsaur", "Neemuch", "Shajapur", "Rajgarh", "Vidisha", "Raisen", "Sehore", "Betul", "Harda", "Narmadapuram", "Chhindwara", "Seoni", "Balaghat", "Mandla", "Dindori", "Anuppur", "Shahdol", "Umaria", "Sidhi", "Panna", "Chhatarpur", "Tikamgarh", "Damoh", "Niwari", "Ashoknagar", "Guna", "Shivpuri", "Datia", "Morena", "Bhind", "Sheopur", "Alirajpur", "Jhabua", "Agar Malwa"]),
            ("Uttar Pradesh", "UP", ["Saharanpur", "Firozabad", "Jhansi", "Muzaffarnagar", "Mathura", "Rampur", "Shahjahanpur", "Farrukhabad", "Ayodhya", "Sitapur", "Hardoi", "Unnao", "Raebareli", "Sultanpur", "Pratapgarh", "Jaunpur", "Ghazipur", "Ballia", "Deoria", "Kushinagar", "Maharajganj", "Basti", "Siddharthnagar", "Sant Kabir Nagar", "Azamgarh", "Mau", "Mirzapur", "Sonbhadra", "Chandauli", "Bhadohi", "Etawah", "Auraiya", "Kannauj", "Etah", "Kasganj", "Mainpuri", "Budaun", "Pilibhit", "Lakhimpur Kheri", "Bahraich", "Shravasti", "Balrampur", "Gonda", "Barabanki", "Amethi", "Bijnor", "Amroha", "Sambhal", "Hapur", "Baghpat", "Shamli", "Hathras", "Kaushambi", "Fatehpur", "Banda", "Chitrakoot", "Hamirpur", "Mahoba", "Jalaun"]),
            ("Bihar", "BR", ["Munger", "Bihar Sharif", "Sasaram", "Aurangabad", "Bhabua", "Buxar", "Siwan", "Gopalganj", "Bettiah", "Motihari", "Sheohar", "Sitamarhi", "Madhubani", "Supaul", "Araria", "Kishanganj", "Madhepura", "Saharsa", "Khagaria", "Samastipur", "Vaishali", "Jamui", "Lakhisarai", "Sheikhpura", "Nawada", "Jehanabad", "Arwal", "Banka"]),
            ("West Bengal", "WB", ["Krishnanagar", "Barasat", "Diamond Harbour", "Midnapore", "Tamluk", "Bankura", "Purulia", "Suri", "Cooch Behar", "Jalpaiguri", "Alipurduar", "Darjeeling", "Kalimpong"]),
            ("Odisha", "OD", ["Baripada", "Angul", "Dhenkanal", "Jharsuguda", "Bargarh", "Bhawanipatna", "Koraput", "Malkangiri", "Nabarangpur", "Nuapada", "Rayagada", "Subarnapur", "Boudh", "Phulbani", "Paralakhemundi", "Ganjam", "Jajpur", "Kendrapara", "Keonjhar", "Nayagarh", "Khordha"]),
            ("Jharkhand", "JH", ["Giridih", "Ramgarh", "Dumka", "Godda", "Sahibganj", "Pakur", "Jamtara", "Chatra", "Koderma", "Latehar", "Lohardaga", "Gumla", "Simdega", "Khunti", "Daltonganj", "Garhwa", "Saraikela", "Chaibasa"]),
            ("Chhattisgarh", "CG", ["Jagdalpur", "Ambikapur", "Dhamtari", "Mahasamund", "Kanker", "Kondagaon", "Narayanpur", "Bijapur", "Dantewada", "Sukma", "Balod", "Bemetara", "Baloda Bazar", "Gariaband", "Kawardha", "Mungeli", "Janjgir", "Raigarh", "Jashpur", "Baikunthpur", "Surajpur", "Balrampur (CG)", "Gaurela"]),
            ("Punjab", "PB", ["Hoshiarpur", "Moga", "Firozpur", "Faridkot", "Muktsar", "Mansa", "Sangrur", "Barnala", "Fazilka", "Kapurthala", "Gurdaspur", "Rupnagar", "Nawanshahr", "Tarn Taran", "Malerkotla", "Fatehgarh Sahib"]),
            ("Haryana", "HR", ["Panchkula", "Sonipat", "Sirsa", "Bhiwani", "Jind", "Kaithal", "Kurukshetra", "Rewari", "Jhajjar", "Mahendragarh", "Palwal", "Fatehabad", "Charkhi Dadri", "Nuh"]),
            ("Himachal Pradesh", "HP", ["Bilaspur (HP)", "Chamba", "Hamirpur", "Una", "Nahan", "Reckong Peo", "Keylong"]),
            ("Uttarakhand", "UK", ["Almora", "Pithoragarh", "Bageshwar", "Champawat", "Rudrapur", "New Tehri", "Pauri", "Gopeshwar", "Uttarkashi", "Rudraprayag"]),
            ("Delhi", "DL", ["North Delhi", "South Delhi", "East Delhi", "West Delhi", "North East Delhi", "North West Delhi", "South East Delhi", "South West Delhi", "Central Delhi", "Shahdara"]),
            ("Karnataka", "KA", ["Bengaluru Rural", "Kalaburagi", "Bidar", "Vijayapura", "Bagalkot", "Raichur", "Koppal", "Yadgir", "Chitradurga", "Chikkamagaluru", "Hassan", "Madikeri", "Mandya", "Ramanagara", "Kolar", "Chikkaballapur", "Udupi", "Karwar", "Dakshina Kannada", "Chamarajanagar", "Haveri", "Gadag"]),
            ("Tamil Nadu", "TN", ["Dindigul", "Kanchipuram", "Cuddalore", "Tiruvannamalai", "Vilupuram", "Namakkal", "Karur", "Krishnagiri", "Dharmapuri", "Ariyalur", "Perambalur", "Pudukkottai", "Ramanathapuram", "Sivaganga", "Virudhunagar", "Theni", "Nagapattinam", "Tiruvarur", "Ooty", "Nagercoil", "Tenkasi", "Tirupattur", "Ranipet", "Chengalpattu", "Kallakurichi", "Mayiladuthurai"]),
            ("Andhra Pradesh", "AP", ["Chittoor", "Eluru", "Machilipatnam", "Ongole", "Srikakulam", "Vizianagaram", "Anakapalli", "Kakinada", "Amaravati", "Bapatla", "Palnadu", "Prakasam", "Nandyal", "Annamayya", "Alluri Sitharama Raju", "Konaseema", "Parvathipuram Manyam", "Sri Sathya Sai"]),
            ("Telangana", "TG", ["Mahbubnagar", "Nalgonda", "Adilabad", "Siddipet", "Suryapet", "Miryalaguda", "Jagtial", "Mancherial", "Nirmal", "Kamareddy", "Bhuvanagiri", "Sangareddy", "Medak", "Vikarabad", "Wanaparthy", "Nagarkurnool", "Gadwal", "Mahabubabad", "Jangaon", "Peddapalli", "Sircilla", "Kothagudem", "Bhupalpally", "Asifabad", "Narayanpet"]),
            ("Kerala", "KL", ["Kottayam", "Idukki", "Wayanad", "Pathanamthitta", "Kasaragod"]),
            ("Goa", "GA", ["Bicholim", "Curchorem", "Sanquelim", "Valpoi", "Canacona", "Pernem", "Quepem", "Sanguem"]),
        ];

        private static void SeedAdditionalCities(MigrationBuilder migrationBuilder)
        {
            var seedTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var totalCities = NewCitiesByState.Sum(s => s.Cities.Length);
            var rows = new object[totalCities, 4];
            var row = 0;
            foreach (var (_, code, cities) in NewCitiesByState)
            {
                var stateId = DeterministicGuid($"STATE:{code}");
                foreach (var city in cities)
                {
                    rows[row, 0] = DeterministicGuid($"CITY:{code}:{city}");
                    rows[row, 1] = stateId;
                    rows[row, 2] = city;
                    rows[row, 3] = seedTime;
                    row++;
                }
            }

            migrationBuilder.InsertData(
                schema: "clinic",
                table: "Cities",
                columns: new[] { "Id", "StateId", "Name", "CreatedAt" },
                values: rows);
        }

        private static Guid DeterministicGuid(string input)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return new Guid(hash);
        }
    }
}
