

namespace MCUSimulator.Core
{
    class MCUStaticRAM : MCUMemory, IRandomAccessMemory, INamedMemory
    {
        public MCUStaticRAM(int address, int width, MemoryAccess access, string? name) : base(0, width, access)
        {
            Address = address;
            Name = name;
        }

        public int Address { get; }
        public string? Name { get; set; }
        int _value;
        int IWritableMemory.ValueNoEvent { set => _value = value; }
        public int Value
        {
            get
            {
                Access.CheckRead();
                return _value;
            }
            set
            {
                Access.CheckWrite();
                RaiseOnChanging(value);
                OldValue = _value;
                _value = value & Mask;
                if (OldValue != _value)
                {
                    RaiseOnChanged(_value);
                }
            }
        }

        public override string Id => $"[{Address:X}]";

        public override string ToString()
        {
            return $"{(Name != null ? Name + "@" : "")}[{Address:X}]{(Access != MemoryAccess.ReadWrite ? " " + Access.AsString() : "")}{(Access.HasFlag(MemoryAccess.Read) ? $" = {Value:X2}({Value:D})" : "")}";
        }
    }
}
