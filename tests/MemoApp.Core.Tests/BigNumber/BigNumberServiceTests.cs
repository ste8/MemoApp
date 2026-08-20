using MemoApp.Core.BigNumber;

namespace MemoApp.Core.Tests.BigNumber;

/// <summary>
/// BigNumberService generates the digit string for the Big Number exercise, formats it for
/// display, and drives the game's phase transitions.
///
/// A generated number is built from digit pairs: every pair is at most maxPairValue, the
/// number is exactly the requested length, and it never starts with a zero.
/// </summary>
public class BigNumberServiceTests
{
    private readonly BigNumberService _sut = new();

    // ---------------------------------------------------------------
    // GenerateNumber
    // ---------------------------------------------------------------

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(20)]
    [InlineData(51)]
    public void GenerateNumber_ProducesExactlyTheRequestedDigitCount(int digits)
    {
        for (var i = 0; i < 200; i++)
        {
            Assert.Equal(digits, _sut.GenerateNumber(digits, 99).Length);
        }
    }

    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(59)]
    [InlineData(99)]
    public void GenerateNumber_NeverEmitsAPairAboveMaxPairValue(int maxPairValue)
    {
        for (var i = 0; i < 500; i++)
        {
            var number = _sut.GenerateNumber(8, maxPairValue);

            for (var p = 0; p + 1 < number.Length; p += 2)
            {
                var pair = int.Parse(number.Substring(p, 2));
                Assert.True(pair <= maxPairValue,
                    $"pair {pair} exceeds maxPairValue {maxPairValue} in \"{number}\"");
            }
        }
    }

    [Theory]
    [InlineData(2, 99)]
    [InlineData(4, 99)]
    [InlineData(5, 99)]
    [InlineData(6, 59)]
    [InlineData(7, 59)]
    public void GenerateNumber_NeverStartsWithZero(int digits, int maxPairValue)
    {
        for (var i = 0; i < 500; i++)
        {
            var number = _sut.GenerateNumber(digits, maxPairValue);

            Assert.NotEqual('0', number[0]);
        }
    }

    [Fact]
    public void GenerateNumber_EmitsOnlyDigits()
    {
        Assert.All(_sut.GenerateNumber(30, 99), c => Assert.True(char.IsDigit(c)));
    }

    [Theory]
    [InlineData(0, 99)]    // digits must be positive
    [InlineData(-5, 99)]
    [InlineData(4, 9)]     // maxPairValue floor is 10
    [InlineData(4, 100)]   // maxPairValue ceiling is 99
    public void GenerateNumber_RejectsInvalidArguments(int digits, int maxPairValue)
    {
        Assert.Throws<ArgumentException>(() => _sut.GenerateNumber(digits, maxPairValue));
    }

    [Fact]
    public void GenerateNumber_ProducesVariedOutput()
    {
        var results = Enumerable.Range(0, 200).Select(_ => _sut.GenerateNumber(10, 99)).ToHashSet();

        // A fixed seed would make every session present the same number.
        Assert.True(results.Count > 150, $"expected varied output, got {results.Count} distinct of 200");
    }

    // ---------------------------------------------------------------
    // CreateNewGame
    // ---------------------------------------------------------------

    [Fact]
    public void CreateNewGame_HonoursSettingsAndStartsInSetupPhase()
    {
        var game = _sut.CreateNewGame(new BigNumberSettings { NumberOfDigits = 12, MaxPairValue = 59 });

        Assert.Equal(12, game.GeneratedNumber.Length);
        Assert.Equal(GamePhase.Setup, game.Phase);
        Assert.False(game.IsTimerRunning);
        Assert.True(game.IsNumberVisible);
        Assert.Equal(TimeSpan.Zero, game.ElapsedTime);
        Assert.Null(game.StartTime);
    }

    // ---------------------------------------------------------------
    // FormatNumber
    // ---------------------------------------------------------------

    [Theory]
    [InlineData("6666", "66 66")]
    [InlineData("250137", "25 01 37")]
    [InlineData("62374", "6 23 74")]      // odd length: leading single digit
    [InlineData("9911750", "9 91 17 50")]
    public void FormatNumber_GroupsIntoPairsWithOddLeadingDigit(string input, string expected)
    {
        Assert.Equal(expected, _sut.FormatNumber(input, showSeparated: true));
    }

    [Fact]
    public void FormatNumber_IsLosslessWhenSeparatorsAreStripped()
    {
        foreach (var digits in new[] { 4, 5, 6, 7, 20, 21 })
        {
            var number = _sut.GenerateNumber(digits, 99);

            Assert.Equal(number, _sut.FormatNumber(number, showSeparated: true).Replace(" ", ""));
        }
    }

    [Fact]
    public void FormatNumber_ReturnsInputUnchangedWhenNotSeparated()
    {
        Assert.Equal("123456", _sut.FormatNumber("123456", showSeparated: false));
    }

    [Fact]
    public void FormatNumber_HandlesEmptyInput()
    {
        Assert.Equal(string.Empty, _sut.FormatNumber("", showSeparated: true));
    }

    // ---------------------------------------------------------------
    // FormatTime
    //
    // Renders mm:ss, carrying into h:mm:ss once the span reaches an hour.
    // Negative spans clamp to zero.
    // ---------------------------------------------------------------

    [Theory]
    [InlineData(5, "00:05")]
    [InlineData(65, "01:05")]
    [InlineData(3570, "59:30")]
    public void FormatTime_RendersMinutesAndSecondsCorrectlyUnderOneHour(int seconds, string expected)
    {
        Assert.Equal(expected, _sut.FormatTime(TimeSpan.FromSeconds(seconds)));
    }

    [Theory]
    [InlineData(2, "2:00:00")]
    [InlineData(1.5, "1:30:00")]
    [InlineData(25, "25:00:00")]
    public void FormatTime_SpanOfAnHourOrMore_CarriesIntoAnHoursComponent(double hours, string expected)
    {
        Assert.Equal(expected, _sut.FormatTime(TimeSpan.FromHours(hours)));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-30)]
    [InlineData(-4000)]
    public void FormatTime_NegativeSpan_ClampsToZero(int seconds)
    {
        Assert.Equal("00:00", _sut.FormatTime(TimeSpan.FromSeconds(seconds)));
    }

    // ---------------------------------------------------------------
    // Elapsed time
    // ---------------------------------------------------------------

    [Fact]
    public void GetElapsedTime_WhileRunning_TracksRealTimePassing()
    {
        var clock = new TestTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var service = new BigNumberService(clock);
        var game = service.CreateNewGame(new BigNumberSettings());

        service.StartGame(game);
        clock.Advance(TimeSpan.FromSeconds(45));

        Assert.Equal(TimeSpan.FromSeconds(45), service.GetElapsedTime(game));
    }

    [Fact]
    public void GetElapsedTime_WhenTheDeviceClockJumps_IsUnaffected()
    {
        var clock = new TestTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var service = new BigNumberService(clock);
        var game = service.CreateNewGame(new BigNumberSettings());

        service.StartGame(game);
        clock.Advance(TimeSpan.FromSeconds(30));
        clock.ShiftWallClock(TimeSpan.FromHours(-1));

        Assert.Equal(TimeSpan.FromSeconds(30), service.GetElapsedTime(game));
    }

    [Fact]
    public void StopGame_WhenTheDeviceClockJumps_RecordsTheRealDuration()
    {
        var clock = new TestTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var service = new BigNumberService(clock);
        var game = service.CreateNewGame(new BigNumberSettings());

        service.StartGame(game);
        clock.Advance(TimeSpan.FromSeconds(20));
        clock.ShiftWallClock(TimeSpan.FromHours(1));
        service.StopGame(game);

        Assert.Equal(TimeSpan.FromSeconds(20), game.ElapsedTime);
    }

    [Fact]
    public void GetElapsedTime_AfterStopping_ReturnsTheRecordedDuration()
    {
        var clock = new TestTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var service = new BigNumberService(clock);
        var game = service.CreateNewGame(new BigNumberSettings());

        service.StartGame(game);
        clock.Advance(TimeSpan.FromSeconds(10));
        service.StopGame(game);
        clock.Advance(TimeSpan.FromMinutes(5));

        Assert.Equal(TimeSpan.FromSeconds(10), service.GetElapsedTime(game));
    }

    [Fact]
    public void StartGame_RecordsTheWallClockStartInUtc()
    {
        var start = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var clock = new TestTimeProvider(start);
        var service = new BigNumberService(clock);
        var game = service.CreateNewGame(new BigNumberSettings());

        service.StartGame(game);

        Assert.Equal(start.UtcDateTime, game.StartTime);
        Assert.Equal(DateTimeKind.Utc, game.StartTime!.Value.Kind);
    }
}
