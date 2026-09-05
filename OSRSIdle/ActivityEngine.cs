namespace OSRSIdle;

public class ActivityEngine
{
    private CancellationTokenSource? cancellationTokenSource;

    public bool IsRunning { get; private set; }

    public Skill? CurrentSkill { get; private set; }

    public SkillActivity? CurrentActivity { get; private set; }

    public double Progress { get; private set; }

    public double TimeRemaining { get; private set; }

    public event Action? ActivityChanged;

    public async Task Start(
        Skill skill,
        SkillActivity activity,
        Action<double> onProgress,
        Action onComplete)
    {
        // Stop whatever is currently running.
        Stop();

        CurrentSkill = skill;
        CurrentActivity = activity;
        Progress = 0;
        TimeRemaining = activity.ActionTicks;

        IsRunning = true;

        ActivityChanged?.Invoke();

        cancellationTokenSource = new CancellationTokenSource();

        try
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                double duration = activity.ActionTicks * 0.6;

                for (double elapsed = 0;
                     elapsed < duration;
                     elapsed += 0.1)
                {
                    if (cancellationTokenSource.Token.IsCancellationRequested)
                        break;

                    Progress = elapsed / duration;

                    TimeRemaining = Math.Max(
                        0,
                        (duration - elapsed) / 0.6);

                    onProgress(Progress);

                    ActivityChanged?.Invoke();

                    await Task.Delay(
                        TimeSpan.FromSeconds(0.1),
                        cancellationTokenSource.Token);
                }

                if (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    skill.AddXP(activity.XP);

                    onComplete();

                    Progress = 0;
                    TimeRemaining = activity.ActionTicks;

                    ActivityChanged?.Invoke();
                }
            }
        }
        catch (TaskCanceledException)
        {
            // Activity was stopped.
        }
        finally
        {
            IsRunning = false;

            ActivityChanged?.Invoke();
        }
    }

    public void Stop()
    {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();

        cancellationTokenSource = null;

        IsRunning = false;

        CurrentSkill = null;
        CurrentActivity = null;
        Progress = 0;
        TimeRemaining = 0;

        ActivityChanged?.Invoke();
    }
}
