
namespace MCUSimulator.Core
{
    class MCURegister : MCUMemory, IRegister
    {
        public MCURegister(string name, int width, MemoryAccess access) : base(0, width, access)
        {
            Name = name;
        }

        public string Name { get; }
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

        public override string Id => Name;

        public override string ToString()
        {
            return $"{Name}{(Access != MemoryAccess.ReadWrite ? " " + Access.AsString() : "")}{(Access.HasFlag(MemoryAccess.Read) ? $" = {Value:X2}({Value:D})" : "")}";
        }
    }
}
