namespace cflat
{
	internal struct FunctionInterface
	{
		private MemoryReadMarshaler reader;

		public FunctionInterface(VirtualMachine vm, int stackTop)
		{
			reader = new MemoryReadMarshaler(vm, stackTop);
		}

		public T Arg<T>() where T : struct, IMarshalable
		{
			System.Diagnostics.Debug.Assert(Marshal.SizeOf<T>.size > 0);

			var value = default(T);
			value.Marshal(ref reader);
			return value;
		}

		public static void Return<T>(VirtualMachine vm, T value) where T : struct, IMarshalable
		{
			System.Diagnostics.Debug.Assert(Marshal.SizeOf<T>.size > 0);

			var marshaler = new MemoryWriteMarshaler(vm, vm.memory.stackCount);
			vm.memory.GrowStack(Marshal.SizeOf<T>.size);
			value.Marshal(ref marshaler);
		}
	}
}