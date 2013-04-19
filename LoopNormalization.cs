using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CompilersFinalProject
{
    public class LoopNormalization
    {
        public static string[] Normalize(string[] code)
        {
            var loops = new List<string>(code);

            loops.ForEach(x =>
                {
                    var original = x;
                    x = x.Trim(' ', '\t');
                    var pattern = @"\b" + "for" + @"\b";
                    if (Regex.IsMatch(x, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace))
                    {
                        var value = x.Substring(x.IndexOf("=") + 1, x.IndexOf(" ", x.IndexOf("=") + 1) - x.IndexOf("=") + 1);
                        Console.WriteLine(value);
                    }
                });

            return code;
        }
    }
}
