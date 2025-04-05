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

public interface ILayoutComponentProvider
{
    public IEnumerable<RenderFragment> GetFooterComponents(IServiceProvider services);
    public IEnumerable<RenderFragment> GetTopBarComponents(IServiceProvider services);
    public IEnumerable<NavBarEntry> GetNavBarComponents(IServiceProvider services);
}
