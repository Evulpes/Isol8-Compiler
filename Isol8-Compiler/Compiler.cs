﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static Isol8_Compiler.Enumerables;
using static Isol8_Compiler.Enumerables.InstructionTypes;
using static Isol8_Compiler.Enumerables.ErrorCodes;
using static Isol8_Compiler.Parser;
using System.Linq;

namespace Isol8_Compiler
{
    class Compiler
    {
        private static string lastError = "NO_ERROR";
        private readonly string inputFileName;
        public readonly string outputName;
        public static string GetLastError() => lastError;

        internal static ErrorCodes SetLastError(int lineIndex, ErrorCodes errorCode, string lineContent)
        {
            lastError = $"Compiler Error: {errorCode} at line index: {lineIndex}. ({lineContent})";
            return errorCode;
        }

        public Compiler(string file, string outputFile)
        {
            inputFileName = file;
            outputName = outputFile;
        }

        public ErrorCodes CreateAssemblyFile()
        {
            //Parse the code and validate
            ErrorCodes error = ParseFile(inputFileName); 
            if (error != NO_ERROR)
                return error;

            //Create the output file
            var outputFile = File.Create($"Output\\{outputName}.asm");

            //Add the .DATA section
            string output = ".DATA\n";
            
            //For every declaration statement found in the parse.
            for (var i = 0; i < declarationStatements.Count; i++)
            {
                //Tab in and add the variable name,
                output += "\t" + declarationStatements[i].variableName + " ";
                //The type,
                switch (declarationStatements[i].type)
                {
                    case (Types.INT):
                        output += "DD ";
                        break;
                        //TODO: ADD STRING
                }
                //And the value.
                output += declarationStatements[i].value + '\n';
            }

            //Add the .CODE section
            output += ".CODE\n";
            
            //For every function found in the parse.
            for (var i = 0; i < functions.Count; i++)
            {
                output +=
                    functions[i].name + " PROC\n";


                //For every local function, sub rsp, X (4 for DD), mov rbp, esp




                //For every instruction of the function.
                for (int x = 0; x < functions[i].body.Count; x++)
                {
                    if (functions[i].body[x].instructionType == RET)
                    {
                        output += $"\tmov RAX, " +
                            $"{functions[i].body[x].lineContent[1]}\n" +
                            $"{functions[i].body[x].lineContent[0]}\n";
                    }
                    else if (functions[i].body[x].instructionType == PLUSEQUALS)
                    {
                        output += $"\tADD " +
                            $"{functions[i].body[x].lineContent[0][1..]}, " +
                            $"{functions[i].body[x].lineContent[2]}\n";
                    }
                }

                output += functions[i].name + " ENDP\n";

            }

            //Add an END directive
            output += "END";
            outputFile.Write(new UTF8Encoding(true).GetBytes(output));

            outputFile.Close();
            return NO_ERROR;
        }

        public ErrorCodes Assemble(string fileName)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
<<<<<<< HEAD
                Process NASM = new Process()
                {
                    StartInfo =
                    {
                        FileName = "/bin/bash",
                        Arguments = "-c \" " + "echo 'Running on Linux!'" + " \"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                    }
                };
                try
                {
                    NASM.Start();
                }
                catch
                {
                    return ML64_ERROR;
                }

                string NASMResult = NASM.StandardOutput.ReadToEnd();
                Console.WriteLine(NASMResult);
                return NO_ERROR;
            }
            Process ml64 = new Process()
            {
=======
>>>>>>> master
                StartInfo =
                {
                    Arguments = $"\"{Environment.CurrentDirectory}\\Output\\{fileName}.asm\" /Zi /link /subsystem:windows /entry:Initial /out:\"{Directory.GetCurrentDirectory()}\\Output\\{outputName}.exe\"",
                    FileName = Path.Combine(Directory.GetCurrentDirectory(), "ML64\\ml64.exe"),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                }

            };
            try
            {
                ml64.Start();
            }
            catch(Exception ex)
            {
                SetLastError(-1, ML64_ERROR, ex.Message);
                return ML64_ERROR;
            }

            string mlResult = ml64.StandardOutput.ReadToEnd();
            if (mlResult.Contains("error"))
            {
                SetLastError(-1, ML64_ERROR, mlResult);
                return ML64_ERROR;
            }
            return NO_ERROR;
        }
    }
}
