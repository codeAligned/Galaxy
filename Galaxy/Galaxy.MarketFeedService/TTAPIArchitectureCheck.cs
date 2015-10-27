using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace Galaxy.MarketFeedService
{
    internal class TTAPIArchitectureCheck
    {
        public string ErrorString { get; private set; }
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        /// <summary>
        /// Default constructor
        /// </summary>
        public TTAPIArchitectureCheck(){}

        /// <summary>
        /// Verify the application build settings match the architecture of the TT API installed
        /// </summary>
        public bool validate()
        {
            try
            {

                var fileVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo((new System.IO.FileInfo("TradingTechnologies.TTAPI.dll")).FullName);
                var appAssembly = System.Reflection.Assembly.GetExecutingAssembly();
                var apiAssembly = System.Reflection.Assembly.ReflectionOnlyLoadFrom(fileVersionInfo.FileName);

                System.Reflection.PortableExecutableKinds appKinds, apiKinds;
                System.Reflection.ImageFileMachine appImgFileMachine, apiImgFileMachine;

                appAssembly.ManifestModule.GetPEKind(out appKinds, out appImgFileMachine);
                apiAssembly.ManifestModule.GetPEKind(out apiKinds, out apiImgFileMachine);

                if (!appKinds.HasFlag(apiKinds))
                {
                    ErrorString = string.Format("WARNING: This application must be compiled as a {0} application to run with a {0} version of TT API.",
                                  (apiKinds.HasFlag(System.Reflection.PortableExecutableKinds.Required32Bit) ? "32Bit" : "64bit"));
                    return false;
                }
                else
                {
                    ErrorString = "";
                    return true;
                }
            }
            catch (Exception err)
            {
                ErrorString =
                    $"ERROR: An error occured while attempting to verify the application build settings match the architecture of the TT API installed. {err.Message}";
                return false;
            }
        }
    }
}
