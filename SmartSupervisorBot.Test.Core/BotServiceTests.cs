using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SmartSupervisorBot.Core;
using SmartSupervisorBot.Core.Settings;
using SmartSupervisorBot.DataAccess;

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
            var mockOptions = new Mock<IOptions<BotConfigurationOptions>>();
            var mockBotConfig = new BotConfigurationOptions
            {
                BotSettings = new BotSettings { BotToken = "token", OpenAiToken = "token" },
                AllowedUpdatesSettings = new AllowedUpdatesSettings { AllowedUpdates = new[] { "message" } }
            };
            mockOptions.Setup(o => o.Value).Returns(mockBotConfig);

            var service = new BotService(mockOptions.Object, mockHttpClientFactory.Object, mockGroupAccess.Object, mockLogger.Object);

            // Act
            service.StartReceivingMessages();

            // Assert
            mockLogger.Verify();
        }

        private BotService CreateBotService(IGroupAccess groupAccess)
        {
            var mockLogger = new Mock<ILogger<BotService>>();
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            var mockOptions = new Mock<IOptions<BotConfigurationOptions>>();
            var config = new BotConfigurationOptions { BotSettings = new BotSettings { BotToken = "token", OpenAiToken = "token" } };
            mockOptions.Setup(o => o.Value).Returns(config);

            return new BotService(mockOptions.Object, mockHttpClientFactory.Object, groupAccess, mockLogger.Object);
        }

        #region AddGroup Tests

        [Fact]
        public async Task AddGroup_CallsAddGroupAsync_WithCorrectParameters()
        {
            // Arrange
            var mockGroupAccess = new Mock<IGroupAccess>();
            var service = CreateBotService(mockGroupAccess.Object);

            var groupName = "TestGroup";
            var language = "Englisch";

            // Act
            await service.AddGroup(groupName, language);

            // Assert
            mockGroupAccess.Verify(g => g.AddGroupAsync(groupName, language), Times.Once);
        }

        [Fact]
        public async Task AddGroup_CallsAddGroupAsync_WithCorrectParameters_AndHandlesSuccess()
        {
            // Arrange
            var mockGroupAccess = new Mock<IGroupAccess>();
            var service = CreateBotService(groupAccess: mockGroupAccess.Object);

            var groupName = "TestGroup";
            var language = "English";
            mockGroupAccess.Setup(x => x.AddGroupAsync(groupName, language))
                           .ReturnsAsync(true);  // Simulating successful addition

            // Act
            await service.AddGroup(groupName, language);

            // Assert
            mockGroupAccess.Verify(x => x.AddGroupAsync(groupName, language), Times.Once);
        }

        [Theory]
        [InlineData(null, "English")]
        [InlineData("", "English")]
        [InlineData("TestGroup", null)]
        [InlineData("TestGroup", "")]
        public async Task AddGroup_InvalidInputs_ThrowsArgumentException(string groupName, string language)
        {
            // Arrange
            var mockGroupAccess = new Mock<IGroupAccess>();
            var service = CreateBotService(groupAccess: mockGroupAccess.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.AddGroup(groupName, language));
        }

        [Fact]
        public async Task AddGroup_ExtremelyLongGroupName_ThrowsArgumentException()
        {
            // Arrange
            var mockGroupAccess = new Mock<IGroupAccess>();
            var service = CreateBotService(groupAccess: mockGroupAccess.Object);
            var longGroupName = new string('a', 256); // Assuming the limit is 255 characters
            var language = "English";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.AddGroup(longGroupName, language));
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
            mockGroupAccess.Setup(g => g.AddGroupAsync(It.IsAny<string>(), It.IsAny<string>()))
                           .ReturnsAsync(true);

            // Act
            for (int i = 0; i < numberOfConcurrentCalls; i++)
            {
                string groupName = $"TestGroup{i}";
                string language = "English";
                tasks.Add(service.AddGroup(groupName, language));
            }

            await Task.WhenAll(tasks);

            // Assert
            mockGroupAccess.Verify(g => g.AddGroupAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(numberOfConcurrentCalls));
        }

        #endregion

        #region DeleteGroup Tests

        [Fact]
        public async Task DeleteGroup_CallsRemoveGroupAsync_WithCorrectParameters()
        {
            // Arrange
            var mockGroupAccess = new Mock<IGroupAccess>();
            var service = CreateBotService(mockGroupAccess.Object);

            var groupName = "TestGroup";

            // Act
            await service.DeleteGroup(groupName);

            // Assert
            mockGroupAccess.Verify(g => g.RemoveGroupAsync(groupName), Times.Once);
        }

        [Fact]
        public async Task DeleteGroup_SuccessfullyDeletesGroup()
        {
            // Arrange
            var mockGroupAccess = new Mock<IGroupAccess>();
            mockGroupAccess.Setup(g => g.RemoveGroupAsync(It.IsAny<string>())).ReturnsAsync(true);
            var service = CreateBotService(mockGroupAccess.Object);

            // Act
            var result = await service.DeleteGroup("ValidGroupName");

            // Assert
            Assert.True(result);
            mockGroupAccess.Verify(g => g.RemoveGroupAsync("ValidGroupName"), Times.Once);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task DeleteGroup_InvalidInputs_ThrowsArgumentException(string invalidGroupName)
        {
            var mockGroupAccess = new Mock<IGroupAccess>();

            var service = CreateBotService(mockGroupAccess.Object);

            await Assert.ThrowsAsync<ArgumentException>(() => service.DeleteGroup(invalidGroupName));
        }

        [Fact]
        public async Task DeleteGroup_ConcurrencyHandling()
        {
            var mockGroupAccess = new Mock<IGroupAccess>();
            mockGroupAccess.Setup(g => g.RemoveGroupAsync(It.IsAny<string>())).ReturnsAsync(true);
            var service = CreateBotService(groupAccess: mockGroupAccess.Object);
            var tasks = new List<Task<bool>>();
            int numberOfConcurrentCalls = 100;

            for (int i = 0; i < numberOfConcurrentCalls; i++)
            {
                tasks.Add(service.DeleteGroup($"Group{i}"));
            }

            var results = await Task.WhenAll(tasks);

            foreach (var result in results)
            {
                Assert.True(result);
            }
            mockGroupAccess.Verify(g => g.RemoveGroupAsync(It.IsAny<string>()), Times.Exactly(numberOfConcurrentCalls));
        }

        #endregion

        #region EditGroup Tests

        [Fact]
        public async Task EditGroup_CallsRenameGroupAsync_WithCorrectParameters()
        {
            // Arrange
            var mockGroupAccess = new Mock<IGroupAccess>();
            var service = CreateBotService(groupAccess: mockGroupAccess.Object);

            var oldGroupName = "OldTestGroup";
            var newGroupName = "NewTestGroup";

            // Act
            await service.EditGroup(oldGroupName, newGroupName);

            // Assert
            mockGroupAccess.Verify(g => g.RenameGroupAsync(oldGroupName, newGroupName), Times.Once);
        }

        [Fact]
        public async Task EditGroup_SuccessfullyEditsGroup()
        {
            var mockGroupAccess = new Mock<IGroupAccess>();
            mockGroupAccess.Setup(g => g.RenameGroupAsync("OldGroupName", "NewGroupName")).ReturnsAsync(true);
            var service = CreateBotService(mockGroupAccess.Object);

            var result = await service.EditGroup("OldGroupName", "NewGroupName");

            Assert.True(result);
            mockGroupAccess.Verify(g => g.RenameGroupAsync("OldGroupName", "NewGroupName"), Times.Once);
        }

        [Theory]
        [InlineData("", "NewValidName")]
        [InlineData("ValidOldName", "")]
        [InlineData(null, "NewValidName")]
        [InlineData("ValidOldName", null)]
        public async Task EditGroup_InvalidInputs_ThrowsArgumentException(string oldName, string newName)
        {
            var mockGroupAccess = new Mock<IGroupAccess>();
            var service = CreateBotService(mockGroupAccess.Object);

            await Assert.ThrowsAsync<ArgumentException>(() => service.EditGroup(oldName, newName));
        }

        [Fact]
        public async Task EditGroup_ConcurrencyHandling()
        {
            var mockGroupAccess = new Mock<IGroupAccess>();
            mockGroupAccess.Setup(g => g.RenameGroupAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            var service = CreateBotService(groupAccess: mockGroupAccess.Object);
            var tasks = new List<Task<bool>>();
            int numberOfConcurrentCalls = 100;

            for (int i = 0; i < numberOfConcurrentCalls; i++)
            {
                tasks.Add(service.EditGroup($"OldGroup{i}", $"NewGroup{i}"));
            }

            var results = await Task.WhenAll(tasks);

            foreach (var result in results)
            {
                Assert.True(result);
            }
            mockGroupAccess.Verify(g => g.RenameGroupAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(numberOfConcurrentCalls));
        }

        #endregion

        #region EditLanguage Tests

        [Fact]
        public async Task EditLanguage_WithCorrectParameters_CallsSetGroupLanguageAsync()
        {
            var mockGroupAccess = new Mock<IGroupAccess>();
            var service = CreateBotService(groupAccess: mockGroupAccess.Object);

            string groupName = "GroupName";
            string language = "English";
            await service.EditLanguage(groupName, language);

            mockGroupAccess.Verify(g => g.SetGroupLanguageAsync(groupName, language), Times.Once);
        }

        [Fact]
        public async Task EditLanguage_SuccessfullyEditsLanguage()
        {
            var mockGroupAccess = new Mock<IGroupAccess>();
            mockGroupAccess.Setup(g => g.SetGroupLanguageAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            var service = CreateBotService(mockGroupAccess.Object);

            var result = await service.EditLanguage("ValidGroup", "German");

            Assert.True(result);
        }

        [Theory]
        [InlineData("", "English")]
        [InlineData("ValidGroup", "")]
        [InlineData(null, "English")]
        [InlineData("ValidGroup", null)]
        public async Task EditLanguage_InvalidInputs_ThrowsArgumentException(string groupName, string language)
        {
            var mockGroupAccess = new Mock<IGroupAccess>();
            var service = CreateBotService(mockGroupAccess.Object);

            await Assert.ThrowsAsync<ArgumentException>(() => service.EditLanguage(groupName, language));
        }

        [Fact]
        public async Task EditLanguage_ConcurrencyHandling()
        {
            var mockGroupAccess = new Mock<IGroupAccess>();
            mockGroupAccess.Setup(g => g.SetGroupLanguageAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            var service = CreateBotService(groupAccess: mockGroupAccess.Object);
            var tasks = new List<Task>();
            int numberOfConcurrentCalls = 100;

            for (int i = 0; i < numberOfConcurrentCalls; i++)
            {
                tasks.Add(service.EditLanguage($"Group{i}", "English"));
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
            var expectedGroups = new List<(string GroupName, string Language)>{
                ("Group1", "English"),
                ("Group2", "German"),
                ("Group3", "French")};

            mockGroupAccess.Setup(g => g.ListAllGroupsWithLanguagesAsync()).ReturnsAsync(expectedGroups);
            var service = CreateBotService(mockGroupAccess.Object);

            // Act
            var result = await service.ListGroups();

            // Assert
            Assert.Equal(expectedGroups.Count, result.Count);
            for (int i = 0; i < expectedGroups.Count; i++)
            {
                Assert.Equal(expectedGroups[i].GroupName, result[i].GroupName);
                Assert.Equal(expectedGroups[i].Language, result[i].Language);
            }
        }

        #endregion
    }
}