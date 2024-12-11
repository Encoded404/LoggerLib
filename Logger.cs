#if UNITY
    using UnityEngine;
#else
    using ConsoleLogger;
#endif

namespace ConsoleLogger
{
    public class Logger
    {
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
#else
            if(importance <= printImportance) { writeline(value); }
#endif
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
#else
            if(importance <= printImportance) { writeline(value); }
#endif
        }
        private static readonly object accesConsoleInfo = new();
        public static (int Left, int Top) safeAccesCursorPosition()
        {
            lock (accesConsoleInfo)
            {
                return Console.GetCursorPosition();
            }
        }
        public static void safeWriteCursorPosition(int Left, int Top)
        {
            lock (accesConsoleInfo)
            {
                Console.SetCursorPosition(Left, Top);
            }
        }
        public static void safeWriteCursorVisibility(bool visible)
        {
            lock (accesConsoleInfo)
            {
                Console.CursorVisible = visible;
            }
        }
        public static (int Height, int Width) safeAccesWindowSize()
        {
            lock (accesConsoleInfo)
            {
                return (Console.WindowHeight, Console.WindowWidth);
            }
        }
        public static (int Height, int Width) safeAccessBufferSize()
        {
            lock (accesConsoleInfo)
            {
                return (Console.BufferHeight, Console.BufferWidth);
            }
        }
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

            int originalX = safeAccesCursorPosition().Left;
            int originalY = safeAccesCursorPosition().Top;

            ClearCurrentConsoleLine();
            SafeWrite(value);
            if(useNewLine) { SafeWrite('\n'); }

            redrawInput();

            safeWriteCursorPosition(originalX, Math.Clamp(originalY + 1, 0, safeAccessBufferSize().Height));
        }
        static void WriteRaw(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if(value[i] == '\n')
                {
                    Logger.Write(@"\\n");
                }
                else
                {
                    Logger.Write(value[i]);
                }
            }
        }
        private static void SafeWrite(object? value)
        {
            lock (writingLock) lock (accesConsoleInfo)
            {
                Console.Write(value);
            }
        }
        private static void SafeWriteLine(object? value)
        {
            lock (writingLock) lock (accesConsoleInfo)
            {
                SafeWrite(value+"\n"); // console.writeline is !posssibly! not completly thread safe, but write is
            }
        }
        public static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = safeAccesCursorPosition().Top;
            SafeWrite("\r");
            SafeWrite(new string(' ', safeAccesWindowSize().Width - 1)); 
            SafeWrite("\r");
        }
        static void redrawInput()
        {
            redoCommandIndex = Math.Clamp(redoCommandIndex, 0, commands.Count);
            //int cursor = Console.CursorLeft;

            ClearCurrentConsoleLine();

            SafeWriteLine("redrawing");

            if(inputChars.Count > 0 || redoCommandIndex > 0)
            {
                SafeWrite(
                    redoCommandIndex > 0 ?
                        commands[redoCommandIndex - 1] :
                        new string(inputChars.ToArray())
                );
            }
        }
        const string moveCursorLeft = "\x1b[D";
        const string moveCursorRight = "\x1b[C";
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
                            SafeWrite(moveCursorLeft+saveCursorPosition+writeBuffer+' '+restoreSavedCursorPosition);

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
                            SafeWrite(saveCursorPosition+writeBuffer+' '+restoreSavedCursorPosition);

                            /* redrawInput();
                            safeWriteCursorPosition(Math.Clamp(cursorIndex, 0, safeAccessBufferSize().Width), safeAccesCursorPosition().Top); */

                            break;
                        case ConsoleKey.LeftArrow:
                            if(cursorIndex > 0) { cursorIndex--; SafeWrite(moveCursorLeft); }
                            break;
                        case ConsoleKey.RightArrow:
                            if(cursorIndex < inputChars.Count || (redoCommandIndex > 0 && cursorIndex < commands[redoCommandIndex - 1].Length)) { cursorIndex++; SafeWrite(moveCursorRight); }
                            break;
                        case ConsoleKey.UpArrow:
                            redoCommandIndex++;
                            redrawInput();
                            cursorIndex = commands[redoCommandIndex - 1].Length;
                            break;
                        case ConsoleKey.DownArrow:
                            redoCommandIndex--;
                            redrawInput();
                            if (redoCommandIndex > 0) { cursorIndex = commands[redoCommandIndex - 1].Length; }
                            break;
                        default:
                            if(redoCommandIndex > 0) {inputChars = commands[redoCommandIndex - 1].ToCharArray().ToList(); redoCommandIndex = 0; }

                            if(input.KeyChar == '\0') { continue; }

                            if(cursorIndex >= inputChars.Count)
                            {
                                inputChars.Add(inputChar);
                                SafeWrite(inputChar);
                                cursorIndex++;
                                                                
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
                                SafeWrite(saveCursorPosition);
                                SafeWrite(writeBuffer+restoreSavedCursorPosition);

                                /* redrawInput();
                                safeWriteCursorPosition(Math.Clamp(cursorIndex, 0, safeAccessBufferSize().Width), safeAccesCursorPosition().Top); */
                            }
                            break;
                    }
                    if(safeAccesCursorPosition().Left != cursorIndex) { redrawInput(); }
                    if (input.Key == ConsoleKey.Enter) { break; }
                    
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
                    ClearCurrentConsoleLine(); // clear line to be ready for next write or read
                    SafeWriteLine(returnValue); //write command to console as history
                    
                    commands.Insert(0, returnValue);
                }

                return returnValue;
            }
        }
        public static void ResetColor()
        {
            WriteLine("reseting console color");
            lock (writingLock) lock (accesConsoleInfo)
            {
                WriteLine("trying to reset console color", true, 4);
                Console.ResetColor();
                WriteLine("reseting console color sucess", true);
            }
        }
#endif
    }
}