namespace LancachePrefill.Common.Enums
{
    public sealed class TransferSpeedUnit : EnumBase<TransferSpeedUnit>
    {
        public static readonly TransferSpeedUnit Bits = new TransferSpeedUnit("bits");
        public static readonly TransferSpeedUnit Bytes = new TransferSpeedUnit("bytes");

        private TransferSpeedUnit(string name) : base(name)
        {
        }
    }
}