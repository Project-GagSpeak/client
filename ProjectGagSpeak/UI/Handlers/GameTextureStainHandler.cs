using Dalamud.Plugin.Services;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Textures;
using GagSpeak.UI.Components.Combos;
using Penumbra.GameData.Data;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak.UI.Handlers;

/// <summary>
/// A handler to manage and provide the resources for stain selection and 
/// item identification within a singular class, to avoid redundant excessive injection.
/// </summary>
public sealed class GameItemStainHandler
{
    private readonly ILogger<GameItemStainHandler> _logger;
    private readonly ItemData _items;
    private readonly DictBonusItems _bonusItems;
    private readonly DictStain _stains;
    private readonly TextureService _iconTextures;
    private readonly IDataManager _gameData;

    public GameItemStainHandler(ILogger<GameItemStainHandler> logger, ItemData items,
        DictBonusItems bonusItems, DictStain stains, TextureService iconTextures, IDataManager gameData)
    {
        _logger = logger;
        _items = items;
        _bonusItems = bonusItems;
        _stains = stains;
        _iconTextures = iconTextures;
        _gameData = gameData;
    }

    public GameItemCombo[] ObtainItemCombos() 
        => EquipSlotExtensions.EqdpSlots.Select(e => new GameItemCombo(_gameData, e, _items, _logger)).ToArray();

    /// <summary>
    /// Public provider for stain color combo data.
    /// </summary>
    /// <param name="comboWidth"> The Width of the color stain combo. </param>
    public StainColorCombo ObtainStainCombos(float comboWidth)
        => new StainColorCombo(comboWidth - 20, _stains, _logger);

    /// <summary>
    /// Public provider for bonus item dictionary data.
    /// </summary>
    /// <returns></returns>
    public BonusItemCombo[] ObtainBonusItemCombos()
        => BonusExtensions.AllFlags.Select(f => new BonusItemCombo(_gameData, f, _logger)).ToArray();

    /// <summary>
    /// Public provider for Icon Textures.
    /// </summary>
    public TextureService IconData => _iconTextures;

    /// <summary>
    /// Returns if the stainId passed in is contained within the stain dictionary.
    /// </summary>
    /// <param name="stain">StainId to check</param>
    /// <param name="data">Stain Data to output.</param>
    /// <returns> True if in the stain dictionary, false otherwise.</returns>
    public bool TryGetStain(StainId stain, out Stain data)
        => _stains.TryGetValue(stain, out data);

}
