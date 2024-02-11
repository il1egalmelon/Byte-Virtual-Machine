/*
    Copyright 2024 il1egalmelon
*/

using static MainFunction;

public enum CALLFRAME : byte {
    SECTION_DATA = 0x01,
    SECTION_CODE = 0x02
}

class Inline {
    //statuses
    public static bool   in_calling_directive    = false;
    public static string which_calling_directive = "";

    public static void RunLine(string line) {
        if (line == "" || line.Contains(";")) {
            return;
        }

        bool ran = false;

        bool scopeChangedDown = CalculateScopeChange(line);
        SectionCalculation(line);

        //only after the scope calculation
        //the GC can be invoked, thus the ordering
        if (scopeChangedDown == true) {
            instancedRuntime.GCtimeMS.Start();

            GarbageCollectorInvoke();
            instancedRuntime.GCtimeMS.Stop();

            ran = true;
        }

        //check if the current stack code is in SECTION_DATA
        if (instancedRuntime.STKPNT != 0)
        if (line != "StaticData =>")
        if (instancedRuntime.stack[instancedRuntime.STKPNT - 1] == (byte) CALLFRAME.SECTION_DATA) {
            CreateNewMemoryObjects(line.Split(' '));
            ran = true;
        }

        //if already ran, that line is already cleared
        if (ran == true) {
            return;
        }

        //fall-through
        if (RunMacros(line) && in_calling_directive == false) {}
        else
        if (RunKeywords(Macros.Split(line, ' ')) && in_calling_directive == false) {}
        else
        if (RunCalls(line)) {}
        else
        if (RunCallInner(line)) {}
        else
        if (line.Contains("()") || line[0] == '%' || line == "{" || line == "}" ||
            line == "StaticData =>" || line == "StaticCode =>" || line == "<>") {}
        else {System.Console.WriteLine("[EE] : Invalid keyword!"); throw new System.Exception("RUNTIME ERROR");}
    }

    //returns TRUE if scope change downwards
    static bool CalculateScopeChange(string line) {
        switch (line) {
            case "Start":
                break;

            case "End":
                throw new System.Exception("DONE");

            case "{":
                instancedRuntime.scope++;

                break;

            case "}":
                instancedRuntime.scope--;

                return true;
        }

        return false;
    }

    static void SectionCalculation(string line) {
        switch (line) {
            case "StaticData =>":
                instancedRuntime.stack[instancedRuntime.STKPNT] = (byte) CALLFRAME.SECTION_DATA; //push onto stackframe
                instancedRuntime.STKPNT++;
                instancedRuntime.layer++;

                break;

            case "StaticCode =>":
                instancedRuntime.stack[instancedRuntime.STKPNT] = (byte) CALLFRAME.SECTION_CODE; //push onto stackframe
                instancedRuntime.STKPNT++;
                instancedRuntime.layer++;

                break;

            case "<>":
                instancedRuntime.STKPNT--;
                instancedRuntime.stack[instancedRuntime.STKPNT] = 0x00;
                instancedRuntime.layer--;

                break;
        }
    }

    public static void GarbageCollectorInvoke() {
        try {
            instancedRuntime.HEPPNT = 0;
            for (int i = 0; i < instancedRuntime.objectMap.Count;) {
                ObjectMapTemplate current = instancedRuntime.objectMap[instancedRuntime.objectMap.Count - i - 1];
                if (current.scope > instancedRuntime.scope) {
                    instancedRuntime.objectMap.RemoveAt(instancedRuntime.objectMap.Count - i - 1);
                }
                else {
                    instancedRuntime.HEPPNT += current.size;
                    i++;
                }
            }
        } catch {
            System.Console.WriteLine("[WW] : Garbage collector error! linePointer: " + instancedRuntime.linePointer);
            System.Console.WriteLine("     : Current line:                         " + instancedRuntime.code[instancedRuntime.linePointer]);
            System.Console.WriteLine("     : Scope:                                " + instancedRuntime.scope);
        }
    }

    public static void GCRemoveVar(string __v) {
        try {
            for (int i = 0; i < instancedRuntime.objectMap.Count;) {
                ObjectMapTemplate current = instancedRuntime.objectMap[instancedRuntime.objectMap.Count - i - 1];
                if (current.varName == __v) {
                    instancedRuntime.objectMap.RemoveAt(instancedRuntime.objectMap.Count - i - 1);
                    return;
                }
                else {
                    i++;
                }
            }

            System.Console.WriteLine("[WW] : Garbage collector variable not found!");
        } catch {
            System.Console.WriteLine("[WW] : Garbage collector error! linePointer: " + instancedRuntime.linePointer);
            System.Console.WriteLine("     : Current line:                         " + instancedRuntime.code[instancedRuntime.linePointer]);
            System.Console.WriteLine("     : Scope:                                " + instancedRuntime.scope);
        }
    }

