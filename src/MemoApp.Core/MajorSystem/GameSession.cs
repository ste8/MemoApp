namespace MemoApp.Core.MajorSystem;

public enum SessionState
{
    NotStarted,
    InProgress,
    Completed
}

public class GameSession
{
    private readonly List<NumberPerformance> _performances = new();
    private readonly Queue<MajorNumber> _remainingNumbers = new();
    private MajorNumber? _currentNumber;
    private DateTime? _currentNumberPresentedAt;
    private DateTime _sessionStartTime;
    private long _sessionStartTimestamp;
    private TimeSpan? _completedElapsed;
    private readonly TimeProvider _timeProvider;

    public string RangeStart { get; }
    public string RangeEnd { get; }
    public SessionState State { get; private set; } = SessionState.NotStarted;
    public MajorNumber? CurrentNumber => _currentNumber;
    public int TotalNumbers { get; }
    public int CompletedNumbers => _performances.Count;
    public int RemainingNumbers => _remainingNumbers.Count + (_currentNumber.HasValue ? 1 : 0);
    public IReadOnlyList<NumberPerformance> Performances => _performances.AsReadOnly();

    /// <summary>
    /// Time spent in this session so far. Zero before it starts, frozen once it completes.
    /// Measured monotonically, so it cannot be distorted by the device clock changing
    /// mid-session. This is the session's own clock - callers should not keep a second one.
    /// </summary>
    public TimeSpan Elapsed => State switch
    {
        SessionState.NotStarted => TimeSpan.Zero,
        SessionState.Completed => _completedElapsed ?? TimeSpan.Zero,
        _ => _timeProvider.GetElapsedTime(_sessionStartTimestamp)
    };

    /// <summary>
    /// Creates a session over a range, resolved with the caller's number format.
    /// </summary>
    public GameSession(string rangeStart, string rangeEnd, NumberFormat format, TimeProvider? timeProvider = null)
        : this(rangeStart, rangeEnd, NumberSequence.GenerateSequence(rangeStart, rangeEnd, format).ToList(), timeProvider)
    {
    }

    /// <summary>
    /// Creates a session over an already-resolved sequence. Preferred where the caller
    /// has built the sequence itself: the session no longer decides what to present,
    /// only the order in which to present it, which makes it testable in isolation.
    /// </summary>
    public GameSession(string rangeStart, string rangeEnd, IReadOnlyList<MajorNumber> sequence, TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        RangeStart = rangeStart ?? throw new ArgumentNullException(nameof(rangeStart));
        RangeEnd = rangeEnd ?? throw new ArgumentNullException(nameof(rangeEnd));
        ArgumentNullException.ThrowIfNull(sequence);

        TotalNumbers = sequence.Count;

        foreach (var number in Shuffle(sequence))
        {
            _remainingNumbers.Enqueue(number);
        }
    }

    /// <summary>
    /// Fisher-Yates. Replaces OrderBy(_ =&gt; random.Next()), which is a biased shuffle:
    /// equal sort keys leave the affected elements in their original relative order.
    /// </summary>
    private static List<MajorNumber> Shuffle(IReadOnlyList<MajorNumber> source)
    {
        var items = source.ToList();

        for (int i = items.Count - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }

        return items;
    }

    public void StartSession()
    {
        if (State != SessionState.NotStarted)
            throw new InvalidOperationException("Session has already been started");
        
        _sessionStartTime = _timeProvider.GetUtcNow().UtcDateTime;
        _sessionStartTimestamp = _timeProvider.GetTimestamp();
        State = SessionState.InProgress;
        PresentNextNumber();
    }

    public void AdvanceToNext()
    {
        if (State != SessionState.InProgress)
            throw new InvalidOperationException("Session is not in progress");
        
        if (!_currentNumber.HasValue || !_currentNumberPresentedAt.HasValue)
            throw new InvalidOperationException("No current number to advance from");
        
        // Record the performance for the current number
        var responseTime = _timeProvider.GetUtcNow().UtcDateTime;
        var performance = new NumberPerformance(_currentNumber.Value, _currentNumberPresentedAt.Value, responseTime);
        _performances.Add(performance);
        
        // Present the next number or complete the session
        if (_remainingNumbers.Count > 0)
        {
            PresentNextNumber();
        }
        else
        {
            CompleteSession();
        }
    }

    public SessionStatistics GetStatistics()
    {
        if (State != SessionState.Completed)
            throw new InvalidOperationException("Session must be completed to get statistics");
        
        var endTime = _performances.LastOrDefault().RespondedAt;
        return new SessionStatistics(_performances, _sessionStartTime, endTime);
    }

    private void PresentNextNumber()
    {
        if (_remainingNumbers.Count == 0)
            return;
        
        _currentNumber = _remainingNumbers.Dequeue();
        _currentNumberPresentedAt = _timeProvider.GetUtcNow().UtcDateTime;
    }

    private void CompleteSession()
    {
        _completedElapsed = _timeProvider.GetElapsedTime(_sessionStartTimestamp);
        _currentNumber = null;
        _currentNumberPresentedAt = null;
        State = SessionState.Completed;
    }
}