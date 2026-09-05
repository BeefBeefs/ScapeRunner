namespace OSRSIdle;

public static class GoldenFrame
{
    private const double EdgeSize = 7;

    public static Grid CreateFrame(
        View content,
        bool useLightVariant,
        Color fillColor,
        bool includeCenter,
        double? widthRequest = null,
        double? heightRequest = null)
    {
        string framePrefix = useLightVariant
            ? "golden_slot_light"
            : "golden_slot_dark";

        // Framed panels should hug their information.  MAUI's default
        // Fill alignment lets a spanning child turn the decorative centre
        // into a full-page area when the frame is placed in a ScrollView.
        content.VerticalOptions = LayoutOptions.Start;

        Grid frame = new Grid
        {
            BackgroundColor = fillColor,
            VerticalOptions = LayoutOptions.Start,
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition(EdgeSize),
                // The middle is content-sized. A star row inside a
                // ScrollView can consume all remaining page height.
                new RowDefinition(GridLength.Auto),
                new RowDefinition(EdgeSize)
            },
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(EdgeSize),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(EdgeSize)
            }
        };

        if (widthRequest.HasValue)
        {
            frame.WidthRequest = widthRequest.Value;
        }

        if (heightRequest.HasValue)
        {
            frame.HeightRequest = heightRequest.Value;
        }

        AddSlice(frame, framePrefix, "top_left", 0, 0);
        AddSlice(frame, framePrefix, "top", 0, 1);
        AddSlice(frame, framePrefix, "top_right", 0, 2);
        AddSlice(frame, framePrefix, "left", 1, 0);

        if (includeCenter)
        {
            AddSlice(frame, framePrefix, "center", 1, 1);
        }

        AddSlice(frame, framePrefix, "right", 1, 2);
        AddSlice(frame, framePrefix, "bottom_left", 2, 0);
        AddSlice(frame, framePrefix, "bottom", 2, 1);
        AddSlice(frame, framePrefix, "bottom_right", 2, 2);

        frame.Add(content, 0, 0);
        Grid.SetRowSpan(content, 3);
        Grid.SetColumnSpan(content, 3);

        return frame;
    }

    private static void AddSlice(
        Grid frame,
        string framePrefix,
        string sliceName,
        int row,
        int column)
    {
        frame.Add(
            new Image
            {
                Source = $"{framePrefix}_{sliceName}.png",
                Aspect = Aspect.Fill,
                InputTransparent = true
            },
            column,
            row);
    }
}
