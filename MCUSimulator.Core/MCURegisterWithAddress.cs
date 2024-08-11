
namespace MCUSimulator.Core
{
    /// <summary>
    /// A named register with address mapped to SRAM
    /// </summary>
    class MCURegisterWithAddress : MCUMemory, IRegister, IRandomAccessMemory
    {
        public MCURegisterWithAddress(string name, int address, int width, MemoryAccess access) : base(0, width, access)
        {
            Name = name;
            Address = address;
        }

        public string Name { get; }
        public int Address { get; }
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

        public override string Id => $"{Name}@{Address:X}";

        public override string ToString()
        {
            return $"{Name} {Access.AsString()}{(Access.HasFlag(MemoryAccess.Read) ? $" = {Value:X}" : "")}";
        }
    }
}
