namespace OSRSIdle;

public enum VisualEffectKind
{
    Sparkle,
    Burst,
    Shimmer,
    FoodHeal,
    WeatherSnow,
    WeatherEmber,
    WeatherMotes,
    StatusRegeneration,
    StatusFrenzy,
    StatusArmored,
    StatusPoison,
    RareDrop
}

/// <summary>
/// A small, pooled drawable effect. Effects are drawn into one GraphicsView
/// instead of creating a control for each particle, keeping combat and area
/// lists independent from animation frame rate.
/// </summary>
public sealed class ParticleEffectView : GraphicsView
{
    private readonly EffectDrawable _drawable;
    private IDispatcherTimer? _timer;
    private VisualEffectKind _kind;
    private bool _ambient;
    private bool _running;
    private int _elapsed;
    private int _duration;
    private EffectParticle[] _particles = Array.Empty<EffectParticle>();

    public ParticleEffectView()
    {
        InputTransparent = true;
        HorizontalOptions = LayoutOptions.Fill;
        VerticalOptions = LayoutOptions.Fill;
        ZIndex = 20;
        _drawable = new EffectDrawable(this);
        Drawable = _drawable;
    }

    public void Start(
        VisualEffectKind kind,
        int durationMilliseconds,
        int particleCount,
        bool ambient = false)
    {
        _kind = kind;
        _ambient = ambient;
        _elapsed = 0;
        _duration = Math.Max(120, durationMilliseconds);
        int count = Math.Clamp(particleCount, 4, 36);
        _particles = Enumerable.Range(0, count)
            .Select(_ => EffectParticle.Create(kind, Random.Shared))
            .ToArray();
        _running = true;
        EnsureTimer();
        Invalidate();
    }

    public void Stop()
    {
        _running = false;
        _timer?.Stop();
        _timer = null;
        Invalidate();
    }

