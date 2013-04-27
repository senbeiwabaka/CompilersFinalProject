using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CompilersFinalProject
{
    public class LoopNormalization
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static string[] Normalize(string[] code)
        {
            var loops = new List<string>(code);
            // counts the number of for loops
            var count = Regex.Matches(string.Join(" ", loops), @"\b" + "for" + @"\b").Count;
            var loopCount = 0;
            //
            var change = new List<LoopNameValue>(count);
            var loopCurrent = 0;
            var endforIndex = loops.FindLastIndex(x => Regex.IsMatch(x, "\\b" + "endfor" + "\\b"));
            var endFor = new List<EndFor>(count);

            loops.ForEach(x =>
                {
                    var original = x;
                    x = x.Trim(' ', '\t');
                    var pattern = @"\b" + "for" + @"\b";
                    var index = loops.IndexOf(original);

                    if (Regex.IsMatch(x, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace))
                    {
                        var lowerBound = x.Substring(x.IndexOf("=") + 1, x.IndexOf("to") - x.IndexOf("=") - 1);

                        if (int.Parse(lowerBound) == 1)
                        {
                            loopCount++;
                            Console.WriteLine(true);
                        }
                        else
                        {
                            var name = x.Substring(3, x.IndexOf("=") - 3);
                            change.Add(new LoopNameValue { Name = name });

                            var variable = x.Substring(4, x.IndexOf("to") - 4);
                            variable = variable.Replace(lowerBound.Trim(), "1");

                            var stepSize = x.Contains("by") ? x.Substring(x.IndexOf("by") + "by".Length) : "1";

                            var upperBound = x.Substring(x.IndexOf("to") + 2, x.Contains("by") ? x.IndexOf("by") - x.IndexOf("to") - 2 : x.Length - x.IndexOf("to") - 2);

                            var lb = int.Parse(lowerBound);
                            var ss = int.Parse(stepSize);
                            var ub = int.Parse(upperBound);

                            original = original.Replace("by" + stepSize, "");

                            original = original.Replace(upperBound, " " + ((ub - lb + ss) / ss).ToString() + " ");

                            var statement = original.Substring(original.IndexOf("for") + "for".Length, original.IndexOf("to") - original.IndexOf("for") - "for".Length);

                            original = original.Replace(statement, " " + variable.Trim() + " ");

                            loops[index] = original;

                            var otherIndex = change.FindIndex(s => s.Name == name ? true : false);

                            change[otherIndex].Value = stepSize.Trim() + " * " + change[otherIndex].Name.Trim() + " - " + (ss - lb).ToString();

                            if (loopCurrent == 0)
                            {
                                endFor.Add(new EndFor { Index = endforIndex, Value = "let " + change[otherIndex].Name.Trim() + " = " + name.Trim() + " * " + stepSize + " - " + stepSize + " + " + lowerBound });
                            }
                            else if (loopCurrent == 1 && loopCurrent + 1 != count)
                            {
                                var first = loops.FindIndex(s => Regex.IsMatch(s, "\\b" + "endfor" + "\\b"));
                                endforIndex = loops.FindIndex(first + 1, s => Regex.IsMatch(s, "\\b" + "endfor" + "\\b"));
                                endFor.Add(new EndFor { Index = endforIndex, Value = "let " + change[otherIndex].Name.Trim() + " = " + name.Trim() + " * " + stepSize + " - " + stepSize + " + " + lowerBound });
                            }
                            else
                            {
                                endforIndex = loops.FindIndex(s => Regex.IsMatch(s, "\\b" + "endfor" + "\\b"));
                                endFor.Add(new EndFor { Index = endforIndex, Value = "let " + change[otherIndex].Name.Trim() + " = " + name.Trim() + " * " + stepSize + " - " + stepSize + " + " + lowerBound });
                            }
                        }

                        ++loopCurrent;
                    }
                    else if (x.StartsWith("let"))
                    {
                        foreach (var item in change)
                        {
                            if (Regex.IsMatch(loops[index], "\\b" + item.Name.Trim() + "\\b"))
                            {
                                loops[index] = Regex.Replace(loops[index], "\\b" + item.Name.Trim() + "\\b", item.Value.Trim());
                            }
                        }
                    }
                });

            endFor.Sort();

            for (int i = 0; i < endFor.Count; i++)
            {
                endFor[i].Index++;
                endFor[i].Index += i;
            }

            foreach (var item in endFor)
            {
                loops.Insert(item.Index, item.Value);
            }

            if (count == loopCount)
            {
                loops.Insert(0, "All loops are already normalized");
            }
            else
            {
                loops.Insert(0, loopCount.ToString() + " loops are already normalized");
            }

            return loops.ToArray();
        }
    }

    /// <summary>
    /// the loops name 
    /// </summary>
    public class LoopNameValue
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class EndFor : IComparable<EndFor>
    {
        public int Index { get; set; }
        public string Value { get; set; }

        public int CompareTo(EndFor b)
        {
            return this.Index.CompareTo(b.Index);
        }
    }
}