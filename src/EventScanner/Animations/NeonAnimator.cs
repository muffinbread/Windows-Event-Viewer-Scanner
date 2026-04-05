using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace EventScanner.Animations;

/// <summary>
/// Spawns subtle ambient neon effects on a WPF Canvas — glowing butterflies
/// that drift gently and an occasional rainbow unicorn. Everything is kept
/// low-opacity and small so it stays firmly in the background without
/// distracting from the content.
/// </summary>
public sealed class NeonAnimator
{
    private readonly Canvas _canvas;
    private readonly Random _random = new();
    private readonly DispatcherTimer _butterflyTimer;
    private readonly DispatcherTimer _unicornTimer;

    private static readonly string[] NeonColors =
    [
        "#00FFFF", "#FF00FF", "#39FF14", "#FF6600",
        "#FFFF00", "#BF00FF", "#00FF88", "#FF1493"
    ];

    private static readonly Color[] RainbowColors =
    [
        Color.FromRgb(255, 0, 0),
        Color.FromRgb(255, 127, 0),
        Color.FromRgb(255, 255, 0),
        Color.FromRgb(0, 255, 0),
        Color.FromRgb(0, 255, 255),
        Color.FromRgb(0, 127, 255),
        Color.FromRgb(139, 0, 255),
        Color.FromRgb(255, 0, 255),
        Color.FromRgb(255, 0, 0),
    ];

    public NeonAnimator(Canvas canvas)
    {
        _canvas = canvas;

        _butterflyTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
        _butterflyTimer.Tick += (_, _) => SpawnButterfly();

        _unicornTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _unicornTimer.Tick += (_, _) => SpawnUnicorn();
    }

    public void Start()
    {
        for (int i = 0; i < 4; i++)
        {
            _canvas.Dispatcher.BeginInvoke(() => SpawnButterfly(),
                DispatcherPriority.Background);
        }

        _butterflyTimer.Start();
        _unicornTimer.Start();
    }

    public void Stop()
    {
        _butterflyTimer.Stop();
        _unicornTimer.Stop();
        _canvas.Children.Clear();
    }

    private void SpawnButterfly()
    {
        if (_canvas.ActualWidth < 10 || _canvas.ActualHeight < 10)
            return;

        // Leave room for kawaii ambient layer + unicorn trails (shared canvas).
        if (_canvas.Children.Count > 48)
            return;

        var colorHex = NeonColors[_random.Next(NeonColors.Length)];
        var color = (Color)ColorConverter.ConvertFromString(colorHex);
        var fontSize = 10 + _random.Next(10);
        var startY = _random.Next(20, Math.Max(21, (int)_canvas.ActualHeight - 20));
        var wobbleRange = 20 + _random.Next(50);
        var duration = TimeSpan.FromSeconds(14 + _random.NextDouble() * 14);

        var butterfly = new TextBlock
        {
            Text = "🦋",
            FontSize = fontSize,
            Opacity = 0,
            IsHitTestVisible = false,
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = new RotateTransform(_random.Next(-15, 15)),
            Effect = new DropShadowEffect
            {
                Color = color,
                BlurRadius = 15,
                ShadowDepth = 0,
                Opacity = 0.6
            }
        };

        _canvas.Children.Add(butterfly);
        Canvas.SetLeft(butterfly, -40);
        Canvas.SetTop(butterfly, startY);

        var maxOpacity = 0.15 + _random.NextDouble() * 0.1;

        var moveX = new DoubleAnimation
        {
            From = -40,
            To = _canvas.ActualWidth + 40,
            Duration = duration,
        };

        var wobbleY = new DoubleAnimationUsingKeyFrames { Duration = duration };
        int steps = 6 + _random.Next(4);
        for (int i = 0; i <= steps; i++)
        {
            var fraction = (double)i / steps;
            var y = startY + Math.Sin(fraction * Math.PI * (2 + _random.Next(3))) * wobbleRange;
            wobbleY.KeyFrames.Add(new SplineDoubleKeyFrame(
                y,
                KeyTime.FromPercent(fraction),
                new KeySpline(0.4, 0, 0.6, 1)));
        }

        var fadeIn = new DoubleAnimation(0, maxOpacity, TimeSpan.FromSeconds(2));
        var fadeOut = new DoubleAnimation
        {
            From = maxOpacity,
            To = 0,
            Duration = TimeSpan.FromSeconds(2),
            BeginTime = duration - TimeSpan.FromSeconds(2)
        };

        moveX.Completed += (_, _) => _canvas.Children.Remove(butterfly);

        butterfly.BeginAnimation(Canvas.LeftProperty, moveX);
        butterfly.BeginAnimation(Canvas.TopProperty, wobbleY);
        butterfly.BeginAnimation(UIElement.OpacityProperty, fadeIn);

        var fadeStory = new Storyboard();
        Storyboard.SetTarget(fadeOut, butterfly);
        Storyboard.SetTargetProperty(fadeOut, new PropertyPath(UIElement.OpacityProperty));
        fadeStory.Children.Add(fadeOut);
        fadeStory.Begin();
    }

