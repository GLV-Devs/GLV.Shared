namespace GLV.Shared.Server.API.Logging;

public class DefaultExceptionDumpFormatter : IExceptionDumpFormatter
{
    public virtual string GetName(DateTimeOffset dateTimeThrown, Guid id, Exception e)
        => $"{e.GetType().Name} {dateTimeThrown:dd_MM_yyyy__HH_mm_ss_fffffff} {id}";

    public static void InternalWriteDump(Exception e, TextWriter writer, int tabs, bool skipFirstTabs)
    {
        if (skipFirstTabs is false)
            WriteTabs();

        writer.WriteLine(e.ToString());

        if (e.InnerException is Exception ie)
        {
            WriteTabs();
            writer.Write("Inner: ");
            InternalWriteDump(ie, writer, tabs + 1, true);
        }

        if (e is AggregateException excp)
        {
            WriteTabs();
            writer.WriteLine("Aggregated: ");
            int i = 0;
            foreach (var ae in excp.InnerExceptions)
            {
                WriteTabs();
                writer.Write($"{++i}: ");
                InternalWriteDump(ae, writer, tabs + 1, true);
            }
        }

        writer.WriteLine();

        void WriteTabs()
        {
            for (int t = 0; t < tabs; t++)
                writer.Write("\t");
        }
    }

    public virtual string GetDumpContent(Exception e)
    {
        using var writer = new StringWriter();
        InternalWriteDump(e, writer, 0, false);
        return writer.ToString();
    }

    public virtual void WriteDump(Exception e, TextWriter writer)
    {
        InternalWriteDump(e, writer, 0, false);
    }
}