    public static void CreateNewMemoryObjects(string[] lineSplit, int scope = -1) {
        ObjectMapTemplate __tmp0;

        (bool success, System.Int128[] vars) = Macros.ObjectfyDataAssignments(lineSplit);

        __tmp0.typeSize = Macros.ReturnDataSize(lineSplit[0]);
        __tmp0.size = Macros.ReturnDataTotalSize(lineSplit[0], vars.Length);
        if (scope == -1) {
            __tmp0.scope = instancedRuntime.scope;
        }
        else {
            __tmp0.scope = scope;
        }
        __tmp0.varName = lineSplit[1];
        __tmp0.locationIndexZero = instancedRuntime.HEPPNT;

        instancedRuntime.HEPPNT += __tmp0.size;

        if (instancedRuntime.HEPPNT > instancedRuntime.heap.Length - 1) {
            System.Console.WriteLine("[EE] : Out of heap memory!");
            throw new System.Exception("RUNTIME ERROR");
        }

        instancedRuntime.objectMap.Add(__tmp0);

        if (success == true) {
            long pointer = __tmp0.locationIndexZero;

            foreach (System.Int128 var in vars) {
                byte[] byteArray = ((System.Numerics.BigInteger) var).ToByteArray();
                if (byteArray.Length > 16) {
                    System.Array.Resize(ref byteArray, 16);
                }
                else if (byteArray.Length < 16) {
                    System.Array.Resize(ref byteArray, 16);
                    System.Array.Reverse(byteArray);
                }

                System.Array.Reverse(byteArray);

                for (int i = 0; i < __tmp0.typeSize; i++) {
                    instancedRuntime.heap[pointer + i] = byteArray[i];
                }

                pointer += __tmp0.typeSize;
            }
        }
    }

    static bool RunMacros(string line) {
        switch (line) {
            case "USE":
                RunMacro.USE();

                return true;

            case "GLOBAL":
                RunMacro.GLOBAL();

                return true;
        }

        return false;
    }

