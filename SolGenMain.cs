#region Copyright MIT License
/*
 * Copyright © 2016 Sergey Shevelev, François St-Arnaud and John Wood
 * 
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * Based on work by François St-Arnaud (http://codeproject.com/SolGen)
 */
#endregion

using System;
using System.Diagnostics;
using System.IO;

namespace SolGen
{
    /// <summary>
    /// A utility for fixing project references and generating solution files.
    /// </summary>
    internal class SolGenMain
    {
        private static void Main(string[] args)
        {
            try
            {
                SolutionMaker maker;
                string solutionPath;
                if (args.Length == 1)
                {
                    string filepath = Path.GetFullPath(args[0]);
                    solutionPath = Path.ChangeExtension(filepath, "sln");
                    maker = new SolutionMaker(solutionPath);
                    maker.AddProject(filepath);
                }
                else
                {
                    string [] projectFiles = Directory.GetFiles(".", "*.*proj", SearchOption.AllDirectories);
                    if (projectFiles.Length == 0)
                    {
                        throw new InvalidOperationException("No project files found");
                    }

                    if (projectFiles.Length == 1)
                    {
                        solutionPath = Path.ChangeExtension(Path.GetFullPath(projectFiles[0]), "sln");
                    }
                    else
                    {
                        string fileName = Path.GetFileName(Environment.CurrentDirectory);
                        solutionPath = Path.Combine(Environment.CurrentDirectory, fileName) + ".sln";
                    }

                    maker = new SolutionMaker(solutionPath);
                    foreach (string projectFile in projectFiles)
                    {
                        maker.AddProject(Path.GetFullPath(projectFile));
                    }
                }

                maker.CreateSolution();

                Process.Start(solutionPath);
            }
            catch (Exception e)
            {
                Console.WriteLine();
                Console.WriteLine("A problem occurred running SolGen and the generation did not complete.");
                Console.WriteLine("Exception: " + e.Message + " (" + e.GetType().Name + ")");
                Console.WriteLine(e.StackTrace);
            }
        }
    }
}
