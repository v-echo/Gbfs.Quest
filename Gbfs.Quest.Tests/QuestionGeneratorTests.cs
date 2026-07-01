using Gbfs.Quest.Data;
using Gbfs.Quest.Models;
using static Gbfs.Quest.Data.QuestionTemplates;

namespace Gbfs.Quest.Tests
{
    public class QuestionGeneratorTests
    {
        readonly StationInfo[] StationInfoTestData;
        readonly StationStatus[] StationStatusTestData;

        public QuestionGeneratorTests()
        {
            StationInfoTestData = [
                new StationInfo() 
                {
                    Address = "Freddie Street 32",
                    Capacity = 100,
                    Lat = 11.23445,
                    Lon = 42.12345,
                    PostCode = "1234 ZZ",
                    Name = "Jackson Station",
                    ShortName = "Jackson's",
                    RentalMethods = ["KEY", "CREDITCARD"],
                    StationId = new Guid().ToString(),
                    IsVirtualStation = false,
                    RegionId = "700"
                }
            ];

            StationStatusTestData = [
                new StationStatus()
                {
                    StationId = StationInfoTestData[0].StationId,
                    IsInstalled = true,
                    IsRenting = true,
                    IsReturning = false,
                    NumBikesAvailable = 10,
                    NumDocksAvailable = 0
                }
            ];
        }

        [Fact]
        public void StationInfo_Questions_Generated()
        {
            var questions = QuestionGenerator.Generate(StationInfoTestData, TestContext.Current.CancellationToken);

            Assert.NotNull(questions);
            Assert.True(questions.Count > 0);

            var q1 = questions.Keys.FirstOrDefault(q => q.StartsWith(string.Format(StationCapacity, StationInfoTestData[0].ShortName)));
            var q2 = questions.Keys.FirstOrDefault(q => q.StartsWith(string.Format(StationRentalTypes, StationInfoTestData[0].ShortName)));
            var q3 = questions.Keys.FirstOrDefault(q => q.StartsWith(string.Format(StationPostCode, StationInfoTestData[0].Address)));
            var q4 = questions.Keys.FirstOrDefault(q => q.StartsWith(string.Format(StationVirtual, StationInfoTestData[0].Name)));
            var q5 = questions.Keys.FirstOrDefault(q => q.StartsWith(string.Format(StationRegion, StationInfoTestData[0].Name)));

            Assert.NotNull(q1);
            Assert.NotNull(q2);
            Assert.NotNull(q3);
            Assert.NotNull(q4);
            Assert.NotNull(q5);

            Assert.NotEmpty(questions[q1]);
            Assert.NotEmpty(questions[q2]);
            Assert.NotEmpty(questions[q3]);
            Assert.NotEmpty(questions[q4]);
            Assert.NotEmpty(questions[q5]);
        }

        [Fact]
        public void StationStatus_Questions_Generated()
        {
            _ = QuestionGenerator.Generate(StationInfoTestData, TestContext.Current.CancellationToken);
            var questions = QuestionGenerator.Generate(StationStatusTestData, TestContext.Current.CancellationToken);

            Assert.NotNull(questions);
            Assert.True(questions.Count > 0);

            var q1 = questions.Keys.FirstOrDefault(q => q.StartsWith(string.Format(StationBikes, StationInfoTestData[0].Name)));
            var q2 = questions.Keys.FirstOrDefault(q => q.StartsWith(string.Format(StationRenting, StationInfoTestData[0].Name)));

            Assert.NotNull(q1);
            Assert.NotNull(q2);

            Assert.NotEmpty(questions[q1]);
            Assert.NotEmpty(questions[q2]);
        }
    }
}
