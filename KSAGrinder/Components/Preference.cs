using System.ComponentModel;

namespace KSAGrinder.Components
{
    public enum Preference
    {
        [Description("1교시 공강 많음")]
        Empty1 = 0,

        [Description("4교시 공강 많음")]
        Empty4,

        [Description("5교시 공강 많음")]
        Empty5,

        [Description("수업 빨리 끝남")]
        Compact
    }
}