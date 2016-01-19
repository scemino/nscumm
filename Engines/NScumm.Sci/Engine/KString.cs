//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2015 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using NScumm.Core;
using NScumm.Core.Common;
using System;

namespace NScumm.Sci.Engine
{
    enum MessageFunction
    {
        GET,
        NEXT,
        SIZE,
        REFNOUN,
        REFVERB,
        REFCOND,
        PUSH,
        POP,
        LASTMESSAGE
    }

    partial class Kernel
    {
        private const int ALIGN_NONE = 0;
        private const int ALIGN_RIGHT = 1;
        private const int ALIGN_LEFT = -1;
        private const int ALIGN_CENTER = 2;


        private static Register kFormat(EngineState s, int argc, StackPtr? argv)
        {
            Register dest = argv.Value[0];
            char[] targetbuf = new char[4096];
            var target = 0;
            Register position = argv.Value[1]; /* source */
            int mode = 0;
            int paramindex = 0; /* Next parameter to evaluate */
            char xfer;
            int i;
            int startarg;
            int strLength = 0; /* Used for stuff like "%13s" */
            bool unsignedVar = false;

            if (position.Segment != 0)
                startarg = 2;
            else {
                // WORKAROUND: QFG1 VGA Mac calls this without the first parameter (dest). It then
                // treats the source as the dest and overwrites the source string with an empty string.
                if (argc < 3)
                    return Register.NULL_REG;

                startarg = 3; /* First parameter to use for formatting */
            }

            int index = (startarg == 3) ? argv.Value[2].ToUInt16() : 0;
            string source_str = SciEngine.Instance.Kernel.LookupText(position, index);
            var source = 0;

            // TODO: debugC(kDebugLevelStrings, "Formatting \"%s\"", source);


            var arguments = new ushort[argc];

            for (i = startarg; i < argc; i++)
                arguments[i - startarg] = argv.Value[i].ToUInt16(); /* Parameters are copied to prevent overwriting */

            while (source < source_str.Length && (xfer = source_str[source++]) != 0)
            {
                if (xfer == '%')
                {
                    if (mode == 1)
                    {
                        //assert((target - targetbuf) + 2 <= maxsize);
                        targetbuf[target++] = '%'; /* Literal % by using "%%" */
                        mode = 0;
                    }
                    else {
                        mode = 1;
                        strLength = 0;
                    }
                }
                else if (mode == 1)
                { /* xfer != '%' */
                    char fillchar = ' ';
                    int align = ALIGN_NONE;

                    var writestart = target; /* Start of the written string, used after the switch */

                    /* int writelength; -- unused atm */

                    if (xfer != 0 && (char.IsDigit(xfer) || xfer == '-' || xfer == '='))
                    {
                        var destp = 0;

                        if (xfer == '0')
                            fillchar = '0';
                        else if (xfer == '=')
                            align = ALIGN_CENTER;
                        else if (char.IsDigit(xfer) || (xfer == '-'))
                            source--; // Go to start of length argument

                        int j;
                        for (j = source; j < source_str.Length; j++)
                        {
                            if (!char.IsDigit(source_str[j]))
                                break;
                        }
                        destp = j;
                        strLength = int.Parse(source_str.Substring(source, destp - source));

                        if (destp > source)
                            source = destp;

                        if (strLength < 0)
                        {
                            align = ALIGN_LEFT;
                            strLength = -strLength;
                        }
                        else if (align != ALIGN_CENTER)
                            align = ALIGN_RIGHT;

                        xfer = source_str[source++];
                    }
                    else
                        strLength = 0;

                    //assert((target - targetbuf) + strLength + 1 <= maxsize);

                    switch (xfer)
                    {
                        case 's':
                            { /* Copy string */
                                Register reg = argv.Value[startarg + paramindex];

# if ENABLE_SCI32
                                // If the string is a string object, get to the actual string in the data selector
                                if (s._segMan.isObject(reg))
                                    reg = readSelector(s._segMan, reg, SELECTOR(data));
#endif

                                string tempsource = SciEngine.Instance.Kernel.LookupText(reg, arguments[paramindex + 1]);
                                int slen = tempsource.Length;
                                int extralen = strLength - slen;
                                //assert((target - targetbuf) + extralen <= maxsize);
                                if (extralen < 0)
                                    extralen = 0;

                                if (reg.Segment != 0) /* Heap address? */
                                    paramindex++;
                                else
                                    paramindex += 2; /* No, text resource address */

                                switch (align)
                                {

                                    case ALIGN_NONE:
                                    case ALIGN_RIGHT:
                                        while (extralen-- > 0)
                                            targetbuf[target++] = ' '; /* Format into the text */
                                        break;

                                    case ALIGN_CENTER:
                                        {
                                            int half_extralen = extralen >> 1;
                                            while (half_extralen-- > 0)
                                                targetbuf[target++] = ' '; /* Format into the text */
                                            break;
                                        }

                                    default:
                                        break;

                                }

                                Array.Copy(tempsource.ToCharArray(), 0, targetbuf, target, tempsource.Length);
                                target += slen;

                                switch (align)
                                {

                                    case ALIGN_CENTER:
                                        {
                                            int half_extralen;
                                            align = 0;
                                            half_extralen = extralen - (extralen >> 1);
                                            while (half_extralen-- > 0)
                                                targetbuf[target++] = ' '; /* Format into the text */
                                            break;
                                        }

                                    default:
                                        break;

                                }

                                mode = 0;
                            }
                            break;

                        case 'c':
                            { /* insert character */
                                //assert((target - targetbuf) + 2 <= maxsize);
                                if (align >= 0)
                                    while (strLength-- > 1)
                                        targetbuf[target++] = ' '; /* Format into the text */
                                char argchar = (char)arguments[paramindex++];
                                if (argchar != 0)
                                    targetbuf[target++] = argchar;
                                mode = 0;
                            }
                            break;

                        case 'x':
                        case 'u':
                        case 'd':
                            {
                                unsignedVar = xfer == 'x' || xfer == 'u';

                                /* Copy decimal */
                                // In the new SCI2 kString function, %d is used for unsigned
                                // integers. An example is script 962 in Shivers - it uses %d
                                // to create file names.
                                if (ResourceManager.GetSciVersion() >= SciVersion.V2)
                                    unsignedVar = true;

                                /* int templen; -- unused atm */
                                var format_string = "{0}";

                                if (xfer == 'x')
                                    format_string = "{0:x}";

                                int val = arguments[paramindex];
                                if (!unsignedVar)
                                    val = (short)arguments[paramindex];

                                var tmp = string.Format(format_string, val);
                                Array.Copy(tmp.ToCharArray(), 0, targetbuf, target, tmp.Length);
                                target += tmp.Length;
                                paramindex++;
                                //assert((target - targetbuf) <= maxsize);

                                unsignedVar = false;

                                mode = 0;
                            }
                            break;
                        default:
                            targetbuf[target] = '%';
                            target++;
                            targetbuf[target] = xfer;
                            target++;
                            mode = 0;
                            break;
                    }

                    if (align != 0)
                    {
                        int written = target - writestart;
                        int padding = strLength - written;

                        if (padding > 0)
                        {
                            if (align > 0)
                            {
                                throw new NotImplementedException();
                                //memmove(writestart + padding, writestart, written);
                                //memset(writestart, fillchar, padding);
                            }
                            else {
                                throw new NotImplementedException();
                                //memset(target, ' ', padding);
                            }
                            target += padding;
                        }
                    }
                }
                else { /* mode != 1 */
                    targetbuf[target] = xfer;
                    target++;
                }
            }

            targetbuf[target] = '\0'; /* Terminate string */

# if ENABLE_SCI32
            // Resize SCI32 strings if necessary
            if (getSciVersion() >= SCI_VERSION_2)
            {
                SciString * string = s._segMan.lookupString(dest);
                string.setSize(strlen(targetbuf) + 1);
            }
#endif

            s._segMan.Strcpy(dest, new string(targetbuf, 0, target));

            return dest; /* Return target addr */
        }

