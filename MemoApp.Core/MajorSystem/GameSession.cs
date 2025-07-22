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

    public string RangeStart { get; }
    public string RangeEnd { get; }
    public SessionState State { get; private set; } = SessionState.NotStarted;
    public MajorNumber? CurrentNumber => _currentNumber;
    public int TotalNumbers { get; }
    public int CompletedNumbers => _performances.Count;
    public int RemainingNumbers => _remainingNumbers.Count + (_currentNumber.HasValue ? 1 : 0);
    public IReadOnlyList<NumberPerformance> Performances => _performances.AsReadOnly();

    public GameSession(string rangeStart, string rangeEnd)
    {
        RangeStart = rangeStart ?? throw new ArgumentNullException(nameof(rangeStart));
        RangeEnd = rangeEnd ?? throw new ArgumentNullException(nameof(rangeEnd));
        
        var sequence = NumberSequence.GenerateSequence(rangeStart, rangeEnd).ToList();
        TotalNumbers = sequence.Count;
        
        // Shuffle the sequence for random presentation
        var random = new Random();
        var shuffledSequence = sequence.OrderBy(_ => random.Next()).ToList();
        
        foreach (var number in shuffledSequence)
        {
            _remainingNumbers.Enqueue(number);
        }
    }

    public void StartSession()
    {
        if (State != SessionState.NotStarted)
            throw new InvalidOperationException("Session has already been started");
        
        _sessionStartTime = DateTime.UtcNow;
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
        var responseTime = DateTime.UtcNow;
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
        _currentNumberPresentedAt = DateTime.UtcNow;
    }

    private void CompleteSession()
    {
        _currentNumber = null;
        _currentNumberPresentedAt = null;
        State = SessionState.Completed;
    }
}