namespace OSRSIdle;

public enum GoldSliceButtonVariant
{
    Neutral,
    Green,
    Red
}

/// <summary>
/// A reusable pixel-art button built from fixed gold end caps and repeating
/// middle slices, so its artwork remains crisp at any horizontal size.
/// </summary>
public sealed class GoldSliceButton : ContentView
{
    private const double SliceWidth = 32;
    private double _lastBuiltWidth = double.NaN;
    private double _lastBuiltHeight = double.NaN;
    private GoldSliceButtonVariant? _lastBuiltVariant;
    private readonly AbsoluteLayout _layout = new();
    private readonly Image _leftSlice;
    private readonly Image _middleSlice;
    private readonly Image _rightSlice;
    private readonly Grid _contentLayout;
    private readonly Image _icon;
    private readonly Label _label;

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(
            nameof(Text),
            typeof(string),
            typeof(GoldSliceButton),
            string.Empty,
            propertyChanged: (bindable, _, value) =>
            {
                GoldSliceButton button = (GoldSliceButton)bindable;
                button._label.Text = (string?)value ?? string.Empty;
                button.RefreshContentLayout();
            });

    public static readonly BindableProperty TextColorProperty =
        BindableProperty.Create(
            nameof(TextColor),
            typeof(Color),
            typeof(GoldSliceButton),
            Colors.White,
            propertyChanged: (bindable, _, value) =>
                ((GoldSliceButton)bindable)._label.TextColor = (Color)value);

    public static readonly BindableProperty FontSizeProperty =
        BindableProperty.Create(
            nameof(FontSize),
            typeof(double),
            typeof(GoldSliceButton),
            14d,
            propertyChanged: (bindable, _, value) =>
            {
                GoldSliceButton button = (GoldSliceButton)bindable;
                button._label.FontSize = (double)value;
                button.RefreshContentLayout();
            });

    public static readonly BindableProperty FontAttributesProperty =
        BindableProperty.Create(
            nameof(FontAttributes),
            typeof(FontAttributes),
            typeof(GoldSliceButton),
            FontAttributes.Bold,
            propertyChanged: (bindable, _, value) =>
            {
                GoldSliceButton button = (GoldSliceButton)bindable;
                button._label.FontAttributes = (FontAttributes)value;
                button.RefreshContentLayout();
            });

    public static readonly BindableProperty IconSourceProperty =
        BindableProperty.Create(
            nameof(IconSource),
            typeof(ImageSource),
            typeof(GoldSliceButton),
            default(ImageSource),
            propertyChanged: (bindable, _, value) =>
            {
                GoldSliceButton button = (GoldSliceButton)bindable;
                button._icon.Source = (ImageSource?)value;
                button.UpdateContentLayout();
            });

    public static readonly BindableProperty IconSizeProperty =
        BindableProperty.Create(
            nameof(IconSize),
            typeof(double),
            typeof(GoldSliceButton),
            24d,
            propertyChanged: (bindable, _, value) =>
            {
                GoldSliceButton button = (GoldSliceButton)bindable;
                button._icon.WidthRequest = (double)value;
                button._icon.HeightRequest = (double)value;
                button.RefreshContentLayout();
            });

    public static readonly BindableProperty CenterTextProperty =
        BindableProperty.Create(
            nameof(CenterText),
            typeof(bool),
            typeof(GoldSliceButton),
            false,
            propertyChanged: (bindable, _, _) =>
                ((GoldSliceButton)bindable).UpdateContentLayout());

