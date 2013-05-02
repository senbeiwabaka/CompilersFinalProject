using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CompilersFinalProject
{
    public class OutputGenerator
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static string[] Generate(List<string> code)
        {
            //code.RemoveAt(0);
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
                    x = x.Trim(' ', '\t');
                    if (x.StartsWith("for"))
                    {
                        loops[count++] = int.Parse(x.Substring(x.IndexOf("to") + 2));
                    }
                });

            count = 0;

            forloops.ForEach(x =>
                {
                    var index = forloops.IndexOf(x);

                    var original = x;

                    x = x.Trim(' ', '\t');

                    if (x.StartsWith("let"))
                    {
                        var beforeEqual = x.Substring(0, x.IndexOf("="));
                        var afterEqual = x.Substring(x.IndexOf("=") + 2);

                        // checks to see if the variable is an array, if not, it is removed
                        if (variable.Exists(s => s.Name == beforeEqual.Substring(3).Trim() && (s.TypeInt != 1 || s.TypeInt != 2 || s.TypeInt != 3)))
                        {
                            forloops[index] = string.Empty;
                            beforeEqual = string.Empty;
                            afterEqual = string.Empty;
                        }

                        Variable array = null;
                        var endfor = 0;
                        var const_coeff = string.Empty;

                        if (!string.IsNullOrEmpty(beforeEqual))
                        {
                            count = LineGeneration(variable, count, forloops, loopIndex, index, beforeEqual.Substring(4).Trim(), out array, out endfor, out const_coeff);

                            lineinfo.Add(new LineInformation { LineNumber = index, Array = array.Position, LoopDepth = (count - endfor), Constant_Coeffient = const_coeff, Write = true });
                        }

                        if (!string.IsNullOrEmpty(afterEqual))
                        {
                            var bracketIndex = afterEqual.IndexOf("]") + 1;
                            var statement = afterEqual.Substring(0, bracketIndex);

                            while (Regex.Matches(afterEqual, "[[]").Count > 0)
                            {
                                count = LineGeneration(variable, count, forloops, loopIndex, index, afterEqual.Substring(0, afterEqual.IndexOf("]") + 1), out array, out endfor, out const_coeff);

                                lineinfo.Add(new LineInformation { LineNumber = index, Array = array.Position, LoopDepth = (count - endfor), Constant_Coeffient = const_coeff, Write = false });

                                if (afterEqual.IndexOf("]") + 1 >= afterEqual.Length || afterEqual.ToCharArray().Count(s => s.Equals("]")) < 0)
                                {
                                    afterEqual = string.Empty;
                                }
                                else
                                {
                                    afterEqual = afterEqual.Substring(afterEqual.IndexOf("]") + 3);
                                }
                            }
                        }
                    }
                });

            #region information added to output

            InformationOutput(output, variable, lineinfo, loops);

            #endregion

            return output.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="variable"></param>
        /// <param name="count"></param>
        /// <param name="forloops"></param>
        /// <param name="loopIndex"></param>
        /// <param name="index"></param>
        /// <param name="statement"></param>
        /// <param name="array"></param>
        /// <param name="endfor"></param>
        /// <param name="const_coeff"></param>
        /// <returns></returns>
        private static int LineGeneration(List<Variable> variable, int count, List<string> forloops, List<LoopInformation> loopIndex, int index, string statement, out Variable array, out int endfor, out string const_coeff)
        {
            array = variable.Find(s => s.Name.Contains(statement.Substring(0, statement.IndexOf("["))));
            //counts of the for and endfor
            count = 0;
            endfor = 0;

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

            var constant = new List<int>(count * array.TypeInt);
            var coefficient = new List<int>(count * array.TypeInt);

            var arraySubstringValue = statement.Substring(statement.IndexOf("[") + 1, array.TypeInt == 1 ? statement.IndexOf("]") - statement.IndexOf("[") - 1 : statement.IndexOf(",") - statement.IndexOf("[") - 1);
            var commaIndex = statement.IndexOf(",");
            var currentComma = 0;

            while (currentComma < array.TypeInt)
            {
                ReadWriteLoopInformation(count, loopIndex, endfor, constant, coefficient, arraySubstringValue, currentComma);

                if (statement.IndexOf(",", commaIndex + 1) > 1)
                {
                    arraySubstringValue = statement.Substring(commaIndex + 2, statement.IndexOf(",", commaIndex + 1) > 0 ? statement.IndexOf(",", commaIndex + 1) - commaIndex - 2 : statement.IndexOf("]") - commaIndex - 2);

                    commaIndex = statement.IndexOf(",", commaIndex + 1);
                    ++currentComma;
                }
                else
                {
                    arraySubstringValue = statement.Substring(commaIndex + 2, statement.IndexOf("]") - commaIndex - 2);
                    ++currentComma;
                }
            }

            const_coeff = string.Empty;

            for (int i = 0; i < constant.Count; i++)
            {
                const_coeff += constant[i] + " ";

                for (int j = 0; j < count - endfor; j++)
                {
                    const_coeff += coefficient[j] + " ";
                }

                coefficient.RemoveRange(0, count - endfor);
            }

            return count;
        }

        /// <summary>
        /// Generates the the constant and coefficients of the current array on the line
        /// </summary>
        /// <param name="count"></param>
        /// <param name="loopIndex"></param>
        /// <param name="endfor"></param>
        /// <param name="constant">the constants for the current array of the line</param>
        /// <param name="coefficient">the coefficients for the current array of the line</param>
        /// <param name="statement">the array statement to fill the constant and coefficient list</param>
        private static void ReadWriteLoopInformation(int count, List<LoopInformation> loopIndex, int endfor, List<int> constant, List<int> coefficient, string statement, int currentDepth)
        {
            var elements = ValueExtration(statement);
            //var k = 0;
            var bigger = false;
            var biggerValue = 0;
            var indexVariable = "";
            if (statement.Contains("("))
            {
                elements = ValueExtration(statement.Substring(statement.IndexOf("(") + 1, statement.IndexOf(")") - statement.IndexOf("(") - 1));
                var s = string.Empty;
                foreach (var item in elements)
                {
                    s += item + " ";
                    if (loopIndex.Exists(x => x.LoopIndex.Trim().Equals(item)))
                    {
                        indexVariable = item.Trim();
                    }
                }
                if (loopIndex.Find(x => x.LoopIndex.Trim().Equals(indexVariable)).Depth > 1)
                {
                    bigger = true;
                }
                ReadWriteHelper(count, loopIndex, constant, coefficient, elements);
                statement = statement.Replace("(" + s.Trim() + ")", constant[0].ToString());
                constant.Clear();
                s = null;
                if (bigger)
                {
                    biggerValue = coefficient[loopIndex.Find(x => x.LoopIndex.Trim().Equals(indexVariable)).Depth - 1];
                    coefficient.Clear();
                }
            }

            ReadWriteHelper(count, loopIndex, constant, coefficient, elements);

            if (bigger)
            {
                coefficient[loopIndex.Find(x => x.LoopIndex.Trim().Equals(indexVariable)).Depth - 1] = biggerValue;
                Console.WriteLine(true);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="count"></param>
        /// <param name="loopIndex"></param>
        /// <param name="constant"></param>
        /// <param name="coefficient"></param>
        /// <param name="elements"></param>
        private static void ReadWriteHelper(int count, List<LoopInformation> loopIndex, List<int> constant, List<int> coefficient, List<string> elements)
        {
            var k = 0;
            var result = 0;
            var depth = 1;
            var loopIndexCount = 0;
            var smallest = new List<LoopInformation>();
            loopIndexCount = ElementChange(elements, loopIndex, smallest);

            if (loopIndexCount == 2 && (smallest.Max().Depth - smallest.Min().Depth) == 1)
            {
                if (smallest.Min().Depth == 1)
                {
                    depth = smallest.Max().Depth;
                }
                else
                {
                    coefficient.Add(0);
                    depth = smallest.Max().Depth;
                }
            }
            else if (loopIndexCount == 2 && (smallest.Max().Depth - smallest.Min().Depth) == 2)
            {
                depth = smallest.Max().Depth - 1;
            }
            else if (loopIndexCount == 3 && (smallest.Max().Depth - smallest.Min().Depth) == 2)
            {
                depth = smallest.Max().Depth - 1;
            }

            foreach (var item in elements)
            {
                Console.WriteLine(item);
            }

            while (k < elements.Count)
            {
                var li = loopIndex.Find(s => s.LoopIndex == elements[k].Trim());

                if (k > 0 && li != null)
                {
                    if (elements[k - 1] == "*")
                    {
                        while (depth < li.Depth)
                        {
                            coefficient.Add(0);
                            ++depth;
                        }

                        if (k - 3 > 0 && elements[k - 3] == "-")
                        {
                            coefficient.Add(int.Parse(elements[k - 2]) * -1);
                            elements.RemoveRange(k - 3, 4);
                        }
                        else if (k - 3 > 0 && elements[k - 3] == "+")
                        {
                            coefficient.Add(int.Parse(elements[k - 2]));
                            elements.RemoveRange(k - 3, 4);
                        }
                        else
                        {
                            coefficient.Add(int.Parse(elements[k - 2]));
                            elements.RemoveRange(k - 2, 3);
                        }

                        k = 0;
                    }
                    else if (elements[k - 1] == "-")
                    {
                        while (depth < li.Depth)
                        {
                            coefficient.Add(0);
                            ++depth;
                        }

                        coefficient.Add(-1);

                        elements.RemoveRange(k - 1, 2);
                        k = 0;
                    }
                    else if (elements[k - 1] == "+")
                    {
                        while (depth < li.Depth)
                        {
                            coefficient.Add(0);
                            ++depth;
                        }

                        coefficient.Add(1);

                        elements.RemoveRange(k - 1, 2);
                        k = 0;
                    }
                }
                else if (k == 0 && li != null)
                {
                    while (depth < li.Depth)
                    {
                        coefficient.Add(0);
                        ++depth;
                    }

                    coefficient.Add(1);

                    if (elements.Count > 1)
                    {
                        if (elements[k + 1] == "+")
                        {
                            elements.RemoveAt(k + 1);
                        }
                    }

                    elements.RemoveAt(k);
                    k = 0;
                }
                else
                {
                    ++k;
                }
            }

            depth = smallest.Count < 1 ? 0 : depth;

            while (depth < count)
            {
                coefficient.Add(0);
                ++depth;
            }

            k = 0;

            if (elements.Count == 0)
            {
                constant.Add(0);
                elements.Clear();
                elements.TrimExcess();
                return;
            }

            if (elements.Count <= 2 && elements.Capacity > 0)
            {
                if (elements[0] == "-")
                {
                    constant.Add(-int.Parse(elements[1]));
                    elements.Clear();
                    return;
                }
                else if (elements[0] == "+")
                {
                    constant.Add(int.Parse(elements[1]));
                    elements.Clear();
                    return;
                }
                else if (int.TryParse(elements[0], out result))
                {
                    constant.Add(result);
                    elements.Clear();
                    return;
                }
            }

            if (elements[0] == "-")
            {
                if (elements[1] == "-")
                {
                    elements.RemoveRange(0, 2);
                }
                else
                {
                    elements.RemoveAt(0);
                    elements[0] = (int.Parse(elements[0]) * -1).ToString();
                }
            }
            else if (elements[0] == "+")
            {
                if (elements[1] == "-")
                {
                    elements.RemoveRange(0, 2);
                    elements[0] = (int.Parse(elements[0]) * -1).ToString();
                }
                else
                {
                    elements.RemoveAt(0);
                    elements[0] = (int.Parse(elements[0]) * -1).ToString();
                }
            }

            result = 0;
            while (elements.Count > 0 && elements.Count > k)
            {
                if (elements[k] == "*")
                {
                    result = int.Parse(elements[k - 1]) * int.Parse(elements[k + 1]);
                    elements[k] = result.ToString();
                    elements.RemoveAt(k + 1);
                    elements.RemoveAt(k - 1);
                    result = 0;
                    k = 0;
                }
                else if (elements[k] == "/")
                {
                    if (int.Parse(elements[k - 1]) == 0)
                    {
                        if (k - 2 > 0)
                        {
                            elements.RemoveRange(k - 2, 4);
                        }
                        else
                        {
                            elements.RemoveRange(k - 1, 3);
                        }
                        k = 0;
                    }
                    else
                    {
                        result = int.Parse(elements[k - 1]) / int.Parse(elements[k + 1]);
                        elements[k] = result.ToString();
                        elements.RemoveAt(k + 1);
                        elements.RemoveAt(k - 1);
                        result = 0;
                        k = 0;
                    }
                    Console.WriteLine(true);
                }
                else
                {
                    ++k;
                }
            }

            k = 0;

            while (elements.Count > 1 && elements.Count > k)
            {
                if (elements[k] == "+")
                {
                    result = int.Parse(elements[k - 1]) + int.Parse(elements[k + 1]);
                    elements[k] = result.ToString();
                    elements.RemoveAt(k + 1);
                    elements.RemoveAt(k - 1);
                    result = 0;
                    k = 0;
                }
                else if (elements[k] == "-")
                {
                    result = int.Parse(elements[k - 1]) - int.Parse(elements[k + 1]);
                    elements[k] = result.ToString();
                    elements.RemoveAt(k + 1);
                    elements.RemoveAt(k - 1);
                    result = 0;
                    k = 0;
                }
                else
                {
                    ++k;
                }
            }

            if (elements.Count == 1)
            {
                constant.Add(int.Parse(elements[0]));
                elements.Clear();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="elements"></param>
        /// <param name="loopIndex"></param>
        /// <param name="smallest"></param>
        /// <returns></returns>
        private static int ElementChange(List<string> elements, List<LoopInformation> loopIndex, List<LoopInformation> smallest)
        {
            var loopIndexCount = 0;

            for (int i = 0; i < elements.Count; i++)
            {
                if (loopIndex.FindIndex(0, x => x.LoopIndex.Trim() == elements[i]) > -1)
                {
                    ++loopIndexCount;
                }
            }

            elements.ForEach(x =>
            {
                for (int i = 0; i < loopIndex.Count; i++)
                {
                    if (loopIndex[i].LoopIndex == x.Trim())
                    {
                        smallest.Add(loopIndex[i]);
                    }
                }
            });

            if (loopIndexCount > 1)
            {
                var lowestloop = smallest.Min();
                var highestloop = smallest.Max();


                var lowestIndex = elements.FindIndex(x => x.Equals(smallest.Min().LoopIndex));
                var highestIndex = elements.FindIndex(x => x.Equals(smallest.Max().LoopIndex));

                if (highestIndex < lowestIndex)
                {
                    if ((lowestIndex - highestIndex) <= 2 && highestIndex == 0)
                    {
                        if (elements[lowestIndex - 1] == "+")
                        {
                            elements[highestIndex] = lowestloop.LoopIndex;
                            elements[lowestIndex] = highestloop.LoopIndex;
                        }
                        else if (elements[lowestIndex - 1] == "-")
                        {
                            elements[highestIndex] = lowestloop.LoopIndex;
                            elements[lowestIndex] = highestloop.LoopIndex;
                            elements[lowestIndex - 1] = "+";
                            elements.Insert(0, "-");
                        }
                    }
                    else if (highestIndex > 0 && lowestIndex > 0)
                    {
                        if (elements[lowestIndex - 1] == "-" && elements[highestIndex - 1] == "-")
                        {
                            elements[highestIndex] = lowestloop.LoopIndex;
                            elements[lowestIndex] = highestloop.LoopIndex;
                        }
                        else if (elements[lowestIndex - 1] == "-" && elements[highestIndex - 1] == "+")
                        {
                            elements[highestIndex] = lowestloop.LoopIndex;
                            elements[lowestIndex] = highestloop.LoopIndex;
                            elements[highestIndex - 1] = "-";
                            elements[lowestIndex - 1] = "+";
                        }
                        else if (elements[highestIndex - 1] == "-" && elements[lowestIndex - 1] == "+")
                        {
                            elements[highestIndex] = lowestloop.LoopIndex;
                            elements[lowestIndex] = highestloop.LoopIndex;
                            elements[highestIndex - 1] = "-";
                            elements[lowestIndex - 1] = "+";
                        }
                    }
                }
            }

            return loopIndexCount;
        }

        /// <summary>
        /// puts the information generated into the output array to be displayed
        /// </summary>
        /// <param name="output">the returned array to be displayed</param>
        /// <param name="variable">the list of the variables and their associated information</param>
        /// <param name="lineinfo">the line information for each read and write</param>
        /// <param name="loops">the array of upper bounds</param>
        private static void InformationOutput(List<string> output, List<Variable> variable, List<LineInformation> lineinfo, int[] loops)
        {
            var variablePositions = string.Empty;

            foreach (var item in variable)
            {
                variablePositions += item.TypeInt.ToString() + " ";
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
                    output.Add(item.LineNumber + " " + item.Array + " " + item.LoopDepth + " " + item.Constant_Coeffient);
                }
            }

            output.Add("0");

            foreach (var item in lineinfo)
            {
                if (!item.Write)
                {
                    output.Add(item.LineNumber + " " + item.Array + " " + item.LoopDepth + " " + item.Constant_Coeffient);
                }
            }

            output.Add("0");
        }

        /// <summary>
        /// creates a list of the variables/constants
        /// </summary>
        /// <param name="statment">statement of variables/constants to be separated</param>
        /// <returns>Returns the statement separated into a list of each element</returns>
        private static List<string> ValueExtration(string statment)
        {
            var words = new List<string>();
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

            return words;
        }

        /// <summary>
        /// 
        /// </summary>
        private class LoopInformation : IComparable<LoopInformation>
        {
            public string LoopIndex { get; set; }
            public int Depth { get; set; }

            public int CompareTo(LoopInformation b)
            {
                return this.Depth.CompareTo(b.Depth);
            }
        }
    }

    /// <summary>
    /// Holds the variable information such as the name (int x), type (list, box), type int (1, 2), and position such as if there are 100
    /// variables, which one are we accessing for the array
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