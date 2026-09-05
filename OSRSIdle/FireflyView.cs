namespace OSRSIdle;

/// <summary>
/// A lightweight reusable particle layer for the landing screen. A fixed pool
/// is redrawn by one GraphicsView, avoiding per-frame layout or control churn.
/// </summary>
public sealed class FireflyView : GraphicsView, IDrawable
{
    // Keep a dense, lively field on the character-select screen while still
    // using one drawable instead of creating individual UI elements.
    private const int FireflyCount = 50;
    private const double FrameSeconds = 0.1;

    private readonly Random _random = new();
    private readonly Firefly[] _fireflies = new Firefly[FireflyCount];
    private IDispatcherTimer? _timer;
    private bool _isRunning;

    public FireflyView()
    {
        Drawable = this;
        InputTransparent = true;

        for (int index = 0; index < _fireflies.Length; index++)
        {
            _fireflies[index] = new Firefly();
            Reset(_fireflies[index], initial: true);
        }
    }

    public void Start()
    {
        if (_isRunning || Dispatcher == null)
            return;

        _timer ??= CreateTimer();
        _isRunning = true;
        _timer.Start();
    }

    public void Stop()
    {
        _isRunning = false;
        _timer?.Stop();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        if (Handler == null)
            Stop();
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        foreach (Firefly firefly in _fireflies)
        {
            float opacity = GetOpacity(firefly);

            if (opacity <= 0)
                continue;

            float x = (float)(firefly.X * dirtyRect.Width);
            float y = (float)(firefly.Y * dirtyRect.Height);
            float radius = firefly.Radius;

            canvas.FillColor = Color.FromRgba(255, 224, 92, opacity * 0.12f);
            canvas.FillCircle(x, y, radius * 2.6f);
            canvas.FillColor = Color.FromRgba(255, 236, 130, opacity * firefly.Brightness);
            canvas.FillCircle(x, y, radius);
        }
    }

    private IDispatcherTimer CreateTimer()
    {
        IDispatcherTimer timer = Dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(FrameSeconds * 1000);
        timer.Tick += (_, _) => Advance();
        return timer;
    }

    private void Advance()
    {
        if (!_isRunning)
            return;

        foreach (Firefly firefly in _fireflies)
        {
            firefly.Age += FrameSeconds;

            if (firefly.Age >= firefly.Lifetime)
            {
                Reset(firefly, initial: false);
                continue;
            }

            if (firefly.Age < firefly.Delay)
                continue;

            firefly.X += firefly.VelocityX * FrameSeconds;
            firefly.Y += firefly.VelocityY * FrameSeconds;
        }

        Invalidate();
    }

    private float GetOpacity(Firefly firefly)
    {
        double visibleAge = firefly.Age - firefly.Delay;

        if (visibleAge < 0)
            return 0;

        if (visibleAge < firefly.FadeIn)
            return (float)(visibleAge / firefly.FadeIn);

        double fadeOutStart = firefly.Lifetime - firefly.FadeOut;

        if (firefly.Age > fadeOutStart)
            return (float)Math.Clamp((firefly.Lifetime - firefly.Age) / firefly.FadeOut, 0, 1);

        return 1;
    }

    private void Reset(Firefly firefly, bool initial)
    {
        double angle = _random.NextDouble() * Math.PI * 2;
        double speed = 0.0025 + _random.NextDouble() * 0.004;

        firefly.X = 0.04 + _random.NextDouble() * 0.92;
        firefly.Y = 0.04 + _random.NextDouble() * 0.92;
        firefly.VelocityX = Math.Cos(angle) * speed;
        firefly.VelocityY = Math.Sin(angle) * speed;
        firefly.Delay = 0.5 + _random.NextDouble() * 4.5;
        firefly.FadeIn = 1.2 + _random.NextDouble() * 1.2;
        firefly.FadeOut = 1.4 + _random.NextDouble() * 1.4;
        firefly.Lifetime = firefly.Delay + firefly.FadeIn +
            0.8 + _random.NextDouble() * 2.2 + firefly.FadeOut;
        firefly.Brightness = (float)(0.45 + _random.NextDouble() * 0.35);
        firefly.Radius = (float)(0.65 + _random.NextDouble() * 0.7);
        firefly.Age = initial
            ? _random.NextDouble() * firefly.Lifetime
            : 0;
    }

    private sealed class Firefly
    {
        public double X;
        public double Y;
        public double VelocityX;
        public double VelocityY;
        public double Age;
        public double Delay;
        public double FadeIn;
        public double FadeOut;
        public double Lifetime;
        public float Brightness;
        public float Radius;
    }
}
