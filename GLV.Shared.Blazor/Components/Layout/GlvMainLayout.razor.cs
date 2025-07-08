using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLV.Shared.Blazor.Components.Layout;

public partial class GlvMainLayout
{
    private bool HasLoaded;
    private List<RenderFragment>? TopBarComponents;
    private List<RenderFragment>? FooterComponents;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (LayoutComponents is not null)
            LayoutComponents.NavMenuStateHasChangedCallback = StateHasChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            TopBarComponents = new();

            foreach (var comp in LayoutComponents.GetTopBarComponents(Services))
                TopBarComponents.Add(comp);

            await foreach (var comp in LayoutComponents.GetTopBarComponentsAsync(Services))
                TopBarComponents.Add(comp);

            FooterComponents = new();

            foreach (var comp in LayoutComponents.GetFooterComponents(Services))
                FooterComponents.Add(comp);

            await foreach (var comp in LayoutComponents.GetFooterComponentsAsync(Services))
                FooterComponents.Add(comp);

            HasLoaded = true;
            StateHasChanged();
        }
    }
}
