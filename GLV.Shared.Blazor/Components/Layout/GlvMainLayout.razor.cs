using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLV.Shared.Blazor.Components.Layout;

public partial class GlvMainLayout
{
    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (LayoutComponents is not null)
            LayoutComponents.NavMenuStateHasChangedCallback = StateHasChanged;
    }
}
