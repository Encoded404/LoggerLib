#if UNITY
    using UnityEngine;
#endif

using System.Drawing;

namespace ConsoleLogger
{
    public class Logger
    {
        static Logger()
        {
            defaultForegroundColor = Console.ForegroundColor;
            defaultBackgroundColor = Console.BackgroundColor;
        }

        static ConsoleColor defaultForegroundColor;
        static ConsoleColor defaultBackgroundColor;
        public static int printImportance = 3;

//unity specefic code
#if UNITY

        public static void Write(object? value)
        {
            Debug.Log(value);
        }
        public static void WriteLine(object? value)
        {
            write(value);
        }
        public static void ClearCurrentConsoleLine()
        {
            throw new NotSupportedException("ClearCurrentConsoleLine is not supported when using unity");
        }
        public static string ReadLine()
        {
            throw new NotSupportedException("ReadLine is not supported when using unity");
        }

//non unity specefic code
#else

        static List<char> inputChars= new List<char>();

        static List<string> commands = new List<string>();
        static int redoCommandIndex = 0;
        /// <summary>
        /// writes a value to the console
        /// </summary>
        /// <param name="value">the value to write</param>
        public static void WriteLine(object? value)
        {
            WriteToConsole(value, true);
        }
        /// <summary>
        /// writes a value to the console
        /// </summary>
        /// <param name="value">the value to write</param>
        /// <param name="isDebug">wheather it should be considered a debug print</param>
        /// <param name="importance">will only print if lower or equal to Logger.printImportance</param>
        public static void WriteLine(object? value, bool isDebug, int importance = 3)
        {
#if DEBUG
            if(isDebug && importance <= printImportance) { WriteLine(value); }
#endif
            if(!isDebug && importance <= printImportance) { WriteLine(value); }

            return;
        }
        /// <summary>
        /// writes a value to the console
        /// </summary>
        /// <param name="value">the value to write</param>
        public static void Write(object? value)
        {
            WriteToConsole(value, false);
        }
        /// <summary>
        /// writes a value to the console
        /// </summary>
        /// <param name="value">the value to write</param>
        /// <param name="isDebug">wheather it should be considered a debug print</param>
        /// <param name="importance">will only print if lower or equal to Logger.printImportance</param>
        public static void Write(object? value, bool isDebug, int importance = 3)
        {
#if DEBUG
            if(isDebug && importance <= printImportance) { WriteLine(value); }
#endif
            if(!isDebug && importance <= printImportance) { WriteLine(value); }
        }
        private static readonly object accesConsoleInfo = new();
        public static (int Left, int Top) safeAccesCursorPosition()
        {
            lock (writingLock)
            {
                return Console.GetCursorPosition();
            }
        }
        public static void safeWriteCursorPosition(int Left, int Top)
        {
            lock (writingLock)
            {
                Console.SetCursorPosition(Left, Top);
            }
        }
        public static void safeWriteCursorVisibility(bool visible)
        {
            lock (writingLock)
            {
                Console.CursorVisible = visible;
            }
        }
        public static (int Height, int Width) safeAccessWindowSize()
        {
            lock (writingLock)
            {
                return (Console.WindowHeight, Console.WindowWidth);
            }
        }
        public static (int Height, int Width) safeAccessBufferSize()
        {
            lock (writingLock)
            {
                return (Console.BufferHeight, Console.BufferWidth);
            }
        }
        private static bool usedNewlineLast = false;
        private static int lineNumber = 0;
        private static readonly object writingLock = new();
        static void WriteToConsole(object? value, bool useNewLine)
        {
            /*if(useNewLine)
            {
                SafeWriteLine(value);
            }
            else
            {
                SafeWrite(value);
            }
            return;*/

            usedNewlineLast = true;
            lineNumber = 0;

            lock (writingLock)
            {
                ClearCurrentConsoleLine();
                
                if(!usedNewlineLast) { Console.Write(moveCursorUp+"\r"+$"\x1b[{lineNumber}C"); }

                Console.Write(value);
                if(!useNewLine) { usedNewlineLast = false; lineNumber = Console.GetCursorPosition().Left; }
                Console.Write("\n");

                RedrawInput();
            }

            //safeWriteCursorPosition(originalX, Math.Clamp(originalY + 1, 0, safeAccessBufferSize().Height));
        }
        static void WriteRaw(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if(value[i] == '\n')
                {
                    Console.Write(@"\\n");
                }
                else
                {
                    Console.Write(value[i]);
                }
            }
        }
        private static void SafeWrite(object? value)
        {
            lock (writingLock)
            {
                Console.Write(value);
            }
        }
        private static void SafeWriteLine(object? value)
        {
            SafeWrite(value+"\n"); // console.writeline is !posssibly! not completly thread safe, but write is
        }
        public static void SafeClearCurrentConsoleLine()
        {
            lock (writingLock)
            {
                Console.Write("\r" + new string(' ', Console.BufferWidth - 1) + "\r");
            }
        }
        public static void ClearCurrentConsoleLine()
        {
            Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r"); 
        }
        static void SafeRedrawInput()
        {
            SafeClearCurrentConsoleLine();

            //SafeWriteLine("redrawing");

            if(inputChars.Count > 0 || redoCommandIndex > 0)
            {
                SafeWrite(
                    redoCommandIndex > 0 ?
                        commands[redoCommandIndex - 1] :
                        new string(inputChars.ToArray())
                );
            }
        }
        static void RedrawInput()
        {
            ClearCurrentConsoleLine();

            //SafeWriteLine("redrawing");

            if(inputChars.Count > 0 || redoCommandIndex > 0)
            {
                Console.Write(
                    redoCommandIndex > 0 ?
                        commands[redoCommandIndex - 1] :
                        new string(inputChars.ToArray())
                );
            }
        }
        const string moveCursorLeft = "\x1b[D";
        const string moveCursorRight = "\x1b[C";
        const string moveCursorUp = "\x1b[A";
        const string moveCursorDown = "\x1b[A";
        const string saveCursorPosition = "\x1b[s";
        const string restoreSavedCursorPosition = "\x1b[u";
        private static readonly object readingLock = new();
        public static string ReadLine()
        {
            lock (readingLock)
            {
                int cursorIndex = safeAccesCursorPosition().Left;
                while (true)
                {
                    ConsoleKeyInfo input = Console.ReadKey(true);
                    char inputChar = input.KeyChar;
                    //WriteLine($"key is: {input.Key} and keychar is: {input.KeyChar} and modifier is: {input.Modifiers.ToString()}");
                    
                    string writeBuffer = "";
                    lock (writingLock)
                    {
                        switch (input.Key)
                        {
                            case ConsoleKey.Enter:
                                break;
                            case ConsoleKey.Backspace:
                                if(cursorIndex <= 0) {  continue; }

                                if(redoCommandIndex > 0) {inputChars = commands[redoCommandIndex - 1].ToCharArray().ToList(); redoCommandIndex = 0; }

                                //WriteLine("backspace pressed");
                                if(inputChars.Count > 0) { cursorIndex--; inputChars.RemoveAt(Math.Clamp(cursorIndex, 0, inputChars.Count-1)); }
                                
                                for(int i = cursorIndex; i < inputChars.Count; i++)
                                {
                                    writeBuffer += inputChars[i];
                                }
                                // moves the cursor left, saves the cursor position, writes the updated text, removes the last char with an empty char, restores the saved cursor position
                                Console.Write(moveCursorLeft+saveCursorPosition+writeBuffer+' '+restoreSavedCursorPosition);

                                /* redrawInput();
                                safeWriteCursorPosition(Math.Clamp(cursorIndex, 0, safeAccessBufferSize().Width), safeAccesCursorPosition().Top); */

                                break;
                            case ConsoleKey.Delete:
                                if(cursorIndex == inputChars.Count) {  continue; }

                                if(redoCommandIndex > 0) {inputChars = commands[redoCommandIndex - 1].ToCharArray().ToList(); redoCommandIndex = 0; }

                                //WriteLine("delete pressed");
                                if(inputChars.Count > 0) { inputChars.RemoveAt(Math.Clamp(cursorIndex, 0, inputChars.Count-1)); }

                                for(int i = cursorIndex; i < inputChars.Count; i++)
                                {
                                    writeBuffer += inputChars[i];
                                }
                                // safes the cursor position, writes the updated text, removes the last char with an empty char, restores the cursor position
                                Console.Write(saveCursorPosition+writeBuffer+' '+restoreSavedCursorPosition);

                                /* redrawInput();
                                safeWriteCursorPosition(Math.Clamp(cursorIndex, 0, safeAccessBufferSize().Width), safeAccesCursorPosition().Top); */

                                break;
                            case ConsoleKey.LeftArrow:
                                if(cursorIndex > 0) { cursorIndex--; Console.Write(moveCursorLeft); }
                                break;
                            case ConsoleKey.RightArrow:
                                if(cursorIndex < inputChars.Count || (redoCommandIndex > 0 && cursorIndex < commands[redoCommandIndex - 1].Length)) { cursorIndex++; Console.Write(moveCursorRight); }
                                break;
                            case ConsoleKey.UpArrow:
                                if(commands.Count <= 0) { break; }

                                redoCommandIndex = Math.Clamp(redoCommandIndex + 1, 0, commands.Count);
                                RedrawInput();
                                cursorIndex = commands[redoCommandIndex - 1].Length;
                                break;
                            case ConsoleKey.DownArrow:
                                if(commands.Count <= 0) { break; }

                                redoCommandIndex = Math.Clamp(redoCommandIndex - 1, 0, commands.Count);
                                RedrawInput();
                                if (redoCommandIndex > 0) { cursorIndex = commands[redoCommandIndex - 1].Length; }
                                break;
                            default:
                                if(redoCommandIndex > 0) {inputChars = commands[redoCommandIndex - 1].ToCharArray().ToList(); redoCommandIndex = 0; }

                                if(input.KeyChar == '\0') { continue; }

                                if(cursorIndex >= inputChars.Count)
                                {
                                    cursorIndex++;
                                    inputChars.Add(inputChar);
                                    Console.Write(inputChar);
                                                                    
                                    /* redrawInput();
                                    safeWriteCursorPosition(Math.Clamp(cursorIndex, 0, safeAccessBufferSize().Width), safeAccesCursorPosition().Top); */
                                }
                                else
                                {
                                    inputChars.Insert(Math.Clamp(cursorIndex, 0, inputChars.Count), inputChar);

                                    for(int i = cursorIndex; i < inputChars.Count; i++)
                                    {
                                        writeBuffer += inputChars[i];
                                    }
                                    Console.Write(saveCursorPosition+writeBuffer+restoreSavedCursorPosition);

                                    /* redrawInput();
                                    safeWriteCursorPosition(Math.Clamp(cursorIndex, 0, safeAccessBufferSize().Width), safeAccesCursorPosition().Top); */
                                }
                                break;
                        }
                        if(Console.GetCursorPosition().Left != cursorIndex) { RedrawInput(); }
                        if (input.Key == ConsoleKey.Enter) { break; }
                    }
                    
                }
                string returnValue = "";
                if(redoCommandIndex > 0)
                {
                    returnValue = new string(commands[redoCommandIndex - 1]);
                    redoCommandIndex = 0;
                }
                else
                {
                    returnValue = new string(inputChars.ToArray()); // set return value
                }
                inputChars.Clear(); //empty input list
                
                if(returnValue != string.Empty)
                {
                    lock (writingLock)
                    {
                        ClearCurrentConsoleLine(); // clear line to be ready for next write or read
                        Console.WriteLine(returnValue); //write command to console as history
                    }
                    commands.Insert(0, returnValue);
                }

                return returnValue;
            }
        }
        public static void ResetColor()
        {
            lock (writingLock)
            {
                Console.WriteLine("reseting console color");
                //resetcolor apparently breaks loggerlib ¯\_(ツ)_/¯
                //Console.ResetColor();
                Console.ForegroundColor = defaultForegroundColor;
                Console.BackgroundColor = defaultBackgroundColor;
                Console.WriteLine("idk_test");
                Console.Out.Flush();
                Console.WriteLine("idk_test2");
            }
            return;
        }
#endif
    }
}