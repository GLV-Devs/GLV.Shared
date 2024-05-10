using System.Reflection;

namespace GLV.Shared.Common;

public static class AssemblyInfo
{
    public const string VersionStringFormat = "{0} ({1})";

    public sealed class AssemblyInfoSnapshot
    {
        internal AssemblyInfoSnapshot(Assembly assembly)
        {
            Version = GetVersion(assembly).ToString();
            AppTitle = GetAssemblyTitle(assembly);
            Author = GetAssemblyAuthors(assembly).AuthorString;
            Company = GetAssemblyCompany(assembly);
            Copyright = GetAssemblyCopyright(assembly);
        }

        public string Version { get; }
        public string AppTitle { get; }
        public string Author { get; }
        public string Company { get; }
        public string Copyright { get; }
    }

    #region Implicit Assembly

    public static AssemblyInfoSnapshot GetInfoSnapshot()
        => new(Assembly.GetCallingAssembly());

    public static Version GetVersion()
        => GetVersion(Assembly.GetCallingAssembly());

    public static DateTime GetBuildDate()
        => GetBuildDate(GetVersion());

    public static string GetVersionString()
        => GetVersionString(GetVersion());

    public static string GetAssemblyTitle()
        => GetAssemblyTitle(Assembly.GetCallingAssembly());

    public static string GetAssemblyCopyright()
        => GetAssemblyCopyright(Assembly.GetCallingAssembly());

    public static string GetAssemblyCompany()
        => GetAssemblyCompany(Assembly.GetCallingAssembly());

    public static AssemblyAuthorAttribute GetAssemblyAuthors()
        => GetAssemblyAuthors(Assembly.GetCallingAssembly());

    #endregion

    public static AssemblyInfoSnapshot GetInfoSnapshot(Assembly assembly)
        => new(assembly);

    public static string GetAssemblyTitle(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        return assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title
            ?? throw new ArgumentException("The assembly does not have any title info", nameof(assembly));
    }

    public static string GetAssemblyCopyright(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        return assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright
            ?? throw new ArgumentException("The assembly does not have any copyright info", nameof(assembly));
    }

    public static string GetAssemblyCompany(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        return assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company
            ?? throw new ArgumentException("The assembly does not have any company info", nameof(assembly));
    }

    public static AssemblyAuthorAttribute GetAssemblyAuthors(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        return assembly.GetCustomAttribute<AssemblyAuthorAttribute>()
            ?? throw new ArgumentException("The assembly does not have any author info", nameof(assembly));
    }

    public static Version GetVersion(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        return assembly.GetName().Version ?? throw new ArgumentException("The assembly does not have any version info", nameof(assembly));
    }

    public static DateTime GetBuildDate(Assembly assembly)
        => GetBuildDate(GetVersion(assembly));

    public static string GetVersionString(Assembly assembly)
        => GetVersionString(GetVersion(assembly));

    public static DateTime GetBuildDate(Version version)
    {
        ArgumentNullException.ThrowIfNull(version);

        DateTime buildDate = new DateTime(2000, 1, 1).AddDays(version.Build).AddSeconds(version.Revision * 2);
        return buildDate;
    }

    public static string GetVersionString(Version version)
        => string.Format(VersionStringFormat, version, GetVersionString(version));
}