    static bool RunKeywords(string[] lines) {
        switch (lines[0]) {
            case "L0":
            case "LoadSRC":
                RunKeyword.LoadSRC(lines[1]);

                return true;

            case "L1":
            case "LoadSRC_const":
                RunKeyword.LoadSRC_const(lines[1]);

                return true;

            case "L2":
            case "LoadSND":
                RunKeyword.LoadSND(lines[1]);

                return true;

            case "L3":
            case "LoadSND_const":
                RunKeyword.LoadSND_const(lines[1]);

                return true;

            case "L4":
            case "LoadIndex":
                RunKeyword.LoadIndex(lines[1]);

                return true;

            case "L5":
            case "LoadRaw":
                RunKeyword.LoadRaw();

                return true;

            case "S0":
            case "Store":
                RunKeyword.Store(lines[1]);

                return true;

            case "S1":
            case "StoreIndex":
                RunKeyword.StoreIndex(lines[1]);

                return true;

            case "S2":
            case "StoreRaw":
                RunKeyword.StoreRaw(lines[1]);

                return true;

            case "M0":
            case "MoveSNDSRC":
                RunKeyword.MoveSNDSRC();

                return true;

            case "M1":
            case "MoveSRCSND":
                RunKeyword.MoveSRCSND();

                return true;

            case "M2":
            case "Swap":
                RunKeyword.Swap();

                return true;

            case "L6":
            case "LoadLabel":
                RunKeyword.LoadLabel(lines[1]);

                return true;

            case "J0":
            case "Jump":
                RunKeyword.Jump();

                return true;

            case "J1":
            case "JumpBR":
                RunKeyword.JumpBR();

                return true;

            case "J2":
            case "Compare":
                RunKeyword.Compare();

                return true;

            case "J3":
            case "JumpIfEqual":
                RunKeyword.JumpIfEqual();

                return true;

            case "J4":
            case "JumpIfNotEqual":
                RunKeyword.JumpIfNotEqual();

                return true;

            case "J5":
            case "JumpIfLess":
                RunKeyword.JumpIfLess();

                return true;

            case "J6":
            case "JumpIfGreater":
                RunKeyword.JumpIfGreater();

                return true;

            case "A0":
            case "Add":
                RunKeyword.Add();

                return true;

            case "A1":
            case "Subtract":
                RunKeyword.Subtract();

                return true;

            case "A2":
            case "Multiply":
                RunKeyword.Multiply();

                return true;

            case "A3":
            case "Divide":
                RunKeyword.Divide();

                return true;

            case "A4":
            case "Modulus":
                RunKeyword.Modulus();

                return true;

            case "A5":
            case "Increment":
                RunKeyword.Increment();

                return true;

            case "A6":
            case "Decrement":
                RunKeyword.Decrement();

                return true;

            case "B0":
            case "BitwiseNot":
                RunKeyword.BitwiseNot();

                return true;

            case "B1":
            case "BitwiseXor":
                RunKeyword.BitwiseXor();

                return true;

            case "B2":
            case "BitwiseAnd":
                RunKeyword.BitwiseAnd();

                return true;

            case "B3":
            case "BitwiseOR":
                RunKeyword.BitwiseOR();

                return true;
            
            case "B4":
            case "ShiftLeft":
                RunKeyword.ShiftLeft();

                return true;

            case "B5":
            case "ShiftRight":
                RunKeyword.ShiftRight();

                return true;

            case "B6":
            case "RotateLeft":
                RunKeyword.RotateLeft();

                return true;

            case "B7":
            case "RotateRight":
                RunKeyword.RotateRight();

                return true;

            case "B8":
            case "ArithmeticLeft":
                RunKeyword.ArithmeticLeft();

                return true;

            case "B9":
            case "ArithmeticRight":
                RunKeyword.ArithmeticRight();

                return true;

            case "C0":
            case "Wait":
                RunKeyword.Wait();

                return true;

            case "M3":
            case "GetZeroIndexOf":
                RunKeyword.GetZeroIndexOf(lines[1]);

                return true;

            case "M4":
            case "Return":
                RunKeyword.Return();

                return true;

            case "M5":
            case "MemoryStackMax":
                RunKeyword.MemoryStackMax();

                return true;

            case "M6":
            case "MemoryHeapMax":
                RunKeyword.MemoryHeapMax();

                return true;

            case "M7":
            case "Realloc":
                RunKeyword.Realloc(lines[1]);

                return true;

            case "M8":
            case "Sizeof":
                RunKeyword.Sizeof(lines[1]);

                return true;

            case "M9":
            case "GetPointers":
                RunKeyword.GetPointers();

                return true;
        }

        return false;
    }

    static bool RunCalls(string line) {
        switch (line) {
            case "ConsoleOut (":
            case "ConsoleOutSND (":
            case "ConsoleReadKey (":
            case "ConsoleReadLine (":
            case "SetCursorPos (":
            case "GetCursorPos (":
            case "MemoryCopy (":
                in_calling_directive = true;
                which_calling_directive = line;

                return true;

            case ")":
                in_calling_directive = false;
                which_calling_directive = "";

                return true;
        }

        return false;
    }

    static bool RunCallInner(string line) {
        switch (which_calling_directive) {
            case "ConsoleOut (":
                RunCall.ConsoleOut();

                return true;

            case "ConsoleOutSND (":
                RunCall.ConsoleOutSND();

                return true;
        }

        return false;
    }
}

class RunMacro {
    public static void USE() {

    }

    public static void GLOBAL() {

    }
}

class RunKeyword {
    public static void LoadSRC(string __v) {
        var __tmp0 = Macros.FindVariable(__v);
        long indexZero = __tmp0.Item3;
        int typeSize = __tmp0.Item4;

        byte[] __tmp1 = new byte[8];
        for (int i = 0; i < typeSize; i++) {
            __tmp1[i] = instancedRuntime.heap[indexZero + i];
        }

        instancedRuntime.SRC = System.BitConverter.ToInt64(__tmp1);
    }

    public static void LoadSRC_const(string __c) {
        if (__c[0] != '$') {
            System.Console.WriteLine("[EE] : Wrong data constant prefix!");
            throw new System.Exception("RUNTIME ERROR");
        } else {
            __c = __c.Substring(1);
        }

        instancedRuntime.SRC = System.Convert.ToInt64(__c);
    }

    public static void LoadSND(string __v) {
        var __tmp0 = Macros.FindVariable(__v);
        long indexZero = __tmp0.Item3;
        int typeSize = __tmp0.Item4;

        byte[] __tmp1 = new byte[8];
        for (int i = 0; i < typeSize; i++) {
            __tmp1[i] = instancedRuntime.heap[indexZero + i];
        }

        instancedRuntime.SND = System.BitConverter.ToInt64(__tmp1);
    }

