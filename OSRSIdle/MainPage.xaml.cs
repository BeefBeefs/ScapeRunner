namespace OSRSIdle;

public partial class MainPage : ContentPage
{
    private readonly Game game;

    public MainPage()
    {
        InitializeComponent();

        game = ((App?)Application.Current)?.Game
            ?? throw new InvalidOperationException("Application is not initialized.");

        BuildSkillList();
    }

    private void BuildSkillList()
    {
        SkillList.Children.Clear();

        foreach (Skill skill in game.Player.Skills)
        {
            GoldSliceButton skillButton = new GoldSliceButton
            {
                Text = $"{skill.Icon}  {skill.Name}",
                FontSize = 22,
                HeightRequest = 70,
                HorizontalOptions = LayoutOptions.Fill,
                Variant = GoldSliceButtonVariant.Neutral
            };

            skillButton.Clicked += (sender, args) =>
            {
                OpenSkill(skill);
            };

            SkillList.Children.Add(skillButton);
        }
    }

    private void OpenSkill(Skill skill)
    {
        GamePage? gamePage = FindParentGamePage();

        gamePage?.ShowSkillPage(skill);
    }

    private GamePage? FindParentGamePage()
    {
        Element? current = Parent;

        while (current != null)
        {
            if (current is GamePage gamePage)
                return gamePage;

            current = current.Parent;
        }

        return null;
    }
}
