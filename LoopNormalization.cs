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
            var statements = new List<string>(code);
            // counts the number of for loops
            var count = Regex.Matches(string.Join(" ", statements), @"\b" + "for" + @"\b").Count;
            var loopCount = 0;
            //
            var loopIndexChange = new List<LoopIndexChange>(count);
            var loopCurrent = 0;
            var endforIndex = statements.FindLastIndex(x => Regex.IsMatch(x, "\\b" + "endfor" + "\\b"));
            var endFor = new List<EndFor>(count);

            statements.ForEach(x =>
                {
                    var original = x;
                    x = x.Trim(' ', '\t');
                    var pattern = @"\b" + "for" + @"\b";
                    var index = statements.IndexOf(original);

                    if (Regex.IsMatch(x, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace))
                    {
                        var lowerBound = x.Substring(x.IndexOf("=") + 1, x.IndexOf("to") - x.IndexOf("=") - 1).Trim();
                        var by = x.IndexOf("by");
                        var byValue = by > -1 ? x.Substring(by + 2).Trim() : string.Empty;

                        if (int.Parse(lowerBound) == 1 && by < 0)
                        {
                            loopCount++;
                        }
                        else if (by > 0 && (int.Parse(byValue)) == 1)
                        {
                            loopCount++;

                            statements[index] = statements[index].Remove(statements[index].IndexOf("by"));

                            Console.WriteLine(true);
                        }
                        else
                        {
                            var name = x.Substring(3, x.IndexOf("=") - 3);
                            loopIndexChange.Add(new LoopIndexChange { ForLoopName = name });

                            var variable = x.Substring(4, x.IndexOf("to") - 4);
                            variable = variable.Replace(lowerBound.Trim(), "1");

                            var stepSize = by > 0 ? byValue : "1";

                            var upperBound = x.Substring(x.IndexOf("to") + 2, x.Contains("by") ? x.IndexOf("by") - x.IndexOf("to") - 2 : x.Length - x.IndexOf("to") - 2);

                            var lb = int.Parse(lowerBound);
                            var ss = int.Parse(stepSize);
                            var ub = int.Parse(upperBound);

                            if (by > 0)
                            {
                                original = original.Remove(original.IndexOf("by"));
                            }

                            original = original.Replace(upperBound, " " + ((ub - lb + ss) / ss).ToString() + " ");

                            var statement = original.Substring(original.IndexOf("for") + "for".Length, original.IndexOf("to") - original.IndexOf("for") - "for".Length);

                            original = original.Replace(statement, " " + variable.Trim() + " ");

                            statements[index] = original;

                            var otherIndex = loopIndexChange.FindIndex(s => s.ForLoopName == name ? true : false);

                            loopIndexChange[otherIndex].NewValue = stepSize.Trim() + " * " + loopIndexChange[otherIndex].ForLoopName.Trim() + " - " + (ss - lb).ToString();

                            if (loopCurrent == 0)
                            {
                                endFor.Add(new EndFor { Index = endforIndex, EndingValue = "let " + loopIndexChange[otherIndex].ForLoopName.Trim() + " = " + name.Trim() + " * " + stepSize + " - " + stepSize + " + " + lowerBound });
                            }
                            else if (loopCurrent == 1 && loopCurrent + 1 != count)
                            {
                                var first = statements.FindIndex(s => Regex.IsMatch(s, "\\b" + "endfor" + "\\b"));
                                endforIndex = statements.FindIndex(first + 1, s => Regex.IsMatch(s, "\\b" + "endfor" + "\\b"));
                                endFor.Add(new EndFor { Index = endforIndex, EndingValue = "let " + loopIndexChange[otherIndex].ForLoopName.Trim() + " = " + name.Trim() + " * " + stepSize + " - " + stepSize + " + " + lowerBound });
                            }
                            else
                            {
                                endforIndex = statements.FindIndex(s => Regex.IsMatch(s, "\\b" + "endfor" + "\\b"));
                                endFor.Add(new EndFor { Index = endforIndex, EndingValue = "let " + loopIndexChange[otherIndex].ForLoopName.Trim() + " = " + name.Trim() + " * " + stepSize + " - " + stepSize + " + " + lowerBound });
                            }
                        }

                        ++loopCurrent;
                    }
                    else if (x.StartsWith("let"))
                    {
                        foreach (var item in loopIndexChange)
                        {
                            if (Regex.IsMatch(statements[index], "\\b" + item.ForLoopName.Trim() + "\\b"))
                            {
                                original = Regex.Replace(statements[index], "\\b" + item.ForLoopName.Trim() + "\\b", "(" + item.NewValue.Trim() + ")");
                                statements[index] = original;

                                if (original.Contains("(" + item.NewValue.Trim() + ") * .5"))
                                {
                                    original = original.Replace("(" + item.NewValue.Trim() + ") * .5", "(" + item.NewValue.Trim() + " * .5)");
                                    Console.WriteLine(true);
                                }

                                if (original.Contains("- (" + item.NewValue.Trim() + " * .5)"))
                                {
                                    original = original.Replace("- (" + item.NewValue.Trim() + ") * .5", "(- " + item.NewValue.Trim() + " * .5)");
                                    Console.WriteLine(true);
                                }

                                statements[index] = original;
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
                statements.Insert(item.Index, item.EndingValue);
            }

            if (count == loopCount)
            {
                statements.Insert(0, "All loops are already normalized");
            }
            else
            {
                statements.Insert(0, loopCount.ToString() + " loops are already normalized");
            }

            return statements.ToArray();
        }

        private static int Count(string statement, string pattern)
        {
            var count = 0;
            var  i = 0;
            while ((i = statement.IndexOf(pattern, i)) != -1)
            {
                i += pattern.Length;
                count++;
            }

            return count;
        }

        /// <summary>
        /// the loops name 
        /// </summary>
        private class LoopIndexChange
        {
            public string ForLoopName { get; set; }
            public string NewValue { get; set; }
        }

        /// <summary>
        /// 
        /// </summary>
        private class EndFor : IComparable<EndFor>
        {
            public int Index { get; set; }
            public string EndingValue { get; set; }

            public int CompareTo(EndFor b)
            {
                return this.Index.CompareTo(b.Index);
            }
        }
    }
}