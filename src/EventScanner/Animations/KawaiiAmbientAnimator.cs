using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace EventScanner.Animations;

/// <summary>
/// Pastel, gentle decorations for the background — hearts, sparkles, puffy blobs,
/// clouds, and tiny "friends". Stays behind the UI, low opacity, slow motion.
/// </summary>
public sealed class KawaiiAmbientAnimator
{
    private const int MaxCanvasChildren = 48;

    private readonly Canvas _canvas;
    private readonly Random _random = new();

    private readonly DispatcherTimer _heartTimer;
    private readonly DispatcherTimer _sparkleTimer;
    private readonly DispatcherTimer _blobTimer;
    private readonly DispatcherTimer _cloudTimer;
    private readonly DispatcherTimer _friendTimer;

    private static readonly Color[] PastelGlow =
    [
        Color.FromRgb(255, 183, 213),
        Color.FromRgb(199, 234, 255),
        Color.FromRgb(207, 250, 214),
        Color.FromRgb(255, 244, 184),
        Color.FromRgb(229, 212, 255),
        Color.FromRgb(255, 214, 231),
    ];

    private static readonly string[] HeartGlyphs = ["♡", "💗", "💕", "🌸", "💮", "🍑"];
    private static readonly string[] SparkleGlyphs = ["✧", "✦", "·", "✨", "⭐", "˚", "‧"],
        FriendGlyphs = ["🐰", "🐱", "🧸", "🍓", "🎀", "🦆", "💫", "🍡"];

