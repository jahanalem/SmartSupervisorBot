using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SmartSupervisorBot.Core;
using SmartSupervisorBot.Core.Settings;
using SmartSupervisorBot.DataAccess;
using SmartSupervisorBot.Model;
using SmartSupervisorBot.TextProcessing;

namespace SmartSupervisorBot.Test.Core
{
    public class BotServiceTests
    {
        [Fact]
        public void StartReceivingMessages_CallsStartReceiving()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<BotService>>();
            mockLogger.Setup(log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Starting message reception...")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>())
            ).Verifiable();

            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            var mockGroupAccess = new Mock<IGroupAccess>();
            var mockTextProcessingService = new Mock<ITextProcessingService>();
            var mockOptions = new Mock<IOptions<BotConfigurationOptions>>();
            var mockBotConfig = new BotConfigurationOptions
            {
                BotSettings = new BotSettings { BotToken = "token", OpenAiToken = "token" },
                AllowedUpdatesSettings = new AllowedUpdatesSettings { AllowedUpdates = new[] { "message" } }
            };
            mockOptions.Setup(o => o.Value).Returns(mockBotConfig);

            var service = new BotService(
                mockOptions.Object,
                mockHttpClientFactory.Object,
                mockGroupAccess.Object,
                mockLogger.Object,
                mockTextProcessingService.Object);

            // Act
            service.StartReceivingMessages();

            // Assert
            mockLogger.Verify();
        }

        private BotService CreateBotService(IGroupAccess groupAccess)
        {
            var mockLogger = new Mock<ILogger<BotService>>();
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            var mockTextProcessingService = new Mock<ITextProcessingService>();
            var mockOptions = new Mock<IOptions<BotConfigurationOptions>>();
            var config = new BotConfigurationOptions { BotSettings = new BotSettings { BotToken = "token", OpenAiToken = "token" } };
            mockOptions.Setup(o => o.Value).Returns(config);

            return new BotService(mockOptions.Object, mockHttpClientFactory.Object, groupAccess, mockLogger.Object, mockTextProcessingService.Object);
        }

        #region AddGroup Tests

        [Fact]
        public async Task AddGroup_CallsAddGroupAsync_WithCorrectParameters()
        {
            // Arrange
            var mockGroupAccess = new Mock<IGroupAccess>();
            var service = CreateBotService(mockGroupAccess.Object);

            var groupId = 123456789;
            var groupInfo = new GroupInfo
            {
                GroupName = "TestGroup",
                Language = "Englisch"
            };


            // Act
            await service.AddGroup(groupId, groupInfo);

            // Assert
            mockGroupAccess.Verify(g => g.AddGroupAsync(groupId, groupInfo), Times.Once);
        }

        [Fact]
        public async Task AddGroup_CallsAddGroupAsync_WithCorrectParameters_AndHandlesSuccess()
        {
            // Arrange
            var mockGroupAccess = new Mock<IGroupAccess>();
            var service = CreateBotService(groupAccess: mockGroupAccess.Object);

            var groupId = -1234567890;
            var language = "English";
            var groupInfo = new GroupInfo
            {
                GroupName = "Test",
                CreatedDate = DateTime.Now,
                IsActive = false,
                Language = "Englisch"
            };
            mockGroupAccess.Setup(x => x.AddGroupAsync(groupId, groupInfo))
                           .ReturnsAsync(true);  // Simulating successful addition

            // Act
            await service.AddGroup(groupId, groupInfo);

            // Assert
            mockGroupAccess.Verify(x => x.AddGroupAsync(groupId, groupInfo), Times.Once);
        }

        public static IEnumerable<object[]> InvalidGroupData()
        {
            yield return new object[]
            {
                0, // Invalid groupId
                new GroupInfo
                {
                    Language = "Englisch",
                    IsActive = false,
                    GroupName = "Test",
                    CreatedDate = DateTime.Now
                }
            };
            // Second test case with a null GroupInfo
            yield return new object[]
            {
                -1002084612207, // Valid groupId but the GroupInfo is null
                null
            };
            // Third test case with invalid language
            yield return new object[]
            {
                -1002084612207, // Valid groupId with incorrect language setting
                new GroupInfo
                {
                    Language = "", // Invalid empty language
                    IsActive = true,
                    GroupName = "AnotherTest",
                    CreatedDate = DateTime.Now
                }
            };
            // Fourth test case with an empty group name
            yield return new object[]
            {
                -1002084612207, // Another valid groupId but the group name is empty
                new GroupInfo
                {
                    Language = "Deutsch",
                    IsActive = true,
                    GroupName = "", // Invalid empty group name
                    CreatedDate = DateTime.Now
                }
            };
        }

