using JunhyunHelper.Core.FarmingGuide;
using Xunit;

namespace JunhyunHelper.Tests.Core;

public sealed class FarmingGuideSlotLabelPolicyTests
{
    [Theory]
    [InlineData("mod_scope", "조준경")]
    [InlineData("mod_muzzle", "총구")]
    [InlineData("mod_stock", "개머리판")]
    [InlineData("mod_pistol_grip", "권총 손잡이")]
    [InlineData("mod_nvg", "야간투시경")]
    [InlineData("mod_face_shield", "안면 보호구")]
    public void AttachmentTranslatesCommonRawIds(string raw, string expected)
    {
        var slot = new FarmingGuideAttachmentSlotDefinition(
            raw,
            raw,
            raw,
            false,
            FarmingGuideItemFilter.Empty);

        Assert.Equal(expected, FarmingGuideSlotLabelPolicy.Attachment(slot));
    }

    [Theory]
    [InlineData("front_plate", "전면 방탄판")]
    [InlineData("back_plate", "후면 방탄판")]
    [InlineData("left_side_plate", "왼쪽 측면 방탄판")]
    [InlineData("right_side_plate", "오른쪽 측면 방탄판")]
    public void ArmorPlateTranslatesCommonRawIds(string raw, string expected)
    {
        var slot = new FarmingGuideArmorSlotDefinition(raw, raw, raw, false, []);

        Assert.Equal(expected, FarmingGuideSlotLabelPolicy.ArmorPlate(slot));
    }

    [Fact]
    public void KoreanSourceNameRemainsAuthoritative()
    {
        var slot = new FarmingGuideAttachmentSlotDefinition(
            "mod_scope",
            "mod_scope",
            "특수 조준장치",
            false,
            FarmingGuideItemFilter.Empty);

        Assert.Equal("특수 조준장치", FarmingGuideSlotLabelPolicy.Attachment(slot));
    }
}
