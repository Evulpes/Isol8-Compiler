﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using static Isol8_Compiler.Enumerables;
using static Isol8_Compiler.Enumerables.ErrorCodes;
using static Isol8_Compiler.Enumerables.InstructionTypes;
using static Isol8_Compiler.Compiler;
using System.Text.RegularExpressions;
namespace Isol8_Compiler
{
    public static class Parser
    {
        internal static readonly List<Variable> variables = new List<Variable>();
        internal static readonly List<Declaration> declarationStatements = new List<Declaration>();
        internal static readonly List<Function> functions = new List<Function>();

        internal static ErrorCodes ParseFile(string inputFileName)
        {
            #region localFunctions
            static ErrorCodes ParseDeclaration(string[] values, string lineContent, int lineIndex, bool local = false)
            {
                //Keyword does not need to be checked as regex will handle this.
                Declaration declaration = new Declaration()
                {
                    keyword = Enum.Parse<Keywords>(values[0])
                };

                //Check variable name is only letters.
                if (!Patterns.lettersOnly.IsMatch(values[1]))
                    return SetLastError(lineIndex, INVALID_VAR_NAME, lineContent);


                //Check the variable name is not already in use
                for (int x = 0; x < variables.Count; x++)
                    if (variables[x].name == values[1])
                        return SetLastError(lineIndex, DUPLICATE_VAR_NAME, lineContent);

                declaration.variableName = values[1];

                if (!Enum.TryParse(values[3], out declaration.type))
                    return SetLastError(lineIndex, INVALID_TYPE, lineContent);


                //ToDo: Revisit, inefficient as string usage
                var trueValue = values[4].Replace(";", string.Empty);

                if (declaration.type == Types.INT)
                {
                    //If value declared as hex, remove 0x notation. //ToDo: Add try catch for 0xSTRING
                    if (trueValue.Contains("0x"))
                    {
                        //If the function contains standard numbers or hex digits only (A-F)
                        if (Patterns.standardOrHexDigitsOnly.Match(trueValue) != Match.Empty)
                            trueValue = Convert.ToInt32(trueValue, 16).ToString();
                        else
                            return SetLastError(lineIndex, INVALID_VAR_VALUE, lineContent);

                    }

                    //Ensure the assigned value is actually an INT when the declare type is INT
                    if (int.TryParse(trueValue, out _))
                        declaration.value = trueValue;
                    else
                        return SetLastError(lineIndex, TYPE_MISMATCH, lineContent);
                }
                else if (declaration.type == Types.STRING)
                {
                    //Ensure the assigned value is actually a String
                    if (Patterns.stringPattern.Match(trueValue) != Match.Empty)
                        declaration.value = trueValue;
                    else
                        return SetLastError(lineIndex, TYPE_MISMATCH, lineContent);
                }

                //Create a new variable and add it to the existing variables list.
                variables.Add(new Variable()
                {
                    name = declaration.variableName,
                    scope = local ? Scope.LOCAL : Scope.GLOBAL,
                    status = VarState.ACTIVE,
                    type = declaration.type,
                    value = declaration.value,
                });
                
                //Add the declaration to the declaration list.
                declarationStatements.Add(declaration);
                return NO_ERROR;
            }
            #endregion


            var fileText = File.ReadLines(inputFileName).ToList();

            for (int i = 0; i < fileText.Count; i++)
            {
                #region Declarations
                //If a declaration pattern is found
                if (Patterns.createPattern.Match(fileText[i]) != Match.Empty)
                {
                    //Generate an array of values.
                    var values = fileText[i].Split(" ");

                    ErrorCodes errorCode = ParseDeclaration(values, fileText[i], i);
                    if (errorCode != NO_ERROR)
                        return errorCode;
                }
                #endregion
                #region Functions
                else if (Patterns.functionPattern.Match(fileText[i]) != Match.Empty)
                {
                    //Get the values of the function declarations.
                    var values = fileText[i].Split(new char[] { ' ', '(', ')' });
                    
                    //Initialize a new function
                    Function func = new Function()
                    {
                        name = values[1],
                    };

                    //Check the return type is a valid type
                    if (!Enum.TryParse(values.Last(), out func.returnType))
                        return SetLastError(i, INVALID_RETURN_TYPE, fileText[i]);
    

                    //Check the function open and closes with the correct brackers, and grab the body.
                    if (fileText[i + 1] == "{")
                    {
                        bool closeFunction = false;

                        //For each line after the initial {
                        for (int initialIndex = i + 2; initialIndex < fileText.Count; initialIndex++)
                        {
                            //If end of function
                            if (fileText[initialIndex] == "}")
                            {
                                closeFunction = true;
                                break;
                            }

                            Instruction instruction = new Instruction
                            {
                                lineContent = fileText[initialIndex].Replace(";","").Split(' '),
                            };

                            //If return instruction
                            if (Patterns.retPattern.Match(fileText[initialIndex].Replace("\t", "")) != Match.Empty)
                            {
                                instruction.instructionType = RET;

                                //If function return type is an int
                                if(func.returnType == Types.INT)
                                {
                                    //If hex declaration
                                    if (instruction.lineContent[1].Contains("0x"))
                                    {
                                        //Check the conversion is valid
                                        try
                                        {
                                            Convert.ToUInt32(instruction.lineContent[1], 16);
                                        }
                                        catch
                                        {
                                            return SetLastError(i, INVALID_RETURN_TYPE, fileText[i]);
                                        }

                                        //If the first letter is a letter, then add a 0 as it's required
                                        if (Patterns.lettersOnly.Match(instruction.lineContent[1][2..][0].ToString()) != Match.Empty)
                                            instruction.lineContent[1] = '0' + instruction.lineContent[1][2..] + 'h';
                                        
                                        //Otherwise just cut the 0x off and append a h
                                        else
                                            instruction.lineContent[1] = instruction.lineContent[1][2..] + 'h';
                                    }
                                    //Else just check the int is valid
                                    else
                                        if (!int.TryParse(instruction.lineContent[1], out _))
                                            return SetLastError(i, INVALID_RETURN_TYPE, fileText[i]);
                                    
                                }
                                else
                                {
                                    //ToDo: string
                                }
                            
                            }



                            func.body.Add(instruction);
                        }

                        //If no closing brack located.
                        if (!closeFunction)
                            return SetLastError(i, NO_CLOSING_BRACKET, fileText[i]);

                        functions.Add(func);
                    }
                    else
                        return SetLastError(i, NO_OPENING_BRACKET, fileText[i]);

                    

                }
                #endregion
                else
                {
                    //No match for line, do what?
                }
            }
            return NO_ERROR;
        }

    }
}
