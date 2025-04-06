using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLV.Shared.Blazor.Services;

public record NavBarEntry(
    string Name,
    string Href,
    string? SpanClass = null,
    string NavLinkClass = "nav-item px-3",
    NavLinkMatch Match = NavLinkMatch.All
);

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

    public abstract IEnumerable<RenderFragment> GetFooterComponents(IServiceProvider services);
    public abstract IEnumerable<RenderFragment> GetTopBarComponents(IServiceProvider services);
    public abstract IEnumerable<NavBarEntry> GetNavBarComponents(IServiceProvider services);
}
