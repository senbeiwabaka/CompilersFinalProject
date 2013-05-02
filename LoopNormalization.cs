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
            code = null;
            // counts the number of for loops
            var count = Regex.Matches(string.Join(" ", statements), @"\b" + "for" + @"\b").Count;
            var loopCount = 0;
            //
            var loopIndexChange = new List<LoopIndexChange>(count);
            var loopCurrent = 0;
            var endforIndex = statements.FindLastIndex(x => Regex.IsMatch(x, "\\b" + "endfor" + "\\b"));
            var endFor = new List<EndFor>(count);
            var ss = 0;
            var lb = 0;
            var ub = 0;

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

                            lb = int.Parse(lowerBound);
                            ss = int.Parse(stepSize);
                            ub = int.Parse(upperBound);

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
                        for (var i = 0; i < loopIndexChange.Count; ++i)
                        {
                            if (Regex.IsMatch(statements[index], "\\b" + loopIndexChange[i].ForLoopName.Trim() + "\\b"))
                            {
                                var change = string.Empty;
                                var secondchange = string.Empty;
                                original = Regex.Replace(statements[index], "\\b" + loopIndexChange[i].ForLoopName.Trim() + "\\b", loopIndexChange[i].NewValue.Trim());
                                statements[index] = original;

                                if (original.Contains(loopIndexChange[i].NewValue.Trim() + " * .5"))
                                {
                                    if (original.Contains("- " + loopIndexChange[i].NewValue.Trim()))
                                    {
                                        secondchange = "+ " + (-1 * ss).ToString() + " * " + loopIndexChange[i].ForLoopName.Trim() + " + " + (ss - lb).ToString();
                                        original = original.Replace("- " + loopIndexChange[i].NewValue.Trim() + " * .5", secondchange);
                                        Console.WriteLine(true);
                                        change = "+ " + (ss * .5).ToString() + " * " + loopIndexChange[i].ForLoopName.Trim() + " + " + ((ss - lb) * .5).ToString();
                                        Console.WriteLine(change);
                                        original = original.Replace(secondchange, change);
                                        Console.WriteLine(true);
                                    }
                                    else
                                    {
                                        change = (ss * .5).ToString() + " * " + loopIndexChange[i].ForLoopName.Trim() + " - " + ((ss - lb) * .5).ToString();
                                        Console.WriteLine(change);
                                        original = original.Replace(secondchange, change);
                                        Console.WriteLine(true);
                                    }
                                }

                                //if (!string.IsNullOrEmpty(change) && original.Contains("- " + change))
                                //{
                                //    original = original.Replace("- " + change, " + (- " + change + ")");
                                //    Console.WriteLine(true);
                                //}

                                statements[index] = original;
                                change = string.Empty;
                                secondchange = string.Empty;
                            }
                        }
                    }
                });

            endFor.Sort();
            loopIndexChange.Clear();
            loopIndexChange = null;

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