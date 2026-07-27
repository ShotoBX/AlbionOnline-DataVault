using StatisticsAnalysisTool.Models;
using StatisticsAnalysisTool.Models.ItemsJsonModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StatisticsAnalysisTool.Crafting;

/// <summary>
/// Resolves refining recipes (raw resource -> refined material, e.g. Wood -> Planks, Ore -> Metal Bar).
/// Albion's game data encodes these recipes on SimpleItem using the exact same craftingrequirements/craftresource
/// tags equipment uses (see CraftingRecipeResolver.GetCraftingRequirements), just under a different item type -
/// so everything else (resource resolution, resource kind, returnability, IsCraftable/GetResources/GetAmountCrafted)
/// is inherited unchanged. Refining has no Focus or Journal mechanics, and SimpleItem is never in the base
/// resolver's GetJournal switch, so GetJournal already returns null here without needing an override.
///
/// SimpleItem also covers non-refining things that happen to carry the same craftingrequirements tag (artefacts,
/// consumables/alchemy, mount food, etc.) - those aren't refined at a refining station in-game, so IsRefinable
/// excludes them by shop category rather than treating every SimpleItem recipe as a refining recipe.
///
/// Raw resources (Fiber/Wood/Ore/Hide/Rock) also carry their own craftingrequirements - not for refining, but for
/// "Transmute" (upgrading a lower tier of the SAME raw resource into the next tier for silver, e.g. T5_FIBER ->
/// T6_FIBER). That recipe uses the identical craftingrequirements/craftresource tags, so IsCraftable alone can't
/// tell the two apart - it's what let the raw resource itself (e.g. Cotton) get flagged as "refinable" instead of
/// its actual refined output (Cloth). The game data distinguishes them via shopsubcategory1: raw resources are
/// "resources", refined materials are "refinedresources" - only the latter should be treated as a refining target.
/// </summary>
public class RefiningRecipeResolver : CraftingRecipeResolver
{
    private const string RefinedResourcesShopSubCategoryId = "refinedresources";

    private static readonly HashSet<string> ExcludedRefiningShopCategoryIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "artefact",
        "artefacts",
        "alchemy",
        "consumables",
        "farming"
    };

    public override CraftingRequirements GetCraftingRequirements(Item item)
    {
        return item?.FullItemInformation is SimpleItem simpleItem
            ? simpleItem.CraftingRequirements?.FirstOrDefault()
            : null;
    }

    public bool IsRefinable(Item item)
    {
        var shopCategory = item?.FullItemInformation?.ShopCategory;
        if (!string.IsNullOrWhiteSpace(shopCategory) && ExcludedRefiningShopCategoryIds.Contains(shopCategory))
        {
            return false;
        }

        if (!string.Equals(item?.FullItemInformation?.ShopSubCategory1, RefinedResourcesShopSubCategoryId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return IsCraftable(item);
    }
}