        private static Register kGetFarText(EngineState s, int argc, StackPtr? argv)
        {
            var textres = SciEngine.Instance.ResMan.FindResource(new ResourceId(ResourceType.Text, argv.Value[0].ToUInt16()), false);
            int counter = argv.Value[1].ToUInt16();

            if (textres == null)
            {
                throw new InvalidOperationException($"text.{argv.Value[0].ToUInt16()} does not exist");
            }

            var seeker = new ByteAccess(textres.data);

            // The second parameter (counter) determines the number of the string
            // inside the text resource.
            while ((counter--) != 0)
            {
                while (seeker.Increment() != 0)
                    ;
            }

            // If the third argument is NULL, allocate memory for the destination. This
            // occurs in SCI1 Mac games. The memory will later be freed by the game's
            // scripts.
            if (argv.Value[2] == Register.NULL_REG)
            {
                Register temp;
                s._segMan.AllocDynmem(ScummHelper.GetTextLength(seeker.Data, seeker.Offset) + 1, "Mac FarText", out temp);
                StackPtr ptr = argv.Value;
                ptr[2] = temp;
            }

            s._segMan.Strcpy(argv.Value[2], ScummHelper.GetText(seeker.Data, seeker.Offset)); // Copy the string and get return value
            return argv.Value[2];
        }

