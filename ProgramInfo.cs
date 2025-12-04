using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FTDMapgen_WinForms
{
    public static class ProgramInfo
    {
        //DELIVERED FOR BURORUS CONFEDERATION BY OIL REICH IN DECEMBER 2025
        //GLORY TO THE PARTY
        //project started at 26.11.2025

        public static string Name = "From the Maps";
        public static string Subname = "Dnische Terrain Editor"; //=DTE
        public static float Version = 0.9f;
        public static string[] Authors = new string[] { "Dark Spark | Dnischenfuhrer and the Great Rod Necron" }; //unnecessary self-glorification is always welcomed?
    }

    public class NonStaticProgramInfo
    {
        [JsonPropertyName("RedactorName")]
        public string Name { get; set; }
        [JsonPropertyName("RedactorSubName")]
        public string Subname { get; set; }
        [JsonPropertyName("Version")]
        public float Version { get; set; }
        [JsonPropertyName("AuthorList")]
        public string[] Authors { get; set; }

        public void refresh()
        {
            this.Name = ProgramInfo.Name;
            this.Subname = ProgramInfo.Subname;
            this.Version = ProgramInfo.Version;
            this.Authors = ProgramInfo.Authors;
        }
    }
}