    public static readonly BindableProperty VariantProperty =
        BindableProperty.Create(
            nameof(Variant),
            typeof(GoldSliceButtonVariant),
            typeof(GoldSliceButton),
            GoldSliceButtonVariant.Neutral,
            propertyChanged: (bindable, _, _) => ((GoldSliceButton)bindable).BuildSlices());

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public Color TextColor
    {
        get => (Color)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public FontAttributes FontAttributes
    {
        get => (FontAttributes)GetValue(FontAttributesProperty);
        set => SetValue(FontAttributesProperty, value);
    }

    public ImageSource? IconSource
    {
        get => (ImageSource?)GetValue(IconSourceProperty);
        set => SetValue(IconSourceProperty, value);
    }

    public double IconSize
    {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    /// <summary>
    /// Centers the text across the whole button while retaining an optional
    /// icon at the leading edge.
    /// </summary>
    public bool CenterText
    {
        get => (bool)GetValue(CenterTextProperty);
        set => SetValue(CenterTextProperty, value);
    }

    public GoldSliceButtonVariant Variant
    {
        get => (GoldSliceButtonVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public event EventHandler? Clicked;

    public GoldSliceButton()
    {
        HeightRequest = UiLayoutMetrics.ActionButtonHeight;
        MinimumHeightRequest = SliceWidth;
        MinimumWidthRequest = SliceWidth * 2;

        // Keep the artwork views alive for the entire control lifetime.
        // Replacing a live MAUI visual tree while a button is tapped can leave
        // the newly selected (green) button without a measured touch surface.
        _leftSlice = CreateSlice();
        _middleSlice = CreateSlice();
        _rightSlice = CreateSlice();

        _icon = new Image
        {
            WidthRequest = IconSize,
            HeightRequest = IconSize,
            Aspect = Aspect.AspectFit,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Center,
            InputTransparent = true
        };

        _label = new Label
        {
            FontAttributes = FontAttributes,
            TextColor = TextColor,
            FontSize = FontSize,
            HorizontalOptions = LayoutOptions.Fill,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            InputTransparent = true
        };

        _contentLayout = new Grid
        {
            ColumnSpacing = 5,
            Padding = new Thickness(8, 0),
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            InputTransparent = true
        };

        _contentLayout.Children.Add(_icon);
        _contentLayout.Children.Add(_label);
        UpdateContentLayout();

        _layout.Children.Add(_leftSlice);
        _layout.Children.Add(_middleSlice);
        _layout.Children.Add(_rightSlice);
        _layout.Children.Add(_contentLayout);

        Content = _layout;

        TapGestureRecognizer tapGesture = new();
        tapGesture.Tapped += (sender, e) =>
        {
            if (IsEnabled)
                Clicked?.Invoke(this, EventArgs.Empty);
        };

        GestureRecognizers.Add(tapGesture);

        SizeChanged += (sender, e) => BuildSlices();
        Loaded += (sender, e) => BuildSlices();
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == IsEnabledProperty.PropertyName)
            Opacity = IsEnabled ? 1 : 0.45;
    }

    protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
    {
        Size labelSize = _label.Measure(double.PositiveInfinity, heightConstraint);
        double width = labelSize.Width + _contentLayout.Padding.HorizontalThickness;

        if (_icon.IsVisible)
            width += _icon.WidthRequest + _contentLayout.ColumnSpacing;

        return new Size(
            Math.Max(MinimumWidthRequest, width),
            Math.Max(MinimumHeightRequest, HeightRequest));
    }

    private void BuildSlices()
    {
        double width = Math.Max(MinimumWidthRequest, Width);
        double height = Math.Max(SliceWidth, Height);

        bool slicesChanged =
            Math.Abs(width - _lastBuiltWidth) >= 0.5 ||
            Math.Abs(height - _lastBuiltHeight) >= 0.5 ||
            _lastBuiltVariant != Variant;

        if (slicesChanged)
        {
            _lastBuiltWidth = width;
            _lastBuiltHeight = height;
            _lastBuiltVariant = Variant;

            string assetPrefix = Variant switch
            {
                GoldSliceButtonVariant.Green => "button_green",
                GoldSliceButtonVariant.Red => "button_red",
                _ => "button_gold"
            };

            _leftSlice.Source = ImageSource.FromFile(
                $"{assetPrefix}_left.png");
            _middleSlice.Source = ImageSource.FromFile(
                $"{assetPrefix}_middle.png");
            _rightSlice.Source = ImageSource.FromFile(
                $"{assetPrefix}_right.png");
        }

        // Reapply the bounds even when the artwork itself is cached. Dynamic
        // text updates can trigger a measure/arrange pass without a size
        // change, and the content layer must still fill the current button.
        AbsoluteLayout.SetLayoutBounds(
            _leftSlice,
            new Rect(0, 0, SliceWidth, height));
        AbsoluteLayout.SetLayoutBounds(
            _middleSlice,
            new Rect(
                SliceWidth,
                0,
                Math.Max(0, width - (SliceWidth * 2)),
                height));
        AbsoluteLayout.SetLayoutBounds(
            _rightSlice,
            new Rect(width - SliceWidth, 0, SliceWidth, height));

        AbsoluteLayout.SetLayoutBounds(
            _contentLayout,
            new Rect(0, 0, width, height));
    }

    private void UpdateContentLayout()
    {
        bool hasIcon = IconSource != null;

        _icon.IsVisible = hasIcon;
        _contentLayout.ColumnDefinitions.Clear();

        if (hasIcon && CenterText)
        {
            _contentLayout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            Grid.SetColumn(_icon, 0);
            Grid.SetColumn(_label, 0);
            _icon.HorizontalOptions = LayoutOptions.Start;
            _label.HorizontalOptions = LayoutOptions.Fill;
            _label.HorizontalTextAlignment = TextAlignment.Center;
        }
        else if (hasIcon)
        {
            _contentLayout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            _contentLayout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            Grid.SetColumn(_icon, 0);
            Grid.SetColumn(_label, 1);
            _icon.HorizontalOptions = LayoutOptions.End;
            _label.HorizontalOptions = LayoutOptions.Fill;
            _label.HorizontalTextAlignment = TextAlignment.Start;
        }
        else
        {
            _contentLayout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            Grid.SetColumn(_icon, 0);
            Grid.SetColumn(_label, 0);
            _label.HorizontalOptions = LayoutOptions.Fill;
            _label.HorizontalTextAlignment = TextAlignment.Center;
        }

        _contentLayout.HorizontalOptions = LayoutOptions.Fill;
        _contentLayout.VerticalOptions = LayoutOptions.Fill;
        _label.VerticalOptions = LayoutOptions.Fill;
    }

    private void RefreshContentLayout()
    {
        UpdateContentLayout();
        InvalidateMeasure();
        BuildSlices();
    }

    private static Image CreateSlice()
    {
        return new Image
        {
            Aspect = Aspect.Fill,
            InputTransparent = true
        };
    }
}
