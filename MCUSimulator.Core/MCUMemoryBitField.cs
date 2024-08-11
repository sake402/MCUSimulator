
namespace MCUSimulator.Core
{
    class MCUMemoryBitField<T> : MCUMemory, IReadWriteMemoryBitField<T>, IReadWriteMemoryBitField, INamedMemory, IMemoryBitField<T>
    {
        public MCUMemoryBitField(IMemory memory, int bitStart, int width, MemoryAccess access) : base(bitStart, width, access)
        {
            Parent = memory;
            readable = Parent as IReadableMemory;
            writable = Parent as IWritableMemory;
            BitStart = bitStart;
        }

        //convert to readable to make reading faster,
        //instead of doing everytime when needed
        IReadableMemory? readable;
        IWritableMemory? writable;
        public IMemory Parent { get; }
        public string? Name { get; set; }
        public string? Description { get; init; }
        public int BitStart { get; }

        public int FromValue(int value)
        {
            return (int)((value & Mask) >> BitStart);
        }

        public int Value
        {
            get
            {
                Access.CheckRead();
                if (readable != null)
                {
                    return (int)((readable.Value & Mask) >> BitStart);
                }
                throw new InvalidOperationException("Not a readable memory");
            }
            set
            {
                Access.CheckWrite();
                if (writable != null && readable != null)
                {
                    RaiseOnChanging(value);
                    OldValue = Value;
                    //use the ValueNoEvent to prevent the parent from raising event handler
                    //which we are going to raise here already
                    writable.ValueNoEvent = (int)((readable.Value & ~Mask) | ((value << BitStart) & Mask));
                    if (OldValue != Value)
                    {
                        RaiseOnChanged(Value);
                    }
                }
                else
                {
                    throw new InvalidOperationException("Not a writable memory");
                }
            }
        }
        public Type Type => typeof(T);

        public Func<int, T>? FValueConverter { get; init; }
        public Func<T, int>? BValueConverter { get; init; }

        public T TValue
        {
            get
            {
                return FValueConverter != null ? FValueConverter(Value) :
                    typeof(T) == typeof(bool) ? (T)(object)(Value == 1) :
                    (T)(object)Value;
            }
            set
            {
                Value = BValueConverter != null ? BValueConverter(value) :
                    typeof(T) == typeof(bool) ? ((bool)(object)value! == true ? 1 : 0) :
                    (int)(object)value!;
            }
        }

        public override string Id
        {
            get
            {
                return $"{Parent.Id}[{BitStart}{(Width > 1 ? $"..{BitStart + Width - 1}" : "")}]";
            }
        }

        int IWritableMemory.ValueNoEvent { set => throw new NotImplementedException(); }

        public override string ToString()
        {
            var name = Name ?? (Parent as INamedMemory)?.Name;
            return $"{(name != null ? name + "@" : "")}{BitStart}{(Width > 1 ? $"..{BitStart + Width - 1}" : "")} {(Access != MemoryAccess.ReadWrite ? " " + Access.AsString() : "")}{(Access.HasFlag(MemoryAccess.Read) ? $" = {Value}" : "")}";
        }
    }
}
