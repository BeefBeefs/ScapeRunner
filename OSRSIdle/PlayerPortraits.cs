namespace OSRSIdle;

public static class PlayerPortraits
{
    public static readonly IReadOnlyList<string> Images = new[]
    {
        "portrait.png",
        "character_portrait_01.png",
        "character_portrait_02.png",
        "character_portrait_03.png",
        "character_portrait_04.png",
        "character_portrait_05.png",
        "character_portrait_06.png",
        "character_portrait_07.png",
        "character_portrait_08.png",
        "character_portrait_09.png",
        "character_portrait_10.png",
        "character_portrait_11.png",
        "character_portrait_12.png",
        "character_portrait_13.png",
        "character_portrait_14.png",
        "character_portrait_15.png",
        "character_portrait_16.png",
        "character_portrait_17.png",
        "character_portrait_18.png",
        "character_portrait_19.png",
        "character_portrait_20.png",
        "character_portrait_21.png",
        "character_portrait_22.png",
        "character_portrait_23.png",
        "character_portrait_24.png",
        "character_portrait_25.png"
    };

    public static int NormalizeIndex(int index)
    {
        int count = Images.Count;

        return count == 0
            ? 0
            : ((index % count) + count) % count;
    }

    public static string GetImage(int index) =>
        Images[NormalizeIndex(index)];
}
