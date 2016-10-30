namespace NScumm.Core.Common
{
    public static class Str
    {
        public static string Tag2String(uint tag)
        {
            char[] str = new char[5];
            str[0] = (char)(tag >> 24);
            str[1] = (char)(tag >> 16);
            str[2] = (char)(tag >> 8);
            str[3] = (char)tag;
            str[4] = '\0';
            // Replace non-printable chars by dot
            for (int i = 0; i < 4; ++i)
            {
                if (!IsPrint(str[i]))
                    str[i] = '.';
            }
            return new string(str);
        }

        private static bool IsPrint(int c)
        {
            if (!IsAsciiChar(c)) return false;
            // TODO: check this
            return c > 0x1f && c != 0x7f;
        }

        private static bool IsAsciiChar(int c)
        {
            if (c < 0 || c > 127)
                return false;
            return true;
        }
    }
}
