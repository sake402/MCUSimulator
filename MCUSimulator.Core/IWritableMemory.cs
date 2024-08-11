namespace MCUSimulator.Core
{
    public interface IWritableMemory : IMemory
    {
        /// <summary>
        /// Change the value without raising event handler
        /// Only used when changing bits of a memory as the bitfield will raise its own event
        /// </summary>
        internal int ValueNoEvent { set; }
        int Value { set; }
    }
}
