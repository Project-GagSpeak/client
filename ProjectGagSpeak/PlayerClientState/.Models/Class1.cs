
/// </summary>
[Serializable]
public record RestraintSet : IMoodlesAssociable, IPadlockable
{
    public RestraintSet()
    {
        // Initialize DrawData in the constructor
        DrawData = EquipSlotExtensions.EqdpSlots.ToDictionary(
            slot => slot, slot => new EquipDrawData(ItemIdVars.NothingItem(slot)) { Slot = slot, IsEnabled = false });

        // Initialize BonusDrawData in the constructor
        BonusDrawData = BonusExtensions.AllFlags.ToDictionary(
            slot => slot, slot => new BonusDrawData(EquipItem.BonusItemNothing(slot)) { Slot = slot, IsEnabled = false });
    }

    public Guid RestraintId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "New Restraint Set";
    public string Description { get; set; } = string.Empty;
    public bool Enabled { get; set; } = false;
    public string EnabledBy { get; set; } = string.Empty;

    public string Padlock { get; set; } = Padlocks.None.ToName();
    public string Password { get; set; } = string.Empty;
    public DateTimeOffset Timer { get; set; } = DateTimeOffset.MinValue;
    public string Assigner { get; set; } = string.Empty;
    public bool ForceHeadgear { get; set; } = false;
    public bool ForceVisor { get; set; } = false;
    public bool ApplyCustomizations { get; set; } = false;
    public Dictionary<EquipSlot, EquipDrawData> DrawData { get; set; } = [];
    public Dictionary<BonusItemFlag, BonusDrawData> BonusDrawData { get; set; } = [];
    public JToken CustomizeObject { get; set; } = new JObject();
    public JToken ParametersObject { get; set; } = new JObject();
    public List<AssociatedMod> AssociatedMods { get; private set; } = new List<AssociatedMod>();
    public List<Guid> AssociatedMoodles { get; set; } = new List<Guid>();
    public Guid AssociatedMoodlePreset { get; set; } = Guid.Empty;
    public Dictionary<string, HardcoreTraits> SetTraits { get; set; } = new Dictionary<string, HardcoreTraits>();

    // parameterless constructor for serialization
    public JObject Serialize()
    {
        var serializer = new JsonSerializer();
        // for the DrawData dictionary.
        JObject drawDataEquipmentObject = new JObject();
        // serialize each item in it
        foreach (var pair in DrawData)
        {
            drawDataEquipmentObject[pair.Key.ToString()] = new JObject()
            {
                ["Slot"] = pair.Value.Slot.ToString(),
                ["IsEnabled"] = pair.Value.IsEnabled,
                ["CustomItemId"] = pair.Value.GameItem.Id.ToString(),
                ["GameStain"] = pair.Value.GameStain.ToString(),
            };
        }

        // for the BonusDrawData dictionary.
        var bonusDrawDataArray = new JArray();
        // serialize each item in it
        foreach (var pair in BonusDrawData)
        {
            bonusDrawDataArray.Add(new JObject()
            {
                ["BonusItemFlag"] = pair.Key.ToString(),
                ["BonusDrawData"] = pair.Value.Serialize()
            });
        }

        // for the AssociatedMods list.
        var associatedModsArray = new JArray();
        // serialize each item in it
        foreach (var mod in AssociatedMods)
        {
            associatedModsArray.Add(mod.Serialize());
        }

        // for the set properties
        var setPropertiesArray = new JArray();
        // serialize each item in it
        var setPropertiesObject = JObject.FromObject(SetTraits);

        // Ensure Customize & Parameters are correctly serialized

        return new JObject()
        {
            ["RestraintId"] = RestraintId.ToString(),
            ["Name"] = Name,
            ["Description"] = Description,
            ["Enabled"] = Enabled,
            ["EnabledBy"] = EnabledBy,
            ["Padlock"] = Padlock,
            ["Password"] = Password,
            ["Timer"] = Timer.UtcDateTime.ToString("o"),
            ["Assigner"] = Assigner,
            ["ForceHeadgear"] = ForceHeadgear,
            ["ForceVisor"] = ForceVisor,
            ["ApplyCustomizations"] = ApplyCustomizations,
            ["CustomizeObject"] = CustomizeObject,
            ["ParametersObject"] = ParametersObject,
            ["DrawData"] = drawDataEquipmentObject,
            ["BonusDrawData"] = bonusDrawDataArray,
            ["AssociatedMods"] = associatedModsArray,
            ["AssociatedMoodles"] = new JArray(AssociatedMoodles),
            ["AssociatedMoodlePresets"] = AssociatedMoodlePreset,
            ["SetTraits"] = setPropertiesObject
        };
    }
