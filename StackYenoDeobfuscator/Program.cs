using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace StackYenoDeobfuscator
{
    class Program
    {
        public static void Print(string str)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("~");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("] ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(str);
        }
        public static ModuleDefMD module = null;
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to yano string decryptor by MindSystem");
            Console.WriteLine("The aim of this tool is to show how to decrypt string using the stack \n");

            try
            {
                module = ModuleDefMD.Load(args[0]);
            }
            catch
            {

            }
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            DecryptString(FindDecryptionMethod());
            watch.Stop();
            Console.WriteLine("\nDone ! Elapsed time : " + watch.Elapsed.TotalSeconds);
            string SavingPath = module.Kind == ModuleKind.Dll ? args[0].Replace(".dll", "-Obfuscated.dll") : args[0].Replace(".exe", "-Obfuscated.exe");
            if (module.IsILOnly)
            {
                var opts = new ModuleWriterOptions(module);
                opts.MetadataOptions.Flags = MetadataFlags.PreserveAll;
                opts.Logger = DummyLogger.NoThrowInstance;
                module.Write(SavingPath, opts);
            }
            else
            {
                var opts = new NativeModuleWriterOptions(module, false);
                opts.MetadataOptions.Flags = MetadataFlags.PreserveAll;
                opts.Logger = DummyLogger.NoThrowInstance;
                module.NativeWrite(SavingPath, opts);
            }
            Console.ReadLine();
        }
        public static void DecryptString(MethodDef decryption)
        {
            foreach (TypeDef type in module.Types)
            {
                foreach (MethodDef method in type.Methods)
                {
                    //Instanciating a new stack to push int and string
                    //Information about the stack : https://docs.microsoft.com/en-us/dotnet/api/system.collections.stack?view=netcore-3.1
                    //https://www.tutorialsteacher.com/csharp/csharp-stack
                    Stack stack = new Stack();
                    if (method.HasBody && method.Body.HasInstructions)
                    {
                        for(int i = 0; i < method.Body.Instructions.Count -1;i++)
                        {
                            if(method.Body.Instructions[i].OpCode == OpCodes.Ldstr)
                            {
                                string operand = (string)method.Body.Instructions[i].Operand;
                                stack.Push(operand);
                            }
                            else if(method.Body.Instructions[i].IsLdcI4())
                            {
                                int operand = method.Body.Instructions[i].GetLdcI4Value();
                                stack.Push(operand);
                            }
                            else if(method.Body.Instructions[i].OpCode == OpCodes.Call && method.Body.Instructions[i].Operand is MethodDef)
                            {
                                MethodDef op = (MethodDef)method.Body.Instructions[i].Operand;
                                if (op == decryption)
                                {
                                    //If method found, we just pop the 2 args and we decrypt the string
                                    int arg = (int)stack.Pop();
                                    string arg2 = (string)stack.Pop();
                                    string decrypted = a(arg2, arg);
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Print(string.Format(" {0} decrypted !", decrypted));
                                    Console.ForegroundColor = ConsoleColor.White;
                                    method.Body.Instructions[i] = Instruction.Create(OpCodes.Ldstr, decrypted);
                                }
                           
                            }
                        }
                    }
                }
            }
        }
        internal static string a(string string_0, int int_0)
        {
            int num = 448617403 + int_0;
            char[] array = string_0.ToCharArray();
            for (int i = 0; i < array.Length; i++)
            {
                char[] array2 = array;
                int num2 = i;
                char c = array[i];
                array2[num2] = (char)(((int)(c & 'ÿ') ^ num++) << 8 | (int)((byte)((int)(c >> 8) ^ num++)));
            }
            return string.Intern(new string(array));
        }
        public static MethodDef FindDecryptionMethod()
        {
            foreach(TypeDef type in module.Types)
            {
                foreach(MethodDef method in type.Methods)
                {
                    if(method.HasBody && method.Body.HasInstructions)
                    {
                        if (method.ReturnType == module.CorLibTypes.String && method.Parameters.Count ==2)
                        {
                            if (method.Parameters[0].Type == module.CorLibTypes.String && method.Parameters[1].Type == module.CorLibTypes.Int32)
                            {
                                //Storing all the call in a list to avoid loop 
                                List<Instruction> instr = method.Body.Instructions.Where(o => o.OpCode == OpCodes.Call).ToList();
                                Instruction Intern = instr.FirstOrDefault(o => o.Operand.ToString().Contains("Intern"));
                                Instruction ToCharArray = instr.FirstOrDefault(o => o.Operand.ToString().Contains("ToCharArray"));
                                //If instruction found, then we're almost sure it's the right method
                                if(Intern != null && ToCharArray != null)
                                {
                                    return method;
                                }
                               
                            }
                           
                        }
                    }
                }
            }
            return null;
        }
    }
}
