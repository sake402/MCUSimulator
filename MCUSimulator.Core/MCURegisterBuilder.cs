using LivingThing.Core.Frameworks.Common.OneOf;
using System.Runtime.CompilerServices;

namespace MCUSimulator.Core
{
    public class MCURegisterBuilder
    {
        public MCURegisterBuilder(string? name, MemoryAccess access)
        {
            Name = name;
            Access = access;
        }

        public MCURegisterBuilder(int address, MemoryAccess access)
        {
            Address = address;
            Access = access;
        }

        public string? Name { get; private set; }
        public int Address { get; private set; } = -1;
        public int Width { get; private set; } = 8;
        public int? ResetValue { get; private set; }
        public MemoryAccess Access { get; }
        public MCURegisterBuilder WithAddress(int address)
        {
            Address = address;
            return this;
        }
        public MCURegisterBuilder WithName(string name)
        {
            Name = name;
            return this;
        }
        public MCURegisterBuilder WithWidth(int width)
        {
            Width = width;
            return this;
        }
        public MCURegisterBuilder WithReset(int resetValue)
        {
            ResetValue = resetValue;
            return this;
        }

        Action<IReadWriteMemory>? _sink;
        public MCURegisterBuilder WithSink(Action<IReadWriteMemory> sink)
        {
            _sink = sink;
            return this;
        }

        List<Func<IMemory, IMemoryBitField>>? bitBuilders;

        public MCURegisterBuilder BooleanBit(
            OneOf<int, Range> range,
            string? name = null,
            MemoryAccess bitAccess = MemoryAccess.ReadWrite,
            string? description = null,
            Action<IMemoryBitField<bool>>? sink = null)
        {
            return Bit<bool>(range, name: name, bitAccess: bitAccess, description: description, sink: sink,
                fValueConverter: (b) => b != 0,
                bValueConverter: (b) => b ? 1 : 0);
        }

        public MCURegisterBuilder EnumBit<T>(
            OneOf<int, Range> range,
            string? name = null,
            MemoryAccess bitAccess = MemoryAccess.ReadWrite,
            string? description = null,
            Action<IMemoryBitField<T>>? sink = null)
            where T : struct, Enum
        {
            return Bit<T>(range, name: name, bitAccess: bitAccess, description: description, sink: sink,
                fValueConverter: (b) => Unsafe.As<int, T>(ref b),
                bValueConverter: (b) => Unsafe.As<T, int>(ref b));
        }

        public MCURegisterBuilder Bit<T>(
            OneOf<int, Range> range,
            string? name = null,
            MemoryAccess bitAccess = MemoryAccess.ReadWrite,
            string? description = null,
            Func<int, T>? fValueConverter = null,
            Func<T, int>? bValueConverter = null,
            Action<IMemoryBitField<T>>? sink = null)
        {
            bitBuilders ??= new List<Func<IMemory, IMemoryBitField>>();
            bitBuilders.Add((memory) =>
            {
                var start = range.IsT0 ? range.AsT0 : range.AsT1.Start.Value;
                var end = range.IsT0 ? range.AsT0 : range.AsT1.End.Value;
                var bitField = new MCUMemoryBitField<T>(memory, Math.Min(start, end), Math.Abs(end - start) + 1, bitAccess)
                {
                    Name = name,
                    Description = description,
                    FValueConverter = fValueConverter,
                    BValueConverter = bValueConverter
                };
                sink?.Invoke(bitField);
                return bitField;
            });
            return this;
        }

        public IReadWriteMemory Build()
        {
            IReadWriteMemory reg;
            if (Address >= 0 && Name != null)
            {
                var _reg = new MCURegisterWithAddress(Name, Address, Width, Access)
                {
                    Value = ResetValue ?? 0,
                    ResetValue = ResetValue
                };
                if (bitBuilders != null)
                {
                    var fields = bitBuilders.Select(bb => bb(_reg)).ToList();
                    _reg.BitFields = fields;
                }
                reg = _reg;
            }
            else if (Address >= 0)
            {
                var _reg = new MCUStaticRAM(Address, Width, Access, Name)
                {
                    Value = ResetValue ?? 0,
                    ResetValue = ResetValue
                };
                if (bitBuilders != null)
                {
                    var fields = bitBuilders.Select(bb => bb(_reg)).ToList();
                    _reg.BitFields = fields;
                }
                reg = _reg;
            }
            else if (Name != null)
            {
                var _reg = new MCURegister(Name, Width, Access)
                {
                    Value = ResetValue ?? 0,
                    ResetValue = ResetValue
                };
                if (bitBuilders != null)
                {
                    var fields = bitBuilders.Select(bb => bb(_reg)).ToList();
                    _reg.BitFields = fields;
                }
                reg = _reg;
            }
            else
            {
                throw new InvalidOperationException("Either name or address of a register must be defined");
            }
            _sink?.Invoke(reg);
            return reg;
        }
    }
}
