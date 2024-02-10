using System;
using System.Reflection;
using static MainFunction;

#pragma warning disable CS8600

public struct CC_Int128 {
    public long High;
    public ulong Low;

    public CC_Int128(long high, ulong low) {
        High = high;
        Low = low;
    }

    public override string ToString() {
        return $"{High:X16}{Low:X16}";
    }
}

public enum DATASIZE : int {
    BYTE = 1,
    SHORT = 2,
    INTEGER = 4,
    LONG = 8,
    LONG_LONG = 16,
}

class Macros {
    public static string RemoveInitialSpaces(string input) {
        int startIndex = 0;
        while (startIndex < input.Length && (input[startIndex] == ' ' || input[startIndex] == '\t')) {
            startIndex++;
        }

        return input.Substring(startIndex);
    }

    public static string RemoveTrailingSpaces(string input) {
        int endIndex = input.Length - 1;
        while (endIndex >= 0 && (input[endIndex] == ' ' || input[endIndex] == '\t')) {
            endIndex--;
        }

        return input.Substring(0, endIndex + 1);
    }

    public static int ReturnDataSize(string input) {
        {
            string __tmp0 = "";
            for (int i = 0; i < input.Length; i++) {
                if (input[i] != '[') {
                    __tmp0 += input[i];
                } else {
                    break;
                }
            }

            input = __tmp0;
        }

        switch (input) {
            case "Bool":
            case "Byte":
                return (int) DATASIZE.BYTE;

            case "Short":
                return (int) DATASIZE.SHORT;

            case "Char":
            case "Integer":
                return (int) DATASIZE.INTEGER;

            case "Long":
                return (int) DATASIZE.LONG;

            case "Longlong":
                return (int) DATASIZE.LONG_LONG;

            default:
                System.Console.WriteLine("[EE] : Wrong data type!");
                throw new Exception("RUNTIME ERROR");
        }
    }

    public static string ReturnEquivalentType(int typeSize) {
        switch (typeSize) {
            case 1:
                return "Byte";

            case 2:
                return "Short";

            case 4:
                return "Integer";

            case 8:
                return "Long";

            case 16:
                return "Longlong";
        }

        return "";
    }

    public static string[] Split(string input, char seperate) {
        string[] retString = new string[2];

        int spaceIndex = input.IndexOf(seperate);
        if (spaceIndex >= 0) {
            retString[0] = input.Substring(0, spaceIndex);
            retString[1] = input.Substring(spaceIndex + 1);
        }
        else {
            retString[0] = input;
        }

        return retString;
    }

    public static void Memcpy(long locZeroFirst, long locZeroSecond, long byteCount) {
        for (int i = 0; i < byteCount; i++) {
            instancedRuntime.heap[locZeroSecond + i] = instancedRuntime.heap[locZeroFirst + i];
        }
    }

    //returns function, scope, location 0, type size, total size
    public static (string, int, long, int, long) FindVariable(string varName) {
        foreach (ObjectMapTemplate obj in instancedRuntime.objectMap) {
            if (obj.varName == varName) {
                return ("", obj.scope, obj.locationIndexZero, obj.typeSize, obj.size);
            }
        }

        System.Console.WriteLine("[EE] : Variable not found!");
        throw new Exception("RUNTIME ERROR");
    }

    public static string ConvertByteArrayToInt128String(byte[] byteArray) {
        ulong low = BitConverter.ToUInt64(byteArray, 0);
        long high = BitConverter.ToInt64(byteArray, 8);

        CC_Int128 int128Value = new CC_Int128(high, low);

        return int128Value.ToString();
    }

    public static void PrintStringArray(string[] array) {
        string formattedString = "[" + string.Join(", ", array) + "]";
        Console.Write(formattedString);
    }

    public static long ReturnDataTotalSize(string input, int countFallback) {
        if (input.Contains('[')) {
            if (!input.Contains("auto")) {
                return Convert.ToInt64(ParseNumberFromArray(input)) * ReturnDataSize(input);
            } else {
                return countFallback * ReturnDataSize(input);
            }
        } else {
            return (long) ReturnDataSize(input);
        }
    }

    static string ParseNumberFromArray(string input) {
        string pattern = @"\[(\d+)\]";
        System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(input, pattern);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        else
        {
            return string.Empty;
        }
    }

    public static long FindSymbol(string input) {
        long line = -1;

        for (int t = 0; t < instancedRuntime.cachedLabels.Count; t++) {
            if (instancedRuntime.cachedLabels[t].Item1 == input) {
                return instancedRuntime.cachedLabels[t].Item2;
            }
        }

        for (int i = 0; i < instancedRuntime.code.Length; i++) {
            if (instancedRuntime.code[i] == input) {
                line = i;
                break;
            }
        }

        if (line == -1) {
            Console.WriteLine("[EE] : Symbol not found!");
            throw new Exception("RUNTIME ERROR");
        }

        instancedRuntime.cachedLabels.Add((input, (int) line));

        return line;
    }

