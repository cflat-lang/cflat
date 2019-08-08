public static class CompilerDeclarationExtensions
{
	// VARIABLES
	public static int AddLocalVariable(this Compiler self, Slice slice, ValueType type, bool mutable, bool isUsed)
	{
		var stackIndex = 0;
		if (self.localVariables.count > 0)
		{
			var lastVar = self.localVariables.buffer[self.localVariables.count - 1];
			stackIndex = lastVar.stackIndex + self.chunk.GetTypeSize(lastVar.type);
		}

		self.localVariables.PushBack(new LocalVariable(
			slice,
			stackIndex,
			self.scopeDepth,
			type,
			mutable,
			isUsed
		));

		return self.localVariables.count - 1;
	}

	public static int DeclareLocalVariable(this Compiler self, Slice slice, bool mutable)
	{
		if (self.localVariables.count >= self.localVariables.buffer.Length)
		{
			self.AddSoftError(slice, "Too many local variables in function");
			return -1;
		}

		var type = ValueType.Unit;
		if (self.typeStack.count > 0)
			type = self.typeStack.buffer[self.typeStack.count - 1];

		return self.AddLocalVariable(slice, type, mutable, false);
	}

	public static int ResolveToLocalVariableIndex(this Compiler self)
	{
		var source = self.parser.tokenizer.source;

		for (var i = self.localVariables.count - 1; i >= 0; i--)
		{
			var local = self.localVariables.buffer[i];
			if (CompilerHelper.AreEqual(source, self.parser.previousToken.slice, local.slice))
				return i;
		}

		return -1;
	}

	// FUNCTIONS
	public static ByteCodeChunk.FunctionTypeBuilder BeginFunctionDeclaration(this Compiler self)
	{
		return self.chunk.BeginAddFunctionType();
	}

	public static void EndFunctionDeclaration(this Compiler self, ByteCodeChunk.FunctionTypeBuilder builder, Slice slice)
	{
		if (self.chunk.functions.count >= ushort.MaxValue)
		{
			self.chunk.functionTypeParams.count -= builder.parameterCount;
			self.AddSoftError(slice, "Too many function declarations");
			return;
		}

		var typeIndex = self.chunk.EndAddFunctionType(builder);
		var name = CompilerHelper.GetSlice(self, slice);
		self.chunk.AddFunction(name, typeIndex);
	}

	public static bool ResolveToFunctionIndex(this Compiler self, out int index)
	{
		index = 0;
		var source = self.parser.tokenizer.source;

		for (var i = 0; i < self.chunk.functions.count; i++)
		{
			var f = self.chunk.functions.buffer[i];
			if (CompilerHelper.AreEqual(source, self.parser.previousToken.slice, f.name))
			{
				index = i;
				return true;
			}
		}

		return false;
	}

	// STRUCTS
	public static ByteCodeChunk.StructTypeBuilder BeginStructDeclaration(this Compiler self)
	{
		return self.chunk.BeginAddStructType();
	}

	public static void EndStructDeclaration(this Compiler self, ByteCodeChunk.StructTypeBuilder builder, Slice slice)
	{
		if (self.chunk.structTypes.count >= ushort.MaxValue)
		{
			self.chunk.structTypes.count -= builder.fieldCount;
			self.AddSoftError(slice, "Too many struct declarations");
			return;
		}

		var name = CompilerHelper.GetSlice(self, slice);

		for (var i = 0; i < self.chunk.structTypes.count; i++)
		{
			if (self.chunk.structTypes.buffer[i].name == name)
			{
				self.chunk.structTypes.count -= builder.fieldCount;
				self.AddSoftError(slice, "There's already a struct with this name");
				return;
			}
		}

		var index = self.chunk.EndAddStructType(builder, name);
		var size = self.chunk.structTypes.buffer[index].size;
		if (size >= byte.MaxValue)
		{
			self.AddSoftError(
				slice,
				"Struct size is too big. Max is {0}. Got {1}",
				byte.MaxValue,
				size
			);
		}
	}

	public static bool ResolveToStructTypeIndex(this Compiler self, out int index)
	{
		index = 0;
		var source = self.parser.tokenizer.source;

		for (var i = 0; i < self.chunk.structTypes.count; i++)
		{
			var s = self.chunk.structTypes.buffer[i];
			if (CompilerHelper.AreEqual(source, self.parser.previousToken.slice, s.name))
			{
				index = i;
				return true;
			}
		}

		return false;
	}
}