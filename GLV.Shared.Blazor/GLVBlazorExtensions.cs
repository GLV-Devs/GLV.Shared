using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using System.Runtime.Intrinsics.X86;

namespace GLV.Shared.Blazor;

public static class GLVBlazorExtensions
{
    public static RenderFragment AsRenderFragment(string html)
        => b => b.AddContent(0, (MarkupString)html);

    public static RenderFragment AsRenderFragment<T>() where T : IComponent
        => b =>
        {
            b.OpenComponent<T>(0);
            b.CloseComponent();
        };
}