    public static void LoadSND_const(string __c) {
        if (__c[0] != '$') {
            System.Console.WriteLine("[EE] : Wrong data constant prefix!");
            throw new System.Exception("RUNTIME ERROR");
        } else {
            __c = __c.Substring(1);
        }

        instancedRuntime.SND = System.Convert.ToInt64(__c);
    }

    public static void LoadIndex(string __v) {
        var __tmp0 = Macros.FindVariable(__v);
        long indexZero = __tmp0.Item3;
        int typeSize = __tmp0.Item4;
        indexZero = indexZero + (typeSize * instancedRuntime.SRC);

        byte[] __tmp1 = new byte[8];
        for (int i = 0; i < typeSize; i++) {
            __tmp1[i] = instancedRuntime.heap[indexZero + i];
        }

        instancedRuntime.SND = System.BitConverter.ToInt64(__tmp1);
    }

    public static void LoadRaw() {
        long indexZero = instancedRuntime.SRC;
        int typeSize = (int) instancedRuntime.SND;

        byte[] __tmp1 = new byte[8];
        for (int i = 0; i < typeSize; i++) {
            __tmp1[i] = instancedRuntime.heap[indexZero + i];
        }

        instancedRuntime.SND = System.BitConverter.ToInt64(__tmp1);
    }

    public static void Store(string __v) {
        var __tmp0 = Macros.FindVariable(__v);
        long indexZero = __tmp0.Item3;
        int typeSize = (int) instancedRuntime.SRC;

        byte[] __tmp1 = new byte[8];
        __tmp1 = System.BitConverter.GetBytes(instancedRuntime.SND);

        for (int i = 0; i < typeSize; i++) {
            instancedRuntime.heap[indexZero + i] = __tmp1[i];
        }
    }

    public static void StoreIndex(string __v) {
        var __tmp0 = Macros.FindVariable(__v);
        long indexZero = __tmp0.Item3;
        int typeSize = __tmp0.Item4;
        indexZero = indexZero + (typeSize * instancedRuntime.SRC);

        byte[] __tmp1 = new byte[8];
        __tmp1 = System.BitConverter.GetBytes(instancedRuntime.SND);

        for (int i = 0; i < typeSize; i++) {
            instancedRuntime.heap[indexZero + i] = __tmp1[i];
        }
    }

    public static void StoreRaw(string __v) {
        var __tmp0 = Macros.FindVariable(__v);
        long indexZero = __tmp0.Item3;
        int typeSize = __tmp0.Item4;

        byte[] __tmp1 = new byte[8];
        for (int i = 0; i < typeSize; i++) {
            __tmp1[i] = instancedRuntime.heap[indexZero + i];
        }

        int size = (int) System.BitConverter.ToInt64(__tmp1);
        byte[] value = new byte[8];
        value = System.BitConverter.GetBytes(instancedRuntime.SND);
        long address = instancedRuntime.SRC;

        for (int i = 0; i < size; i++) {
            instancedRuntime.heap[address + i] = value[i];
        }
    }

    public static void MoveSNDSRC() {
        instancedRuntime.SRC = instancedRuntime.SND;
    }

    public static void MoveSRCSND() {
        instancedRuntime.SND = instancedRuntime.SRC;
    }

    public static void Swap() {
        long __tmp0 = instancedRuntime.SND;
        MoveSRCSND();
        instancedRuntime.SRC = __tmp0;
    }

    public static void LoadLabel(string __l) {
        instancedRuntime.loadedLabel = __l;
    }

    public static void Jump() {
        instancedRuntime.linePointer = Macros.FindSymbol(instancedRuntime.loadedLabel);
    }

    public static void JumpBR() {
        long addressNext = instancedRuntime.linePointer + 1;
        instancedRuntime.linePointer = Macros.FindSymbol(instancedRuntime.loadedLabel);
        byte[] address = new byte[8];
        address = System.BitConverter.GetBytes(addressNext);

        for (int i = 0; i < 8; i++) {
            instancedRuntime.stack[instancedRuntime.STKPNT] = address[i];
            instancedRuntime.STKPNT++;
        }
    }