    private void SpawnUnicorn()
    {
        if (_canvas.ActualWidth < 10 || _canvas.ActualHeight < 10)
            return;

        var startY = 40 + _random.Next(Math.Max(1, (int)(_canvas.ActualHeight * 0.4)));
        var flyDuration = TimeSpan.FromSeconds(7 + _random.NextDouble() * 4);
        bool leftToRight = _random.Next(2) == 0;

        var unicorn = new TextBlock
        {
            Text = "🦄",
            FontSize = 28 + _random.Next(12),
            Opacity = 0,
            IsHitTestVisible = false,
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = new ScaleTransform(leftToRight ? 1 : -1, 1)
        };

        _canvas.Children.Add(unicorn);

        double fromX = leftToRight ? -60 : _canvas.ActualWidth + 60;
        double toX = leftToRight ? _canvas.ActualWidth + 60 : -60;

        Canvas.SetLeft(unicorn, fromX);
        Canvas.SetTop(unicorn, startY);

        var moveX = new DoubleAnimation(fromX, toX, flyDuration);
        var arcY = new DoubleAnimation
        {
            From = startY,
            To = startY - 40 - _random.Next(30),
            Duration = new Duration(flyDuration),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
        };

        var fadeIn = new DoubleAnimation(0, 0.3, TimeSpan.FromSeconds(1.5));
        var fadeOut = new DoubleAnimation
        {
            From = 0.3,
            To = 0,
            Duration = TimeSpan.FromSeconds(1.5),
            BeginTime = flyDuration - TimeSpan.FromSeconds(1.5)
        };

        unicorn.Effect = new DropShadowEffect
        {
            Color = RainbowColors[0],
            BlurRadius = 30,
            ShadowDepth = 0,
            Opacity = 0.7
        };

        var rainbowGlow = CreateRainbowGlowAnimation(flyDuration);

        moveX.Completed += (_, _) => _canvas.Children.Remove(unicorn);

        unicorn.BeginAnimation(Canvas.LeftProperty, moveX);
        unicorn.BeginAnimation(Canvas.TopProperty, arcY);
        unicorn.BeginAnimation(UIElement.OpacityProperty, fadeIn);

        var fadeStory = new Storyboard();
        Storyboard.SetTarget(fadeOut, unicorn);
        Storyboard.SetTargetProperty(fadeOut, new PropertyPath(UIElement.OpacityProperty));
        fadeStory.Children.Add(fadeOut);
        fadeStory.Begin();

        rainbowGlow.Begin(unicorn);

        SpawnSubtleTrail(fromX, toX, startY, flyDuration, leftToRight);
    }

    private void SpawnSubtleTrail(double fromX, double toX, double startY,
        TimeSpan flyDuration, bool leftToRight)
    {
        int trailCount = 5;
        for (int i = 0; i < trailCount; i++)
        {
            var delay = TimeSpan.FromMilliseconds(i * 250);
            var color = RainbowColors[i % RainbowColors.Length];

            var spark = new TextBlock
            {
                Text = "·",
                FontSize = 14,
                Opacity = 0,
                IsHitTestVisible = false,
                Foreground = new SolidColorBrush(color),
                Effect = new DropShadowEffect
                {
                    Color = color,
                    BlurRadius = 10,
                    ShadowDepth = 0,
                    Opacity = 0.5
                }
            };

            _canvas.Children.Add(spark);
            Canvas.SetLeft(spark, fromX);
            Canvas.SetTop(spark, startY + _random.Next(-10, 10));

            var moveSparkX = new DoubleAnimation(fromX, toX, flyDuration + TimeSpan.FromMilliseconds(300))
            {
                BeginTime = delay
            };
            var sparkFade = new DoubleAnimation
            {
                From = 0,
                To = 0.15,
                Duration = TimeSpan.FromSeconds(0.8),
                BeginTime = delay,
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(4)
            };

            moveSparkX.Completed += (_, _) => _canvas.Children.Remove(spark);

            spark.BeginAnimation(Canvas.LeftProperty, moveSparkX);
            spark.BeginAnimation(UIElement.OpacityProperty, sparkFade);
        }
    }

    private Storyboard CreateRainbowGlowAnimation(TimeSpan totalDuration)
    {
        var storyboard = new Storyboard();
        var colorAnim = new ColorAnimationUsingKeyFrames
        {
            Duration = new Duration(TimeSpan.FromSeconds(4)),
            RepeatBehavior = RepeatBehavior.Forever
        };

        for (int i = 0; i < RainbowColors.Length; i++)
        {
            colorAnim.KeyFrames.Add(new LinearColorKeyFrame(
                RainbowColors[i],
                KeyTime.FromPercent((double)i / (RainbowColors.Length - 1))));
        }

        Storyboard.SetTargetProperty(colorAnim,
            new PropertyPath("(UIElement.Effect).(DropShadowEffect.Color)"));
        storyboard.Children.Add(colorAnim);

        return storyboard;
    }

    /// <summary>
    /// Creates a slow RGB wave border brush — a LinearGradientBrush whose
    /// StartPoint animates to create a flowing color wave effect.
    /// Attach the returned brush to a Border.BorderBrush.
    /// Call the returned Action to start the animation.
    /// </summary>
    public static (LinearGradientBrush brush, Action start) CreateWaveBorderBrush()
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0.5),
            EndPoint = new Point(2, 0.5),
            SpreadMethod = GradientSpreadMethod.Repeat,
            MappingMode = BrushMappingMode.RelativeToBoundingBox,
        };

        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 255, 255), 0.0));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 127, 255), 0.15));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(139, 0, 255), 0.3));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 0, 255), 0.45));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 0, 127), 0.6));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 127, 0), 0.75));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 255, 128), 0.9));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 255, 255), 1.0));

        var transform = new TranslateTransform(0, 0);
        brush.RelativeTransform = transform;

        Action start = () =>
        {
            var waveAnim = new DoubleAnimation
            {
                From = 0,
                To = -1,
                Duration = TimeSpan.FromSeconds(8),
                RepeatBehavior = RepeatBehavior.Forever
            };
            transform.BeginAnimation(TranslateTransform.XProperty, waveAnim);
        };

        return (brush, start);
    }
}
