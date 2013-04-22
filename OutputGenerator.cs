using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CompilersFinalProject
{
    public class OutputGenerator
    {
        public static string[] Generate(List<string> code)
        {
            // the output to be retuned to the caller
            var output = new List<string>();
            // to hold the statements between var and begin
            var variable = new List<Variable>();
            var count = 0;
            // array initialized to the size of the code and is to hold only the for loops
            var forloops = new List<string>(code.Count);

            for (int i = 0; i < forloops.Capacity; i++)
            {
                forloops.Add(string.Empty);
            }

            // needed because the copy methods require [] instead of lists
            var arr = forloops.ToArray();

            //copys only the for loops to the end of the program (based on midterm programs) to the string[] arr array
            code.CopyTo(code.FindIndex(x => Regex.IsMatch(x, "\\b" + "for" + "\\b")), arr, code.FindIndex(x => Regex.IsMatch(x, "\\b" + "for" + "\\b")), code.FindIndex(x => Regex.IsMatch(x, "\\b" + "end" + "\\b")) - code.FindIndex(x => Regex.IsMatch(x, "\\b" + "for" + "\\b")));

            // sets the forloops array to the contents of arr
            forloops = new List<string>(arr);

            arr = null;

            // returns a string array of all the variable statements listed below
            var variables = code.FindAll(x => x.Contains("int") || x.Contains("list") || x.Contains("table") || x.Contains("box"));

            // takes all the variables from this array and adds them to the array of type Variable along with their position and type
            variables.ForEach(x => 
                {
                    x = x.Trim(' ', '\t');

                    if (x.StartsWith("int"))
                    {
                        variable.Add(new Variable { Name = x.Substring(4), Position = count++, Type = "int", TypeInt = 0 });
                    }
                    else if (x.StartsWith("list"))
                    {
                        variable.Add(new Variable { Name = x.Substring(x.IndexOf("]") + 1), Position = count++, Type = "list", TypeInt = 1 });
                    }
                    else if (x.StartsWith("table"))
                    {
                        variable.Add(new Variable { Name = x.Substring(x.IndexOf("]") + 1), Position = count++, Type = "table", TypeInt = 2 });
                    }
                    else if (x.StartsWith("box"))
                    {
                        variable.Add(new Variable { Name = x.Substring(x.IndexOf("]") + 1), Position = count++, Type = "box", TypeInt = 3 });
                    }
                });

            // array to hold the read and write information
            var lineinfo = new List<LineInformation>();

            //"dynamic" array for N number of upper bounds
            var loops = new int[Regex.Matches(string.Join("\n", forloops), "\\b" + "for" + "\\b").Count];

            count = 0;

            var loopIndex = new List<LoopInformation>(Regex.Matches(string.Join("\n", forloops), "\\b" + "for" + "\\b").Count);

            count =0;

            forloops.ForEach(x => 
                {
                    if (Regex.IsMatch(x, "\\b" + "for" + "\\b"))
                    {
                        loopIndex.Add(new LoopInformation { LoopIndex = x.Substring(x.IndexOf("for") + 4, x.IndexOf("=") - x.IndexOf("for") - 5), Depth = ++count });
                    }
                });

            count = 0;

            forloops.ForEach(x =>
                {
                    var index = forloops.IndexOf(x);

                    var original = x;

                    x = x.Trim(' ', '\t');

                    if (x.StartsWith("for"))
                    {
                        // adds the loop upper bounds to the list
                        loops[count++] = int.Parse(x.Substring(x.IndexOf("to") + 2));
                    }
                    else if (x.StartsWith("let"))
                    {
                        var beforeEqual = x.Substring(0, x.IndexOf("="));
                        var afterEqual = x.Substring(x.IndexOf("=") + 1);

                        // checks to see if the variable is an array, if not, it is removed
                        if (variable.Exists(s => s.Name == beforeEqual.Substring(3).Trim() && (s.TypeInt != 1 || s.TypeInt != 2 || s.TypeInt != 3)))
                        {
                            forloops[index] = string.Empty;
                            beforeEqual = string.Empty;
                            afterEqual = string.Empty;
                        }

                        if(!string.IsNullOrEmpty(beforeEqual))
                        {
                            // finds the array that is being used in this let statement
                            var v = variable.Find(s => s.Name.Contains(beforeEqual.Substring(3, beforeEqual.IndexOf("[") - 3).Trim()));
                            
                            //counts the loop depth for this array access
                            count = 0;
                            var endfor = 0;
                            for (int i = 0; i < index; i++)
                            {
                                if (Regex.IsMatch(forloops[i], "\\b" + "for" + "\\b"))
                                {
                                    ++count;
                                }

                                if (Regex.IsMatch(forloops[i], "\\b" + "endfor" + "\\b"))
                                {
                                    ++endfor;
                                }
                            }

                            var constant = new List<int>(count);
                            var coefficient=new List<int>(count);

                            if (v.TypeInt == 1)
                            {
                                var listIndex = beforeEqual.Substring(beforeEqual.IndexOf("[") + 1, beforeEqual.IndexOf("]") - beforeEqual.IndexOf("[") - 1);

                                ReadWriteLoopInformation(count, loopIndex, endfor, constant, coefficient, listIndex);

                            }
                            else
                            {
                                var substring = beforeEqual.Substring(beforeEqual.IndexOf("[") + 1, beforeEqual.IndexOf(",") - beforeEqual.IndexOf("[") - 1);
                                var commaIndex=beforeEqual.IndexOf(",");

                                var commaCount = beforeEqual.ToCharArray().Count(c => c == ',');
                                var currentComma = 0;

                                while (currentComma <= commaCount)
                                {
                                    ReadWriteLoopInformation(count, loopIndex, endfor, constant, coefficient, substring);

                                    if (beforeEqual.IndexOf(",", commaIndex + 1) > 1)
                                    {
                                        substring = beforeEqual.Substring(commaIndex + 2, beforeEqual.IndexOf(",", commaIndex + 1) > 0 ? beforeEqual.IndexOf(",", commaIndex + 1) - commaIndex - 2: beforeEqual.IndexOf("]") - commaIndex - 2);

                                        commaIndex = beforeEqual.IndexOf(",", commaIndex + 1);
                                        ++currentComma;
                                    }
                                    else
                                    {
                                        substring = beforeEqual.Substring(commaIndex + 2, beforeEqual.IndexOf("]") - commaIndex - 2);
                                        ++currentComma;
                                    }
                                }
                            }

                            var const_coeff = string.Empty;

                            for (int i = 0; i < constant.Count; i++)
                            {
                                const_coeff += constant[i] + " ";

                                for (int j = 0; j < count - endfor; j++)
                                {
                                    const_coeff += coefficient[j] + " ";
                                }

                                coefficient.RemoveRange(0, count - endfor);
                            }

                            lineinfo.Add(new LineInformation { LineNumber = index, Array = v.Position, LoopDepth = (count - endfor), Constant_Coeffient = const_coeff, Write = true });
                        }

                        if (!string.IsNullOrEmpty(afterEqual))
                        {

                        }
                    }
                });

            #region information added to output

            InformationOutput(output, variable, lineinfo, loops);

            #endregion

            return output.ToArray();
        }

        private static void ReadWriteLoopInformation(int count, List<LoopInformation> loopIndex, int endfor, List<int> constant, List<int> coefficient, string listIndex)
        {
            if (listIndex.Length == 1)
            {
                var li = loopIndex.Find(s => s.LoopIndex == listIndex.Trim());

                if (li != null)
                {
                    constant.Add(0);
                    var depth = 1;
                    while (depth < li.Depth)
                    {
                        coefficient.Add(0);
                        ++depth;
                    }
                    coefficient.Add(1);
                    depth = li.Depth;
                    while (depth < (count - endfor))
                    {
                        coefficient.Add(0);
                        ++depth;
                    }
                }
                else
                {
                    constant.Add(int.Parse(listIndex));
                    var depth = 1;
                    while (depth < (count - endfor))
                    {
                        coefficient.Add(0);
                        ++depth;
                    }
                }
            }
            else
            {
                var notModified = listIndex;
                var words = new List<string>();

                ValueExtration(words, listIndex);

                var result = 0;
                var depth = 1;

                var k = 0;
                while (k < words.Count)
                {
                    var li = loopIndex.Find(s => s.LoopIndex == words[k].Trim());
                    if (li != null)
                    {
                        while (depth < li.Depth)
                        {
                            coefficient.Add(0);
                            ++depth;
                        }
                        if (k > 0)
                        {
                            if (words[k - 1] == "-")
                            {
                                coefficient.Add(-1);
                                words.RemoveRange(k - 1, 2);
                            }
                            else if (words[k - 1] == "*")
                            {
                                coefficient.Add(int.Parse(words[k - 2]));
                                words.RemoveRange(k - 2, 3);
                            }
                            else
                            {
                                coefficient.Add(1);
                                words.RemoveAt(k);
                            }

                            k = 0;
                        }
                        else
                        {
                            coefficient.Add(1);
                            words.RemoveAt(k);
                            k = 0;
                            //++depth;
                            for (int i = 0; i < words.Count; i++)
                            {
                                if (loopIndex.Exists(x => x.LoopIndex == words[i]))
                                {
                                    depth = loopIndex.Find(x => x.LoopIndex == words[i]).Depth;
                                }
                                else
                                {
                                    depth = li.Depth;
                                }
                            }
                            //depth = li.Depth;
                            while (depth < (count - endfor))
                            {
                                coefficient.Add(0);
                                ++depth;
                            }
                            //++depth;
                        }

                        //depth = li.Depth;
                        //while (depth < (count - endfor))
                        //{
                        //    coefficient.Add(0);
                        //    ++depth;
                        //}
                    }
                    else
                    {
                        k++;
                    }
                }

                k = 0;
                while (k < words.Count)
                {
                    if (int.TryParse(words[k], out result))
                    {
                        if (k > 0)
                        {
                            if (words[k - 1] == "-")
                            {
                                constant.Add(-result);
                                words.RemoveRange(k - 1, 2);
                            }
                            else if (words[k - 1] == "*")
                            {
                                constant.Add(int.Parse(words[k - 2]) * result);
                                words.RemoveRange(k - 2, 3);
                            }
                            else
                            {
                                constant.Add(result);
                                words.RemoveAt(k);
                            }

                            k = 0;
                        }
                    }
                    else
                    {
                        k++;
                    }
                }
                var amount = constant.Sum();
                constant.Clear();
                constant.Add(amount);
            }
        }

        private static void InformationOutput(List<string> output, List<Variable> variable, List<LineInformation> lineinfo, int[] loops)
        {
            var variablePositions = string.Empty;

            foreach (var item in variable)
            {
                variablePositions += item.Position.ToString() + " ";
            }

            output.Add(variablePositions);

            foreach (var item in loops)
            {
                output.Add(item.ToString());
            }

            output.Add("0");

            foreach (var item in lineinfo)
            {
                if (item.Write)
                {
                    //var 
                    output.Add(item.LineNumber + " " + item.Array + " " + item.LoopDepth + " " + item.Constant_Coeffient);
                }
            }

            output.Add("0");

            foreach (var item in lineinfo)
            {
                if (!item.Write)
                {
                    //var 
                    output.Add(item.LineNumber.ToString() + " " + item.Array.ToString() + " " + item.LoopDepth);
                }
            }
        }

        /// <summary>
        /// creates a list of the variables/constants in a non 3OP statement
        /// </summary>
        /// <param name="words">returned list of the statement</param>
        /// <param name="statment">statement of variables/constants to be separated</param>
        private static void ValueExtration(List<string> words, string statment)
        {
            while (statment.Length > 0)
            {
                if (statment.IndexOf(" ", StringComparison.Ordinal) < 0)
                {
                    words.Add(statment);
                    statment = "";
                }
                else
                {
                    words.Add(statment.Substring(0, statment.IndexOf(" ")));
                    statment = statment.Substring(statment.IndexOf(" ") + 1);
                }
            }
        }

        private class LoopInformation
        {
            public string LoopIndex { get; set; }
            public int Depth { get; set; }
        }
    }

    /// <summary>
    /// Holds the variable information such as the name (int x), type (list, box), type int (1, 2), and position such as if there are 100 variables, which one are we accessing for the array
    /// </summary>
    public class Variable
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public int TypeInt { get; set; }
        public int Position { get; set; }
    }

    /// <summary>
    /// holds the read and write information
    /// </summary>
    public class LineInformation
    {
        public int LineNumber { get; set; }
        public int Array { get; set; }
        public int LoopDepth { get; set; }
        public string Constant_Coeffient { get; set; }
        public bool Write { get; set; }
    }
}