namespace NScumm.Core
{
    public interface ITraceFactory
    {
        ITrace CreateTrace(IEnableTrace trace);
    }
}