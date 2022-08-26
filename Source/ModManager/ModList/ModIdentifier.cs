namespace ModManager {
    public class ModIdentifier {
        public ModIdentifier() { }
        public ModIdentifier(string id, string name, string steamWorkshopId) {
            Id = id;
            Name = name;
            SteamWorkshopId = steamWorkshopId;
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string SteamWorkshopId { get; set; }
    }
}
