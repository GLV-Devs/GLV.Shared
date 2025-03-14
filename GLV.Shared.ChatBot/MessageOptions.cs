namespace GLV.Shared.ChatBot;

public readonly record struct MessageOptions(bool Html, bool SendWithoutNotification)
{
    public static MessageOptions HtmlContent => new(true, false);
    public static MessageOptions NoNotification => new(false, true);
    public static MessageOptions HtmlNoNotification => new(true, true);
}
