﻿using System.Globalization;

namespace cflat
{
	[System.Flags]
	internal enum VariableFlags : byte
	{
		None = 0b0000,
		Iteration = 0b0001,
		Mutable = 0b0010,
		Used = 0b0100,
		Changed = 0b1000,
	}

	internal struct LocalVariable
	{
		public Slice slice;
		public ValueType type;
		public VariableFlags flags;
		public byte stackIndex;

		public bool IsIteration
		{
			get { return (flags & VariableFlags.Iteration) != 0; }
		}

		public bool IsMutable
		{
			get { return (flags & VariableFlags.Mutable) != 0; }
		}

		public bool IsUsed
		{
			get { return (flags & VariableFlags.Used) != 0; }
		}

		public bool IsChanged
		{
			get { return (flags & VariableFlags.Changed) != 0; }
		}

		public LocalVariable(Slice slice, byte stackIndex, ValueType type, VariableFlags flags)
		{
			this.slice = slice;
			this.stackIndex = stackIndex;
			this.type = type;
			this.flags = flags;
		}
	}

	internal readonly struct Scope
	{
		public readonly int localVariablesStartIndex;

		public Scope(int localVarStartIndex)
		{
			this.localVariablesStartIndex = localVarStartIndex;
		}
	}

	internal readonly struct LoopBreak
	{
		public readonly int jump;
		public readonly byte nesting;

		public LoopBreak(int jump, byte nesting)
		{
			this.jump = jump;
			this.nesting = nesting;
		}
	}

	internal static class CompilerHelper
	{
		public static bool AreEqual(string source, Slice a, Slice b)
		{
			if (a.length != b.length)
				return false;

			for (var i = 0; i < a.length; i++)
			{
				if (source[a.index + i] != source[b.index + i])
					return false;
			}

			return true;
		}

		public static bool AreEqual(string source, Slice slice, string other)
		{
			if (slice.length != other.Length)
				return false;

			for (var i = 0; i < slice.length; i++)
			{
				if (source[slice.index + i] != other[i])
					return false;
			}

			return true;
		}

		public static string GetSlice(CompilerIO io, Slice slice)
		{
			return io.parser.tokenizer.source.Substring(slice.index, slice.length);
		}

		public static int GetParsedInt(CompilerIO io)
		{
			var sub = GetSlice(io, io.parser.previousToken.slice);
			int.TryParse(sub, out var value);
			return value;
		}

		public static float GetParsedFloat(CompilerIO io)
		{
			var sub = GetSlice(io, io.parser.previousToken.slice);
			float.TryParse(
				sub,
				NumberStyles.Float,
				CultureInfo.InvariantCulture.NumberFormat,
				out var value);
			return value;
		}

		public static string GetParsedString(CompilerIO io)
		{
			var slice = new Slice(
				io.parser.previousToken.slice.index + 1,
				io.parser.previousToken.slice.length - 2
			);
			return GetSlice(io, slice);
		}

		public static bool IsFunctionVisible(ByteCodeChunk chunk, int functionIndex, int currentSourceFunctionsStartIndex)
		{
			return
				currentSourceFunctionsStartIndex <= functionIndex ||
				chunk.functions.buffer[functionIndex].isPublic;
		}

		public static bool IsStructTypeVisible(ByteCodeChunk chunk, int structTypeIndex, int currentSourceStructTypesStartIndex)
		{
			return
				currentSourceStructTypesStartIndex <= structTypeIndex ||
				chunk.structTypes.buffer[structTypeIndex].isPublic;
		}
	}
}