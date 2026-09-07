using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OSRSIdle;

/// <summary>
/// Adds a crisp, one-pixel black text layer beneath every styled Label.
/// The two labels share one Grid cell so the shadow never changes layout.
/// </summary>
public static class LabelShadow
{
    public static readonly BindableProperty IsEnabledProperty =
        BindableProperty.CreateAttached(
            "IsEnabled",
            typeof(bool),
            typeof(LabelShadow),
            false,
            propertyChanged: OnIsEnabledChanged);

    private static readonly ConditionalWeakTable<Label, ShadowState> States = new();

    public static bool GetIsEnabled(BindableObject view) =>
        (bool)view.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(BindableObject view, bool value) =>
        view.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        if (bindable is not Label label)
            return;

        if ((bool)newValue)
        {
            ShadowState state = States.GetValue(
                label,
                static currentLabel => new ShadowState(currentLabel));
            state.TryAttach();
        }
        else if (States.TryGetValue(label, out ShadowState? state))
        {
            state.Detach();
            States.Remove(label);
        }
    }

    private sealed class ShadowState
    {
        private readonly Label _label;
        private Grid? _wrapper;
        private Label? _shadow;
        private Element? _parent;

        public ShadowState(Label label)
        {
            _label = label;
            _label.ParentChanged += OnParentChanged;
            _label.PropertyChanged += OnLabelPropertyChanged;
        }

        public void TryAttach()
        {
            if (_wrapper != null)
                return;

            if (_label.Parent is Layout layout)
            {
                int index = layout.Children.IndexOf(_label);
                if (index < 0)
                    return;

                Grid wrapper = CreateWrapper();
                Label shadow = CreateShadow();
                CopyParentLayoutProperties(_label, wrapper);

                _wrapper = wrapper;
                _shadow = shadow;
                _parent = layout;

                layout.Children.RemoveAt(index);
                layout.Children.Insert(index, wrapper);

                wrapper.Children.Add(shadow);
                wrapper.Children.Add(_label);

                _label.Margin = 0;
                SyncLabelProperties();
                return;
            }

            if (_label.Parent is ScrollView scrollView &&
                ReferenceEquals(scrollView.Content, _label))
            {
                Grid wrapper = CreateWrapper();
                Label shadow = CreateShadow();

                _wrapper = wrapper;
                _shadow = shadow;
                _parent = scrollView;

                scrollView.Content = wrapper;
                wrapper.Children.Add(shadow);
                wrapper.Children.Add(_label);

                _label.Margin = 0;
                SyncLabelProperties();
            }
        }

        public void Detach()
        {
            if (_wrapper == null || _shadow == null)
                return;

            if (_parent is Layout layout && layout.Children.Contains(_wrapper))
            {
                int index = layout.Children.IndexOf(_wrapper);
                _wrapper.Children.Remove(_shadow);
                _wrapper.Children.Remove(_label);
                layout.Children.Remove(_wrapper);
                layout.Children.Insert(index, _label);
            }
            else if (_parent is ScrollView scrollView &&
                     ReferenceEquals(scrollView.Content, _wrapper))
            {
                _wrapper.Children.Remove(_shadow);
                _wrapper.Children.Remove(_label);
                scrollView.Content = _label;
            }

            _label.Margin = _wrapper.Margin;
            _wrapper = null;
            _shadow = null;
            _parent = null;
        }

        private void OnParentChanged(object? sender, EventArgs e)
        {
            if (_wrapper == null && GetIsEnabled(_label))
                TryAttach();
        }

        private void OnLabelPropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            if (_wrapper == null || _shadow == null)
                return;

            // Text counters and animations change frequently. Updating one
            // property must not clone every formatted span and resync layout.
            switch (e.PropertyName)
            {
                case nameof(Label.Text): _shadow.Text = _label.Text; break;
                case nameof(Label.TextColor): break; // Shadow stays black.
                case nameof(Label.Opacity): _shadow.Opacity = _label.Opacity; break;
                case nameof(Label.TranslationX): _shadow.TranslationX = _label.TranslationX + 1; break;
                case nameof(Label.TranslationY): _shadow.TranslationY = _label.TranslationY + 1; break;
                case nameof(Label.Scale): _shadow.Scale = _label.Scale; break;
                case nameof(Label.ScaleX): _shadow.ScaleX = _label.ScaleX; break;
                case nameof(Label.ScaleY): _shadow.ScaleY = _label.ScaleY; break;
                case nameof(Label.Rotation): _shadow.Rotation = _label.Rotation; break;
                case nameof(Label.IsVisible):
                    _wrapper.IsVisible = _shadow.IsVisible = _label.IsVisible;
                    break;
                default: SyncLabelProperties(); break;
            }
        }

        private Grid CreateWrapper() =>
            new()
            {
                BackgroundColor = Colors.Transparent,
                HorizontalOptions = _label.HorizontalOptions,
                VerticalOptions = _label.VerticalOptions,
                Margin = _label.Margin,
                WidthRequest = _label.WidthRequest,
                HeightRequest = _label.HeightRequest,
                MinimumWidthRequest = _label.MinimumWidthRequest,
                MinimumHeightRequest = _label.MinimumHeightRequest,
                MaximumWidthRequest = _label.MaximumWidthRequest,
                MaximumHeightRequest = _label.MaximumHeightRequest,
                IsVisible = _label.IsVisible,
                ZIndex = _label.ZIndex
            };

        private static Label CreateShadow()
        {
            Label shadow = new()
            {
                InputTransparent = true,
                TextColor = Colors.Black,
                BackgroundColor = Colors.Transparent,
                TranslationX = 1,
                TranslationY = 1,
                ZIndex = 0
            };

            SetIsEnabled(shadow, false);
            return shadow;
        }

        private void SyncLabelProperties()
        {
            if (_wrapper == null || _shadow == null)
                return;

            _wrapper.IsVisible = _label.IsVisible;
            _wrapper.HorizontalOptions = _label.HorizontalOptions;
            _wrapper.VerticalOptions = _label.VerticalOptions;
            _wrapper.WidthRequest = _label.WidthRequest;
            _wrapper.HeightRequest = _label.HeightRequest;
            _wrapper.MinimumWidthRequest = _label.MinimumWidthRequest;
            _wrapper.MinimumHeightRequest = _label.MinimumHeightRequest;
            _wrapper.MaximumWidthRequest = _label.MaximumWidthRequest;
            _wrapper.MaximumHeightRequest = _label.MaximumHeightRequest;
            _wrapper.ZIndex = _label.ZIndex;

            _shadow.Text = _label.Text;
            _shadow.FormattedText = CloneFormattedText(_label.FormattedText);
            _shadow.FontFamily = _label.FontFamily;
            _shadow.FontSize = _label.FontSize;
            _shadow.FontAttributes = _label.FontAttributes;
            _shadow.CharacterSpacing = _label.CharacterSpacing;
            _shadow.LineBreakMode = _label.LineBreakMode;
            _shadow.MaxLines = _label.MaxLines;
            _shadow.TextDecorations = _label.TextDecorations;
            _shadow.TextTransform = _label.TextTransform;
            _shadow.LineHeight = _label.LineHeight;
            _shadow.HorizontalTextAlignment = _label.HorizontalTextAlignment;
            _shadow.VerticalTextAlignment = _label.VerticalTextAlignment;
            _shadow.Padding = _label.Padding;
            _shadow.FontAutoScalingEnabled = _label.FontAutoScalingEnabled;
            _shadow.IsVisible = _label.IsVisible;
            _shadow.Opacity = _label.Opacity;
            _shadow.TranslationX = _label.TranslationX + 1;
            _shadow.TranslationY = _label.TranslationY + 1;
            _shadow.Scale = _label.Scale;
            _shadow.ScaleX = _label.ScaleX;
            _shadow.ScaleY = _label.ScaleY;
            _shadow.Rotation = _label.Rotation;
            _shadow.RotationX = _label.RotationX;
            _shadow.RotationY = _label.RotationY;
            _shadow.AnchorX = _label.AnchorX;
            _shadow.AnchorY = _label.AnchorY;
            _shadow.ZIndex = _label.ZIndex;

            CopyParentLayoutProperties(_label, _wrapper);
        }

        private static void CopyParentLayoutProperties(
            Label source,
            Grid target)
        {
            Grid.SetRow(target, Grid.GetRow(source));
            Grid.SetColumn(target, Grid.GetColumn(source));
            Grid.SetRowSpan(target, Grid.GetRowSpan(source));
            Grid.SetColumnSpan(target, Grid.GetColumnSpan(source));

            AbsoluteLayout.SetLayoutBounds(
                target,
                AbsoluteLayout.GetLayoutBounds(source));
            AbsoluteLayout.SetLayoutFlags(
                target,
                AbsoluteLayout.GetLayoutFlags(source));

            FlexLayout.SetOrder(target, FlexLayout.GetOrder(source));
            FlexLayout.SetGrow(target, FlexLayout.GetGrow(source));
            FlexLayout.SetShrink(target, FlexLayout.GetShrink(source));
            FlexLayout.SetBasis(target, FlexLayout.GetBasis(source));
            FlexLayout.SetAlignSelf(target, FlexLayout.GetAlignSelf(source));
        }

        private static FormattedString? CloneFormattedText(
            FormattedString? source)
        {
            if (source == null)
                return null;

            FormattedString clone = new();
            foreach (Span span in source.Spans)
            {
                clone.Spans.Add(
                    new Span
                    {
                        Text = span.Text,
                        FontFamily = span.FontFamily,
                        FontSize = span.FontSize,
                        FontAttributes = span.FontAttributes,
                        CharacterSpacing = span.CharacterSpacing,
                        TextColor = Colors.Black,
                        TextDecorations = span.TextDecorations
                    });
            }

            return clone;
        }
    }
}
