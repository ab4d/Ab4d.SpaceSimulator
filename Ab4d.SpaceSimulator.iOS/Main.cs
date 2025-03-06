using Ab4d.SpaceSimulator.Shared;
using UIKit;

namespace Ab4d.SpaceSimulator.iOS
{
    public class Application
    {
        // This is the main entry point of the application.
        static void Main(string[] args)
        {
            // This free open source license is valid only for the open source project at the following URL:
            // https://github.com/ab4d/Ab4d.SpaceSimulator
            // Assembly name: 'Ab4d.SpaceSimulator'
            Ab4d.SharpEngine.Licensing.SetLicense(licenseOwner: "AB4D", 
                                                  licenseType: "OpenSourceLicense", 
                                                  platforms: "All", 
                                                  license: "6177-EC3F-9ECB-28D9-1FF8-A057-FB36-AEA4-E99B-F2E3-A557-AA35-AFBA-B2A0-DE67-9554-D0F5-F474-01D0-D460-2395-45C7-F91D-4A63-3E01-D1F8-7E1C");


            // if you want to use a different Application Delegate class from "AppDelegate"
            // you can specify it here.
            UIApplication.Main(args, null, typeof(AppDelegate));
        }
    }
}
