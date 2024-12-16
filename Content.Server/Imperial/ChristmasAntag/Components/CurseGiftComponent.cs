using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.ChristmasAntag.Components;

[RegisterComponent]
public sealed partial class CurseGiftComponent : Component
{
    [DataField]
    public TimeSpan CurseDelay = TimeSpan.FromSeconds(5);

    [DataField]
    public List<EntProtoId> GiftGoal = new() { "PresentRandomUnsafeNG3" };

    [DataField]
    public EntityUid? CurseTargetEntity;

    [DataField]
    public float MaxCurseRange = 2.5f;

    [DataField]
    public EntityUid OwnerGift;
}
