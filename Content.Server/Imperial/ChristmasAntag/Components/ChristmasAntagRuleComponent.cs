using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.ChristmasAntag.Components;

[RegisterComponent]
public sealed partial class ChristmasAntagRuleComponent : Component
{
    [DataField]
    public string ChristmasAntagObjective = "BecomeChristmasAntagRandomPersonObjective";

    [DataField]
    public ProtoId<AntagPrototype> ChristmasAntagPrototypeId = "ChristmasAntag";

    [DataField]
    public List<EntityUid> ChristmasAntagMinds = new();

    [DataField]
    public int MaxAllowChristmasAntag = 1;

    [DataField]
    public SoundSpecifier? GreetingSound = new SoundPathSpecifier("/Audio/Imperial/ChristmasAntag/christmasAntag_greeting.ogg");
}