    public static void Compare() {
        instancedRuntime.flag_GT = false;
        instancedRuntime.flag_LT = false;
        instancedRuntime.flag_NQ = false;
        instancedRuntime.flag_EQ = false;

        if (instancedRuntime.SRC == instancedRuntime.SND) {
            instancedRuntime.flag_EQ = true;
        } 
        if (instancedRuntime.SRC != instancedRuntime.SND) {
            instancedRuntime.flag_NQ = true;
        } 
        if (instancedRuntime.SRC < instancedRuntime.SND) {
            instancedRuntime.flag_LT = true;
        }
        if (instancedRuntime.SRC > instancedRuntime.SND) {
            instancedRuntime.flag_GT = true;
        }
    }

    public static void JumpIfEqual() {
        if (instancedRuntime.flag_EQ == true) {
            Jump();
        }
    }

    public static void JumpIfNotEqual() {
        if (instancedRuntime.flag_NQ == true) {
            Jump();
        }
    }

    public static void JumpIfLess() {
        if (instancedRuntime.flag_LT == true) {
            Jump();
        }
    }

    public static void JumpIfGreater() {
        if (instancedRuntime.flag_GT == true) {
            Jump();
        }
    }

    public static void Add() {
        instancedRuntime.SND = instancedRuntime.SRC + instancedRuntime.SND;
    }

    public static void Subtract() {
        instancedRuntime.SND = instancedRuntime.SRC - instancedRuntime.SND;
    }

    public static void Multiply() {
        instancedRuntime.SND = instancedRuntime.SRC * instancedRuntime.SND;
    }

    public static void Divide() {
        instancedRuntime.SND = instancedRuntime.SRC / instancedRuntime.SND;
    }

    public static void Modulus() {
        instancedRuntime.SND = instancedRuntime.SRC % instancedRuntime.SND;
    }

    public static void Increment() {
        instancedRuntime.SND = instancedRuntime.SRC + 1;
    }

    public static void Decrement() {
        instancedRuntime.SND = instancedRuntime.SRC - 1;
    }

    public static void BitwiseNot() {
        instancedRuntime.SND = ~instancedRuntime.SRC;
    }

    public static void BitwiseXor() {
        instancedRuntime.SND = instancedRuntime.SRC ^ instancedRuntime.SND;
    }

    public static void BitwiseAnd() {
        instancedRuntime.SND = instancedRuntime.SRC & instancedRuntime.SND;
    }

    public static void BitwiseOR() {
        instancedRuntime.SND = instancedRuntime.SRC | instancedRuntime.SND;
    }

    public static void ShiftLeft() {
        instancedRuntime.SND = instancedRuntime.SRC << (int) instancedRuntime.SND;
    }

    public static void ShiftRight() {
        instancedRuntime.SND = instancedRuntime.SRC >>> (int) instancedRuntime.SND;
    }

    public static void RotateLeft() {
        long destination = instancedRuntime.SND;
        long source = instancedRuntime.SRC;
        int rotationAmount = (int) instancedRuntime.SND;

        long rotated = (destination << rotationAmount) | (destination >> (sizeof(long) * 8 - rotationAmount));
        instancedRuntime.SND = rotated;
    }

    public static void RotateRight() {
        long destination = instancedRuntime.SND;
        long source = instancedRuntime.SRC;
        int rotationAmount = (int) instancedRuntime.SND;

        long rotated = (destination >> rotationAmount) | (destination << (sizeof(long) * 8 - rotationAmount));
        instancedRuntime.SND = rotated;
    }

    public static void ArithmeticLeft() {
        instancedRuntime.SND = instancedRuntime.SRC << (int) instancedRuntime.SND;
    }

    public static void ArithmeticRight() {
        instancedRuntime.SND = instancedRuntime.SRC >> (int) instancedRuntime.SND;
    }

    public static void Wait() {
        System.Threading.Thread.Sleep((int) instancedRuntime.SRC);
    }

    public static void GetZeroIndexOf(string __v) {
        var __tmp0 = Macros.FindVariable(__v);
        instancedRuntime.SND = __tmp0.Item3;
    }

    public static void Return() {
        instancedRuntime.STKPNT -= 8;
        byte[] __tmp0 = new byte[8];
        for (int i = 0; i < 8; i++) {
            __tmp0[i] = instancedRuntime.stack[instancedRuntime.STKPNT + i];
        }
        long __tmp1 = System.BitConverter.ToInt64(__tmp0);

        instancedRuntime.linePointer = __tmp1;
    }

    public static void MemoryStackMax() {
        instancedRuntime.SND = instancedRuntime.stack.Length;
    }

    public static void MemoryHeapMax() {
        instancedRuntime.SND = instancedRuntime.heap.Length;
    }