        private static Register kGetMessage(EngineState s, int argc, StackPtr? argv)
        {
            var tuple = new MessageTuple((byte)argv.Value[0].ToUInt16(), (byte)argv.Value[2].ToUInt16());

            s._msgState.GetMessage(argv.Value[1].ToUInt16(), tuple, argv.Value[3]);

            return argv.Value[3];
        }

        private static Register kMessage(EngineState s, int argc, StackPtr? argv)
        {
            uint func = argv.Value[0].ToUInt16();
            ushort module = (ushort)((argc >= 2) ? argv.Value[1].ToUInt16() : 0);

# if ENABLE_SCI32
            if (getSciVersion() >= SCI_VERSION_2)
            {
                // In complete weirdness, SCI32 bumps up subops 3-8 to 4-9 and stubs off subop 3.
                if (func == 3)
                    error("SCI32 kMessage(3)");
                else if (func > 3)
                    func--;
            }
#endif

            //	TODO: Perhaps fix this check, currently doesn't work with PUSH and POP subfunctions
            //	Pepper uses them to to handle the glossary
            //	if ((func != K_MESSAGE_NEXT) && (argc < 2)) {
            //		warning("Message: not enough arguments passed to subfunction %d", func);
            //		return NULL_REG;
            //	}

            MessageTuple tuple = new MessageTuple();

            if (argc >= 6)
                tuple = new MessageTuple((byte)argv.Value[2].ToUInt16(), (byte)argv.Value[3].ToUInt16(), (byte)argv.Value[4].ToUInt16(), (byte)argv.Value[5].ToUInt16());

            // WORKAROUND for a script bug in Pepper. When using objects together,
            // there is code inside script 894 that shows appropriate messages.
            // In the case of the jar of cabbage (noun 26), the relevant message
            // shown when using any object with it is missing. This leads to the
            // script code being triggered, which modifies the jar's noun and
            // message selectors, and renders it useless. Thus, when using any
            // object with the jar of cabbage, it's effectively corrupted, and
            // can't be used on the goat to empty it, therefore the game reaches
            // an unsolvable state. It's almost impossible to patch the offending
            // script, as it is used in many cases. But we can prevent the
            // corruption of the jar here: if the message is found, the offending
            // code is never reached and the jar is never corrupted. To do this,
            // we substitute all verbs on the cabbage jar with the default verb,
            // which shows the "Cannot use this object with the jar" message, and
            // never triggers the offending script code that corrupts the object.
            // This only affects the jar of cabbage - any other object, including
            // the empty jar has a different noun, thus it's unaffected.
            // Fixes bug #3601090.
            // NOTE: To fix a corrupted jar object, type "send Glass_Jar message 52"
            // in the debugger.
            if (SciEngine.Instance.GameId == SciGameId.PEPPER && func == 0 && argc >= 6 && module == 894 &&
                tuple.noun == 26 && tuple.cond == 0 && tuple.seq == 1 &&
                s._msgState.GetMessage(module, tuple, Register.NULL_REG) == 0)
                tuple.verb = 0;

            switch ((MessageFunction)func)
            {
                case MessageFunction.GET:
                    return Register.Make(0, (ushort)s._msgState.GetMessage(module, tuple, (argc == 7 ? argv.Value[6] : Register.NULL_REG)));
                case MessageFunction.NEXT:
                    return Register.Make(0, s._msgState.NextMessage((argc == 2 ? argv.Value[1] : Register.NULL_REG)));
                case MessageFunction.SIZE:
                    return Register.Make(0, s._msgState.MessageSize(module, tuple));
                case MessageFunction.REFCOND:
                case MessageFunction.REFVERB:
                case MessageFunction.REFNOUN:
                    {
                        MessageTuple t;

                        if (s._msgState.MessageRef(module, tuple, out t))
                        {
                            switch ((MessageFunction)func)
                            {
                                case MessageFunction.REFCOND:
                                    return Register.Make(0, t.cond);
                                case MessageFunction.REFVERB:
                                    return Register.Make(0, t.verb);
                                case MessageFunction.REFNOUN:
                                    return Register.Make(0, t.noun);
                            }
                        }

                        return Register.SIGNAL_REG;
                    }
                case MessageFunction.LASTMESSAGE:
                    {
                        MessageTuple msg;
                        int lastModule;

                        s._msgState.LastQuery(out lastModule, out msg);

                        bool ok = false;

                        if (s._segMan.Dereference(argv.Value[1]).isRaw)
                        {
                            var buffer = s._segMan.DerefBulkPtr(argv.Value[1], 10);

                            if (buffer != null)
                            {
                                ok = true;
                                buffer.WriteUInt16(0, (ushort)lastModule);
                                buffer.WriteUInt16(2, msg.noun);
                                buffer.WriteUInt16(4, msg.verb);
                                buffer.WriteUInt16(6, msg.cond);
                                buffer.WriteUInt16(8, msg.seq);
                            }
                        }
                        else {
                            var buffer = s._segMan.DerefRegPtr(argv.Value[1], 5);

                            if (buffer != null)
                            {
                                ok = true;
                                buffer[0] = Register.Make(0, (ushort)lastModule);
                                buffer[1] = Register.Make(0, msg.noun);
                                buffer[2] = Register.Make(0, msg.verb);
                                buffer[3] = Register.Make(0, msg.cond);
                                buffer[4] = Register.Make(0, msg.seq);
                            }
                        }

                        // TODO:
                        //if (!ok)
                        //    warning("Message: buffer %04x:%04x invalid or too small to hold the tuple", PRINT_REG(argv.Value[1]));

                        return Register.NULL_REG;
                    }
                case MessageFunction.PUSH:
                    s._msgState.PushCursorStack();
                    break;
                case MessageFunction.POP:
                    s._msgState.PopCursorStack();
                    break;
                default:
                    // TODO: warning("Message: subfunction %i invoked (not implemented)", func);
                    break;
            }

            return Register.NULL_REG;
        }

