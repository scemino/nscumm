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
using System;
using System.Collections.Generic;
using System.Text;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Engine
{
    internal enum MessageFunction
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

    internal partial class Kernel
    {
        private const int ALIGN_NONE = 0;
        private const int ALIGN_RIGHT = 1;
        private const int ALIGN_LEFT = -1;
        private const int ALIGN_CENTER = 2;


        private static Register kFormat(EngineState s, int argc, StackPtr argv)
        {
            Register dest = argv[0];
            char[] targetbuf = new char[4096];
            var target = 0;
            Register position = argv[1]; /* source */
            int mode = 0;
            int paramindex = 0; /* Next parameter to evaluate */
            char xfer;
            int i;
            int startarg;
            int strLength = 0; /* Used for stuff like "%13s" */
            bool unsignedVar = false;

            if (position.Segment != 0)
                startarg = 2;
            else
            {
                // WORKAROUND: QFG1 VGA Mac calls this without the first parameter (dest). It then
                // treats the source as the dest and overwrites the source string with an empty string.
                if (argc < 3)
                    return Register.NULL_REG;

                startarg = 3; /* First parameter to use for formatting */
            }

            int index = (startarg == 3) ? argv[2].ToUInt16() : 0;
            string source_str = SciEngine.Instance.Kernel.LookupText(position, index);
            var source = 0;

            DebugC(DebugLevels.Strings, "Formatting \"{0}\"", source);


            var arguments = new ushort[argc];

            for (i = startarg; i < argc; i++)
                arguments[i - startarg] = argv[i].ToUInt16(); /* Parameters are copied to prevent overwriting */

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
                    else
                    {
                        mode = 1;
                        strLength = 0;
                    }
                }
                else if (mode == 1)
                {
                    /* xfer != '%' */
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
                            {
                                /* Copy string */
                                Register reg = argv[startarg + paramindex];

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
                            {
                                /* insert character */
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
                                Array.Copy(targetbuf, writestart, targetbuf, writestart + padding, written);
                                targetbuf.Set(writestart, fillchar, padding);
                            }
                            else
                            {
                                targetbuf.Set(target, ' ', padding);
                            }
                            target += padding;
                        }
                    }
                }
                else
                {
                    /* mode != 1 */
                    targetbuf[target] = xfer;
                    target++;
                }
            }

            targetbuf[target] = '\0'; /* Terminate string */

            s._segMan.Strcpy(dest, new string(targetbuf, 0, target));

            return dest; /* Return target addr */
        }

        private static Register kGetFarText(EngineState s, int argc, StackPtr argv)
        {
            var textres = SciEngine.Instance.ResMan.FindResource(new ResourceId(ResourceType.Text, argv[0].ToUInt16()),
                false);
            int counter = argv[1].ToUInt16();

            if (textres == null)
            {
                throw new InvalidOperationException($"text.{argv[0].ToUInt16()} does not exist");
            }

            var seeker = new ByteAccess(textres.data);

            // The second parameter (counter) determines the number of the string
            // inside the text resource.
            while ((counter--) != 0)
            {
                while (seeker.Increment() != 0)
                {
                }
            }

            // If the third argument is NULL, allocate memory for the destination. This
            // occurs in SCI1 Mac games. The memory will later be freed by the game's
            // scripts.
            if (argv[2] == Register.NULL_REG)
            {
                Register temp;
                s._segMan.AllocDynmem(ScummHelper.GetTextLength(seeker.Data, seeker.Offset) + 1, "Mac FarText", out temp);
                StackPtr ptr = argv;
                ptr[2] = temp;
            }

            s._segMan.Strcpy(argv[2], ScummHelper.GetText(seeker.Data, seeker.Offset));
            // Copy the string and get return value
            return argv[2];
        }

        private static Register kGetMessage(EngineState s, int argc, StackPtr argv)
        {
            var tuple = new MessageTuple((byte)argv[0].ToUInt16(), (byte)argv[2].ToUInt16());

            s._msgState.GetMessage(argv[1].ToUInt16(), tuple, argv[3]);

            return argv[3];
        }

        private static Register kMessage(EngineState s, int argc, StackPtr argv)
        {
            uint func = argv[0].ToUInt16();
            ushort module = (ushort)((argc >= 2) ? argv[1].ToUInt16() : 0);

# if ENABLE_SCI32
            if (ResourceManager.GetSciVersion() >= SciVersion.V2)
            {
                // In complete weirdness, SCI32 bumps up subops 3-8 to 4-9 and stubs off subop 3.
                if (func == 3)
                    Error("SCI32 kMessage(3)");
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
                tuple = new MessageTuple((byte)argv[2].ToUInt16(), (byte)argv[3].ToUInt16(), (byte)argv[4].ToUInt16(),
                    (byte)argv[5].ToUInt16());

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
                    return Register.Make(0,
                        (ushort)s._msgState.GetMessage(module, tuple, (argc == 7 ? argv[6] : Register.NULL_REG)));
                case MessageFunction.NEXT:
                    return Register.Make(0, s._msgState.NextMessage((argc == 2 ? argv[1] : Register.NULL_REG)));
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

                        if (s._segMan.Dereference(argv[1]).isRaw)
                        {
                            var buffer = s._segMan.DerefBulkPtr(argv[1], 10);

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
                        else
                        {
                            var buffer = (StackPtr)s._segMan.DerefRegPtr(argv[1], 5);

                            if (buffer != StackPtr.Null)
                            {
                                ok = true;
                                buffer[0] = Register.Make(0, (ushort)lastModule);
                                buffer[1] = Register.Make(0, msg.noun);
                                buffer[2] = Register.Make(0, msg.verb);
                                buffer[3] = Register.Make(0, msg.cond);
                                buffer[4] = Register.Make(0, msg.seq);
                            }
                        }

                        if (!ok)
                            Warning($"Message: buffer {argv[1]} invalid or too small to hold the tuple");

                        return Register.NULL_REG;
                    }
                case MessageFunction.PUSH:
                    s._msgState.PushCursorStack();
                    break;
                case MessageFunction.POP:
                    s._msgState.PopCursorStack();
                    break;
                default:
                    Warning($"Message: subfunction {func} invoked (not implemented)");
                    break;
            }

            return Register.NULL_REG;
        }

        private static Register kReadNumber(EngineState s, int argc, StackPtr argv)
        {
            string source_str = s._segMan.GetString(argv[0]);
            var source = 0;

            while (source < source_str.Length && char.IsWhiteSpace(source_str[source]))
                source++; /* Skip whitespace */

            short result = 0;
            short sign = 1;

            if (source < source_str.Length && source_str[source] == '-')
            {
                sign = -1;
                source++;
            }
            if (source < source_str.Length && source_str[source] == '$')
            {
                // Hexadecimal input
                source++;
                char c;
                while (source < source_str.Length && (c = source_str[source++]) != 0)
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
            else
            {
                // Decimal input. We can not use strtol/atoi in here, because while
                // Sierra used atoi, it was a non standard compliant atoi, that didn't
                // do clipping. In SQ4 we get the door code in here and that's even
                // larger than uint32!
                char c;
                for (int i = source; i < source_str.Length; i++)
                {
                    c = source_str[i];
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

        private static Register kSetQuitStr(EngineState s, int argc, StackPtr argv)
        {
            //Common::String quitStr = s._segMan.getString(argv[0]);
            //debug("Setting quit string to '%s'", quitStr.c_str());
            return s.r_acc;
        }

        private static Register kStrAt(EngineState s, int argc, StackPtr argv)
        {
            if (argv[0] == Register.SIGNAL_REG)
            {
                // TOO: warning("Attempt to perform kStrAt() on a signal reg");
                return Register.NULL_REG;
            }

            SegmentRef dest_r = s._segMan.Dereference(argv[0]);
            if (!dest_r.IsValid)
            {
                Warning($"Attempt to StrAt at invalid pointer {argv[0]}");
                return Register.NULL_REG;
            }

            byte value;
            byte newvalue = 0;
            ushort offset = argv[1].ToUInt16();
            if (argc > 2)
                newvalue = (byte)argv[2].ToInt16();

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
            else
            {
                if (dest_r.skipByte)
                    offset++;

                var tmp = dest_r.reg.Add(offset / 2);

                bool oddOffset = (offset & 1) != 0;
                if (SciEngine.Instance.IsBe)
                    oddOffset = !oddOffset;

                if (!oddOffset)
                {
                    value = (byte)(tmp[0].Offset & 0x00ff);
                    if (argc > 2)
                    {
                        /* Request to modify this char */
                        ushort tmpOffset = tmp[0].ToUInt16();
                        tmpOffset &= 0xff00;
                        tmpOffset |= newvalue;
                        tmp[0] = Register.Make(0, tmpOffset);
                    }
                }
                else
                {
                    value = (byte)(tmp[0].Offset >> 8);
                    if (argc > 2)
                    {
                        /* Request to modify this char */
                        ushort tmpOffset = tmp[0].ToUInt16();
                        tmpOffset &= 0x00ff;
                        tmpOffset |= (ushort)(newvalue << 8);
                        tmp[0] = Register.Make(0, tmpOffset);
                    }
                }
            }

            return Register.Make(0, value);
        }

        private static Register kStrCat(EngineState s, int argc, StackPtr argv)
        {
            string s1 = s._segMan.GetString(argv[0]);
            string s2 = s._segMan.GetString(argv[1]);

            // Japanese PC-9801 interpreter splits strings here
            //  see bug #5834
            //  Verified for Police Quest 2 + Quest For Glory 1
            //  However Space Quest 4 PC-9801 doesn't
            if ((SciEngine.Instance.Language == Core.Language.JA_JPN)
                && (ResourceManager.GetSciVersion() <= SciVersion.V01))
            {
                s1 = SciEngine.Instance.StrSplit(s1, null);
                s2 = SciEngine.Instance.StrSplit(s2, null);
            }

            s1 += s2;
            s._segMan.Strcpy(argv[0], s1);
            return argv[0];
        }

        private static Register kStrCmp(EngineState s, int argc, StackPtr argv)
        {
            string s1 = s._segMan.GetString(argv[0]);
            string s2 = s._segMan.GetString(argv[1]);

            if (argc > 2)
                return Register.Make(0, (ushort)string.CompareOrdinal(s1, 0, s2, 0, argv[2].ToUInt16()));
            else
                return Register.Make(0, (ushort)string.CompareOrdinal(s1, s2));
        }

        private static Register kStrCpy(EngineState s, int argc, StackPtr argv)
        {
            if (argc > 2)
            {
                int length = argv[2].ToInt16();

                if (length >= 0)
                    s._segMan.Strncpy(argv[0], argv[1], (uint)length);
                else
                    s._segMan.Memcpy(argv[0], argv[1], -length);
            }
            else
            {
                s._segMan.Strcpy(argv[0], argv[1]);
            }

            return argv[0];
        }

        private static Register kStrEnd(EngineState s, int argc, StackPtr argv)
        {
            Register address = argv[0];
            address = Register.IncOffset(address, (short)s._segMan.Strlen(address));
            return address;
        }

        private static Register kStrLen(EngineState s, int argc, StackPtr argv)
        {
            return Register.Make(0, (ushort)s._segMan.Strlen(argv[0]));
        }

        private static Register kStrSplit(EngineState s, int argc, StackPtr argv)
        {
            string format = s._segMan.GetString(argv[1]);
            string sep_str;
            string sep = null;
            if (!argv[2].IsNull)
            {
                sep_str = s._segMan.GetString(argv[2]);
                sep = sep_str;
            }
            string str = SciEngine.Instance.StrSplit(format, sep);

            // Make sure target buffer is large enough
            SegmentRef buf_r = s._segMan.Dereference(argv[0]);
            if (!buf_r.IsValid || buf_r.maxSize < (int)str.Length + 1)
            {
                Warning(
                    $"StrSplit: buffer {argv[0]} invalid or too small to hold the following text of {str.Length + 1} bytes: '{str}'");
                return Register.NULL_REG;
            }
            s._segMan.Strcpy(argv[0], str);
            return argv[0];
        }

#if ENABLE_SCI32
        private static Register kString(EngineState s, int argc, StackPtr argv)
        {
            if (s == null)
                return Register.Make(0, (ushort)ResourceManager.GetSciVersion());
            Error("not supposed to call this");
            return Register.NULL_REG;
        }

        // TODO: there is an unused second argument, happens at least in LSL6 right during the intro
        private static Register kStringNew(EngineState s, int argc, StackPtr argv)
        {
            Register stringHandle;
            ushort size = argv[0].ToUInt16();
            s._segMan.AllocateArray(SciArrayType.String, size, out stringHandle);
            return stringHandle;
        }

        private static Register kStringSize(EngineState s, int argc, StackPtr argv)
        {
            return Register.Make(0, (ushort)s._segMan.GetString(argv[0]).Length);
        }

        private static Register kStringFree(EngineState s, int argc, StackPtr argv)
        {
            // Freeing of strings is handled by the garbage collector
            return s.r_acc;
        }

        private static Register kStringCompare(EngineState s, int argc, StackPtr argv)
        {
            string string1 = argv[0].IsNull ? string.Empty : s._segMan.GetString(argv[0]);
            string string2 = argv[1].IsNull ? string.Empty : s._segMan.GetString(argv[1]);

            if (argc == 3) // Strncmp
                return Register.Make(0, (ushort)string.CompareOrdinal(string1, 0, string2, 0, argv[2].ToUInt16()));
            return Register.Make(0, (ushort)string.CompareOrdinal(string1, string2));
        }

        // was removed for SCI2.1 Late+
        private static Register kStringGetData(EngineState s, int argc, StackPtr argv)
        {
            if (!s._segMan.IsHeapObject(argv[0]))
                return argv[0];

            return SciEngine.ReadSelector(s._segMan, argv[0], o => o.data);
        }

        private static Register kStringLen(EngineState s, int argc, StackPtr argv)
        {
            return Register.Make(0, (ushort)s._segMan.Strlen(argv[0]));
        }

        private static Register kStringFormat(EngineState s, int argc, StackPtr argv)
        {
            Register stringHandle;
            SciArray target = s._segMan.AllocateArray(SciArrayType.String, 0, out stringHandle);
            Register source = argv[0];
            // Str objects may be passed in place of direct references to string data
            if (s._segMan.IsObject(argv[0]))
            {
                source = SciEngine.ReadSelector(s._segMan, argv[0], o => o.data);
            }
            target.FromString(Format(s._segMan.GetString(source), argc - 1, argv + 1));
            return stringHandle;
        }

        private static string Format(string source, int argc, StackPtr argv)
        {
            var @out = new StringBuilder();
            var @in = 0;
            int argIndex = 0;
            while (@in < source.Length)
            {
                if (source[@in] == '%')
                {
                    if (source[@in + 1] == '%')
                    {
                        @in += 2;
                        @out.Append("%");
                        continue;
                    }

                    System.Diagnostics.Debug.Assert(argIndex < argc);
                    @out.Append(ReadPlaceholder(source, ref @in, argv[argIndex++]));
                }
                else
                {
                    @out.Append(source[@in++]);
                }
            }

            return @out.ToString();
        }

        private static Register kStringFormatAt(EngineState s, int argc, StackPtr argv)
        {
            SciArray target = s._segMan.LookupArray(argv[0]);
            Register source = argv[1];
            // Str objects may be passed in place of direct references to string data
            if (s._segMan.IsObject(argv[1]))
            {
                source = SciEngine.ReadSelector(s._segMan, argv[1], o => o.data);
            }
            target.FromString(Format(s._segMan.GetString(source), argc - 2, argv + 2));
            return argv[0];
        }

        private static bool IsFlag(char c)
        {
            return "-+ 0#".IndexOf(c, 0) != -1;
        }

        private static bool IsPrecision(char c)
        {
            return ".0123456789*".IndexOf(c, 0) != -1;
        }

        private static bool IsWidth(char c)
        {
            return "0123456789*".IndexOf(c, 0) != -1;
        }

        private static bool IsLength(char c)
        {
            return "hjlLtz".IndexOf(c, 0) != -1;
        }

        private static bool IsType(char c)
        {
            return "dsuxXaAceEfFgGinop".IndexOf(c, 0) != -1;
        }

        private static bool IsSignedType(char type)
        {
            return type == 'd' || type == 'i';
        }

        private static bool IsUnsignedType(char type)
        {
            return "uxXoc".IndexOf(type, 0) != -1;
        }

        private static bool IsStringType(char type)
        {
            return type == 's';
        }

        private static string ReadPlaceholder(string @in, ref int index, Register arg)
        {
            var start = index;

            System.Diagnostics.Debug.Assert(@in[index] == '%');
            ++index;

            while (IsFlag(@in[index]))
            {
                ++index;
            }
            while (IsWidth(@in[index]))
            {
                ++index;
            }
            while (IsPrecision(@in[index]))
            {
                ++index;
            }
            while (IsLength(@in[index]))
            {
                ++index;
            }

            char[] format = new char[64];
            format[0] = '\0';
            char type = @in[index++];
            var len = Math.Min(64, index - start);
            Array.Copy(@in.ToCharArray(), start, format, 0, len);
            var fmt = new string(format, 0, len);

            if (IsType(type))
            {
                if (IsSignedType(type))
                {
                    int value = arg.ToInt16();
                    return value.ToString();
                }
                else if (IsUnsignedType(type))
                {
                    uint value = arg.ToUInt16();
                    return value.ToString();
                }
                else if (IsStringType(type))
                {
                    string value;
                    SegManager segMan = SciEngine.Instance.EngineState._segMan;
                    if (segMan.IsObject(arg))
                    {
                        value = segMan.GetString(SciEngine.ReadSelector(segMan, arg, o => o.data));
                    }
                    else
                    {
                        value = segMan.GetString(arg);
                    }
                    return value;
                }
                else
                {
                    Error("Unsupported format type {0}", type);
                    return null;
                }
            }
            else
            {
                return fmt;
            }
        }

        private static Register kStringPrintfBuf(EngineState s, int argc, StackPtr argv)
        {
            return kFormat(s, argc, argv);
        }

        private static Register kStringAtoi(EngineState s, int argc, StackPtr argv)
        {
            string @string = s._segMan.GetString(argv[0]);
            return Register.Make(0, (ushort)int.Parse(@string));
        }

        private static Register kStringTrim(EngineState s, int argc, StackPtr argv)
        {
            string @string = s._segMan.GetString(argv[0]);

            @string = @string.Trim();
            // TODO: Second parameter (bitfield, trim from left, right, center)
            Warning("kStringTrim (%d)", argv[1].Offset);
            s._segMan.Strcpy(argv[0], @string);
            return Register.NULL_REG;
        }

        private static Register kStringUpper(EngineState s, int argc, StackPtr argv)
        {
            string @string = s._segMan.GetString(argv[0]);

            @string = @string.ToUpperInvariant();
            s._segMan.Strcpy(argv[0], @string);
            return Register.NULL_REG;
        }

        private static Register kStringLower(EngineState s, int argc, StackPtr argv)
        {
            string @string = s._segMan.GetString(argv[0]);

            @string = @string.ToLowerInvariant();
            s._segMan.Strcpy(argv[0], @string);
            return Register.NULL_REG;
        }

        private static Register kStringReplaceSubstring(EngineState s, int argc, StackPtr argv)
        {
            Error("TODO: kStringReplaceSubstring not implemented");
            return argv[3];
        }

        private static Register kStringReplaceSubstringEx(EngineState s, int argc, StackPtr argv)
        {
            Error("TODO: kStringReplaceSubstringEx not implemented");
            return argv[3];
        }
#endif
    }
}