    public static void Realloc(string __v) {
        (string __, int scope, long locZero, int typeSize, long TSize) = Macros.FindVariable(__v);
        Inline.GCRemoveVar(__v);

        string[] objectInfo = new string[2];
        objectInfo[0] = Macros.ReturnEquivalentType(typeSize);
        objectInfo[0] += "[" + instancedRuntime.SRC + "]";
        objectInfo[1] = __v;

        Inline.CreateNewMemoryObjects(objectInfo, scope);
        var __tmp0 = Macros.FindVariable(__v);
        Macros.Memcpy(locZero, __tmp0.Item3, TSize);
    }

    public static void Sizeof(string __v) {
        var __tmp0 = Macros.FindVariable(__v);
        instancedRuntime.SND = __tmp0.Item5;
    }

    public static void GetPointers() {
        instancedRuntime.SRC = instancedRuntime.STKPNT;
        instancedRuntime.SND = instancedRuntime.HEPPNT;
    }
}

class RunCall {
    public static void ConsoleOut() {
        string line = instancedRuntime.code[instancedRuntime.linePointer];

        if (line[0] == '$') {
            string[] headers = { "Char[]", "&\"Name\"", "assign" };
            string[] lineSplit = line.Split(' ');
            string[] lineArray = new string[headers.Length + lineSplit.Length];
            headers.CopyTo(lineArray, 0);
            lineSplit.CopyTo(lineArray, headers.Length);

            var objects = Macros.ObjectfyDataAssignments(lineArray);
            System.Int128[] Int128EncodedChars = objects.Item2;
            int[] Int32EncodedChars = new int[Int128EncodedChars.Length];

            for (int i = 0; i < Int32EncodedChars.Length; i++) {
                Int32EncodedChars[i] = (int) (uint) (Int128EncodedChars[i] & uint.MaxValue);
            }

            string printout = Macros.ConvertIntArrayToUTF16(Int32EncodedChars);
            System.Console.Write(printout);
        }
        else if (line[0] == '&') {
            var varInfo = Macros.FindVariable(line);
            int typeSize = varInfo.Item4;
            long totalSize = varInfo.Item5;
            long totalElements = totalSize / (long) typeSize;
            long addressZero = varInfo.Item3;

            string[] printoutString = new string[totalElements];
            for (int i = 0; i < totalElements; i++) {
                byte[] __tmp0 = new byte[16];
                for (int j = 0; j < typeSize; j++) {
                    __tmp0[j] = instancedRuntime.heap[addressZero + (i * typeSize) + j];
                }

                printoutString[i] = Macros.ConvertByteArrayToInt128String(__tmp0);
                System.Int128 __tmp1 = 0;
                System.Int128.TryParse(printoutString[i], System.Globalization.NumberStyles.HexNumber, null, out __tmp1);
                printoutString[i] = __tmp1.ToString();
            }
            Macros.PrintStringArray(printoutString);
        } 
        else if (line == ".empty") {}
        else if (line.Split(' ')[0] == "__string") {
            var varInfo = Macros.FindVariable(line.Split(' ')[1]);
            int typeSize = varInfo.Item4;
            long totalSize = varInfo.Item5;
            long totalElements = totalSize / (long) typeSize;
            long addressZero = varInfo.Item3;

            int[] printoutUTF32 = new int[totalElements];
            for (int i = 0; i < totalElements; i++) {
                byte[] __tmp0 = new byte[4];
                for (int j = 0; j < (byte) DATASIZE.INTEGER; j++) {
                    __tmp0[j] = instancedRuntime.heap[addressZero + (i * (byte) DATASIZE.INTEGER) + j];
                }

                printoutUTF32[i] = (__tmp0[3] << 24) | (__tmp0[2] << 16) | (__tmp0[1] << 8) | __tmp0[0];
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            foreach (int utf32Char in printoutUTF32) {
                sb.Append(char.ConvertFromUtf32(utf32Char));
            }

            string printoutString = sb.ToString();
            System.Console.Write(printoutString);
        }
        else {
            System.Console.WriteLine("[EE] : ConsoleOut calling directive invalid input!");
            throw new System.Exception("RUNTIME ERROR");
        }
    }

    public static void ConsoleOutSND() {
        System.Console.Write(instancedRuntime.SND);
    }

    public static void ConsoleReadKey() {

    }

    public static void ConsoleReadLine() {

    }

    public static void SetCursorPos() {

    }

    public static void GetCursorPos() {

    }

    public static void MemoryCopy() {

    }
}
