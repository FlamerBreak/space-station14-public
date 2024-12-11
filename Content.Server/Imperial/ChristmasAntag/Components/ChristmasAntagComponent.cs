using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Imperial.ChristmasAntag.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ChristmasAntagComponent : Component
{

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    [DataField]
    public EntityUid? Target;

    [DataField]
    public EntityUid? LastConveyed;

    [DataField]
    public EntityUid? LastCursedGift;

    [DataField]
    public bool TargetGoaled;

    [DataField]
    public TimeSpan CurseTimer = TimeSpan.FromMinutes(10);
}
