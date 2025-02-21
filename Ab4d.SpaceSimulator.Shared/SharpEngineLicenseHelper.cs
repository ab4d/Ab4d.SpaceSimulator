namespace Ab4d.SpaceSimulator.Shared;

public static class SharpEngineLicenseHelper
{
    public static void ActivateLicense()
    {
        // NOTE:
        // SetLicense method must be called from the entry assembly (otherwise an SDK license is needed).
        // This class is called from each entry assembly so we have license written in a single location.

        // This is just temporal trial license and will be replaced by a free open source license:
        // Ab4d.SharpEngine Trial License can be used for testing the Ab4d.SharpEngine and is valid until April 15, 2025.
        Ab4d.SharpEngine.Licensing.SetLicense(licenseOwner: "AB4D",
                                              licenseType: "TrialLicense",
                                              platforms: "All",
                                              license: "4045-4200-4577-BCD0-4186-154D-0519-2487-6A96-1677-6027-87D1-C2DF-D1D5-3064-F1B1-34EA");
    }
}