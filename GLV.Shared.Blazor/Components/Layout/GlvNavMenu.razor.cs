using GLV.Shared.Blazor.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLV.Shared.Blazor.Components.Layout;

public partial class GlvNavMenu()
{
    private List<NavBarEntry>? NavBarEntries;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            NavBarEntries = new();

            foreach (var comp in LayoutComponents.GetNavBarComponents(Services))
                NavBarEntries.Add(comp);

            await foreach (var comp in LayoutComponents.GetNavBarComponentsAsync(Services))
                NavBarEntries.Add(comp);

            StateHasChanged();
        }
    }
}