    private void EnsureTimer()
    {
        if (!_running || _timer != null || Dispatcher == null)
            return;

        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(50);
        _timer.Tick += OnTick;
        _timer.Start();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (!_running)
            return;

        const int delta = 50;
        _elapsed += delta;
        foreach (EffectParticle particle in _particles)
        {
            particle.Age += delta;
            particle.X += particle.VelocityX * delta / 1000d;
            particle.Y += particle.VelocityY * delta / 1000d;

            if (_ambient)
            {
                if (particle.X < -0.1 || particle.X > 1.1 || particle.Y < -0.1 || particle.Y > 1.1)
                    particle.Reset(_kind, Random.Shared);
            }
        }

        Invalidate();
        if (!_ambient && _elapsed >= _duration)
        {
            Stop();
            if (Parent is Layout host)
                host.Children.Remove(this);
        }
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler == null)
            Stop();
        else
            EnsureTimer();
    }

    private sealed class EffectDrawable : IDrawable
    {
        private readonly ParticleEffectView _owner;

        public EffectDrawable(ParticleEffectView owner) => _owner = owner;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (!_owner._running || dirtyRect.Width <= 0 || dirtyRect.Height <= 0)
                return;

            foreach (EffectParticle particle in _owner._particles)
            {
                double progress = Math.Clamp(
                    particle.Age / (double)_owner._duration,
                    0,
                    1);
                double alpha = _owner._ambient
                    ? 0.42 + 0.28 * Math.Sin((particle.Age + particle.Phase) / 180d)
                    : Math.Sin(Math.PI * progress);
                if (alpha <= 0)
                    continue;

                float x = (float)(particle.X * dirtyRect.Width);
                float y = (float)(particle.Y * dirtyRect.Height);
                float size = (float)(particle.Size * Math.Min(dirtyRect.Width, dirtyRect.Height));
                (byte red, byte green, byte blue) = Palette(_owner._kind);
                Color particleColor = Color.FromRgba(red, green, blue, (byte)Math.Clamp(alpha * 255, 0, 255));
                canvas.FillColor = particleColor;
                canvas.StrokeColor = particleColor;
                canvas.StrokeSize = Math.Max(1, size * 0.18f);

                switch (_owner._kind)
                {
                    case VisualEffectKind.Burst:
                    case VisualEffectKind.RareDrop:
                        canvas.DrawLine(x - size * 1.8f, y, x + size * 1.8f, y);
                        canvas.DrawLine(x, y - size * 1.8f, x, y + size * 1.8f);
                        canvas.FillCircle(x, y, size * 0.45f);
                        break;
                    case VisualEffectKind.Shimmer:
                        PathF diamond = new();
                        diamond.MoveTo(x, y - size);
                        diamond.LineTo(x + size, y);
                        diamond.LineTo(x, y + size);
                        diamond.LineTo(x - size, y);
                        diamond.Close();
                        canvas.FillPath(diamond);
                        break;
                    case VisualEffectKind.WeatherSnow:
                        canvas.DrawLine(x, y - size, x, y + size);
                        canvas.DrawLine(x - size, y, x + size, y);
                        break;
                    default:
                        canvas.FillCircle(x, y, size);
                        break;
                }
            }
        }

        private static (byte red, byte green, byte blue) Palette(VisualEffectKind kind) => kind switch
        {
            VisualEffectKind.FoodHeal or VisualEffectKind.StatusRegeneration => (139, 255, 157),
            VisualEffectKind.StatusFrenzy or VisualEffectKind.WeatherEmber => (255, 126, 54),
            VisualEffectKind.StatusArmored => (111, 168, 220),
            VisualEffectKind.StatusPoison => (112, 216, 144),
            VisualEffectKind.Shimmer => (102, 217, 255),
            VisualEffectKind.RareDrop => (255, 77, 255),
            VisualEffectKind.WeatherSnow => (210, 235, 255),
            VisualEffectKind.WeatherMotes => (214, 178, 92),
            _ => (255, 226, 106)
        };
    }

    private sealed class EffectParticle
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double VelocityX { get; set; }
        public double VelocityY { get; set; }
        public double Size { get; set; }
        public int Age { get; set; }
        public int Phase { get; set; }

        public static EffectParticle Create(VisualEffectKind kind, Random random)
        {
            EffectParticle particle = new();
            particle.Reset(kind, random);
            particle.Age = kind is VisualEffectKind.WeatherSnow or VisualEffectKind.WeatherEmber or VisualEffectKind.WeatherMotes or VisualEffectKind.StatusRegeneration or VisualEffectKind.StatusFrenzy or VisualEffectKind.StatusArmored or VisualEffectKind.StatusPoison
                ? random.Next(0, 1200)
                : 0;
            return particle;
        }

        public void Reset(VisualEffectKind kind, Random random)
        {
            X = random.NextDouble();
            Y = random.NextDouble();
            VelocityX = (random.NextDouble() - 0.5) * 0.08;
            VelocityY = kind switch
            {
                VisualEffectKind.WeatherSnow => 0.03 + random.NextDouble() * 0.06,
                VisualEffectKind.WeatherEmber => -0.04 - random.NextDouble() * 0.06,
                _ => (random.NextDouble() - 0.5) * 0.06
            };
            Size = 0.006 + random.NextDouble() * 0.014;
            Age = 0;
            Phase = random.Next(0, 360);
        }
    }
}

public static class VisualEffects
{
    public static ParticleEffectView PlayParticles(
        Layout host,
        VisualEffectKind kind,
        int durationMilliseconds = 850,
        int particleCount = 16,
        bool ambient = false)
    {
        ParticleEffectView effect = new();
        host.Children.Add(effect);
        effect.Start(kind, durationMilliseconds, particleCount, ambient);
        return effect;
    }

    public static async Task PulseAsync(VisualElement target, double scale = 1.06, uint duration = 140)
    {
        try
        {
            target.AbortAnimation("visualPulse");
            await target.ScaleToAsync(scale, duration, Easing.CubicOut);
            await target.ScaleToAsync(1, duration + 40, Easing.CubicIn);
        }
        catch
        {
            target.Scale = 1;
        }
    }

    public static async Task ShowFloatingTextAsync(
        AbsoluteLayout host,
        string text,
        Color color,
        double x = 0.5,
        double y = 0.4)
    {
        Label label = new()
        {
            Text = text,
            TextColor = color,
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
            WidthRequest = 150,
            HeightRequest = 30,
            Opacity = 0,
            InputTransparent = true
        };
        AbsoluteLayout.SetLayoutFlags(label, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.PositionProportional);
        AbsoluteLayout.SetLayoutBounds(label, new Rect(x, y, 150, 30));
        host.Children.Add(label);
        try
        {
            await Task.WhenAll(
                label.FadeToAsync(1, 90),
                label.TranslateToAsync(0, -18, 90, Easing.CubicOut));
            await Task.WhenAll(
                label.FadeToAsync(0, 420, Easing.CubicIn),
                label.TranslateToAsync(0, -48, 420, Easing.CubicIn));
        }
        finally
        {
            host.Children.Remove(label);
        }
    }
}
