#define DEBUG_TRACE
using System.Text;

namespace cflat
{
	internal static class VirtualMachineInstructions
	{
		public static void RunTopFunction(VirtualMachine vm)
		{
#if DEBUG_TRACE
		var debugSb = new StringBuilder();
#endif

			var bytes = vm.chunk.bytes.buffer;
			var memory = vm.memory;

			var codeIndex = vm.callFrameStack.buffer[vm.callFrameStack.count - 1].codeIndex;
			var baseStackIndex = vm.callFrameStack.buffer[vm.callFrameStack.count - 1].baseStackIndex;

			while (true)
			{
#if DEBUG_TRACE
			debugSb.Clear();
			vm.memory = memory;
			VirtualMachineHelper.TraceStack(vm, debugSb);
			vm.chunk.DisassembleInstruction(codeIndex, debugSb);
			System.Console.WriteLine(debugSb);
#endif

				var nextInstruction = (Instruction)bytes[codeIndex++];
				switch (nextInstruction)
				{
				case Instruction.Halt:
					--vm.callFrameStack.count;
					vm.memory = memory;
					return;
				case Instruction.Call:
					{
						baseStackIndex = memory.stackCount - bytes[codeIndex++];
						var functionIndex = memory.values[baseStackIndex - 1].asInt;

						vm.callFrameStack.buffer[vm.callFrameStack.count - 1].codeIndex = codeIndex;
						codeIndex = vm.chunk.functions.buffer[functionIndex].codeIndex;

						vm.callFrameStack.PushBackUnchecked(
							new CallFrame(
								codeIndex,
								baseStackIndex,
								(ushort)functionIndex,
								CallFrame.Type.Function
							)
						);
						break;
					}
				case Instruction.CallNative:
					{
						var stackTop = memory.stackCount - bytes[codeIndex++];
						var functionIndex = memory.values[stackTop - 1].asInt;
						var function = vm.chunk.nativeFunctions.buffer[functionIndex];

						vm.memory = memory;
						vm.callFrameStack.buffer[vm.callFrameStack.count - 1].codeIndex = codeIndex;
						vm.callFrameStack.PushBackUnchecked(
							new CallFrame(
								0,
								stackTop,
								(ushort)functionIndex,
								CallFrame.Type.NativeFunction
							)
						);

						try
						{
							function.callback(vm, stackTop);
						}
						catch (System.Exception e)
						{
							vm.Error(string.Format("{0}\nnative stack trace:\n{1}", e.Message, e.StackTrace));
							return;
						}

						memory = vm.memory;

						var dstIdx = --stackTop;
						var srcIdx = memory.stackCount - function.returnSize;

						while (srcIdx < memory.stackCount)
							memory.values[dstIdx++] = memory.values[srcIdx++];

						memory.stackCount = stackTop + function.returnSize;

						--vm.callFrameStack.count;
						break;
					}
				case Instruction.Return:
					{
						var size = bytes[codeIndex++];
						var dstIdx = --baseStackIndex;
						var srcIdx = memory.stackCount - size;

						while (srcIdx < memory.stackCount)
							memory.values[dstIdx++] = memory.values[srcIdx++];

						memory.stackCount = baseStackIndex + size;

						--vm.callFrameStack.count;
						codeIndex = vm.callFrameStack.buffer[vm.callFrameStack.count - 1].codeIndex;
						baseStackIndex = vm.callFrameStack.buffer[vm.callFrameStack.count - 1].baseStackIndex;
						break;
					}
				case Instruction.Print:
					{
						var type = ValueType.Read(
							bytes[codeIndex++],
							bytes[codeIndex++],
							bytes[codeIndex++],
							bytes[codeIndex++]
						);

						var sb = new StringBuilder();
						var size = type.GetSize(vm.chunk);

						VirtualMachineHelper.ValueToString(
							vm,
							memory.stackCount - size,
							type,
							sb
						);
						memory.stackCount -= size;

						System.Console.WriteLine(sb);
						break;
					}
				case Instruction.Pop:
					--memory.stackCount;
					break;
				case Instruction.PopMultiple:
					memory.stackCount -= bytes[codeIndex++];
					break;
				case Instruction.Move:
					{
						var sizeUnderMove = bytes[codeIndex++];
						var sizeToMove = bytes[codeIndex++];

						var srcIdx = memory.stackCount - sizeToMove;
						var dstIdx = srcIdx - sizeUnderMove;

						while (srcIdx < memory.stackCount)
							memory.values[dstIdx++] = memory.values[srcIdx++];

						memory.stackCount = dstIdx;
					}
					break;
				case Instruction.LoadUnit:
					memory.PushBackStack(new ValueData());
					break;
				case Instruction.LoadFalse:
					memory.PushBackStack(new ValueData(false));
					break;
				case Instruction.LoadTrue:
					memory.PushBackStack(new ValueData(true));
					break;
				case Instruction.LoadLiteral:
					{
						var index = BytesHelper.BytesToUShort(bytes[codeIndex++], bytes[codeIndex++]);
						memory.PushBackStack(vm.chunk.literalData.buffer[index]);
						break;
					}
				case Instruction.LoadFunction:
				case Instruction.LoadNativeFunction:
					{
						var index = BytesHelper.BytesToUShort(bytes[codeIndex++], bytes[codeIndex++]);
						memory.PushBackStack(new ValueData(index));
						break;
					}
				case Instruction.SetLocal:
					{
						var index = baseStackIndex + bytes[codeIndex++];
						memory.values[index] = memory.values[--memory.stackCount];
						break;
					}
				case Instruction.LoadLocal:
					{
						var index = baseStackIndex + bytes[codeIndex++];
						memory.PushBackStack(memory.values[index]);
						break;
					}
				case Instruction.SetLocalMultiple:
					{
						var dstIdx = baseStackIndex + bytes[codeIndex++];

						var endIndex = memory.stackCount;
						memory.stackCount -= bytes[codeIndex++];
						var srcIdx = memory.stackCount;

						while (srcIdx < endIndex)
							memory.values[dstIdx++] = memory.values[srcIdx++];
						break;
					}
				case Instruction.LoadLocalMultiple:
					{
						var srcIdx = baseStackIndex + bytes[codeIndex++];
						var dstIdx = memory.stackCount;
						memory.GrowStack(bytes[codeIndex++]);

						while (dstIdx < memory.stackCount)
							memory.values[dstIdx++] = memory.values[srcIdx++];
						break;
					}
				case Instruction.IncrementLocalInt:
					{
						var index = baseStackIndex + bytes[codeIndex++];
						++memory.values[index].asInt;
						break;
					}
				case Instruction.IntToFloat:
					memory.values[memory.stackCount - 1] = new ValueData((float)memory.values[memory.stackCount - 1].asInt);
					break;
				case Instruction.FloatToInt:
					memory.values[memory.stackCount - 1] = new ValueData((int)memory.values[memory.stackCount - 1].asFloat);
					break;
				case Instruction.CreateArrayFromStack:
					{
						var elementSize = bytes[codeIndex++];
						var arrayLength = bytes[codeIndex++];
						var arraySize = elementSize * arrayLength;
						memory.GrowHeap(arraySize + 1);
						var headAddress = memory.heapStart;
						memory.stackCount -= arraySize;

						memory.values[headAddress++] = new ValueData(arrayLength);

						for (var i = 0; i < arraySize; i++)
							memory.values[headAddress + i] = memory.values[memory.stackCount + i];

						memory.values[memory.stackCount++] = new ValueData(headAddress);
						break;
					}
				case Instruction.CreateArrayFromDefault:
					{
						var elementSize = bytes[codeIndex++];
						var arrayLength = memory.values[--memory.stackCount].asInt;
						if (arrayLength < 0)
						{
							vm.callFrameStack.buffer[vm.callFrameStack.count - 1].codeIndex = codeIndex;
							vm.Error("Negative array length");
							vm.memory = memory;
							return;
						}

						var arraySize = elementSize * arrayLength;
						memory.GrowHeap(arraySize + 1);
						var headAddress = memory.heapStart;
						memory.stackCount -= elementSize;

						memory.values[headAddress++] = new ValueData(arrayLength);

						for (var i = 0; i < arraySize; i += elementSize)
						{
							for (var j = 0; j < elementSize; j++)
								memory.values[headAddress + i + j] = memory.values[memory.stackCount + j];
						}

						memory.values[memory.stackCount++] = new ValueData(headAddress);
						break;
					}
				case Instruction.LoadArrayLength:
					memory.values[memory.stackCount - 1] = memory.values[memory.values[memory.stackCount - 1].asInt - 1];
					break;
				case Instruction.SetArrayElement:
					{
						var elementSize = bytes[codeIndex++];
						var fieldSize = bytes[codeIndex++];
						var offset = bytes[codeIndex++];

						memory.stackCount -= fieldSize;
						var stackStartIndex = memory.stackCount;

						var index = memory.values[--memory.stackCount].asInt;
						var heapStartIndex = memory.values[--memory.stackCount].asInt;

						var length = memory.values[heapStartIndex - 1].asInt;
						if (index < 0 || index >= length)
						{
							vm.callFrameStack.buffer[vm.callFrameStack.count - 1].codeIndex = codeIndex;
							vm.Error(string.Format("Index out of bounds. Index: {0}. Array length: {1}", index, length));
							vm.memory = memory;
							return;
						}

						heapStartIndex += index * elementSize + offset;

						for (var i = 0; i < fieldSize; i++)
							memory.values[heapStartIndex + i] = memory.values[stackStartIndex + i];
						break;
					}
				case Instruction.LoadArrayElement:
					{
						var elementSize = bytes[codeIndex++];
						var fieldSize = bytes[codeIndex++];
						var offset = bytes[codeIndex++];

						var index = memory.values[--memory.stackCount].asInt;
						var heapStartIndex = memory.values[--memory.stackCount].asInt;

						var length = memory.values[heapStartIndex - 1].asInt;
						if (index < 0 || index >= length)
						{
							vm.callFrameStack.buffer[vm.callFrameStack.count - 1].codeIndex = codeIndex;
							vm.Error(string.Format("Index out of bounds. Index: {0}. Array length: {1}", index, length));
							vm.memory = memory;
							return;
						}

						heapStartIndex += index * elementSize + offset;
						var stackStartIndex = memory.stackCount;
						memory.GrowStack(fieldSize);

						for (var i = 0; i < fieldSize; i++)
							memory.values[stackStartIndex + i] = memory.values[heapStartIndex + i];
						break;
					}
				case Instruction.CreateStackReference:
					{
						var index = baseStackIndex + bytes[codeIndex++];
						memory.PushBackStack(new ValueData(index));
						break;
					}
				case Instruction.CreateArrayElementReference:
					{
						var elementSize = bytes[codeIndex++];
						var arrayIndexOffset = elementSize * memory.values[--memory.stackCount].asInt;
						var index = memory.values[--memory.stackCount].asInt + arrayIndexOffset + bytes[codeIndex++];
						memory.PushBackStack(new ValueData(index));
						break;
					}
				case Instruction.SetReference:
					{
						var dstIdx = baseStackIndex + bytes[codeIndex++];
						dstIdx = memory.values[dstIdx].asInt + bytes[codeIndex++];

						var endIndex = memory.stackCount;
						memory.stackCount -= bytes[codeIndex++];
						var srcIdx = memory.stackCount;

						while (srcIdx < endIndex)
							memory.values[dstIdx++] = memory.values[srcIdx++];
						break;
					}
				case Instruction.LoadReference:
					{
						var srcIdx = baseStackIndex + bytes[codeIndex++];
						srcIdx = memory.values[srcIdx].asInt + bytes[codeIndex++];
						var dstIdx = memory.stackCount;
						memory.GrowStack(bytes[codeIndex++]);

						while (dstIdx < memory.stackCount)
							memory.values[dstIdx++] = memory.values[srcIdx++];
						break;
					}
				case Instruction.NegateInt:
					memory.values[memory.stackCount - 1].asInt = -memory.values[memory.stackCount - 1].asInt;
					break;
				case Instruction.NegateFloat:
					memory.values[memory.stackCount - 1].asFloat = -memory.values[memory.stackCount - 1].asFloat;
					break;
				case Instruction.AddInt:
					memory.values[memory.stackCount - 2].asInt += memory.values[--memory.stackCount].asInt;
					break;
				case Instruction.AddFloat:
					memory.values[memory.stackCount - 2].asFloat += memory.values[--memory.stackCount].asFloat;
					break;
				case Instruction.SubtractInt:
					memory.values[memory.stackCount - 2].asInt -= memory.values[--memory.stackCount].asInt;
					break;
				case Instruction.SubtractFloat:
					memory.values[memory.stackCount - 2].asFloat -= memory.values[--memory.stackCount].asFloat;
					break;
				case Instruction.MultiplyInt:
					memory.values[memory.stackCount - 2].asInt *= memory.values[--memory.stackCount].asInt;
					break;
				case Instruction.MultiplyFloat:
					memory.values[memory.stackCount - 2].asFloat *= memory.values[--memory.stackCount].asFloat;
					break;
				case Instruction.DivideInt:
					memory.values[memory.stackCount - 2].asInt /= memory.values[--memory.stackCount].asInt;
					break;
				case Instruction.DivideFloat:
					memory.values[memory.stackCount - 2].asFloat /= memory.values[--memory.stackCount].asFloat;
					break;
				case Instruction.Not:
					memory.values[memory.stackCount - 1].asBool = !memory.values[memory.stackCount - 1].asBool;
					break;
				case Instruction.EqualBool:
					memory.PushBackStack(new ValueData(memory.values[--memory.stackCount].asBool == memory.values[--memory.stackCount].asBool));
					break;
				case Instruction.EqualInt:
					memory.PushBackStack(new ValueData(memory.values[--memory.stackCount].asInt == memory.values[--memory.stackCount].asInt));
					break;
				case Instruction.EqualFloat:
					memory.PushBackStack(new ValueData(memory.values[--memory.stackCount].asFloat == memory.values[--memory.stackCount].asFloat));
					break;
				case Instruction.EqualString:
					memory.PushBackStack(new ValueData(
							(vm.nativeObjects.buffer[memory.values[--memory.stackCount].asInt] as string).Equals(
							vm.nativeObjects.buffer[memory.values[--memory.stackCount].asInt] as string)
						));
					break;
				case Instruction.GreaterInt:
					memory.PushBackStack(new ValueData(memory.values[--memory.stackCount].asInt < memory.values[--memory.stackCount].asInt));
					break;
				case Instruction.GreaterFloat:
					memory.PushBackStack(new ValueData(memory.values[--memory.stackCount].asFloat < memory.values[--memory.stackCount].asFloat));
					break;
				case Instruction.LessInt:
					memory.PushBackStack(new ValueData(memory.values[--memory.stackCount].asInt > memory.values[--memory.stackCount].asInt));
					break;
				case Instruction.LessFloat:
					memory.PushBackStack(new ValueData(memory.values[--memory.stackCount].asFloat > memory.values[--memory.stackCount].asFloat));
					break;
				case Instruction.JumpForward:
					{
						var offset = BytesHelper.BytesToUShort(bytes[codeIndex++], bytes[codeIndex++]);
						codeIndex += offset;
						break;
					}
				case Instruction.JumpBackward:
					{
						var offset = BytesHelper.BytesToUShort(bytes[codeIndex++], bytes[codeIndex++]);
						codeIndex -= offset;
						break;
					}
				case Instruction.JumpForwardIfFalse:
					{
						var offset = BytesHelper.BytesToUShort(bytes[codeIndex++], bytes[codeIndex++]);
						if (!memory.values[memory.stackCount - 1].asBool)
							codeIndex += offset;
						break;
					}
				case Instruction.JumpForwardIfTrue:
					{
						var offset = BytesHelper.BytesToUShort(bytes[codeIndex++], bytes[codeIndex++]);
						if (memory.values[memory.stackCount - 1].asBool)
							codeIndex += offset;
						break;
					}
				case Instruction.PopAndJumpForwardIfFalse:
					{
						var offset = BytesHelper.BytesToUShort(bytes[codeIndex++], bytes[codeIndex++]);
						if (!memory.values[--memory.stackCount].asBool)
							codeIndex += offset;
						break;
					}
				case Instruction.RepeatLoopCheck:
					{
						var index = baseStackIndex + bytes[codeIndex++];
						var less = memory.values[index].asInt < memory.values[index + 1].asInt;
						memory.PushBackStack(new ValueData(less));
						break;
					}
				case Instruction.DebugHook:
					if (vm.debugger.isSome)
					{
						vm.memory = memory;
						vm.callFrameStack.buffer[vm.callFrameStack.count - 1].codeIndex = codeIndex;
						vm.debugger.value.OnDebugHook();
					}
					break;
				case Instruction.DebugPushFrame:
					vm.debugData.frameStack.PushBack(new DebugData.Frame(
						(ushort)vm.debugData.stackTypes.count,
						(ushort)vm.debugData.stackNames.count
					));
					break;
				case Instruction.DebugPopFrame:
					{
						var frame = vm.debugData.frameStack.PopLast();
						vm.debugData.stackTypes.count = frame.stackTypesBaseIndex;
						vm.debugData.stackNames.count = frame.stackNamesBaseIndex;
					}
					break;
				case Instruction.DebugPushType:
					{
						var type = ValueType.Read(
							bytes[codeIndex++],
							bytes[codeIndex++],
							bytes[codeIndex++],
							bytes[codeIndex++]
						);

						vm.debugData.stackTypes.PushBack(type);
						break;
					}
				case Instruction.DebugPopTypeMultiple:
					vm.debugData.stackTypes.count -= bytes[codeIndex++];
					break;
				case Instruction.DebugPushLocalVariableName:
					{
						var literalIndex = BytesHelper.BytesToUShort(bytes[codeIndex++], bytes[codeIndex++]);
						var stringIndex = vm.chunk.literalData.buffer[literalIndex].asInt;
						var name = vm.chunk.stringLiterals.buffer[stringIndex];
						vm.debugData.stackNames.PushBack(name);
						break;
					}
				case Instruction.DebugPopLocalVariableNameMultiple:
					vm.debugData.stackNames.count -= bytes[codeIndex++];
					break;
				default:
					break;
				}
			}
		}
	}
}