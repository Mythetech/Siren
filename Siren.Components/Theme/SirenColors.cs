using System;
namespace Siren.Components.Theme
{
    public static class SirenColors
    {
        public static class Dark
        {
            //public static string Primary => "03045e";
            public static string Primary => "00001A";

            public static string Secondary => "640ADB";

            public static string Tertiary => "002538";

            public static string AppBarBackground => "00162E";

            //public static string AppBarBackground => "001520";

        }

        public static string Primary => "0077B6";

        public static string Secondary => "170035";

        public static string Tertiary => "AED2E5";

        public static string Light => "caf0f8";

        public static string Darken => "00001A";

        public static string Black => "000000";

        public static string Action => "";

        public static string ActionDisabled => "AED2E5";

        public static string Hash(string s)
        {
            return $"#{s}";
        }
    }
}

