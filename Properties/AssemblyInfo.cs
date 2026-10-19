using System.Reflection;
using MelonLoader;

[assembly: AssemblyTitle(MoreMath.BuildInfo.Description)]
[assembly: AssemblyDescription(MoreMath.BuildInfo.Description)]
[assembly: AssemblyCompany(MoreMath.BuildInfo.Company)]
[assembly: AssemblyProduct(MoreMath.BuildInfo.Name)]
[assembly: AssemblyCopyright("Created by " + MoreMath.BuildInfo.Author)]
[assembly: AssemblyTrademark(MoreMath.BuildInfo.Company)]
[assembly: AssemblyVersion(MoreMath.BuildInfo.Version)]
[assembly: AssemblyFileVersion(MoreMath.BuildInfo.Version)]
[assembly: MelonInfo(typeof(MoreMath.MoreMath), MoreMath.BuildInfo.Name, MoreMath.BuildInfo.Version, MoreMath.BuildInfo.Author, MoreMath.BuildInfo.DownloadLink)]
[assembly: MelonColor()]

// Create and Setup a MelonGame Attribute to mark a Melon as Universal or Compatible with specific Games.
// If no MelonGame Attribute is found or any of the Values for any MelonGame Attribute on the Melon is null or empty it will be assumed the Melon is Universal.
// Values for MelonGame Attribute can be found in the Game's app.info file or printed at the top of every log directly beneath the Unity version.
[assembly: MelonGame("CuriousOwlGames", "StellarDrive")]