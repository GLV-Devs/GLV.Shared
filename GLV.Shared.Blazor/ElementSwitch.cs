namespace GLV.Shared.Blazor;

public readonly record struct ElementSwitch(string WhenTrue, string WhenFalse, bool IsActive = true)
{
    public bool IsActive { get; } = IsActive;

    public ElementSwitch Toggle() => new(WhenTrue, WhenFalse, !IsActive);
    public ElementSwitch ToFalse() => new(WhenTrue, WhenFalse, false);
    public ElementSwitch ToTrue() => new(WhenTrue, WhenFalse, true);

    public string StateString => IsActive ? WhenTrue : WhenFalse;
    public string ReverseStateString => IsActive ? WhenFalse : WhenTrue;

    public static class ClassTag
    {
        public static ElementSwitch IsEnabled(bool startingValue = true) => new("", "disabled", startingValue);
    }

    public static class StyleTag
    {
        public static ElementSwitch IsVisible(bool startingValue = true) => new("display: revert-layer", "display: none", startingValue);
    }
}
