using Microsoft.Extensions.Logging;
using Moq;
using RideWaitTime.Business;

namespace RideWaitTimeMonitor.Tests;

public class WhenMonitoringRideWaitTimes
{
    private readonly Mock<ILogger<Worker>> _logger;
    private readonly Mock<IQueueTimesClient> _queueTimesClient;
    private readonly Mock<INotifier> _notifier;
    private readonly Mock<IWaitTimeThresholdLoader> _waitTimeThresholdLoader;

    private readonly Land[] _lands =
    {
        Land("Coasters",
            Ride("Steel Vengeance", 45),
            Ride("Raptor", 35),
            Ride("Millennium Force", 60)),
        Land("Thrill",
            Ride("MaXair", 10))
    };

    public WhenMonitoringRideWaitTimes()
    {
        _logger = new Mock<ILogger<Worker>>();
        _queueTimesClient = new Mock<IQueueTimesClient>();
        _queueTimesClient.Setup(m => m.GetQueueTimesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueueTimesResponse(_lands, Array.Empty<Ride>()));
        _notifier = new Mock<INotifier>();
        _waitTimeThresholdLoader = new Mock<IWaitTimeThresholdLoader>();

        Dictionary<string, int?> waitTimeThresholds = new()
        {
            { "Steel Vengeance", 45 },
            { "Raptor", 30 },
            { "Millennium Force", 50 }
        };

        _waitTimeThresholdLoader.Setup(m => m.LoadWaitTimeThresholds()).Returns(waitTimeThresholds);
    }

    [Fact]
    public async Task ItFetchesTheWaitTimeThresholds()
    {
        var worker = Worker();

        await worker.MonitorRideWaitTimesAsync(CancellationToken.None);

        _waitTimeThresholdLoader.Verify(m => m.LoadWaitTimeThresholds(), Times.Once);
    }

