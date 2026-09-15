using BANxOpen.Foundation.Contracts.Materials;

namespace BANxOpen.Foundation.Core.Materials.Constraints;

/// <summary>One material being considered for a body, described in the terms a feature domain can judge
/// it by without the engine knowing anything about that domain.
///
/// <see cref="Name"/> is always present — it is the name NX itself stores on a body, and the only thing
/// available when the material was never resolved against a loaded library.
/// <see cref="LibraryMaterial"/> is present only when it was, and carries the category and MatML
/// properties with it. That split is what keeps this usable beyond sheet metal: a bead constrains on a
/// grade label derived from the name, while a casting or weldment domain can constrain on
/// <see cref="Material.Category"/> or a <see cref="MaterialPropertyValue"/> instead.
///
/// A domain that needs the richer form must cope with it being null rather than assume resolution
/// succeeded — failing open there is usually wrong, so say so explicitly in the constraint's message.</summary>
public sealed record MaterialCandidate(string Name, Material? LibraryMaterial)
{
    public static MaterialCandidate FromLibrary(Material material) => new(material.Name, material);

    public static MaterialCandidate FromName(string name) => new(name, null);
}