    public KawaiiAmbientAnimator(Canvas canvas)
    {
        _canvas = canvas;

        _heartTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.6) };
        _heartTimer.Tick += (_, _) => SpawnHeart();

        _sparkleTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.4) };
        _sparkleTimer.Tick += (_, _) => SpawnSparkle();

        _blobTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5.5) };
        _blobTimer.Tick += (_, _) => SpawnBlob();

        _cloudTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(11) };
        _cloudTimer.Tick += (_, _) => SpawnCloud();

        _friendTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(18) };
        _friendTimer.Tick += (_, _) => SpawnFriend();
    }

    public void Start()
    {
        for (int i = 0; i < 4; i++)
        {
            _canvas.Dispatcher.BeginInvoke(() => SpawnSparkle(), DispatcherPriority.Background);
            _canvas.Dispatcher.BeginInvoke(() => SpawnHeart(), DispatcherPriority.Background);
        }

        _canvas.Dispatcher.BeginInvoke(() => SpawnBlob(), DispatcherPriority.Background);
        _canvas.Dispatcher.BeginInvoke(() => SpawnCloud(), DispatcherPriority.Background);

        _heartTimer.Start();
        _sparkleTimer.Start();
        _blobTimer.Start();
        _cloudTimer.Start();
        _friendTimer.Start();
    }

    public void Stop()
    {
        _heartTimer.Stop();
        _sparkleTimer.Stop();
        _blobTimer.Stop();
        _cloudTimer.Stop();
        _friendTimer.Stop();
    }

    private bool CanSpawn() =>
        _canvas.ActualWidth > 10
        && _canvas.ActualHeight > 10
        && _canvas.Children.Count < MaxCanvasChildren;

    private void SpawnHeart()
    {
        if (!CanSpawn())
            return;

        var glow = PastelGlow[_random.Next(PastelGlow.Length)];
        var glyph = HeartGlyphs[_random.Next(HeartGlyphs.Length)];
        var fontSize = 12 + _random.Next(14);
        var x0 = _random.Next(0, Math.Max(10, (int)_canvas.ActualWidth - 20));
        var y0 = _canvas.ActualHeight + 20 + _random.Next(40);
        var rise = 120 + _random.Next(180);
        var duration = TimeSpan.FromSeconds(16 + _random.NextDouble() * 12);
        var sway = 25 + _random.Next(45);
        var maxOpacity = 0.1 + _random.NextDouble() * 0.1;

        var tb = new TextBlock
        {
            Text = glyph,
            FontSize = fontSize,
            Opacity = 0,
            IsHitTestVisible = false,
            RenderTransformOrigin = new Point(0.5, 0.5),
            Effect = new DropShadowEffect
            {
                Color = glow,
                BlurRadius = 18,
                ShadowDepth = 0,
                Opacity = 0.55,
            },
        };

        _canvas.Children.Add(tb);
        Canvas.SetLeft(tb, x0);
        Canvas.SetTop(tb, y0);

        AnimateRiseAndFade(tb, x0, y0, y0 - rise, sway, duration, maxOpacity);
    }

    private void SpawnSparkle()
    {
        if (!CanSpawn())
            return;

        var glow = PastelGlow[_random.Next(PastelGlow.Length)];
        var glyph = SparkleGlyphs[_random.Next(SparkleGlyphs.Length)];
        var fontSize = 9 + _random.Next(10);
        var x0 = _random.Next(0, Math.Max(10, (int)_canvas.ActualWidth - 10));
        var y0 = _random.Next(20, Math.Max(30, (int)_canvas.ActualHeight - 20));
        var duration = TimeSpan.FromSeconds(10 + _random.NextDouble() * 10);
        var drift = 40 + _random.Next(80);
        var maxOpacity = 0.12 + _random.NextDouble() * 0.12;

        var tb = new TextBlock
        {
            Text = glyph,
            FontSize = fontSize,
            Foreground = new SolidColorBrush(Color.FromArgb(200, glow.R, glow.G, glow.B)),
            Opacity = 0,
            IsHitTestVisible = false,
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = new RotateTransform(_random.Next(0, 360)),
        };

        _canvas.Children.Add(tb);
        Canvas.SetLeft(tb, x0);
        Canvas.SetTop(tb, y0);

        bool goRight = _random.Next(2) == 0;
        var x1 = goRight ? x0 + drift : x0 - drift;
        var y1 = y0 - 30 - _random.Next(60);

        var moveX = new DoubleAnimation(x0, x1, duration);
        var moveY = new DoubleAnimation(y0, y1, duration);
        var fadeIn = new DoubleAnimation(0, maxOpacity, TimeSpan.FromSeconds(1.2));
        var fadeOut = new DoubleAnimation
        {
            From = maxOpacity,
            To = 0,
            Duration = TimeSpan.FromSeconds(1.4),
            BeginTime = duration - TimeSpan.FromSeconds(1.4),
        };

        var pulse = new DoubleAnimation(1, 1.15, duration)
        {
            AutoReverse = true,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
        };

        moveX.Completed += (_, _) => _canvas.Children.Remove(tb);

        tb.BeginAnimation(Canvas.LeftProperty, moveX);
        tb.BeginAnimation(Canvas.TopProperty, moveY);
        tb.BeginAnimation(UIElement.OpacityProperty, fadeIn);

        var fadeSb = new Storyboard();
        Storyboard.SetTarget(fadeOut, tb);
        Storyboard.SetTargetProperty(fadeOut, new PropertyPath(UIElement.OpacityProperty));
        fadeSb.Children.Add(fadeOut);
        fadeSb.Begin();

        var scale = new ScaleTransform(1, 1);
        tb.RenderTransform = new TransformGroup
        {
            Children = [new RotateTransform(_random.Next(0, 360)), scale],
        };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, pulse);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, pulse);
    }

    private void SpawnBlob()
    {
        if (!CanSpawn())
            return;

        var c = PastelGlow[_random.Next(PastelGlow.Length)];
        var size = 18 + _random.Next(36);
        var x = _random.Next(0, Math.Max(5, (int)_canvas.ActualWidth - size));
        var y = _canvas.ActualHeight + size;

        var ellipse = new Ellipse
        {
            Width = size,
            Height = size * (0.85 + _random.NextDouble() * 0.2),
            Opacity = 0,
            IsHitTestVisible = false,
            Effect = new BlurEffect { Radius = 8 },
            Fill = new RadialGradientBrush
            {
                GradientOrigin = new Point(0.35, 0.35),
                Center = new Point(0.5, 0.5),
                RadiusX = 0.55,
                RadiusY = 0.55,
                GradientStops =
                [
                    new GradientStop(Color.FromArgb(180, c.R, c.G, c.B), 0),
                    new GradientStop(Color.FromArgb(30, c.R, c.G, c.B), 0.65),
                    new GradientStop(Color.FromArgb(0, c.R, c.G, c.B), 1),
                ],
            },
        };

        _canvas.Children.Add(ellipse);
        Canvas.SetLeft(ellipse, x);
        Canvas.SetTop(ellipse, y);

        var rise = 100 + _random.Next(140);
        var duration = TimeSpan.FromSeconds(20 + _random.NextDouble() * 14);
        var maxOp = 0.14 + _random.NextDouble() * 0.08;
        var sway = 18 + _random.Next(35);
        AnimateRiseAndFade(ellipse, x, y, y - rise, sway, duration, maxOp);
    }

    private void SpawnCloud()
    {
        if (!CanSpawn())
            return;

        var tb = new TextBlock
        {
            Text = "☁️",
            FontSize = 26 + _random.Next(22),
            Opacity = 0,
            IsHitTestVisible = false,
            Effect = new DropShadowEffect
            {
                Color = Color.FromRgb(230, 240, 255),
                BlurRadius = 24,
                ShadowDepth = 0,
                Opacity = 0.35,
            },
        };

        bool fromLeft = _random.Next(2) == 0;
        var y = 30 + _random.Next(Math.Max(1, (int)(_canvas.ActualHeight * 0.55)));
        var duration = TimeSpan.FromSeconds(28 + _random.NextDouble() * 16);

        _canvas.Children.Add(tb);
        double x0 = fromLeft ? -70 : _canvas.ActualWidth + 20;
        double x1 = fromLeft ? _canvas.ActualWidth + 70 : -70;
        Canvas.SetLeft(tb, x0);
        Canvas.SetTop(tb, y);

        var moveX = new DoubleAnimation(x0, x1, duration);
        var bob = new DoubleAnimationUsingKeyFrames { Duration = duration };
        int steps = 8;
        for (int i = 0; i <= steps; i++)
        {
            var t = (double)i / steps;
            bob.KeyFrames.Add(new SplineDoubleKeyFrame(
                y + Math.Sin(t * Math.PI * 3) * 12,
                KeyTime.FromPercent(t),
                new KeySpline(0.45, 0, 0.55, 1)));
        }

        var fadeIn = new DoubleAnimation(0, 0.16, TimeSpan.FromSeconds(3));
        var fadeOut = new DoubleAnimation
        {
            From = 0.16,
            To = 0,
            Duration = TimeSpan.FromSeconds(3),
            BeginTime = duration - TimeSpan.FromSeconds(3),
        };

        moveX.Completed += (_, _) => _canvas.Children.Remove(tb);
        tb.BeginAnimation(Canvas.LeftProperty, moveX);
        tb.BeginAnimation(Canvas.TopProperty, bob);
        tb.BeginAnimation(UIElement.OpacityProperty, fadeIn);

        var sb = new Storyboard();
        Storyboard.SetTarget(fadeOut, tb);
        Storyboard.SetTargetProperty(fadeOut, new PropertyPath(UIElement.OpacityProperty));
        sb.Children.Add(fadeOut);
        sb.Begin();
    }

    private void SpawnFriend()
    {
        if (!CanSpawn())
            return;

        var glyph = FriendGlyphs[_random.Next(FriendGlyphs.Length)];
        var fontSize = 22 + _random.Next(14);
        var glow = PastelGlow[_random.Next(PastelGlow.Length)];
        var startY = _random.Next(30, Math.Max(40, (int)(_canvas.ActualHeight * 0.5)));
        var duration = TimeSpan.FromSeconds(12 + _random.NextDouble() * 8);
        var leftToRight = _random.Next(2) == 0;

        var tb = new TextBlock
        {
            Text = glyph,
            FontSize = fontSize,
            Opacity = 0,
            IsHitTestVisible = false,
            RenderTransform = new ScaleTransform(leftToRight ? 1 : -1, 1),
            RenderTransformOrigin = new Point(0.5, 0.5),
            Effect = new DropShadowEffect
            {
                Color = glow,
                BlurRadius = 20,
                ShadowDepth = 0,
                Opacity = 0.45,
            },
        };

        _canvas.Children.Add(tb);
        double x0 = leftToRight ? -50 : _canvas.ActualWidth + 50;
        double x1 = leftToRight ? _canvas.ActualWidth + 50 : -50;
        Canvas.SetLeft(tb, x0);
        Canvas.SetTop(tb, startY);

        var moveX = new DoubleAnimation(x0, x1, duration);
        var bob = new DoubleAnimation
        {
            From = startY,
            To = startY - 25 - _random.Next(20),
            Duration = new Duration(duration),
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
        };

        var fadeIn = new DoubleAnimation(0, 0.2, TimeSpan.FromSeconds(1.5));
        var fadeOut = new DoubleAnimation
        {
            From = 0.2,
            To = 0,
            Duration = TimeSpan.FromSeconds(1.5),
            BeginTime = duration - TimeSpan.FromSeconds(1.5),
        };

        moveX.Completed += (_, _) => _canvas.Children.Remove(tb);
        tb.BeginAnimation(Canvas.LeftProperty, moveX);
        tb.BeginAnimation(Canvas.TopProperty, bob);
        tb.BeginAnimation(UIElement.OpacityProperty, fadeIn);

        var sb = new Storyboard();
        Storyboard.SetTarget(fadeOut, tb);
        Storyboard.SetTargetProperty(fadeOut, new PropertyPath(UIElement.OpacityProperty));
        sb.Children.Add(fadeOut);
        sb.Begin();
    }

    private void AnimateRiseAndFade(
        UIElement element,
        double x0,
        double y0,
        double y1,
        double swayAmplitude,
        TimeSpan duration,
        double maxOpacity)
    {
        var wobbleX = new DoubleAnimationUsingKeyFrames { Duration = duration };
        int steps = 10;
        for (int i = 0; i <= steps; i++)
        {
            var t = (double)i / steps;
            var x = x0 + Math.Sin(t * Math.PI * 2.5 + _random.NextDouble()) * swayAmplitude;
            wobbleX.KeyFrames.Add(new SplineDoubleKeyFrame(
                x,
                KeyTime.FromPercent(t),
                new KeySpline(0.42, 0, 0.58, 1)));
        }

        var riseY = new DoubleAnimation(y0, y1, duration);
        var fadeIn = new DoubleAnimation(0, maxOpacity, TimeSpan.FromSeconds(2));
        var fadeOut = new DoubleAnimation
        {
            From = maxOpacity,
            To = 0,
            Duration = TimeSpan.FromSeconds(2.2),
            BeginTime = duration - TimeSpan.FromSeconds(2.2),
        };

        wobbleX.Completed += (_, _) => _canvas.Children.Remove(element);
        element.BeginAnimation(Canvas.LeftProperty, wobbleX);
        element.BeginAnimation(Canvas.TopProperty, riseY);
        element.BeginAnimation(UIElement.OpacityProperty, fadeIn);

        var sb = new Storyboard();
        Storyboard.SetTarget(fadeOut, element);
        Storyboard.SetTargetProperty(fadeOut, new PropertyPath(UIElement.OpacityProperty));
        sb.Children.Add(fadeOut);
        sb.Begin();
    }
}
