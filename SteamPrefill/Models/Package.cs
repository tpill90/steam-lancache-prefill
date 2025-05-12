namespace SteamPrefill.Models
{
    public sealed class Package
    {
        public readonly uint Id;

        public readonly List<uint> AppIds;
        public readonly List<uint> DepotIds;

        public uint? MasterSubscriptionAppId;

        public readonly bool IsFreeWeekend;
        private DateTime? FreeWeekendExpiryTimeUtc;
        public bool FreeWeekendHasExpired => DateTime.UtcNow > FreeWeekendExpiryTimeUtc;

        public Package(KeyValue rootKeyValue)
        {
            Id = UInt32.Parse(rootKeyValue.Name);

            AppIds = rootKeyValue["appids"].Children.Select(e => uint.Parse(e.Value)).ToList();
            DepotIds = rootKeyValue["depotids"].Children.Select(e => uint.Parse(e.Value)).ToList();

            MasterSubscriptionAppId = rootKeyValue["extended"]["mastersubscriptionappid"].AsUnsignedIntNullable();

            IsFreeWeekend = rootKeyValue["extended"]["freeweekend"].AsBoolean();
            FreeWeekendExpiryTimeUtc = rootKeyValue["extended"]["expirytime"].AsDateTimeUtc();
        }

        public override string ToString()
        {
            if (MasterSubscriptionAppId != null)
            {
                return $"Id : {Id}  MasterSubscriptionId : {MasterSubscriptionAppId}";
            }
            return $"Id : {Id}";
        }
    }
}