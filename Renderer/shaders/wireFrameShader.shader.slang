#language slang 2026

struct FragmentOutput
{
    float4 color : SV_Target;
}

[shader("fragment")]
FragmentOutput FragmentMain()
{
    FragmentOutput output;

    output.color = float4(1,1,1,1);

    return output;
}
