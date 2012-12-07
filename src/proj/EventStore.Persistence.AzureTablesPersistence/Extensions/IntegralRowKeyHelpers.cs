namespace EventStore.Persistence.AzureTablesPersistence.Extensions
{
    internal static class IntegralRowKeyHelpers
    {
        public static string EncodeDouble(double d)
        {
            long ieee = System.BitConverter.DoubleToInt64Bits(d);
            ulong widezero = 0;
            ulong lex = ((ieee < 0) ? widezero : ((~widezero) >> 1)) ^ (ulong)~ieee;
            return lex.ToString("X16");
        }

        public static double DecodeDouble(string s)
        {
            ulong lex = ulong.Parse(s, System.Globalization.NumberStyles.AllowHexSpecifier);
            ulong widezero = 0;
            long ieee = (long)(((0 <= (long)lex) ? widezero : ((~widezero) >> 1)) ^ ~lex);
            return System.BitConverter.Int64BitsToDouble(ieee);
        }

    }
}