        private static Register kReadNumber(EngineState s, int argc, StackPtr? argv)
        {
            string source_str = s._segMan.GetString(argv.Value[0]);
            var source = 0;

            while (char.IsWhiteSpace(source_str[source]))
                source++; /* Skip whitespace */

            short result = 0;
            short sign = 1;

            if (source_str[source] == '-')
            {
                sign = -1;
                source++;
            }
            if (source_str[source] == '$')
            {
                // Hexadecimal input
                source++;
                char c;
                while ((c = source_str[source++]) != 0)
                {
                    short x = 0;
                    if ((c >= '0') && (c <= '9'))
                        x = (short)(c - '0');
                    else if ((c >= 'a') && (c <= 'f'))
                        x = (short)(c - 'a' + 10);
                    else if ((c >= 'A') && (c <= 'F'))
                        x = (short)(c - 'A' + 10);
                    else
                        // Stop if we encounter anything other than a digit (like atoi)
                        break;
                    result *= 16;
                    result += x;
                }
            }
            else {
                // Decimal input. We can not use strtol/atoi in here, because while
                // Sierra used atoi, it was a non standard compliant atoi, that didn't
                // do clipping. In SQ4 we get the door code in here and that's even
                // larger than uint32!
                char c;
                while ((c = source_str[source++]) != 0)
                {
                    if ((c < '0') || (c > '9'))
                        // Stop if we encounter anything other than a digit (like atoi)
                        break;
                    result *= 10;
                    result += (short)(c - '0');
                }
            }

            result *= sign;

            return Register.Make(0, (ushort)result);
        }

        private static Register kSetQuitStr(EngineState s, int argc, StackPtr? argv)
        {
            //Common::String quitStr = s._segMan.getString(argv[0]);
            //debug("Setting quit string to '%s'", quitStr.c_str());
            return s.r_acc;
        }

