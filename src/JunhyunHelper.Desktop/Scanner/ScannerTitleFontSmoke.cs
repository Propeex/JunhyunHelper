namespace JunhyunHelper.Desktop.Scanner;

internal static class ScannerTitleFontSmoke
{
    public static void VerifyProductContract()
    {
        var sfnt = new byte[44];
        sfnt[0] = (byte)'O';
        sfnt[1] = (byte)'T';
        sfnt[2] = (byte)'T';
        sfnt[3] = (byte)'O';
        sfnt[4] = 0;
        sfnt[5] = 1;

        // One table record starts at byte 12. Table payload starts immediately after
        // the 28-byte SFNT directory and occupies the remaining 16 bytes.
        sfnt[12] = (byte)'n';
        sfnt[13] = (byte)'a';
        sfnt[14] = (byte)'m';
        sfnt[15] = (byte)'e';
        sfnt[20] = 0;
        sfnt[21] = 0;
        sfnt[22] = 0;
        sfnt[23] = 28;
        sfnt[24] = 0;
        sfnt[25] = 0;
        sfnt[26] = 0;
        sfnt[27] = 16;

        if (!TarkovTitleFontProvider.TryGetSfntLength(sfnt, 0, out var length) || length != sfnt.Length)
            throw new InvalidOperationException("Scanner Tarkov-font SFNT parser smoke failed.");

        sfnt[5] = 0;
        if (TarkovTitleFontProvider.TryGetSfntLength(sfnt, 0, out _))
            throw new InvalidOperationException("Scanner Tarkov-font parser accepted an invalid empty table directory.");

        if (!ScannerTitleFontVerifier.UsesKoreanFallback('가') ||
            ScannerTitleFontVerifier.UsesKoreanFallback('A'))
        {
            throw new InvalidOperationException("Scanner title-font fallback segmentation contract failed.");
        }
    }
}
