using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.MobDrops.Components;

[RegisterComponent]
public sealed partial class MobDropsComponent : Component
{
    [ViewVariables]
    [DataField("Drop", required: true)]
    public EntProtoId Drop = "PresentRandomInsane";

    [ViewVariables]
    [DataField("AlredyDroped")]
    public bool AlredyDroped = false;
}
