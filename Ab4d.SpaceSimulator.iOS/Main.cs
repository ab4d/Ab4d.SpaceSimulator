using UIKit;

namespace Ab4d.SpaceSimulator.iOS
{
    public class Application
    {
        // This is the main entry point of the application.
        static void Main(string[] args)
        {
            // This is just temporal trial license and will be replaced by a free open source license:
            // Ab4d.SharpEngine Trial License can be used for testing the Ab4d.SharpEngine and is valid until April 15, 2025.
            Ab4d.SharpEngine.Licensing.SetLicense(licenseOwner: "AB4D",
                                                  licenseType: "TrialLicense",
                                                  platforms: "All",
                                                  license: "4045-4200-4577-BCD0-4186-154D-0519-2487-6A96-1677-6027-87D1-C2DF-D1D5-3064-F1B1-34EA");

            // if you want to use a different Application Delegate class from "AppDelegate"
            // you can specify it here.
            UIApplication.Main(args, null, typeof(AppDelegate));
        }
    }
}
