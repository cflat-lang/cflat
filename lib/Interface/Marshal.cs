public interface IStruct : IMarshalable
{
}

public interface IMarshalable
{
	void Marshal<M>(ref M marshaler) where M : IMarshaler;
}

internal static class Marshal
{
	public sealed class InvalidReflectionType : System.Exception { }

	public static ReflectionData ReflectOn<T>(VirtualMachine vm) where T : struct, IMarshalable
	{
		var type = typeof(T);
		if (typeof(IStruct).IsAssignableFrom(type))
		{
			var marshaler = new StructDefinitionMarshaler(vm);
			return ReflectOn<T, StructDefinitionMarshaler>(vm, ref marshaler);
		}
		else if (typeof(ITuple).IsAssignableFrom(type))
		{
			var marshaler = new TupleDefinitionMarshaler(vm);
			return ReflectOn<T, TupleDefinitionMarshaler>(vm, ref marshaler);
		}

		throw new InvalidReflectionType();
	}

	public static ReflectionData ReflectOn<T, M>(VirtualMachine vm, ref M marshaler)
		where T : struct, IMarshalable
		where M : IDefinitionMarshaler
	{
		var type = typeof(T);
		if (vm.reflectionData.TryGetValue(type, out var data))
			return data;

		var name = type.Name;
		default(T).Marshal(ref marshaler);
		marshaler.FinishDefinition<T>();

		return vm.reflectionData[type];
	}

	public static ReflectionData ReflectOnStruct<T>(VirtualMachine vm) where T : struct, IStruct
	{
		var marshaler = new StructDefinitionMarshaler(vm);
		return ReflectOn<T, StructDefinitionMarshaler>(vm, ref marshaler);
	}

	public static ReflectionData ReflectOnTuple<T>(VirtualMachine vm) where T : struct, ITuple
	{
		var marshaler = new TupleDefinitionMarshaler(vm);
		return ReflectOn<T, TupleDefinitionMarshaler>(vm, ref marshaler);
	}
}

public interface IMarshaler
{
	void Marshal(ref bool value, string name);
	void Marshal(ref int value, string name);
	void Marshal(ref float value, string name);
	void Marshal(ref string value, string name);
	void Marshal<T>(ref T value, string name) where T : struct, IMarshalable;
	void Marshal(ref object value, string name);
}

internal interface IDefinitionMarshaler : IMarshaler
{
	void FinishDefinition<T>() where T : struct, IMarshalable;
}

internal struct ReadMarshaler : IMarshaler
{
	private VirtualMachine vm;
	private int stackIndex;

	public ReadMarshaler(VirtualMachine vm, int stackIndex)
	{
		this.vm = vm;
		this.stackIndex = stackIndex;
	}

	public void Marshal(ref bool value, string name) => value = vm.valueStack.buffer[stackIndex++].asBool;

	public void Marshal(ref int value, string name) => value = vm.valueStack.buffer[stackIndex++].asInt;

	public void Marshal(ref float value, string name) => value = vm.valueStack.buffer[stackIndex++].asFloat;

	public void Marshal(ref string value, string name) => value = vm.heap.buffer[vm.valueStack.buffer[stackIndex++].asInt] as string;

	public void Marshal<T>(ref T value, string name) where T : struct, IMarshalable
	{
		value = default;
		value.Marshal(ref this);
	}

	public void Marshal(ref object value, string name) => value = vm.heap.buffer[vm.valueStack.buffer[stackIndex++].asInt];
}

internal struct WriteMarshaler : IMarshaler
{
	private VirtualMachine vm;
	private int stackIndex;

	public WriteMarshaler(VirtualMachine vm, int stackIndex)
	{
		this.vm = vm;
		this.stackIndex = stackIndex;
	}

	public void Marshal(ref bool value, string name) => vm.valueStack.buffer[stackIndex++].asBool = value;

	public void Marshal(ref int value, string name) => vm.valueStack.buffer[stackIndex++].asInt = value;

	public void Marshal(ref float value, string name) => vm.valueStack.buffer[stackIndex++].asFloat = value;

	public void Marshal(ref string value, string name)
	{
		vm.valueStack.buffer[stackIndex++].asInt = vm.heap.count;
		vm.heap.PushBack(value);
	}

	public void Marshal<T>(ref T value, string name) where T : struct, IMarshalable => value.Marshal(ref this);

	public void Marshal(ref object value, string name)
	{
		vm.valueStack.buffer[stackIndex++].asInt = vm.heap.count;
		vm.heap.PushBack(value);
	}
}


internal struct TupleDefinitionMarshaler : IDefinitionMarshaler
{
	internal VirtualMachine vm;
	internal TupleTypeBuilder builder;

	public TupleDefinitionMarshaler(VirtualMachine vm)
	{
		this.vm = vm;
		this.builder = vm.chunk.BeginTupleType();
	}

	public void Marshal(ref bool value, string name) => builder.WithElement(new ValueType(TypeKind.Bool));
	public void Marshal(ref int value, string name) => builder.WithElement(new ValueType(TypeKind.Int));
	public void Marshal(ref float value, string name) => builder.WithElement(new ValueType(TypeKind.Float));
	public void Marshal(ref string value, string name) => builder.WithElement(new ValueType(TypeKind.String));
	public void Marshal<T>(ref T value, string name) where T : struct, IMarshalable => builder.WithElement(global::Marshal.ReflectOn<T>(vm).type);
	public void Marshal(ref object value, string name) => builder.WithElement(new ValueType(TypeKind.NativeObject));

	public void FinishDefinition<T>() where T : struct, IMarshalable
	{
		var result = builder.Build(out var typeIndex);
		if (result == TupleTypeBuilder.Result.Success)
		{
			vm.reflectionData[typeof(T)] = new ReflectionData(
				new ValueType(TypeKind.Tuple, typeIndex),
				vm.chunk.tupleTypes.buffer[typeIndex].size
			);
		}
	}
}

internal struct StructDefinitionMarshaler : IDefinitionMarshaler
{
	internal VirtualMachine vm;
	internal StructTypeBuilder builder;

	public StructDefinitionMarshaler(VirtualMachine vm)
	{
		this.vm = vm;
		this.builder = vm.chunk.BeginStructType();
	}

	public void Marshal(ref bool value, string name) => builder.WithField(name, new ValueType(TypeKind.Bool));
	public void Marshal(ref int value, string name) => builder.WithField(name, new ValueType(TypeKind.Int));
	public void Marshal(ref float value, string name) => builder.WithField(name, new ValueType(TypeKind.Float));
	public void Marshal(ref string value, string name) => builder.WithField(name, new ValueType(TypeKind.String));
	public void Marshal<T>(ref T value, string name) where T : struct, IMarshalable => builder.WithField(name, global::Marshal.ReflectOn<T>(vm).type);
	public void Marshal(ref object value, string name) => builder.WithField(name, new ValueType(TypeKind.NativeObject));

	public void FinishDefinition<T>() where T : struct, IMarshalable
	{
		var result = builder.Build(typeof(T).Name, out var typeIndex);
		if (result == StructTypeBuilder.Result.Success)
		{
			vm.reflectionData[typeof(T)] = new ReflectionData(
				new ValueType(TypeKind.Struct, typeIndex),
				vm.chunk.structTypes.buffer[typeIndex].size
			);
		}
	}
}
