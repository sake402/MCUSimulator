namespace MCUSimulator.Core
{
    [Flags]
    public enum MemoryAccess
    {
        Read = 1 << 0,
        Write = 1 << 1,
        ReadWrite = Read | Write
    }

    public static class MemoryAccessExtension
    {
        public static void CheckWrite(this MemoryAccess access)
        {
            if (!access.HasFlag(MemoryAccess.Write))
                throw new InvalidOperationException("Attempt to write a non-writable memory");
        }

        public static void CheckRead(this MemoryAccess access)
        {
            //if (!access.HasFlag(MemoryAccess.Read))
            //    throw new InvalidOperationException("Attempt to write a non-readable memory");
        }

        public static string AsString(this MemoryAccess access)
        {
            return access switch
            {
                MemoryAccess.Read => "RO",
                MemoryAccess.Write => "WO",
                MemoryAccess.ReadWrite => "RW",
                _ => ""
            };
        }
    }
}
