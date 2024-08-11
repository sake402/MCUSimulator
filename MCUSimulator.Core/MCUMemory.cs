
namespace MCUSimulator.Core
{
    public abstract class MCUMemory : IMemory
    {
        protected MCUMemory(int bitStart, int width, MemoryAccess access)
        {
            Width = width;
            Access = access;
            int mask = 0;
            for (int i = 0; i < 32; i++)
            {
                if (i >= bitStart && i < bitStart + width)
                {
                    mask |= (1 << i);
                }
            }
            Mask = mask;
        }

        public int Mask { get; }
        public int? ResetValue { get; set; }
        public int OldValue { get; set; }
        public int Width { get; }
        public MemoryAccess Access { get; }
        public IEnumerable<IMemoryBitField>? BitFields { get; set; }
        public abstract string Id { get; }

        public event EventHandler<MemoryChangingEventArg>? OnChanging;
        public event EventHandler<MemoryChangedEventArg>? OnChanged;

        protected void RaiseOnChanging(int newValue)
        {
            OnChanging?.Invoke(this, new MemoryChangingEventArg
            {
                Memory = this,
                To = newValue
            });
        }

        protected void RaiseOnChanged(int newValue)
        {
            OnChanged?.Invoke(this, new MemoryChangedEventArg
            {
                Memory = this,
                From = OldValue,
            });
            if (BitFields?.Any() ?? false)
            {
                foreach (var bitField in BitFields)
                {
                    if (bitField is IReadableMemory rm && bitField is MCUMemory mcum)
                    {
                        var oldBitValue = bitField.FromValue(OldValue);
                        var newBitValue = bitField.FromValue(newValue);
                        if (oldBitValue != newBitValue)
                        {
                            mcum.RaiseOnChanged(newBitValue);
                        }
                    }
                }
            }
        }
    }
}