        private static Register kStrAt(EngineState s, int argc, StackPtr? argv)
        {
            if (argv.Value[0] == Register.SIGNAL_REG)
            {
                // TOO: warning("Attempt to perform kStrAt() on a signal reg");
                return Register.NULL_REG;
            }

            SegmentRef dest_r = s._segMan.Dereference(argv.Value[0]);
            if (!dest_r.IsValid)
            {
                // TODO: warning($"Attempt to StrAt at invalid pointer {argv.Value[0]}");
                return Register.NULL_REG;
            }

            byte value;
            byte newvalue = 0;
            ushort offset = argv.Value[1].ToUInt16();
            if (argc > 2)
                newvalue = (byte)argv.Value[2].ToInt16();

            // in kq5 this here gets called with offset 0xFFFF
            //  (in the desert wheng getting the staff)
            if ((int)offset >= dest_r.maxSize)
            {
                // TOO: warning("kStrAt offset %X exceeds maxSize", offset);
                return s.r_acc;
            }

            // FIXME: Move this to segman
            if (dest_r.isRaw)
            {
                value = dest_r.raw[offset];
                if (argc > 2) /* Request to modify this char */
                    dest_r.raw[offset] = newvalue;
            }
            else {
                if (dest_r.skipByte)
                    offset++;

                var tmp = new StackPtr(dest_r.reg, offset / 2);

                bool oddOffset = (offset & 1) != 0;
                if (SciEngine.Instance.IsBE)
                    oddOffset = !oddOffset;

                if (!oddOffset)
                {
                    value = (byte)(tmp[0].Offset & 0x00ff);
                    if (argc > 2)
                    { /* Request to modify this char */
                        ushort tmpOffset = tmp[0].ToUInt16();
                        tmpOffset &= 0xff00;
                        tmpOffset |= newvalue;
                        tmp[0] = Register.SetOffset(tmp[0], tmpOffset);
                        tmp[0] = Register.SetSegment(tmp[0], 0);
                    }
                }
                else {
                    value = (byte)(tmp[0].Offset >> 8);
                    if (argc > 2)
                    { /* Request to modify this char */
                        ushort tmpOffset = tmp[0].ToUInt16();
                        tmpOffset &= 0x00ff;
                        tmpOffset |= (ushort)(newvalue << 8);
                        tmp[0] = Register.SetOffset(tmp[0], tmpOffset);
                        tmp[0] = Register.SetSegment(tmp[0], 0);
                    }
                }
            }

            return Register.Make(0, value);

        }

        private static Register kStrCat(EngineState s, int argc, StackPtr? argv)
        {
            string s1 = s._segMan.GetString(argv.Value[0]);
            string s2 = s._segMan.GetString(argv.Value[1]);

            // Japanese PC-9801 interpreter splits strings here
            //  see bug #5834
            //  Verified for Police Quest 2 + Quest For Glory 1
            //  However Space Quest 4 PC-9801 doesn't
            if ((SciEngine.Instance.Language == Core.Common.Language.JA_JPN)
                && (ResourceManager.GetSciVersion() <= SciVersion.V01))
            {
                s1 = SciEngine.Instance.StrSplit(s1, null);
                s2 = SciEngine.Instance.StrSplit(s2, null);
            }

            s1 += s2;
            s._segMan.Strcpy(argv.Value[0], s1);
            return argv.Value[0];
        }

        private static Register kStrCmp(EngineState s, int argc, StackPtr? argv)
        {
            string s1 = s._segMan.GetString(argv.Value[0]);
            string s2 = s._segMan.GetString(argv.Value[1]);

            if (argc > 2)
                return Register.Make(0, (ushort)string.CompareOrdinal(s1, 0, s2, 0, argv.Value[2].ToUInt16()));
            else
                return Register.Make(0, (ushort)string.CompareOrdinal(s1, s2));
        }

        private static Register kStrCpy(EngineState s, int argc, StackPtr? argv)
        {
            if (argc > 2)
            {
                int length = argv.Value[2].ToInt16();

                if (length >= 0)
                    s._segMan.Strncpy(argv.Value[0], argv.Value[1], (uint)length);
                else
                    s._segMan.Memcpy(argv.Value[0], argv.Value[1], -length);
            }
            else {
                s._segMan.Strcpy(argv.Value[0], argv.Value[1]);
            }

            return argv.Value[0];
        }

        private static Register kStrEnd(EngineState s, int argc, StackPtr? argv)
        {
            Register address = argv.Value[0];
            address = Register.IncOffset(address, (short)s._segMan.Strlen(address));
            return address;
        }

        private static Register kStrLen(EngineState s, int argc, StackPtr? argv)
        {
            return Register.Make(0, (ushort)s._segMan.Strlen(argv.Value[0]));
        }

        private static Register kStrSplit(EngineState s, int argc, StackPtr? argv)
        {
            string format = s._segMan.GetString(argv.Value[1]);
            string sep_str;
            string sep = null;
            if (!argv.Value[2].IsNull)
            {
                sep_str = s._segMan.GetString(argv.Value[2]);
                sep = sep_str;
            }
            string str = SciEngine.Instance.StrSplit(format, sep);

            // Make sure target buffer is large enough
            SegmentRef buf_r = s._segMan.Dereference(argv.Value[0]);
            if (!buf_r.IsValid || buf_r.maxSize < (int)str.Length + 1)
            {
                //TODO: warning("StrSplit: buffer %04x:%04x invalid or too small to hold the following text of %i bytes: '%s'",
                //                PRINT_REG(argv.Value[0]), str.Length + 1, str);
                return Register.NULL_REG;
            }
            s._segMan.Strcpy(argv.Value[0], str);
            return argv.Value[0];
        }

    }
}