        [Theory]
        [MemberData(nameof(InvalidGroupData))]
        public async Task AddGroup_InvalidInputs_ThrowsArgumentException(long groupId, GroupInfo groupInfo)
        {
            // Arrange
            var mockGroupAccess = new Mock<IGroupAccess>();
            var service = CreateBotService(groupAccess: mockGroupAccess.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.AddGroup(groupId, groupInfo));
        }

        [Fact]
        public async Task AddGroup_ExtremelyLongGroupName_ThrowsArgumentException()
        {
            // Arrange
            var mockGroupAccess = new Mock<IGroupAccess>();
            var groupId = -1234567890;
            var longGroupName = new string('a', 256); // Assuming the limit is 255 characters
            var groupInfo = new GroupInfo
            {
                GroupName = longGroupName,
                CreatedDate = DateTime.Now,
                IsActive = true,
                Language = "Englisch"
            };
            var service = CreateBotService(groupAccess: mockGroupAccess.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.AddGroup(groupId, groupInfo));
        }

        [Fact]
        public async Task AddGroup_HandlesConcurrentCalls_Gracefully()
        {
            // Arrange
            var mockGroupAccess = new Mock<IGroupAccess>();
            var service = CreateBotService(groupAccess: mockGroupAccess.Object);
            var tasks = new List<Task>();
            int numberOfConcurrentCalls = 100;

            // Setup mock to handle any group name and language correctly
            mockGroupAccess.Setup(g => g.AddGroupAsync(It.IsAny<long>(), It.IsAny<GroupInfo>()))
                           .ReturnsAsync(true);

            // Act
            for (int i = 0; i < numberOfConcurrentCalls; i++)
            {
                long groupId = -9999999999 + i;
                var groupInfo = new GroupInfo
                {
                    GroupName = $"GroupName-{i}",
                };
                tasks.Add(service.AddGroup(groupId, groupInfo));
            }

            await Task.WhenAll(tasks);

            // Assert
            mockGroupAccess.Verify(g => g.AddGroupAsync(It.IsAny<long>(), It.IsAny<GroupInfo>()), Times.Exactly(numberOfConcurrentCalls));
        }

        #endregion

        #region DeleteGroup Tests

        [Fact]
        public async Task DeleteGroup_CallsRemoveGroupAsync_WithCorrectParameters()
        {
            // Arrange
            var mockGroupAccess = new Mock<IGroupAccess>();
            var service = CreateBotService(mockGroupAccess.Object);

            var groupId = "-1234567890";

            // Act
            await service.DeleteGroup(groupId);

            // Assert
            mockGroupAccess.Verify(g => g.RemoveGroupAsync(groupId), Times.Once);
        }

