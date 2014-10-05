
namespace NScumm.Dump
{
    public abstract class Statement: AstNodeBase
    {
        public long? StartOffset { get; set; }

        public long? EndOffset { get; set; }

        public bool Contains(long offset)
        {
            return StartOffset.HasValue && EndOffset.HasValue && StartOffset.Value <= offset && offset < EndOffset.Value;
        }
    }
}