    //returns if it is assignable and array of LONG_LONG
    //input:
    //TYPE VAR ?ASSIGN ?ARRAY_DATA
    public static (bool, Int128[]) ObjectfyDataAssignments(string[] input) {
        if (input.Length > 2) {
            System.Collections.Generic.List<Int128> elements = new System.Collections.Generic.List<Int128>();

            if (input[2] != "assign" || input.Length < 4) {
                Console.WriteLine("[EE] : Variable assignment error!");
                throw new Exception("RUNTIME ERROR");
            }

            int startIndex = 2;
            int length = input.Length - startIndex - 1;
            string[] valueList = new string[length];
            Array.Copy(input, startIndex + 1, valueList, 0, length);

            foreach (string value in valueList) {
                string valueNew = value.Substring(1);

                if (valueNew[0] == '"') {
                    valueNew = valueNew.Substring(1);
                    valueNew = valueNew.Substring(0, valueNew.Length - 1);

                    System.Collections.Generic.List<int> __tmp0 = new System.Collections.Generic.List<int>();
                    foreach (char c in valueNew) {
                        int byteValues = char.ConvertToUtf32(c.ToString(), 0);
                        __tmp0.Add(byteValues);
                    }

                    foreach (Int128 e in __tmp0) {
                        elements.Add(e);
                    }
                } else {
                    elements.Add((Int128) System.Numerics.BigInteger.Parse(valueNew));
                }
            }

            return (true, elements.ToArray());
        }

        return (false, new Int128[] {0});
    }

    public static string ConvertIntArrayToUTF16(int[] codePoints) {
        // Create a StringBuilder to store the UTF-16 characters
        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();

        // Convert each code point to a UTF-16 character and append it to the StringBuilder
        foreach (int codePoint in codePoints) {
            stringBuilder.Append(char.ConvertFromUtf32(codePoint));
        }

        // Get the final UTF-16 string from the StringBuilder
        string utf16String = stringBuilder.ToString();

        return utf16String;
    }
}

class Debug {
    public static void PrintOutStackFrame() {
        System.Console.WriteLine("STKPNT: " + instancedRuntime.STKPNT);
        foreach(byte n in instancedRuntime.stack) {
            System.Console.WriteLine(n);
        }
        System.Console.WriteLine("---");
    }

    public static void DumpObjectVariables(object obj) {
        Type type = obj.GetType();
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

        foreach (FieldInfo field in fields) {
            object value = field.GetValue(obj);
            Console.WriteLine($"{field.Name}: {value}");
        }
    }

    public static void ObjectDumpOnScreen() {
        foreach (ObjectMapTemplate obj in instancedRuntime.objectMap) {
            Console.WriteLine("VarName:   " + obj.varName);
            Console.WriteLine("SizeTotal: " + obj.size);
            Console.WriteLine("TypeSize:  " + obj.typeSize);
            Console.WriteLine("Location:  " + obj.locationIndexZero);
            Console.WriteLine("Scope:     " + obj.scope);
            Console.WriteLine();
        }
        Console.ReadKey(true);
        for (int i = 0; i < Console.WindowWidth; i++) {
            Console.Write("-");
        }
        Console.WriteLine();
    }

    public static void HeapDumpOnScreen(bool wait = true) {
        int j = 0;
        foreach (byte sd in instancedRuntime.heap) {
            if (sd > 0) {
                Console.ForegroundColor = ConsoleColor.Yellow;
            }
            Console.Write(sd.ToString("D3") + " ");
            if (sd > 0) {
                Console.ResetColor();
            }

            if (j >= 31) {
                Console.WriteLine();
                j = 0;
            } else {
                j++;
            }
        }
        Console.WriteLine();
        if (wait == true) {
            Console.ReadKey(true);
        }
        for (int i = 0; i < Console.WindowWidth; i++) {
            Console.Write("-");
        }
        Console.WriteLine();
    }

    public static void StackDumpOnScreen(bool wait = true) {
        int j = 0;
        foreach (byte sd in instancedRuntime.stack) {
            if (sd > 0) {
                Console.ForegroundColor = ConsoleColor.Yellow;
            }
            Console.Write(sd.ToString("D3") + " ");
            if (sd > 0) {
                Console.ResetColor();
            }

            if (j >= 31) {
                Console.WriteLine();
                j = 0;
            } else {
                j++;
            }
        }
        Console.WriteLine();
        if (wait == true) {
            Console.ReadKey(true);
        }
        for (int i = 0; i < Console.WindowWidth; i++) {
            Console.Write("-");
        }
        Console.WriteLine();
    }

    public static void DumpObjectMap(bool wait = true) {
        foreach (ObjectMapTemplate obj in instancedRuntime.objectMap) {
            Console.WriteLine(obj.varName + ": " + obj.locationIndexZero);
        }
        if (wait == true) {
            Console.ReadKey(true);
        }
    }

    public static void DumpAll() {
        StackDumpOnScreen(false);
        HeapDumpOnScreen(false);
        DumpObjectVariables(instancedRuntime);
        DumpObjectMap(false);
        Console.WriteLine("CallingDirective: " + Inline.which_calling_directive);
        Console.WriteLine(instancedRuntime.code[instancedRuntime.linePointer]);

        Console.ReadKey(true);
        Console.Clear();
    }
}
