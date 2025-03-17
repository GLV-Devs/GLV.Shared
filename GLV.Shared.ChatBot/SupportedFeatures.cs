namespace GLV.Shared.ChatBot;

public sealed class SupportedFeatures
{
    public required bool InlineKeyboards { get; init; }
    public required bool MessageAttachments { get; init; }
    public required bool ImageMessageAttachment { get; init; }
    public required bool AudioMessageAttachment { get; init; }
    public required bool VideoMessageAttachment { get; init; }
    public required bool SpoilerAttachments { get; init; }
    public required bool AttachmentTitles { get; init; }
    public required bool ThumbnailAttachments { get; init; }
    public required bool AttachmentDuration { get; init; }
    public required bool MultipleAttachments { get; init; }
    public required bool AttachmentDescriptions { get; init; }
    public required bool SendWithoutNotification { get; init; }
    public required bool ProtectMediaContent { get; init; }
    public required bool HtmlText { get; init; }
    public required bool UserInfoInMessage { get; init; }
    public required bool ResponseMessages { get; init; }
}