    [Fact]
    public async Task ItFetchesTheQueueTimes()
    {
        var worker = Worker();
        await worker.MonitorRideWaitTimesAsync(CancellationToken.None);

        _queueTimesClient.Verify(
            m => m.GetQueueTimesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenANullQueueTimesResponse_ItThrowsAnException()
    {
        QueueTimesResponse? response = null;
        _queueTimesClient.Setup(m => m.GetQueueTimesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var worker = Worker();

        var exception =
            await Assert.ThrowsAsync<Exception>(() => worker.MonitorRideWaitTimesAsync(CancellationToken.None));
        Assert.Equal("No Lands were found for this park", exception.Message);
    }

    [Fact]
    public async Task GivenThereAreNoLands_ItThrowsAnException()
    {
        var response = Response();
        _queueTimesClient.Setup(m => m.GetQueueTimesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var worker = Worker();

        var exception =
            await Assert.ThrowsAsync<Exception>(() => worker.MonitorRideWaitTimesAsync(CancellationToken.None));
        Assert.Equal("No Lands were found for this park", exception.Message);
    }

    [Fact]
    public async Task GivenThereIsNoCoasterLand_ItThrowsAnException()
    {
        var response = Response(
            Land("Family"),
            Land("Kids")
        );

        _queueTimesClient.Setup(m => m.GetQueueTimesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var worker = Worker();

        var exception =
            await Assert.ThrowsAsync<Exception>(() => worker.MonitorRideWaitTimesAsync(CancellationToken.None));
        Assert.Equal("No Coasters or Thrill rides were found for this park", exception.Message);
    }

    [Fact]
    public async Task GivenThereIsAreNoCoasters_ItThrowsAnException()
    {
        var response = Response(
            Land("Family"),
            Land("Kids"),
            Land("Coasters")
        );

        _queueTimesClient.Setup(m => m.GetQueueTimesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var worker = Worker();

        var exception =
            await Assert.ThrowsAsync<Exception>(() => worker.MonitorRideWaitTimesAsync(CancellationToken.None));
        Assert.Equal("No Coasters or Thrill rides were found for this park", exception.Message);
    }

    [Fact]
    public async Task GivenACoasterMeetsTheWaitTimeThreshold_AndThisIsTheFirstTimeCheckingIt_ItNotifiesTheUser()
    {
        var response = Response(
            Land("Coasters",
                Ride("Steel Vengeance", 45)));

        var thresholds = new Dictionary<string, int?>
        {
            { "Steel Vengeance", 45 }
        };

        _waitTimeThresholdLoader.Setup(m => m.LoadWaitTimeThresholds()).Returns(thresholds);

        _queueTimesClient.Setup(m => m.GetQueueTimesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var worker = Worker();

        await worker.MonitorRideWaitTimesAsync(CancellationToken.None);

        _notifier.Verify(m => m.NotifyAsync("Steel Vengeance is now a 45 minute wait"), Times.Once);
    }

    [Fact]
    public async Task GivenAThrillRideMeetsTheWaitTimeThreshold_ItNotifiesTheUser()
    {
        var response = Response(
            Land("Thrill",
                Ride("MaXair", 10)));

        var thresholds = new Dictionary<string, int?>
        {
            { "MaXair", 15 }
        };

        _waitTimeThresholdLoader.Setup(m => m.LoadWaitTimeThresholds()).Returns(thresholds);

        _queueTimesClient.SetupSequence(m => m.GetQueueTimesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var worker = Worker();

        await worker.MonitorRideWaitTimesAsync(CancellationToken.None);

        _notifier.Verify(m => m.NotifyAsync("MaXair is now a 10 minute wait"), Times.Once);
    }


    [Fact]
    public async Task GivenACoasterMeetsTheWaitTimeThreshold_AndItHasChangedSinceLastRun_ItNotifiesTheUser()
    {
        var firstResponse = Response(
            Land("Coasters",
                Ride("Steel Vengeance", 45)));

        var secondResponse = Response(
            Land("Coasters",
                Ride("Steel Vengeance", 30)));

        var thresholds = new Dictionary<string, int?>
        {
            { "Steel Vengeance", 45 }
        };

        _waitTimeThresholdLoader.Setup(m => m.LoadWaitTimeThresholds()).Returns(thresholds);

        _queueTimesClient.SetupSequence(m => m.GetQueueTimesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstResponse)
            .ReturnsAsync(secondResponse);

        var worker = Worker();

        await worker.MonitorRideWaitTimesAsync(CancellationToken.None);
        await worker.MonitorRideWaitTimesAsync(CancellationToken.None);

        _notifier.Verify(m => m.NotifyAsync("Steel Vengeance is now a 45 minute wait"), Times.Once);
        _notifier.Verify(m => m.NotifyAsync("Steel Vengeance is now a 30 minute wait"), Times.Once);
    }

    [Fact]
    public async Task
        GivenACoasterMeetsTheWaitTimeThreshold_AndItHasNotChangedSinceLastRun_ItDoesNotNotifyTheUserTheSecondTime()
    {
        var firstResponse = Response(
            Land("Coasters",
                Ride("Steel Vengeance", 45)));

        var secondResponse = Response(
            Land("Coasters",
                Ride("Steel Vengeance", 45)));

        var thresholds = new Dictionary<string, int?>
        {
            { "Steel Vengeance", 45 }
        };

        _waitTimeThresholdLoader.Setup(m => m.LoadWaitTimeThresholds()).Returns(thresholds);

        _queueTimesClient.SetupSequence(m => m.GetQueueTimesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstResponse)
            .ReturnsAsync(secondResponse);

        var worker = Worker();

        await worker.MonitorRideWaitTimesAsync(CancellationToken.None);
        await worker.MonitorRideWaitTimesAsync(CancellationToken.None);

        _notifier.Verify(m => m.NotifyAsync("Steel Vengeance is now a 45 minute wait"), Times.Once);
    }

    [Fact]
    public async Task
        GivenACoasterMeetsTheWaitTimeThreshold_AndItHasChangedSinceLastRun_AndItHasGottenSlower_ItNotifiesTheUserBothTimes()
    {
        var firstResponse = Response(
            Land("Coasters",
                Ride("Steel Vengeance", 30)));

        var secondResponse = Response(
            Land("Coasters",
                Ride("Steel Vengeance", 45)));

        var thresholds = new Dictionary<string, int?>
        {
            { "Steel Vengeance", 45 }
        };

        _waitTimeThresholdLoader.Setup(m => m.LoadWaitTimeThresholds()).Returns(thresholds);

        _queueTimesClient.SetupSequence(m => m.GetQueueTimesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstResponse)
            .ReturnsAsync(secondResponse);

        var worker = Worker();

        await worker.MonitorRideWaitTimesAsync(CancellationToken.None);
        await worker.MonitorRideWaitTimesAsync(CancellationToken.None);

        _notifier.Verify(m => m.NotifyAsync("Steel Vengeance is now a 30 minute wait"), Times.Once);
        _notifier.Verify(m => m.NotifyAsync("Steel Vengeance is now a 45 minute wait"), Times.Once);
    }

    [Fact]
    public async Task
        GivenACoasterIsClosed_ItNotifiesTheUser()
    {
        var response = Response(
            Land("Coasters",
                Ride("Steel Vengeance", 0, isOpen: false)));

        var thresholds = new Dictionary<string, int?>
        {
            { "Steel Vengeance", 45 }
        };

        _waitTimeThresholdLoader.Setup(m => m.LoadWaitTimeThresholds()).Returns(thresholds);

        _queueTimesClient.SetupSequence(m => m.GetQueueTimesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var worker = Worker();

        await worker.MonitorRideWaitTimesAsync(CancellationToken.None);

        _notifier.Verify(m => m.NotifyAsync("Steel Vengeance is now closed"), Times.Once);
    }

    [Fact]
    public async Task
        GivenACoasterWasClosedLastCheck_ItDoesNotNotifyTheUserTheSecondTime()
    {
        var firstResponse = Response(
            Land("Coasters",
                Ride("Steel Vengeance", 0, isOpen: false)));

        var secondResponse = Response(
            Land("Coasters",
                Ride("Steel Vengeance", 0, isOpen: false)));

        var thresholds = new Dictionary<string, int?>
        {
            { "Steel Vengeance", 45 }
        };

        _waitTimeThresholdLoader.Setup(m => m.LoadWaitTimeThresholds()).Returns(thresholds);

        _queueTimesClient.SetupSequence(m => m.GetQueueTimesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstResponse)
            .ReturnsAsync(secondResponse);

        var worker = Worker();

        await worker.MonitorRideWaitTimesAsync(CancellationToken.None);
        await worker.MonitorRideWaitTimesAsync(CancellationToken.None);

        _notifier.Verify(m => m.NotifyAsync("Steel Vengeance is now closed"), Times.Once);
    }

    [Fact]
    public async Task
        GivenACoasterWasClosed_AndNowIsOpen_AndIsStillBelowTheThreshold_ItNotifiesTheUserBothTimes()
    {
        var firstResponse = Response(
            Land("Coasters",
                Ride("Steel Vengeance", 0, isOpen: false)));

        var secondResponse = Response(
            Land("Coasters",
                Ride("Steel Vengeance", 45, isOpen: true)));

        var thresholds = new Dictionary<string, int?>
        {
            { "Steel Vengeance", 45 }
        };

        _waitTimeThresholdLoader.Setup(m => m.LoadWaitTimeThresholds()).Returns(thresholds);

        _queueTimesClient.SetupSequence(m => m.GetQueueTimesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstResponse)
            .ReturnsAsync(secondResponse);

        var worker = Worker();

        await worker.MonitorRideWaitTimesAsync(CancellationToken.None);
        await worker.MonitorRideWaitTimesAsync(CancellationToken.None);

        _notifier.Verify(m => m.NotifyAsync("Steel Vengeance is now closed"), Times.Once);
        _notifier.Verify(m => m.NotifyAsync("Steel Vengeance is now open with a 45 minute wait"), Times.Once);
    }

    private static Ride Ride(string name, int waitTime, bool isOpen = true)
        => new(1, name, isOpen, waitTime, DateTime.Now);

    private static Land Land(string name, params Ride[] rides)
        => new(1, name, rides);

    private static QueueTimesResponse Response(params Land[] lands)
        => new(lands, Array.Empty<Ride>());

    private Worker Worker()
        => new(_logger.Object, _queueTimesClient.Object, _waitTimeThresholdLoader.Object, _notifier.Object);
}