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
    }
}
