namespace Atc.Installer.Wpf.ComponentProvider;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "OK.")]
[SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "OK.")]
public static class RegexPatternConstants
{
    public static class Boolean
    {
        public const string Base = "True|False";
        public const string Strict = $"^({Base})$";
        public const string Optional = $"^(({Base})?)$";
    }

    public static class Numbers
    {
        public const string Base = @"-?\d+";
        public const string Strict = $"^{Base}$";
        public const string Optional = $"^({Base})?$";

        public const string PositiveBase = @"\d+";
        public const string PositiveStrict = $"^{PositiveBase}$";
        public const string PositiveOptional = $"^({PositiveBase})?$";
    }

    public static class Time
    {
        public const string Base = "([01]?[0-9]|2[0-3]):[0-5][0-9](:[0-5][0-9])?";
        public const string Strict = $"^{Base}$";
        public const string Optional = $"^({Base})?$";
    }
}