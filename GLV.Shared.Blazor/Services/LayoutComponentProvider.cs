using GLV.Shared.Common;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLV.Shared.Blazor.Services;

public readonly struct NavBarEntry(string name, string href)
{
    public string Name { get; } = name;
    public string Href { get; } = href;
    public string? SpanClass { get; init; }
    public string? SpanStyle { get; init; }
    public string NavLinkClass { get; init; } = "nav-item px-3";
    public string AnchorClass { get; init; } = "nav-link";
    public string? ActiveAnchorClass { get; init; } 
    public NavLinkMatch Match { get; init; } = NavLinkMatch.All;
}

public abstract class LayoutComponentProvider
{
    internal Action? NavMenuStateHasChangedCallback;
    // We don't want it to be an event.
    // The lifetime of UI elements is uncertain at best, and now deconstructor-esque overridable method is available in blazor.
    // So we just replace this callback with whichever one is active. A memory leak is still possible but largely unlikely.
    // The worst that can happen is that a useless NavMenu is left hanging around for no reason

    public ElementSwitch NavBarStyleSwitch
    {
        get => field;
        set
        {
            if (field != value)
                NavMenuStateHasChangedCallback?.Invoke();
        }
    }

    public string? NavBarTitle { get; set; }

    public string? TopBarClass { get; set; }

    public bool ContainArticleInDiv { get; set; }

    public virtual IEnumerable<RenderFragment> GetFooterComponents(IServiceProvider services) => Array.Empty<RenderFragment>();
    public virtual IAsyncEnumerable<RenderFragment> GetFooterComponentsAsync(IServiceProvider services) => AsyncEmpty<RenderFragment>.Empty();

    public virtual IEnumerable<RenderFragment> GetTopBarComponents(IServiceProvider services) => Array.Empty<RenderFragment>();
    public virtual IAsyncEnumerable<RenderFragment> GetTopBarComponentsAsync(IServiceProvider services) => AsyncEmpty<RenderFragment>.Empty();

    public virtual IEnumerable<NavBarEntry> GetNavBarComponents(IServiceProvider services) => Array.Empty<NavBarEntry>();
    public virtual IAsyncEnumerable<NavBarEntry> GetNavBarComponentsAsync(IServiceProvider services) => AsyncEmpty<NavBarEntry>.Empty();

}
