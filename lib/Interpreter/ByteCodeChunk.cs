using System.Diagnostics;

namespace cflat
{
	[DebuggerTypeProxy(typeof(ByteCodeChunkDebugView))]
	public sealed class ByteCodeChunk
	{
		public enum AddFunctionResult
		{
			Success,
			AlreadyDefined,
			VisibilityMismatch,
			TypeMismatch,
		}

		public Buffer<byte> bytes = new Buffer<byte>(256);
		public Buffer<Slice> sourceSlices = new Buffer<Slice>(256);
		public Buffer<int> sourceStartIndexes = new Buffer<int>();
		public Buffer<ValueData> literalData = new Buffer<ValueData>(64);
		public Buffer<TypeKind> literalKinds = new Buffer<TypeKind>(64);
		public Buffer<string> stringLiterals = new Buffer<string>(16);
		public Buffer<FunctionType> functionTypes = new Buffer<FunctionType>(16);
		public Buffer<ValueType> functionParamTypes = new Buffer<ValueType>(16);
		public Buffer<Function> functions = new Buffer<Function>(8);
		public Buffer<NativeFunction> nativeFunctions = new Buffer<NativeFunction>(8);
		public Buffer<TupleType> tupleTypes = new Buffer<TupleType>(8);
		public Buffer<ValueType> tupleElementTypes = new Buffer<ValueType>(16);
		public Buffer<StructType> structTypes = new Buffer<StructType>(8);
		public Buffer<StructTypeField> structTypeFields = new Buffer<StructTypeField>(16);
		public Buffer<NativeClassType> nativeClassTypes = new Buffer<NativeClassType>(8);

		public void WriteByte(byte value, Slice slice)
		{
			bytes.PushBack(value);
			sourceSlices.PushBack(slice);
		}

		public int AddValueLiteral(ValueData value, TypeKind kind)
		{
			var index = FindValueIndex(value, kind);
			if (index < 0)
			{
				index = literalData.count;
				literalData.PushBack(value);
				literalKinds.PushBack(kind);
			}

			return index;
		}

		public int AddStringLiteral(string literal)
		{
			var stringIndex = System.Array.IndexOf(stringLiterals.buffer, literal);
			if (stringIndex < 0)
			{
				stringIndex = stringLiterals.count;
				stringLiterals.PushBack(literal);
			}

			return AddValueLiteral(new ValueData(stringIndex), TypeKind.String);
		}

		public AddFunctionResult AddFunction(string name, bool isPublic, ushort typeIndex, bool hasBody, Slice slice, int currentSourceFunctionsStartIndex, out int functionIndex)
		{
			functionIndex = functions.count;
			var result = AddFunctionResult.Success;

			if (name.Length > 0)
			{
				for (var i = 0; i < functions.count; i++)
				{
					var function = functions.buffer[i];
					if (function.name != name || !CompilerHelper.IsFunctionVisible(this, i, currentSourceFunctionsStartIndex))
						continue;

					functionIndex = i;

					if (!hasBody || function.codeIndex >= 0)
					{
						result = AddFunctionResult.AlreadyDefined;
						break;
					}

					if (function.isPublic != isPublic)
					{
						result = AddFunctionResult.VisibilityMismatch;
						break;
					}

					if (function.typeIndex != typeIndex)
					{
						result = AddFunctionResult.TypeMismatch;
						break;
					}

					functions.buffer[i] = new Function(
						name,
						function.isPublic,
						bytes.count,
						function.typeIndex
					);
					return AddFunctionResult.Success;
				}
			}

			functions.PushBack(new Function(
				name,
				isPublic,
				hasBody ? bytes.count : -slice.index,
				typeIndex
			));

			return result;
		}

		public bool GetFunctionType(ValueType type, out FunctionType functionType)
		{
			if (type.kind == TypeKind.Function || type.kind == TypeKind.NativeFunction)
			{
				functionType = functionTypes.buffer[type.index];
				return true;
			}

			functionType = new FunctionType();
			return false;
		}

		public bool GetStructType(ValueType type, out StructType structType)
		{
			if (type.kind == TypeKind.Struct)
			{
				structType = structTypes.buffer[type.index];
				return true;
			}

			structType = new StructType();
			return false;
		}

		private int FindValueIndex(ValueData value, TypeKind kind)
		{
			for (var i = 0; i < literalData.count; i++)
			{
				if (kind != literalKinds.buffer[i])
					continue;

				var v = literalData.buffer[i];
				switch (kind)
				{
				case TypeKind.Bool:
					if (v.asBool == value.asBool)
						return i;
					break;
				case TypeKind.Int:
					if (v.asInt == value.asInt)
						return i;
					break;
				case TypeKind.Float:
					if (v.asFloat == value.asFloat)
						return i;
					break;
				default: break;
				}
			}

			return -1;
		}
	}
}