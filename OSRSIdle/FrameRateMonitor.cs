using System.Diagnostics;

#if ANDROID
using Android.Views;
#elif IOS || MACCATALYST
using CoreAnimation;
using Foundation;
#elif WINDOWS
using Microsoft.UI.Xaml.Media;
#endif

namespace OSRSIdle;

/// <summary>
/// Reports frames delivered by the platform compositor rather than counting
/// arbitrary dispatcher-timer callbacks.
/// </summary>
public sealed class FrameRateMonitor : IDisposable
{
    private const double ReportWindowSeconds = 0.5d;

    private readonly Action<double> _report;
    private long _windowStartedTimestamp;
    private int _frameCount;
    private bool _running;
    private bool _disposed;

#if ANDROID
    private AndroidFrameCallback? _androidCallback;
#elif IOS || MACCATALYST
    private CADisplayLink? _displayLink;
    private bool _displayLinkAddedToRunLoop;
#endif

    public FrameRateMonitor(Action<double> report)
    {
        _report = report;
    }

    public void Start()
    {
        if (_disposed || _running)
            return;

        _running = true;
        _frameCount = 0;
        _windowStartedTimestamp = Stopwatch.GetTimestamp();

#if ANDROID
        _androidCallback ??= new AndroidFrameCallback(this);
        Choreographer.Instance.PostFrameCallback(_androidCallback);
#elif IOS || MACCATALYST
        _displayLink ??= CADisplayLink.Create(OnDisplayLinkFrame);
        if (!_displayLinkAddedToRunLoop)
        {
            _displayLink.AddToRunLoop(NSRunLoop.Main, NSRunLoopMode.Common);
            _displayLinkAddedToRunLoop = true;
        }
        _displayLink.Paused = false;
#elif WINDOWS
        CompositionTarget.Rendering += OnCompositionTargetRendering;
#endif
    }

    public void Stop()
    {
        if (!_running)
            return;

        _running = false;

#if ANDROID
        if (_androidCallback != null)
            Choreographer.Instance.RemoveFrameCallback(_androidCallback);
#elif IOS || MACCATALYST
        if (_displayLink != null)
            _displayLink.Paused = true;
#elif WINDOWS
        CompositionTarget.Rendering -= OnCompositionTargetRendering;
#endif
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Stop();

#if IOS || MACCATALYST
        _displayLink?.Invalidate();
        _displayLink?.Dispose();
        _displayLink = null;
        _displayLinkAddedToRunLoop = false;
#endif

        _disposed = true;
    }

    private void OnFrame()
    {
        if (!_running)
            return;

        _frameCount++;

        long now = Stopwatch.GetTimestamp();
        double elapsedSeconds =
            (now - _windowStartedTimestamp) /
            (double)Stopwatch.Frequency;

        if (elapsedSeconds < ReportWindowSeconds)
            return;

        double framesPerSecond = _frameCount / elapsedSeconds;
        _frameCount = 0;
        _windowStartedTimestamp = now;
        _report(framesPerSecond);
    }

#if ANDROID
    private sealed class AndroidFrameCallback : Java.Lang.Object, Choreographer.IFrameCallback
    {
        private readonly FrameRateMonitor _owner;

        public AndroidFrameCallback(FrameRateMonitor owner)
        {
            _owner = owner;
        }

        public void DoFrame(long frameTimeNanos)
        {
            _owner.OnFrame();

            if (_owner._running)
                Choreographer.Instance.PostFrameCallback(this);
        }
    }
#elif IOS || MACCATALYST
    private void OnDisplayLinkFrame()
    {
        OnFrame();
    }
#elif WINDOWS
    private void OnCompositionTargetRendering(
        object? sender,
        object args)
    {
        OnFrame();
    }
#endif
}
