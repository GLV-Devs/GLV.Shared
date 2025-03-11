using System.Diagnostics;

namespace GLV.Shared.ChatBot;

public class MessageAttachment : IDisposable
{
    public MessageAttachmentKind AttachmentKind { get; }

    private readonly Func<(Stream content, bool shouldDispose)>? _getContent;
    private Stream? _content;
    private bool dispose_content;

    public virtual Stream GetContent()
    {
        Debug.Assert(_content is not null || _getContent is not null);
        if (_content is not null)
            return _content;

        (_content, dispose_content) = _getContent!.Invoke();
        return _content is null ? throw new InvalidOperationException("Content Getter function returned a null stream") : _content;
    }

    public MessageAttachment(MessageAttachmentKind attachmentKind, Func<(Stream content, bool shouldDispose)> contentGetter)
    {
        AttachmentKind = attachmentKind;
        _getContent = contentGetter ?? throw new ArgumentNullException(nameof(contentGetter));
    }

    public MessageAttachment(MessageAttachmentKind attachmentKind, Stream content)
    {
        AttachmentKind = attachmentKind;
        _content = content ?? throw new ArgumentNullException(nameof(content));
    }

    public string? AttachmentTitle { get; set; }
    public string? Description { get; set; }
    public bool IsSpoiler { get; set; }
    public bool IsThumbnail { get; set; }
    public double? Duration { get; set; }
    public bool ProtectContent { get; set; }

    public static MessageAttachment CreateFileAttachment(
        string path,
        MessageAttachmentKind attachmentKind = MessageAttachmentKind.File,
        string? title = null
    )
        => new(attachmentKind, () => (File.OpenRead(path), true))
        {
            AttachmentTitle = Path.GetFileName(title)
        };

    protected virtual void Dispose(bool isDisposing) { }

    private void OnDispose(bool isDisposing)
    {
        Dispose(isDisposing);
        if (dispose_content)
            _content?.Dispose();
    }

    public bool IsDisposed { get; private set; }

    ~MessageAttachment()
    {
        OnDispose(false);
        IsDisposed = true;
    }

    public virtual void Dispose()
    {
        OnDispose(true);
        IsDisposed = true;
        GC.SuppressFinalize(this);
    }
}