        [Fact]
        public async Task DeleteGroup_SuccessfullyDeletesGroup()
        {
            // Arrange
            var mockGroupAccess = new Mock<IGroupAccess>();
            mockGroupAccess.Setup(g => g.RemoveGroupAsync(It.IsAny<string>())).ReturnsAsync(true);
            var service = CreateBotService(mockGroupAccess.Object);

            // Act
            var result = await service.DeleteGroup("-1234567890");

            // Assert
            Assert.True(result);
            mockGroupAccess.Verify(g => g.RemoveGroupAsync("-1234567890"), Times.Once);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task DeleteGroup_InvalidInputs_ThrowsArgumentException(string invalidGroupId)
        {
            var mockGroupAccess = new Mock<IGroupAccess>();

            var service = CreateBotService(mockGroupAccess.Object);

            await Assert.ThrowsAsync<ArgumentException>(() => service.DeleteGroup(invalidGroupId));
        }

        [Fact]
        public async Task DeleteGroup_ConcurrencyHandling()
        {
            var mockGroupAccess = new Mock<IGroupAccess>();
            mockGroupAccess.Setup(g => g.RemoveGroupAsync(It.IsAny<string>())).ReturnsAsync(true);
            var service = CreateBotService(groupAccess: mockGroupAccess.Object);
            var tasks = new List<Task<bool>>();
            int numberOfConcurrentCalls = 100;

            for (int i = 100; i < (numberOfConcurrentCalls + 100); i++)
            {
                tasks.Add(service.DeleteGroup($"-1234567{i}"));
            }

            var results = await Task.WhenAll(tasks);

            foreach (var result in results)
            {
                Assert.True(result);
            }
            mockGroupAccess.Verify(g => g.RemoveGroupAsync(It.IsAny<string>()), Times.Exactly(numberOfConcurrentCalls));
        }

        #endregion

        #region EditLanguage Tests

        [Fact]
        public async Task EditLanguage_WithCorrectParameters_CallsSetGroupLanguageAsync()
        {
            var mockGroupAccess = new Mock<IGroupAccess>();
            var service = CreateBotService(groupAccess: mockGroupAccess.Object);

            string groupId = "-1234567890";
            string language = "Englisch";
            await service.EditLanguage(groupId, language);

            mockGroupAccess.Verify(g => g.SetGroupLanguageAsync(groupId, language), Times.Once);
        }

        [Fact]
        public async Task EditLanguage_SuccessfullyEditsLanguage()
        {
            var mockGroupAccess = new Mock<IGroupAccess>();
            mockGroupAccess.Setup(g => g.SetGroupLanguageAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            var service = CreateBotService(mockGroupAccess.Object);

            var result = await service.EditLanguage("-1234567890", "Deutsch");

            Assert.True(result);
        }

        [Theory]
        [InlineData("", "Englisch")]
        [InlineData("-1234567890", "")]
        [InlineData(null, "English")]
        [InlineData("-1234567890", null)]
        public async Task EditLanguage_InvalidInputs_ThrowsArgumentException(string groupId, string language)
        {
            var mockGroupAccess = new Mock<IGroupAccess>();
            var service = CreateBotService(mockGroupAccess.Object);

            await Assert.ThrowsAsync<ArgumentException>(() => service.EditLanguage(groupId, language));
        }

        [Fact]
        public async Task EditLanguage_ConcurrencyHandling()
        {
            var mockGroupAccess = new Mock<IGroupAccess>();
            mockGroupAccess.Setup(g => g.SetGroupLanguageAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            var service = CreateBotService(groupAccess: mockGroupAccess.Object);
            var tasks = new List<Task>();
            int numberOfConcurrentCalls = 100;

            for (int i = 100; i < (numberOfConcurrentCalls + 100); i++)
            {
                tasks.Add(service.EditLanguage($"1234567{i}", "Englisch"));
            }

            await Task.WhenAll(tasks);

            mockGroupAccess.Verify(g => g.SetGroupLanguageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(numberOfConcurrentCalls));
        }

        #endregion

        #region ListGroups Method

        [Fact]
        public async Task ListGroups_ReturnsAllGroups()
        {
            // Arrange
            var mockGroupAccess = new Mock<IGroupAccess>();
            var expectedGroups = new List<(string GroupId, GroupInfo GroupInfo)>{
                ("-1234567890", new GroupInfo {GroupName="G1", Language="Deutsch",IsActive=true,CreatedDate=DateTime.Now}),
                ("-2234567890", new GroupInfo {GroupName="G2", Language="Englisch",IsActive=true,CreatedDate=DateTime.Now}),
                ("-3234567890", new GroupInfo {GroupName="G3", Language="Persisch",IsActive=true,CreatedDate=DateTime.Now})};

            mockGroupAccess.Setup(g => g.ListAllGroupsWithLanguagesAsync()).ReturnsAsync(expectedGroups);
            var service = CreateBotService(mockGroupAccess.Object);

            // Act
            var result = await service.ListGroups();

            // Assert
            Assert.Equal(expectedGroups.Count, result.Count);
            for (int i = 0; i < expectedGroups.Count; i++)
            {
                Assert.Equal(expectedGroups[i].GroupId, result[i].GroupId);
                Assert.Equal(expectedGroups[i].GroupInfo.Language, result[i].GroupInfo.Language);
            }
        }

        #endregion
    }
}