using LivingThing.Core.Frameworks.Common.Attributes;

namespace MCUSimulator.Core.ProjectSystem
{
    public class MCUSimulatorWatch
    {
        public string? Name { get; set; }
        public string? RegisterId { get; set; }
        public MCUSimulatorWatchType Type { get; set; }
        public ulong? WatchPoint { get; set; }
        [Schema(Exclude = SchemaType.All)]
        public int Width
        {
            get
            {
                switch (Type)
                {
                    case MCUSimulatorWatchType.bit0:
                        return 1;
                    case MCUSimulatorWatchType.bit1:
                        return 1;
                    case MCUSimulatorWatchType.bit2:
                        return 1;
                    case MCUSimulatorWatchType.bit3:
                        return 1;
                    case MCUSimulatorWatchType.bit4:
                        return 1;
                    case MCUSimulatorWatchType.bit5:
                        return 1;
                    case MCUSimulatorWatchType.bit6:
                        return 1;
                    case MCUSimulatorWatchType.bit7:
                        return 1;
                    case MCUSimulatorWatchType.@sbyte:
                        return 8;
                    case MCUSimulatorWatchType.@ubyte:
                        return 8;
                    case MCUSimulatorWatchType.@short:
                        return 16;
                    case MCUSimulatorWatchType.@ushort:
                        return 16;
                    case MCUSimulatorWatchType.@uint:
                        return 32;
                    case MCUSimulatorWatchType.@int:
                        return 32;
                    case MCUSimulatorWatchType.@ulong:
                        return 64;
                    case MCUSimulatorWatchType.@long:
                        return 64;
                }
                return 0;
            }
        }

        public ulong Read(MCUSimulatorEngine simulator)
        {
            var register = simulator.Registers.GetById(RegisterId) as IReadableMemory;
            ulong value = 0;
            if (register != null)
            {
                value = (ulong)register.Value;
                if (register is IRandomAccessMemory ram)
                {
                    int remainingWidth = Width - register.Width;
                    int gotBit = register.Width;
                    while (remainingWidth > 0)
                    {
                        ram = simulator.Registers[ram.Address + 1];
                        if (ram == null)
                            break;
                        if (simulator.MCU.Endianess == MCUEndianess.Little)
                        {
                            value |= (ulong)ram.Value << gotBit;
                        }
                        else
                        {
                            value <<= simulator.RegisterBits;
                            value |= (uint)ram.Value;
                        }
                        gotBit += ram.Width;
                        remainingWidth -= ram.Width;
                    }
                }
            }
            return value;
        }
    }
}
