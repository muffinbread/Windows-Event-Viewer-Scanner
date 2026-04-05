using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Animation;
using EventScanner.Animations;
using EventScanner.ViewModels;
using Wpf.Ui.Controls;

namespace EventScanner;

public partial class MainWindow : FluentWindow
{
    private readonly DashboardViewModel _viewModel;
    private NeonAnimator? _neonAnimator;
    private KawaiiAmbientAnimator? _kawaiiAnimator;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new DashboardViewModel();
        DataContext = _viewModel;

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        Loaded += OnWindowLoaded;
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        _neonAnimator = new NeonAnimator(AnimationCanvas);
        _neonAnimator.Start();

        _kawaiiAnimator = new KawaiiAmbientAnimator(AnimationCanvas);
        _kawaiiAnimator.Start();

        ApplyWaveBorders();
    }

    private void ApplyWaveBorders()
    {
        var (gradeBrush, startGradeWave) = NeonAnimator.CreateWaveBorderBrush();
        GradeSection.BorderBrush = gradeBrush;
        startGradeWave();

        var (statusBrush, startStatusWave) = NeonAnimator.CreateWaveBorderBrush();
        StatusBar.BorderBrush = statusBrush;
        startStatusWave();

        var (selectionBrush, startSelectionWave) = NeonAnimator.CreateWaveBorderBrush();
        Resources["WaveSelectionBrush"] = selectionBrush;
        startSelectionWave();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DashboardViewModel.HasScanned) && _viewModel.HasScanned)
        {
            PlayGradeEntranceAnimation();
        }
    }

    private void PlayGradeEntranceAnimation()
    {
        var storyboard = new Storyboard();

        var scaleX = new DoubleAnimation
        {
            From = 0.4,
            To = 1.0,
            Duration = TimeSpan.FromSeconds(0.8),
            EasingFunction = new ElasticEase
            {
                Oscillations = 1,
                Springiness = 4,
                EasingMode = EasingMode.EaseOut
            }
        };
        Storyboard.SetTarget(scaleX, GradeSection);
        Storyboard.SetTargetProperty(scaleX,
            new PropertyPath("RenderTransform.ScaleX"));

        var scaleY = new DoubleAnimation
        {
            From = 0.4,
            To = 1.0,
            Duration = TimeSpan.FromSeconds(0.8),
            EasingFunction = new ElasticEase
            {
                Oscillations = 1,
                Springiness = 4,
                EasingMode = EasingMode.EaseOut
            }
        };
        Storyboard.SetTarget(scaleY, GradeSection);
        Storyboard.SetTargetProperty(scaleY,
            new PropertyPath("RenderTransform.ScaleY"));

        var fadeIn = new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = TimeSpan.FromSeconds(0.5),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(fadeIn, GradeSection);
        Storyboard.SetTargetProperty(fadeIn, new PropertyPath("Opacity"));

        storyboard.Children.Add(scaleX);
        storyboard.Children.Add(scaleY);
        storyboard.Children.Add(fadeIn);

        storyboard.Begin();
    }
}
