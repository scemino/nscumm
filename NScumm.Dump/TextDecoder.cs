using NScumm.Core;
using System.Text;

namespace NScumm.Tmp
{
    class TextDecoder: IScummTextDecoder
    {
        readonly StringBuilder sb;

        public TextDecoder(StringBuilder sb)
        {
            this.sb = sb;
        }

        #region IScummTextDecoder implementation

        public void WriteVariable(int variable)
        {
            sb.AppendFormat("{{var{0}}}", variable);
        }

        public void WriteVerbMessage(int id)
        {
            sb.AppendFormat("{{verb{0}}}", id);
        }

        public void WriteObjectOrActorName(int id)
        {
            sb.AppendFormat("{{objOrActor{0}}}", id);
        }

        public void WriteString(int val)
        {
            sb.AppendFormat("{{str{0}}}", val);
        }

        public void Write(byte c)
        {
            sb.Append((char)c);
        }

        public void WriteNewLine()
        {
            sb.Append("{newLine}");
        }

        public void WriteKeep()
        {
            sb.Append("{keep}");
        }

        public void WriteWait()
        {
            sb.Append("{wait}");
        }

        public void SetColor(int val)
        {
            sb.AppendFormat("{{color{0}}}", val);
        }

        public void UseCharset(int val)
        {
            sb.AppendFormat("{{charset{0}}}", val);
        }

        public void PlaySound(int val)
        {
            sb.AppendFormat("{{sound{0}}}", val);
        }

        public void StartActorAnim(int val)
        {
            sb.AppendFormat("{{actorAnim{0}}}", val);
        }

        #endregion

    }
}

