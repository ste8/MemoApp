using MemoApp.UI.MauiApp.ViewModels;

namespace MemoApp.UI.MauiApp.Views;

public partial class ImageViewerPage : ContentPage
{
    private double _currentScale = 1;
    private double _startScale = 1;
    private double _xOffset = 0;
    private double _yOffset = 0;
    private double _startX = 0;
    private double _startY = 0;

    public ImageViewerPage(ImageViewerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
    {
        if (e.Status == GestureStatus.Started)
        {
            _startScale = _currentScale;
        }
        else if (e.Status == GestureStatus.Running)
        {
            _currentScale = Math.Min(Math.Max(1, _startScale * e.Scale), 5);

            var renderedX = MainImage.X + _xOffset;
            var deltaX = renderedX / MainImage.Width;
            var deltaWidth = MainImage.Width / (MainImage.Width * _startScale);
            var originX = (e.ScaleOrigin.X - deltaX) * deltaWidth;

            var renderedY = MainImage.Y + _yOffset;
            var deltaY = renderedY / MainImage.Height;
            var deltaHeight = MainImage.Height / (MainImage.Height * _startScale);
            var originY = (e.ScaleOrigin.Y - deltaY) * deltaHeight;

            var targetX = _xOffset - (originX * MainImage.Width) * (_currentScale - _startScale);
            var targetY = _yOffset - (originY * MainImage.Height) * (_currentScale - _startScale);

            MainImage.TranslationX = Math.Min(0, Math.Max(targetX, -MainImage.Width * (_currentScale - 1)));
            MainImage.TranslationY = Math.Min(0, Math.Max(targetY, -MainImage.Height * (_currentScale - 1)));

            MainImage.Scale = _currentScale;
        }
        else if (e.Status == GestureStatus.Completed)
        {
            _xOffset = MainImage.TranslationX;
            _yOffset = MainImage.TranslationY;
        }
    }

    private void OnDoubleTapped(object sender, TappedEventArgs e)
    {
        if (_currentScale > 1)
        {
            // Reset zoom
            MainImage.Scale = 1;
            MainImage.TranslationX = 0;
            MainImage.TranslationY = 0;
            _currentScale = 1;
            _xOffset = 0;
            _yOffset = 0;
        }
        else
        {
            // Zoom to 2x at tap location
            var tapPosition = e.GetPosition(MainImage);
            if (tapPosition != null)
            {
                _currentScale = 2;
                MainImage.Scale = _currentScale;
                
                var targetX = -tapPosition.Value.X * (_currentScale - 1);
                var targetY = -tapPosition.Value.Y * (_currentScale - 1);
                
                MainImage.TranslationX = Math.Min(0, Math.Max(targetX, -MainImage.Width * (_currentScale - 1)));
                MainImage.TranslationY = Math.Min(0, Math.Max(targetY, -MainImage.Height * (_currentScale - 1)));
                
                _xOffset = MainImage.TranslationX;
                _yOffset = MainImage.TranslationY;
            }
        }
    }

    private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _startX = MainImage.TranslationX;
                _startY = MainImage.TranslationY;
                break;

            case GestureStatus.Running:
                if (_currentScale > 1)
                {
                    var maxTranslationX = (MainImage.Width * _currentScale - MainImage.Width) / 2;
                    var maxTranslationY = (MainImage.Height * _currentScale - MainImage.Height) / 2;

                    var newTranslationX = _startX + e.TotalX;
                    var newTranslationY = _startY + e.TotalY;

                    MainImage.TranslationX = Math.Min(maxTranslationX, Math.Max(-maxTranslationX, newTranslationX));
                    MainImage.TranslationY = Math.Min(maxTranslationY, Math.Max(-maxTranslationY, newTranslationY));
                }
                break;

            case GestureStatus.Completed:
                _xOffset = MainImage.TranslationX;
                _yOffset = MainImage.TranslationY;
                break;
        }
    }

    protected override bool OnBackButtonPressed()
    {
        if (BindingContext is ImageViewerViewModel viewModel)
        {
            viewModel.CloseCommand.Execute(null);
            return true;
        }
        return base.OnBackButtonPressed();
    }
}