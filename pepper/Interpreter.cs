public static class Interpreter
{
	public const int TabSize = 8;

	public struct Point : IMarshalable
	{
		public int x;
		public int y;
		public int z;

		public int Size => 2;

		public void Read<M>(ref M marshal) where M : IMarshal
		{
			marshal.Read(out x);
			marshal.Read(out y);
			marshal.Read(out z);
		}

		public void Write<M>(ref M marshal) where M : IMarshal
		{
			marshal.Write(x, nameof(x));
			marshal.Write(y, nameof(y));
			marshal.Write(z, nameof(z));
		}
	}

	public static void TestFunction<C>(C context) where C : IContext
	{
		context.Arg(out Point p);
		context.ReturnsUnit();
		System.Console.WriteLine("HELLO FROM C# {0}, {1}, {2}", p.x, p.y, p.z);
		context.Push();
	}

	public static void RunSource(string source, bool printDisassembled)
	{
		var pepper = new Pepper();

		pepper.AddFunction(TestFunction, TestFunction);

		var compileErrors = pepper.CompileSource(source);
		if (compileErrors.Count > 0)
		{
			var error = CompilerHelper.FormatError(source, compileErrors, 2, TabSize);
			ConsoleHelper.Error("COMPILER ERROR\n");
			ConsoleHelper.Error(error);
			ConsoleHelper.LineBreak();

			System.Environment.ExitCode = 65;
			return;
		}

		if (printDisassembled)
		{
			ConsoleHelper.Write(pepper.Disassemble());
			ConsoleHelper.LineBreak();
		}

		var runError = pepper.RunLastFunction();
		if (runError.isSome)
		{
			var error = VirtualMachineHelper.FormatError(source, runError.value, 2, TabSize);
			ConsoleHelper.Error("RUNTIME ERROR\n");
			ConsoleHelper.Error(error);
			ConsoleHelper.LineBreak();
			ConsoleHelper.Error(pepper.TraceCallStack());

			System.Environment.ExitCode = 70;
		}
		else
		{
			System.Environment.ExitCode = 0;
		}

		ConsoleHelper.LineBreak();
	}
}