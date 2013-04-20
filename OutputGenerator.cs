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
            var output = new List<string>();
            var variable = new List<Variable>();
            var count = 0;
            var forloops = new List<string>(code.Count);
            for (int i = 0; i < forloops.Capacity; i++)
            {
                forloops.Add("");
            }

            Array.Copy(code.ToArray(), code.FindIndex(x => Regex.IsMatch(x, "\\b" + "for" + "\\b")), forloops.ToArray(), code.FindIndex(x => Regex.IsMatch(x, "\\b" + "for" + "\\b")) - 1, 5);

            var arr = forloops.ToArray();

            code.CopyTo(code.FindIndex(x => Regex.IsMatch(x, "\\b" + "for" + "\\b")), arr, code.FindIndex(x => Regex.IsMatch(x, "\\b" + "for" + "\\b")), code.FindIndex(x => Regex.IsMatch(x, "\\b" + "end" + "\\b")) - code.FindIndex(x => Regex.IsMatch(x, "\\b" + "for" + "\\b")));

            forloops = new List<string>(arr);

            arr = null;

            var variables = code.FindAll(x => x.Contains("int") || x.Contains("list") || x.Contains("table") || x.Contains("box"));

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

            var lineinfo = new List<LineInformation>();

            var loops = new int[Regex.Matches(string.Join("\n", forloops), "\\b" + "for" + "\\b").Count];

            count = 0;

            forloops.ForEach(x =>
                {
                    var index = forloops.IndexOf(x);

                    var original = x;

                    x = x.Trim(' ', '\t');

                    if (x.StartsWith("for"))
                    {
                        loops[count++] = int.Parse(x.Substring(x.IndexOf("to") + 2));
                    }
                    else if (x.StartsWith("let"))
                    {
                        var beforeEqual = x.Substring(0, x.IndexOf("="));

                        //foreach (var item in variable)
                        //{
                        //    if (Regex.IsMatch(beforeEqual, "\\b" + item.Name + "\\b"))
                        //    {
                        //        forloops[index] = "";
                        //    }
                        //}

                        var something = beforeEqual.Substring(3);
                        Console.WriteLine(variable.Exists(s => s.Name == something.Trim()));

                        if (variable.Exists(s => s.Name == beforeEqual.Substring(3).Trim()))
                        {
                            forloops[index] = "";
                            Console.WriteLine(true);
                        }
                        else
                        {
                            something = beforeEqual.Substring(3, beforeEqual.IndexOf("[") - 3).Trim();
                            var v = variable.Find(s => s.Name.Contains(beforeEqual.Substring(3, beforeEqual.IndexOf("[") - 3).Trim()));
                            lineinfo.Add(new LineInformation { LineNumber = index, Array = v.Position });
                        }
                    }
                });

            var variablePositions = string.Empty;

            foreach (var item in variable)
            {
                variablePositions += item.Position.ToString() + " ";
            }

            output.Add(variablePositions);

            //output.Add(Environment.NewLine);

            foreach (var item in loops)
            {
                output.Add(item.ToString());
            }

            output.Add("0");

            return output.ToArray();
        }
    }

    public class Variable
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public int TypeInt { get; set; }
        public int Position { get; set; }
    }

    public class LineInformation
    {
        public int LineNumber { get; set; }
        public int Array { get; set; }
        public int LoopDepth { get; set; }
        public int[] Constant { get; set; }
        public int[] Coefficient { get; set; }
        public bool Write { get; set; }
    }
}
